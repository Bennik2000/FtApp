namespace TXCommunication.Packets
{
    /// <summary>
    /// This packet is sent to request the information
    /// </summary>
    class RequestInfoPacket : Packet
    {
        public RequestInfoPacket()
        {
            CommandCode = 0x06;
        }

        public override int GetPacketLength()
        {
            return 16 + 15;
        }
    }
}
