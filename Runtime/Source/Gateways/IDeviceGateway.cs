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
using System;
using System.Collections.Generic;

namespace OVR.Gateways
{
  public interface IDeviceGateway : IDisposable
  {
    void BeginConnect();
    bool CheckConnection();
    bool Connect();
    void Init(int maxCommandsPerPacket);
    void AddCommand(OdorantCommand command);
    void AddCommands(IEnumerable<OdorantCommand> commands);
    void RemoveCommand(byte slot);
    void ClearCommands();
    bool SendCommands();
    bool HasCommandsToSend();
    string GatewayLabel();
    bool IsConnected();
  }
}
