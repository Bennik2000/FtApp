namespace TXCommunication.Packets
{
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
