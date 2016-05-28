namespace TXCommunication.Packets
{
    class ConfigResponsePacket : Packet
    {
        public ConfigResponsePacket()
        {
            CommandCode = 0x69;
        }

        public override int GetPacketLength()
        {
            return 16 + 15;
        }
    }
}
