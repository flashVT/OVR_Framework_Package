namespace OVR.API
{
  public class MarshalHeader
  {
    private static readonly byte _checksumValue = 251;
    public static int Size => 4;
    public byte Checksum { get; private set; }
    public byte MessageType { get; private set; }
    public ushort MessageSize { get; private set; }
    public int TotalMessageSize => Size + MessageSize;
    public bool Valid => (Checksum + MessageType + MessageSize) % _checksumValue == 0;

    public MarshalHeader(MessageTypes messageType = 0, ushort messageSize = 0)
    {
      Set(messageType, messageSize);
    }
    public static void Set(ref byte[] buffer, MessageTypes messageType, ushort messageSize = 0)
    {
      MarshalHeader header = new MarshalHeader(messageType, messageSize);
      header.ToBytes(ref buffer);
    }

    public void Set(MessageTypes messageType, ushort messageSize = 0)
    {
      MessageType = (byte)messageType;
      MessageSize = messageSize;
      Checksum = (byte)(_checksumValue - (MessageType + MessageSize) % _checksumValue);
    }

    public void Set(byte[] buffer, int offset = 0)
    {
      Checksum = buffer[0 + offset];
      MessageType = buffer[1 + offset];
      MessageSize = (ushort)((buffer[3 + offset] << 8) + buffer[2 + offset]);
    }

    public void ToBytes(ref byte[] buffer)
    {
      buffer[0] = Checksum;
      buffer[1] = MessageType;
      buffer[2] = (byte)MessageSize;
      buffer[3] = (byte)(MessageSize >> 8);
    }
  }
}
