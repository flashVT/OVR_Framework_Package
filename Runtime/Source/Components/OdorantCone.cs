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

using OVR.Data;
using System.Collections;
using UnityEngine;

namespace OVR.Components
{
  /// <summary>
  /// Nested-spherical sectors of odor that persists based on distance and angle in relation to the game-object.
  /// </summary>
  public class OdorantCone : Odorant
  {
    public Vector3 Position { get { return transform.TransformPoint(Offset); } }
    public Vector3 Offset;
    public float DecaySeconds { get; private set; }
    public bool ShouldEmitOdorant { get; private set; }
    
    [SerializeField] private Vector3 _odorantVector = Vector3.up * 0.2f;
    [Range(0.0f, 180.0f)] [SerializeField] private float _innerDiffusionAngle = 25.0f;
    [SerializeField] private AnimationCurve _radialScalar = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 0.0f);
    [Range(0.0f, 180.0f)] [SerializeField] private float _outerDiffusionAngle = 65.0f;
    [SerializeField] private float _decaySeconds = -1.0f;

    void Start()
    {
      Validate();
      OdorantCommand = new OdorantCommand(OdorantConfig, OdorantAlgorithm.Burst);
      DecaySeconds = _decaySeconds;
    }

    void OnEnable()
    {
      ShouldStop = false;
      StartCoroutine(CoprocessOdorantCommand());
    }

    void OnDisable()
    {
      ShouldStop = true;
    }

    /// <summary>
    /// The specific odorant node removes itself from the Olfactory's registry.
    /// </summary>
    void OnDestroy()
    {
      ShouldStop = true;
    }

    public IEnumerator CoprocessOdorantCommand()
    {
      yield return new WaitWhile(delegate () { return !OlfactoryEpithelium.Instanced() || OlfactoryEpithelium.WaitForLoadBalance; });

      while (!ShouldStop)
      {
        OlfactoryEpithelium.OdorantsProcessingThisFrame++;
        if (!gameObject.activeSelf || !gameObject.activeInHierarchy)
        {
          yield return new WaitForSeconds(OlfactoryEpithelium.Get().BurstUpdateInterval);
          continue;
        }
        
        var toOlfactory = OlfactoryEpithelium.Get().Position - Position;

        if (toOlfactory.sqrMagnitude > _odorantVector.sqrMagnitude)
        {
          yield return new WaitForSeconds(OlfactoryEpithelium.Get().BurstUpdateInterval);
          continue;
        }

        var distanceScaler = toOlfactory.magnitude / _odorantVector.magnitude;
        var decayScaler = 1.0f;

        if (_decaySeconds > 0.0f)
          decayScaler = DecaySeconds / _decaySeconds;

        var objectAngleToOlfactory = Vector3.Angle(transform.rotation * _odorantVector.normalized, toOlfactory);

        var directionalScaler = 0.0f;

        if (objectAngleToOlfactory < _innerDiffusionAngle)
          directionalScaler = 1.0f;
        else if (objectAngleToOlfactory < _outerDiffusionAngle)
          directionalScaler = (_outerDiffusionAngle - Vector3.Angle(transform.rotation * _odorantVector.normalized, toOlfactory)) / (_outerDiffusionAngle - _innerDiffusionAngle);
        
        OdorantCommand.Intensity = (byte)(_radialScalar.Evaluate(distanceScaler) * decayScaler * directionalScaler * Intensity);
        
        // Won't add to queue 
        OlfactoryEpithelium.Get().AddOdorantCommand(OdorantCommand);

        yield return new WaitForSeconds(OlfactoryEpithelium.Get().BurstUpdateInterval);
      }
    }

    public void EmitOdorant()
    {
      if (_decaySeconds <= 0.0f)
        return;

      ShouldEmitOdorant = true;
      DecaySeconds = _decaySeconds;
    }

    void OnDrawGizmosSelected()
    {
      Gizmos.color = OvrColor.primary;
      Gizmos.DrawRay(Position, transform.TransformDirection(_odorantVector));
      
      var baseAngle = Quaternion.LookRotation(_odorantVector);
      var length = _odorantVector.magnitude;
      int numLines = 64;
      float delta = 360.0f / numLines;

      Gizmos.color = OvrColor.inner;
      for (var i = 0; i < numLines; i++)
      {
        Gizmos.DrawRay(Position, transform.rotation * baseAngle * Quaternion.Euler(Vector3.forward * delta * i) * Quaternion.Euler(Vector3.up * _innerDiffusionAngle) * Vector3.forward * length);
      }

      Gizmos.color = OvrColor.outer;
      for (var i = 0; i < numLines; i++)
      {
        Gizmos.DrawRay(Position, transform.rotation * baseAngle * Quaternion.Euler(Vector3.forward * delta * i) * Quaternion.Euler(Vector3.up * _outerDiffusionAngle) * Vector3.forward * length);
      }
    }

    private void Validate()
    {
      BaseValidate();

      if (_outerDiffusionAngle <= 0.0f)
        Debug.LogWarningFormat("<b>[OVR]</b> {0}: The outer diffusion angle must be greater than zero.", GetParentList() + gameObject.name);

      if (_outerDiffusionAngle < _innerDiffusionAngle)
        Debug.LogWarningFormat("<b>[OVR]</b> {0}: The outer diffusion angle must be greater than the inner diffusion angle.", GetParentList() + gameObject.name);

      if (_odorantVector.sqrMagnitude == 0.0f)
        Debug.LogWarningFormat("<b>[OVR]</b> {0}: The odorant vector must be greater than zero.", GetParentList() + gameObject.name);
    }
  }
}
