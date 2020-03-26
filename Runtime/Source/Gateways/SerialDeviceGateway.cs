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

#if NET_4_6 && UNITY_STANDALONE_WIN
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO.Ports;
using OVR.Data;
using OVR.DataCollection;
using System.Threading;

namespace OVR.Gateways
{
  /// <summary>
  /// Communication gateway between the OVR device via USB.
  /// </summary>
  public class SerialDeviceGateway : IDeviceGateway
  {
    public static string SerialMessageFromOVR = "";

    public int PacketSizeBytes { get; private set; }

    private List<OdorantCommand> _commands = new List<OdorantCommand>();

    private byte[] _packet;

    private static SerialPort _serialPort = new SerialPort();

    private int _maxCommandsPerPacket;

    private NetworkLogging _odorantActivationLog = null;

    public SerialDeviceGateway(bool scentActivationLogging = false, int localhostDebugPort = 0)
    {
      if (scentActivationLogging)
        _odorantActivationLog = new NetworkLogging(localhostDebugPort);
    }

    public void Dispose()
    {
      if (_serialPort.IsOpen)
      {
        // SendFanSpeed(0);
        _serialPort.Close();
      }
      _serialPort.Dispose();
    }

    public void BeginConnect()
    {
    }

    public bool CheckConnection()
    {
      if (!_serialPort.IsOpen)
        return false;

      if (!writeString("OVR Version Query"))
      {
        _serialPort.Close();
        return false;
      }

      var read = string.Empty;
      do
      {
        read = _serialPort.ReadLine();
        if (read.Length > 10)
        {
          if(read.Remove(11) == "OVR version")
            return true;
        }
      } while (read != string.Empty) ;

      return false;
    }

    public bool Connect()
    {
      if (!_serialPort.IsOpen)
      {
        _serialPort.BaudRate = 115200;
        _serialPort.Parity = Parity.None;
        _serialPort.DataBits = 8;
        _serialPort.StopBits = StopBits.One;

        _serialPort.DataReceived += new SerialDataReceivedEventHandler(dataReceivedHandler);
        _serialPort.ErrorReceived += new SerialErrorReceivedEventHandler(errorReceivedHandler);

        foreach (var portName in SerialPort.GetPortNames())
        {
          _serialPort.PortName = portName;

          try
          {
            _serialPort.Open();
          }
          catch(System.IO.IOException e)
          {
            continue;
          }

          if(!writeString("OVR Version Query"))
          {
            _serialPort.Close();
            continue;
          }
          
          var read = _serialPort.ReadLine();
          if (read.Length < 11)
          {
            _serialPort.Close();
            continue;
          }

          if (read.Remove(11) == "OVR version")
            break;
          else
            _serialPort.Close();
        }
      }

      return _serialPort.IsOpen;
    }

    public void Init(int maxCommandsPerPacket)
    {
      _maxCommandsPerPacket = maxCommandsPerPacket;
      PacketSizeBytes = _maxCommandsPerPacket * OdorantCommand.Size + sizeof(ushort) + 1;
      _packet = new byte[PacketSizeBytes];
      // Insures that the device is in serial mode once connected
      writeString("RunSerial");
    }
    
    public void AddCommand(OdorantCommand command)
    {
      if (_commands.Count >= _maxCommandsPerPacket)
        return;

      _commands.Add(command);
    }
    public void AddCommands(IEnumerable<OdorantCommand> commands)
    {
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
      if(_odorantActivationLog != null)
        foreach (var command in _commands)
          _odorantActivationLog.Add(command.ToCsv());

      return writeBytes(CommandsToBytes());
    }

    public byte[] CommandsToBytes()
    {
      int index = 0;
      _packet[index++] = (byte)MessageType.ODORANT_COMMAND;
      _packet[index++] = (byte)_commands.Count;

      foreach (var command in _commands)
      {
        _packet[index++] = command.Slot;
        _packet[index++] = command.Algorithm;
        _packet[index++] = command.Intensity;
      }

      return _packet;
    }

    public bool HasCommandsToSend()
    {
      return _commands.Any();
    }

    public override string ToString()
    {
      return string.Join("\t", _commands.Select(c => c.ToString()).ToArray());
    }

    public string GatewayLabel()
    {
      return "USB";
    }

    private bool writeString(string message)
    {
      var bytes = new byte[message.Length + 2];
      bytes[0] = (byte)(message.Length >> 8);
      bytes[1] = (byte)message.Length;

      for (var i = 2; i < message.Length + 2; i++)
      {
        bytes[i] = (byte)message[i - 2];
      }

      try
      {
        _serialPort.Write(bytes, 0, bytes.Length);
        _serialPort.BaseStream.Flush();
      }
      catch (Exception e)
      {
        _serialPort.Close();
        return false;
      }

      return true;
    }

    private bool writeBytes(byte[] data)
    {
      var bytes = new byte[data.Length + 2];
      bytes[0] = (byte)(data.Length >> 8);
      bytes[1] = (byte)data.Length;

      for (var i = 2; i < data.Length + 2; i++)
      {
        bytes[i] = data[i - 2];
      }
      try
      {
        _serialPort.Write(bytes, 0, bytes.Length);
        _serialPort.BaseStream.Flush();
      }
      catch(Exception e)
      {
        _serialPort.Close();
        return false;
      }

      return true;
    }

    private static void dataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
    {
      SerialMessageFromOVR = _serialPort.ReadExisting();
    }

    private void errorReceivedHandler(object sender, SerialErrorReceivedEventArgs e)
    {
    }
  }
}
#endif
