namespace TXCommunication.Packets
{
    class EchoResponsePacket : Packet
    {
        public EchoResponsePacket()
        {
            CommandCode = 0x65;
        }
    }
}
