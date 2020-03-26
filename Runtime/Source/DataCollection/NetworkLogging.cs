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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace OVR.DataCollection
{
  /// <summary>
  /// Generic logging helper via UDP.
  /// </summary>
  public class NetworkLogging : IDisposable
  {
    public static bool ShouldLogAll = true;
    private Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    private IPEndPoint _endPoint;
    private List<string> _logs = new List<string>();
    private static List<NetworkLogging> _instances = new List<NetworkLogging>();

    public NetworkLogging(int port)
    {
      _endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
      _instances.Add(this);
    }

    public NetworkLogging(IPAddress ipAddress, int port)
    {
      _endPoint = new IPEndPoint(ipAddress, port);
      _instances.Add(this);
    }
    public NetworkLogging(IPEndPoint endPoint)
    {
      _endPoint = endPoint;
      _instances.Add(this);
    }

    public void Dispose()
    {
      _instances.Remove(this);
    }

    public static void SendAllLogs()
    {
      if (!ShouldLogAll)
        return;

      foreach (var instance in _instances)
      {
        foreach (var log in instance._logs)
        {
          var message = string.Format("{0},{1}", DateTime.Now, log);

          instance._socket.SendTo(message.Select(o => (byte)o).ToArray(), instance._endPoint);
        }
        instance._logs.Clear();
      }
    }

    public void Add(string logMessage)
    {
      if (!ShouldLogAll)
        return;

      _logs.Add(logMessage);
    }
  }
}
