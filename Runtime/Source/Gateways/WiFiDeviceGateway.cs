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
using OVR.Components;
using OVR.Data;
using OVR.DataCollection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace OVR.Gateways
{
  public struct UdpState
  {
    public UdpClient udpClient;
    public IPEndPoint endPoint;
    public string ovrHostname;
  }

  /// <summary>
  /// Communication gateway to the OVR device over UDP.
  /// </summary>
  public class WiFiDeviceGateway : IDeviceGateway
  {
    private List<OdorantCommand> _commands = new List<OdorantCommand>();
    private byte[] _packet;
    private static IPEndPoint _currentDeviceEndPoint = null;
    private byte[] _checkConnectionPacket = new byte[MarshalHeader.Size + sizeof(ushort)];
    private int _remotePort = 4210;
    private Socket _socketSender;
    private UdpState _socketRecieveState;
    private int _socketRecievePort;
    private int _maxCommandsPerPacket;
    private string _ovrDeviceHostname;
    private NetworkLogging _scentActivationLog = null;
    private static Stopwatch _connectionTimer = new Stopwatch();
    private static MarshalHeader _header = new MarshalHeader();

    private object _asyncLock = new object();
    private static Action<IPEndPoint> _onConnect;
    private static Action _onDisconnect;
    public static bool DeviceDebugMode { get; private set; }

    public WiFiDeviceGateway(string ovrDeviceHostname, bool scentActivationLogging = false, int localhostDebugPort = 0)
    {
      _socketSender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
      _ovrDeviceHostname = ovrDeviceHostname;
      _socketRecieveState.ovrHostname = _ovrDeviceHostname;
      _socketRecieveState.udpClient = new UdpClient(0, AddressFamily.InterNetwork);

      var localIp = IPAddressExtensions.GetLocalIP();
      UnityEngine.Debug.LogFormat("<b>[OVR]</b> Local IP: {0}", localIp);

      if (scentActivationLogging)
        _scentActivationLog = new NetworkLogging(localhostDebugPort);

      _socketRecievePort = ((IPEndPoint)_socketRecieveState.udpClient.Client.LocalEndPoint).Port;

      // Setup for broadcasting hostname request from device
      MarshalHeader.Set(ref _checkConnectionPacket, MessageTypes.DEVICE_HOSTNAME_REQUEST, sizeof(ushort));
      _checkConnectionPacket[MarshalHeader.Size] = (byte)_socketRecievePort;
      _checkConnectionPacket[MarshalHeader.Size + 1] = (byte)(_socketRecievePort >> 8);
      _onConnect += OnConnect;
      _onDisconnect += OnDisconnect;
    }

    public void Dispose()
    {
      if (_socketRecieveState.udpClient.Client.IsBound)
      {
        _socketRecieveState.udpClient.Client.Close();
      }
      _currentDeviceEndPoint = null;
    }

    public void BeginConnect()
    {
      if (_currentDeviceEndPoint == null)
      {
        _socketSender.EnableBroadcast = true;
        foreach (var address in IPAddressExtensions.GetBroadcastAddresses())
          _socketSender.SendTo(_checkConnectionPacket, SocketFlags.None, new IPEndPoint(address, _remotePort));

        _socketSender.EnableBroadcast = false;
      }
      else
      {
        // UnityEngine.Debug.LogFormat("<b>[OVR]</b> Device IP: {0}", _currentDeviceEndPoint);
        _socketSender.SendTo(_checkConnectionPacket, _currentDeviceEndPoint);
      }
      _socketRecieveState.udpClient.BeginReceive(ReceiveCallback, _socketRecieveState);
    }

    public bool CheckConnection()
    {
      if (_connectionTimer.ElapsedMilliseconds > 2000)
      {
        _onDisconnect.Invoke();
        return false;
      }
      return true;
    }

    public bool Connect()
    {
      IPAddress[] hostEntries;
      try
      {
        hostEntries = Dns.GetHostAddresses(_ovrDeviceHostname);
      }
      catch (SocketException e)
      {
        return false;
      }

      foreach (var hostEntry in hostEntries)
      {
        Console.WriteLine(hostEntry.ToString());
      }

      if (hostEntries.Any())
        _socketSender.Connect(hostEntries.First(), _remotePort);
      else
        return false;

      return _socketSender.Connected;
    }

    public void Init(int maxCommandsPerPacket)
    {
      _maxCommandsPerPacket = maxCommandsPerPacket;
      _packet = new byte[MarshalHeader.Size + _maxCommandsPerPacket * OdorantCommand.Size];
      _connectionTimer.Start();
    }

    public void AddCommand(OdorantCommand command)
    {
      if (_commands.Count >= _maxCommandsPerPacket)
        return;

      _commands.Add(command);
    }

    public void AddCommands(IEnumerable<OdorantCommand> commands)
    {
      if (commands.Count() + _commands.Count > _maxCommandsPerPacket)
        return;

      _commands.AddRange(commands);
    }

    public void RemoveCommand(byte slot)
    {
      _commands.RemoveAll(c => c.Slot == slot);
    }

    public void ClearCommands()
    {
      _commands.Clear();
    }

    public bool SendCommands()
    {
      if (_currentDeviceEndPoint == null)
        return false;

      if (_scentActivationLog != null)
        foreach (var command in _commands)
          _scentActivationLog.Add(command.ToCsv());

      _socketSender.SendTo(CommandsToPacket(), MarshalHeader.Size + _commands.Count * OdorantCommand.Size, SocketFlags.None, _currentDeviceEndPoint);
      return true;
    }

    public byte[] CommandsToPacket()
    {
      int index = MarshalHeader.Size;
      MarshalHeader.Set(ref _packet, MessageTypes.ODORANT_COMMANDS, (ushort)(_commands.Count * OdorantCommand.Size));
      
      foreach (var command in _commands)
      {
        Array.Copy(command.GetPacket(), 0, _packet, index, OdorantCommand.Size);
        index += OdorantCommand.Size;
      }

      return _packet;
    }

    public bool HasCommandsToSend()
    {
      return _commands.Any();
    }

    public string GatewayLabel()
    {
      return "WiFi";
    }

    public bool IsConnected()
    {
      return _currentDeviceEndPoint != null;
    }

    public void OnConnect(IPEndPoint ip)
    {
      lock (_asyncLock)
      {
        _currentDeviceEndPoint = ip;
        MarshalHeader.Set(ref _packet, MessageTypes.DEVICE_DEBUGMODE_REQUEST);
        _socketSender.SendTo(_packet, MarshalHeader.Size, SocketFlags.None, _currentDeviceEndPoint);
      }
    }
    public void OnDisconnect()
    {
      lock (_asyncLock)
      {
        _currentDeviceEndPoint = null;
      }
    }

    public static void ReceiveCallback(IAsyncResult ar)
    {
      UdpState udpState = (UdpState)(ar.AsyncState);

      var data = udpState.udpClient.EndReceive(ar, ref udpState.endPoint);
      _header.Set(data);
      if (!_header.Valid)
        return;

      if (_header.MessageType == (byte)MessageTypes.DEVICE_HOSTNAME)
      {
        var result = Encoding.Default.GetString(data, MarshalHeader.Size, _header.MessageSize);
        if (result == udpState.ovrHostname)
        {
          _connectionTimer.Restart();
          if (_currentDeviceEndPoint == null)
          {
            UnityEngine.Debug.LogFormat("<b>[OVR]</b> Device Found: {0}", result);
            _onConnect.Invoke(udpState.endPoint);
          }
        }
        else
        {
          // UnityEngine.Debug.LogFormat("<b>[OVR]</b> Rejected Device Found: {0}. Does not match target hostname.", result);
        }
      }
      else if (_header.MessageType == (byte)MessageTypes.DEVICE_DEBUGMODE)
      {
        DeviceDebugMode = (data[MarshalHeader.Size] > 0);
      }
    }
  }
}
