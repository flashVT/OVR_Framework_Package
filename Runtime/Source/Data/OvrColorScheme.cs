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

using UnityEngine;

namespace OVR.Data
{
  public static class OvrColor
  {
    public static readonly Color shark = new Color(0.173f, 0.176f, 0.188f);
    public static readonly Color pickledBluewood = new Color(0.169f, 0.216f, 0.326f);
    public static readonly Color comet = new Color(0.314f, 0.329f, 0.424f);
    public static readonly Color pictonBlue = new Color(0.326f, 0.69f, 0.886f);
    public static readonly Color warmGrey = new Color(0.694f, 0.702f, 0.69f);
    public static readonly Color iron = new Color(0.839f, 0.847f, 0.867f);
    public static readonly Color white = new Color(1.0f, 1.0f, 1.0f);

    public static readonly Color primary = white;
    public static readonly Color inner = warmGrey;
    public static readonly Color outer = pictonBlue;
  }
}