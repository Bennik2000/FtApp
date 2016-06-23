namespace TXCommunication.Packets
{
    /// <summary>
    /// This packet is the response which is sent back after an echo packet
    /// </summary>
    class EchoResponsePacket : Packet
    {
        public EchoResponsePacket()
        {
            CommandCode = 0x65;
        }
    }
}
