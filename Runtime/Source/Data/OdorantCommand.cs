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

using OVR.Components;
using System;

namespace OVR.Data
{
  public class OdorantCommand
  {
    public OdorantCommand(OdorantConfig config, OdorantAlgorithm algorithm, byte intensity = 0)
    {
      Name = config.name;
      Slot = config.Slot;
      Algorithm = (byte)algorithm;
      Intensity = intensity;
    }

    public byte[] GetPacket()
    {
      return _packet;
    }

    public override string ToString()
    {
      return string.Format("N:{0} S:{1} A:{2} I:{3}", Name, Slot, Algorithm, Intensity);
    }

    public string ToCsv()
    {
      var algorithm = "Burst";

      if (Algorithm == (int)OdorantAlgorithm.Ambient)
        algorithm = "Ambient";

      return string.Format("{0},{1},{2},{3}", Name, Slot + 1, algorithm, Intensity);
    }

    public const int Size = 4;

    public string Name { get; private set; }

    public byte Slot
    {
      get
      {
        return _packet[0];
      }
      set
      {
        _packet[0] = value;
      }
    }

    public byte Algorithm
    {
      get
      {
        return _packet[1];
      }
      set
      {
        _packet[1] = value;
      }
    }

    public ushort Intensity
    {
      get
      {
        return BitConverter.ToUInt16(_packet, 2);
        // return (ushort)(_packet[2] + (_packet[3] << 8));
      }
      set
      {
        BitConverter.GetBytes(value).CopyTo(_packet, 2);
        // _packet[2] = (byte)value;
        // _packet[3] = (byte)(value >> 8);
      }
    }

    private byte[] _packet = new byte[Size];
  }
}
