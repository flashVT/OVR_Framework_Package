using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace OVR.Gateways
{
  public static class IPAddressExtensions
  {
    /// <summary>
    /// Requires internet access :(
    /// </summary>
    /// <returns></returns>
    public static IPAddress GetLocalIP()
    {
      var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
      socket.Connect("8.8.8.8", 65530);
      return (socket.LocalEndPoint as IPEndPoint).Address;
    }

    public static List<IPAddress> GetAllLocalIPv4()
    {
      var ipAddressList = new List<IPAddress>();

      foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
      {
        if (item.OperationalStatus == OperationalStatus.Up)
        {
          foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
          {
            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
            {
              ipAddressList.Add(ip.Address);
            }
          }
        }
      }

      return ipAddressList;
    }

    public static IPAddress GetSubnetMask(this IPAddress address)
    {
      foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
      {
        foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
        {
          if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
          {
            if (address.Equals(unicastIPAddressInformation.Address))
            {
              return unicastIPAddressInformation.IPv4Mask;
            }
          }
        }
      }
      throw new ArgumentException(string.Format("Can't find subnet mask from IP address [{0}]", address));
    }

#pragma warning disable CS0618 // we are checking the address family
    public static List<IPAddress> GetBroadcastAddresses()
    {
      var broadcastAddresses = new List<IPAddress>();
      broadcastAddresses.Add(IPAddress.Broadcast);
      foreach (var address in GetAllLocalIPv4())
      {
        broadcastAddresses.Add(address.GetBroadcastAddress());
      }

      return broadcastAddresses;
    }
    public static IPAddress GetBroadcastAddress(this IPAddress address)
    {
      if (address.AddressFamily != AddressFamily.InterNetwork)
        throw new ArgumentException(string.Format("Can only generate IPv4 broadcast addresses."));

      var subnetMask = address.GetSubnetMask();

      return new IPAddress((address.Address & subnetMask.Address) | (subnetMask.Address ^ 0xFFFFFFFF));
    }
    public static IPAddress GetBroadcastAddress(this IPAddress address, IPAddress subnetMask)
    {
      if (address.AddressFamily != AddressFamily.InterNetwork || subnetMask.AddressFamily != AddressFamily.InterNetwork)
        throw new ArgumentException(string.Format("Can only generate IPv4 broadcast addresses."));

      return new IPAddress((address.Address & subnetMask.Address) | (subnetMask.Address ^ 0xFFFFFFFF));
    }
#pragma warning restore CS0618
  }
}
