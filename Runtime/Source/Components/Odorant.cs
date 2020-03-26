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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OVR.Components
{
  /// <summary>
  /// Base component for all odorant components for extensibility.
  /// </summary>
  public class Odorant : MonoBehaviour
  {
    public OdorantConfig OdorantConfig;
    [SerializeField] [Range(0.0f, 1.0f)]
    protected float _intensity = 0.5f;
    public bool ShouldStop { get; protected set; }
    [SerializeField]
    public OdorantCommand OdorantCommand { get; protected set; }
    public byte Intensity { get { return (byte)(255 * _intensity); } }

    protected void BaseValidate()
    {
      if (OdorantConfig == null)
        Debug.LogWarningFormat("<b>[OVR]</b> {0}: An Odorant Configuration must be referenced.", GetParentList() + gameObject.name);

      if (_intensity <= float.Epsilon)
        Debug.LogWarningFormat("<b>[OVR]</b> {0}: Maximum threshold must be greater than zero.", GetParentList() + gameObject.name);
    }

    protected string GetParentList()
    {
      var parentNames = new List<string>();
      var currentParent = transform.parent;
      while (currentParent != null)
      {
        parentNames.Add(currentParent.name);
        currentParent = currentParent.parent;
      }

      if (parentNames.Any())
      {
        parentNames.Reverse();
        return parentNames.Aggregate((a, b) => a + " -> " + b) + " -> ";
      }

      return string.Empty;
    }
  }
}
