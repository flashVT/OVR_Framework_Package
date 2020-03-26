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
  /// An expanding spherical area of scent that diffuses over time.
  /// </summary>
  public class OdorantBurst : Odorant
  {
    public Vector3 Position { get { return transform.TransformPoint(Offset); } }
    public Vector3 Offset;
    [SerializeField]
    private GameObject _debugRadius = null;
    [SerializeField]
    [Range(0.0f, 1.0f)]
    float _impactScaler = 0.1f;
    [SerializeField]
    AnimationCurve _impactCurve = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 0.0f);
    [SerializeField]
    private float _effusionRateMetersPerSecond = 10.0f;
    [SerializeField]
    private bool _parentTransform = false;

    void Start()
    {
      Validate();
      OdorantCommand = new OdorantCommand(OdorantConfig, OdorantAlgorithm.Burst);
    }

    public void Burst()
    {
      StartCoroutine(CoprocessOdorantCommand());
    }

    public IEnumerator CoprocessOdorantCommand()
    {
      // Cache position so the odorant stays where it was when Burst was called
      var position = Position;

      yield return new WaitWhile(delegate () { return !OlfactoryEpithelium.Instanced(); });

      // This will allow the initial radius to be non-zero
      var startTime = Time.time - OlfactoryEpithelium.Get().BurstUpdateInterval;

      var shouldStop = false;
      var elapsedSeconds = 0.0f;

      var innerRadius = 0.0f;
      var outerRadius = 0.0f;

      var innerRadiusSquared = 0.0f;
      var outerRadiusSquared = 0.0f;

      var fadeRateSeconds = 1.0f / Mathf.Pow(_effusionRateMetersPerSecond, 0.33333f);
      var fadeScaler = 1.0f;

      GameObject innerDebugRadius = null;
      GameObject outerDebugRadius = null;
      if (_debugRadius != null)
      {
        innerDebugRadius = Instantiate(_debugRadius, position, Quaternion.identity);
        outerDebugRadius = Instantiate(_debugRadius, position, Quaternion.identity);
      }

      while (!shouldStop)
      {
        // cannot use Time.deltaTime because that's meaningless in a coroutine
        var deltaTime = Time.time - startTime;
        startTime = Time.time;

        OlfactoryEpithelium.OdorantsProcessingThisFrame++;
        if (!gameObject.activeSelf || !gameObject.activeInHierarchy)
        {
          yield return new WaitForSeconds(OlfactoryEpithelium.Get().BurstUpdateInterval);
          continue;
        }
        
        elapsedSeconds += deltaTime;

        // radius = Mathf.Pow(_diffusivity * elapsedSeconds, 0.33333f);
        outerRadius = _effusionRateMetersPerSecond * elapsedSeconds;
        innerRadius = outerRadius * _impactScaler;

        if (_debugRadius != null)
        {
          innerDebugRadius.transform.localScale = new Vector3(innerRadius * 2.0f, innerRadius * 2.0f, innerRadius * 2.0f);
          outerDebugRadius.transform.localScale = new Vector3(outerRadius * 2.0f, outerRadius * 2.0f, outerRadius * 2.0f);
        }
        
        outerRadiusSquared = outerRadius * outerRadius;
        innerRadiusSquared = innerRadius * innerRadius;

        if (_parentTransform)
        {
          position = Position;
          innerDebugRadius.transform.position = position;
          outerDebugRadius.transform.position = position;
        }

        var sqrDistance = Vector3.SqrMagnitude(position - OlfactoryEpithelium.Get().Position);
        if (sqrDistance < innerRadiusSquared)
        {
          OdorantCommand.Intensity = (byte)(Intensity * fadeScaler);
          OlfactoryEpithelium.Get().AddOdorantCommand(OdorantCommand);
        }
        else if (sqrDistance < outerRadiusSquared)
        {
          var normalizedDistance = (Mathf.Sqrt(sqrDistance) - innerRadius) / (outerRadius - innerRadius);
          float userIntensity = Intensity * fadeScaler * _impactCurve.Evaluate(normalizedDistance);
          OdorantCommand.Intensity = (byte)userIntensity;
          OlfactoryEpithelium.Get().AddOdorantCommand(OdorantCommand);
        }

        yield return new WaitForSeconds(OlfactoryEpithelium.Get().BurstUpdateInterval);

        fadeScaler -= fadeRateSeconds * deltaTime;
        if (fadeScaler <= 0.0f)
          shouldStop = true;
      }

      if (_debugRadius != null)
      {
        Destroy(innerDebugRadius);
        Destroy(outerDebugRadius);
      }
    }

    void OnDrawGizmosSelected()
    {
      Gizmos.color = OvrColor.primary;
      Gizmos.DrawWireSphere(Position, 0.02f);
    }

    private void Validate()
    {
      BaseValidate();
    }
  }
}
