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
  /// A nested-spherical zone of odor that persists based on distance from the closest particle in the emitter
  /// </summary>
  [RequireComponent(typeof(ParticleSystem))]
  public class OdorantParticleSystem : Odorant
  {
    public float RemainingSuspendSeconds { get; private set; }
    private ParticleSystem _particleSystem;
    private ParticleSystem.Particle[] _particles;
    private int _numParticles;
    
    [SerializeField] private float _innerRadius = 0.1f;
    [SerializeField] private AnimationCurve _radialScalar = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 0.0f);
    [SerializeField] private float _outerRadius = 0.3f;

    public float InnerRadius
    {
      get { return _innerRadius; }
      set { _innerRadius = value; InnerRadiusSqrd = value * value; }
    }

    public float OuterRadius
    {
      get { return _outerRadius; }
      set { _outerRadius = value; OuterRadiusSqrd = value * value; }
    }

    public float OuterRadiusSqrd { get; private set; }
    public float InnerRadiusSqrd { get; private set; }

    void Start()
    {
      Validate();
      // Have to do this to set inner and outer radius squared properties
      InnerRadius = _innerRadius;
      OuterRadius = _outerRadius;

      OdorantCommand = new OdorantCommand(OdorantConfig, OdorantAlgorithm.Burst);
      _particleSystem = GetComponent<ParticleSystem>();
      _particles = new ParticleSystem.Particle[_particleSystem.main.maxParticles];
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
    /// The particle system odorant component removes itself from the Olfactory's registry.
    /// </summary>
    void OnDestroy()
    {
      ShouldStop = true;
    }

    void LateUpdate()
    {
      RemainingSuspendSeconds -= Time.deltaTime;

      if (RemainingSuspendSeconds <= 0.0f)
      {
        InitializeIfNeeded();
        _numParticles = _particleSystem.GetParticles(_particles);
      }
    }

    public IEnumerator CoprocessOdorantCommand()
    {
      yield return new WaitWhile(delegate () { return !OlfactoryEpithelium.Instanced() || OlfactoryEpithelium.WaitForLoadBalance; });

      while (!ShouldStop)
      {
        OlfactoryEpithelium.OdorantsProcessingThisFrame++;
        if (!gameObject.activeSelf || !gameObject.activeInHierarchy)
        {
          RemainingSuspendSeconds = OlfactoryEpithelium.Get().BurstUpdateInterval - Time.deltaTime;
          yield return new WaitForSeconds(OlfactoryEpithelium.Get().BurstUpdateInterval);
          continue;
        }
        
        Vector3 particlePosition;
        int closestIndex = -1;
        float closestSqrDistance = float.MaxValue;
        float sqrDistance;
        for (var i = 0; i < _numParticles; i++)
        {
          if (_particleSystem.main.simulationSpace == ParticleSystemSimulationSpace.Local)
            particlePosition = transform.localToWorldMatrix.MultiplyPoint(_particles[i].position);
          else
            particlePosition = _particles[i].position;

          sqrDistance = Vector3.SqrMagnitude(particlePosition - OlfactoryEpithelium.Get().Position);
          if (sqrDistance < OuterRadiusSqrd && sqrDistance < closestSqrDistance)
          {
            closestSqrDistance = sqrDistance;
            closestIndex = i;
          }
        }
        
        if (closestIndex >= 0)
        {
          if (closestSqrDistance < InnerRadiusSqrd)
          {
            OdorantCommand.Intensity = Intensity;
          }
          else
          {
            var normalizedIntensity = Mathf.Clamp01(_radialScalar.Evaluate((Mathf.Sqrt(closestSqrDistance) - InnerRadius) / (OuterRadius - InnerRadius)));
            OdorantCommand.Intensity = (byte)Mathf.Lerp(0.0f, Intensity, normalizedIntensity);
          }

          OlfactoryEpithelium.Get().AddOdorantCommand(OdorantCommand);
        }

        RemainingSuspendSeconds = OlfactoryEpithelium.Get().BurstUpdateInterval - Time.deltaTime;
        yield return new WaitForSeconds(OlfactoryEpithelium.Get().BurstUpdateInterval);
      }
    }

    void InitializeIfNeeded()
    {
      if (_particleSystem == null)
        _particleSystem = GetComponent<ParticleSystem>();

      if (_particles == null || _particles.Length < _particleSystem.main.maxParticles)
        _particles = new ParticleSystem.Particle[_particleSystem.main.maxParticles];
    }

    void OnDrawGizmosSelected()
    {
      _particleSystem = GetComponent<ParticleSystem>();
      _particles = new ParticleSystem.Particle[_particleSystem.main.maxParticles];
      _numParticles = _particleSystem.GetParticles(_particles);
      Vector3 particlePosition;
      for (var i = 0; i < _numParticles; i++)
      {
        if (_particleSystem.main.simulationSpace == ParticleSystemSimulationSpace.Local)
          particlePosition = transform.localToWorldMatrix.MultiplyPoint(_particles[i].position);
        else
          particlePosition = _particles[i].position;
        
        Gizmos.color = OvrColor.inner;
        Gizmos.DrawWireSphere(particlePosition, _innerRadius * _particles[i].GetCurrentSize(_particleSystem));
        Gizmos.color = OvrColor.outer;
        Gizmos.DrawWireSphere(particlePosition, _outerRadius * _particles[i].GetCurrentSize(_particleSystem));
      }
    }

    private void Validate()
    {
      BaseValidate();

      if (OuterRadius <= 0.0f)
        Debug.LogWarningFormat("<b>[OVR]</b> {0}: The outer radius must be greater than zero.", GetParentList() + gameObject.name);

      if (OuterRadius <= InnerRadius)
        Debug.LogWarningFormat("<b>[OVR]</b> {0}: The outer radius ({1}) must be greater than inner radius ({2}).", GetParentList() + gameObject.name, OuterRadius, InnerRadius);
    }
  }
}
