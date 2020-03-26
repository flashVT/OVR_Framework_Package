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
  /// A box area of odor that persists based on time
  /// </summary>
  public class OdorantBox : Odorant
  {
    public Vector3 Position { get { return transform.TransformPoint(Offset); } }
    public Vector3 Offset;
    [SerializeField]
    private Vector3 _boxSize = Vector3.one * 2.0f;

    private float _suspendSeconds = 1.0f;

    private Vector3 localScaleBoxSize { get { return new Vector3(_boxSize.x * transform.localScale.x, _boxSize.y * transform.localScale.y, _boxSize.z * transform.localScale.z); } }

    void Start()
    {
      Validate();
      OdorantCommand = new OdorantCommand(OdorantConfig, OdorantAlgorithm.Ambient, Intensity);
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
    /// The ambient odorant node removes itself from the Olfactory's registry.
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
          yield return new WaitForSeconds(_suspendSeconds);
          continue;
        }

        // IsSuspended = true;
        var bounds = new Bounds(Position, localScaleBoxSize);
        if (OlfactoryEpithelium.Get() && !bounds.Contains(OlfactoryEpithelium.Get().Position))
        {
          yield return new WaitForSeconds(_suspendSeconds);
          continue;
        }

        OlfactoryEpithelium.Get().AddOdorantCommand(OdorantCommand);
        yield return new WaitForSeconds(_suspendSeconds);
      }
    }

    void OnDrawGizmosSelected()
    {
      Gizmos.color = OvrColor.primary;
      Gizmos.DrawWireCube(Position, localScaleBoxSize);
    }

    private void Validate()
    {
      BaseValidate();
      
      if (_boxSize.sqrMagnitude == 0.0f)
        Debug.LogWarningFormat("<b>[OVR]</b> {0}: Collision box must be greater than zero.", GetParentList() + gameObject.name);

    }
  }
}
