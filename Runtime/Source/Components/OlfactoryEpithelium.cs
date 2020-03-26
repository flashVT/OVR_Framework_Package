//////////////////////////////////////////////////////////////////////////////////
//
//  Architecture of Scent(TM) Software Suite - OVR Unity Framework
//  Copyright (C) 2018-2019 OVR Tech LLC
//
//    OVR Unity Framework is proprietary software: you can modify it under the
//    terms of the Development Kit Agreement signed before receiving the
//    software. This Software is owned by OVR Tech LLC and any modifications,
//    changes, or alterations are subject to the terms of the Agreement.
//
//    OVR Unity Framework is distributed in the hope that it will be useful,for
//    testing purposes, and for feedback, but WITHOUT ANY WARRANTY; without even
//    the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
//    See the Agreement for more details.
//
//    You should have received a copy of the Development Kit Agreement before
//    receiving the OVR Unity Framework. If not, this software may be in violation
//    of the Agreement. Please contact info@ovrtechnology.com or click the link
//    below to request the document.
//
//  https://ovrtechnology.com/contact/
//
//////////////////////////////////////////////////////////////////////////////////

using OVR.API;
using OVR.Data;
using OVR.Exceptions;
using OVR.Gateways;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace OVR.Components
{
  /// <summary>
  /// This component represents the nose of the player and should be placed on the HMD observant
  /// game-object in the scene hierarchy.
  /// </summary>
  public class OlfactoryEpithelium : MonoBehaviour
  {
    public static bool IsActive = true;
    public string OvrDeviceHostname = "OVRv0_2_0";
    public Vector3 Position { get { return transform.TransformPoint(_offset); } }
    public static bool WaitForLoadBalance { get { return OdorantsProcessingThisFrame > 10; } }
    public static int OdorantsProcessingThisFrame = 0;
    public float BurstUpdateInterval { get; private set; }
    public GameObject OdorantLogPrefab;
    public bool DisplayOdorantMessages { get; private set; }
    [SerializeField] private Vector3 _offset = new Vector3(0.0f, -0.05f, 0.08f);

    private static OlfactoryEpithelium Instance = null;
    private static List<OdorantCommand> _odorantCommands = new List<OdorantCommand>();
    private GameObject _odorantLog;
    private List<string> _odorantLogMessages = new List<string>();
    private IGatewayBlacklist _blacklist = null;
    private int _maxNumOdorants = 9;
    private float _maintainConnectionTimer = 0.5f;
    private float _maintainConnectionTimerInterval = 1.0f;
    private static IDeviceGateway _gateway = null;
    private static IDeviceGateway _usb = null;
    private static IDeviceGateway _wifi = null;

    private TextMesh _textMesh = null;
    private float _textTimer = 0.1f;

    void OnEnable()
    {
      BurstUpdateInterval = 0.05f;

      if (Instance == this)
        return;

      if (!Instance)
      {
        Instance = this;
      }
      else
      {
        Destroy(this);
        throw new MultipleOlfactoryException("More than one OlfactoryEpithelium constructed.");
      }
    }
    void OnDisable()
    {
      _odorantCommands.Clear();
    }
    void Start()
    {
      var ovrConfigFilePath = string.Format("{0}OVRTechnology\\OvrEngineConfig.json", Path.GetTempPath());

      if (File.Exists(ovrConfigFilePath))
      {
        var config = JsonUtility.FromJson(File.ReadAllText(ovrConfigFilePath), typeof(OvrEngineConfig)) as OvrEngineConfig;
        OvrDeviceHostname = config.OvrDeviceHostname;
        _maxNumOdorants = 200000 / config.CapRechargeMicros;
      }
      else
        Debug.Log(string.Format("<b>[OVR]</b> Failed to find {0}", ovrConfigFilePath));

      Debug.Log(string.Format("<b>[OVR]</b> Device Hostname Preference: {0}", OvrDeviceHostname));

      createTextMeshForOdorantLogging();
      DisplayOdorantMessages = !Application.isEditor;
      SetShouldDisplayOdorantMessages(Application.isEditor);
    }
    private void OnDestroy()
    {
    }
    void Update()
    {

      updateOdorantMessageText();

      if (!maintainDeviceConnection())
        _odorantCommands.Clear();

      // reset coroutine counter for load balancing
      OdorantsProcessingThisFrame = 0;

      if (_blacklist != null)
        _blacklist.Update();

      if (_gateway != null)
      {
        _gateway.AddCommands(_odorantCommands);
        if(_gateway.HasCommandsToSend())
          _gateway.SendCommands();
        _gateway.ClearCommands();
      }
      _odorantCommands.Clear();

      if (_wifi != null && _wifi.IsConnected() && WiFiDeviceGateway.DeviceDebugMode)
      {
        SetShouldDisplayOdorantMessages(true);
      }
    }
    /// <summary>
    /// Adds a command only if the registry is active, the odorant command is not in the blacklist and
    /// there is not already another odorant destine for the same slot.
    /// </summary>
    /// <param name="command">Incoming odorant commands from Odorant components in the scene.</param>
    public void AddOdorantCommand(OdorantCommand command)
    {
      // active
      if (!IsActive)
        return;
      
      // not in blacklist
      if (Get()._blacklist != null)
      {
        if (Get()._blacklist.Slots().Contains(command.Slot) || Get()._blacklist.Names().Contains(command.Name))
          return;
      }

      // not already full of commands and command is none zero
      if (_odorantCommands.Count >= Get()._maxNumOdorants || command.Intensity == 0)
      {
        return;
      }

      // take average value if there are more than one of the same command.
      var indexOfDuplicate = _odorantCommands.FindIndex(c => c.Slot == command.Slot);

      if (indexOfDuplicate >= 0)
      {
        _odorantCommands[indexOfDuplicate].Intensity = (byte)((command.Intensity + _odorantCommands[indexOfDuplicate].Intensity) / 2);
      }

      _odorantCommands.Add(command);
    }
    public static bool Instanced ()
    {
      return Instance != null;
    }
    /// <summary>
    /// A temporary solution for getting an instance of this object in Odorant Components. This
    /// strategy will work fine in standalone systems or where there can only be one VR user.
    /// </summary>
    /// <returns>The one and only instance of the object.</returns>
    public static OlfactoryEpithelium Get()
    {
      if (!Instance)
      {
        Debug.LogWarningFormat("<b>[OVR]</b> Olfactory could not be found. Please place one on the player's HMD observant game-object.");
      }

      return Instance;
    }
    /// <summary>
    /// Constructs an instance of the odorant log from a prefab.
    /// </summary>
    private void createTextMeshForOdorantLogging()
    {
      if (Get().OdorantLogPrefab == null)
      {
        Debug.LogErrorFormat("<b>[OVR]</b> Odorant log prefab is missing. You will not be able to see what odorants are activating.");
        return;
      }

      Get()._odorantLog = Instantiate(Get().OdorantLogPrefab, transform, false);

      if (Get()._odorantLog == null)
        return;

      Get()._textMesh = Get()._odorantLog.GetComponent<TextMesh>();
    }
    /// <summary>
    /// Sets the mesh renderer's render state for the odorant log
    /// </summary>
    /// <param name="shouldDisplayOdorantMessages">The new value for enabling or disabling the odorant log</param>
    public void SetShouldDisplayOdorantMessages(bool shouldDisplayOdorantMessages)
    {
      if (DisplayOdorantMessages == shouldDisplayOdorantMessages)
        return;

      DisplayOdorantMessages = shouldDisplayOdorantMessages;
      var meshRenderer = Get()._odorantLog.GetComponent<MeshRenderer>();
      if (meshRenderer == null)
        Debug.LogFormat("<b>[OVR]</b> Odorant Log mesh renderer could not be found");
      else
      {
        Debug.LogFormat("<b>[OVR]</b> Odorant Log: {0}", shouldDisplayOdorantMessages ? "Enabled" : "Disabled");
        meshRenderer.enabled = shouldDisplayOdorantMessages;
      }
    }
    /// <summary>
    /// Used to connect and maintain device connectivity. Defaults to USB, if that fails WiFi connection
    /// is attempted. OVR Gateway, a standalone desktop service, will eventually replace this functionality.
    /// </summary>
    /// <returns>Only returns false if connection fails or no device is connected</returns>
    private bool maintainDeviceConnection()
    {
      Get()._maintainConnectionTimer -= Time.deltaTime;
      if (Get()._maintainConnectionTimer > 0.0f)
        return true;

      Get()._maintainConnectionTimer = Get()._maintainConnectionTimerInterval;

#if NET_4_6 && UNITY_STANDALONE_WIN
      if (_serialGateway.CheckConnection())
      {
        _currentGateway = _serialGateway;
        return true;
      }

      if (_serialGateway.Connect())
      {
        _currentGateway = _serialGateway;
        _currentGateway.Init(_maxNumOdorants);
        _currentGateway.SendFanSpeed(127);
        Debug.Log("<b>[OVR]</b> Device found via USB");
        return true;
      }
#endif
      if (_wifi == null)
      {
        _wifi = new WiFiDeviceGateway(Get().OvrDeviceHostname);
      }

      _wifi.BeginConnect();
      if (_wifi.CheckConnection())
      {
        if(_gateway != _wifi)
        {
          _gateway = _wifi;
          _gateway.Init(Get()._maxNumOdorants);
        }
        return true;
      }

      _gateway = null;
      return false;
    }
    /// <summary>
    /// Aggregates pending odorant commands and logs them to the display.
    /// </summary>
    private void updateOdorantMessageText()
    {
      if (Get()._textMesh == null)
        return;

      var newMessages = getGuiMessages();
      Get()._odorantLogMessages.AddRange(newMessages);

      if (newMessages.Any())
        Get()._textTimer = 0.2f;

      Get()._textTimer -= Time.deltaTime;
      if (Get()._textTimer <= 0.0f)
      {
        if (Get()._odorantLogMessages.Any())
        {
          Get()._odorantLogMessages.RemoveAt(0);
        }
      }

      if (Get()._odorantLogMessages.Count > 5)
      {
        int count = Get()._odorantLogMessages.Count - 5;
        for (int i = 0; i < count; i++)
        {
          Get()._odorantLogMessages.RemoveAt(0);
        }
      }

      if (Get()._odorantLogMessages.Any())
        Get()._textMesh.text = Get()._odorantLogMessages.Aggregate((a, b) => a + "\n" + b);
      else
        Get()._textMesh.text = string.Empty;
    }
    /// <summary>
    /// Aggregates odorant messages do be displayed depending on the gateway currently in use.
    /// </summary>
    /// <returns>Odorant messages line by line prepended by gateway type.</returns>
    private IEnumerable<string> getGuiMessages()
    {
      if (_wifi != null && _wifi.IsConnected())
        return _odorantCommands.Where(o => o.Slot != 255).Select(c => string.Format("[{0}] \t{1} \t{2} \t{3}%", "WIFI", c.Slot + 1, c.Name, (int)(c.Intensity / 2.55f)));

      return _odorantCommands.Where(o => o.Slot != 255).Select(c => string.Format("[{0}] \t{1} \t{2} \t{3}%", "NONE", c.Slot + 1, c.Name, (int)(c.Intensity / 2.55f)));
      /*if(_currentGateway == null)
        return _odorantCommands.Where(o => o.Slot != 255).Select(c => string.Format("[{0}]\t{1}\t{2}\t{3}", "no device", c.Slot + 1, c.Name, c.Intensity));
      */
      // return _odorantCommands.Where(o => o.Slot != 255).Select(c => string.Format("[{0}]\t{1}\t{2}\t{3}", _currentGateway.GatewayLabel(), c.Slot + 1, c.Name, c.Intensity));
    }
    /// <summary>
    /// Shows the offset of the nose in relation to the tracked origin of the HMD.
    /// </summary>
    void OnDrawGizmos()
    {
      Gizmos.color = Color.white;
      Gizmos.DrawLine(transform.position, Position);
      Gizmos.DrawRay(Position, transform.rotation * Quaternion.Euler(Vector3.right * 30.0f) * _offset * -0.3f);
    }
  }
}
