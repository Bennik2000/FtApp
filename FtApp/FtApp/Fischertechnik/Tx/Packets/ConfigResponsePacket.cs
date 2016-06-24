namespace TXCommunication.Packets
{
    /// <summary>
    /// This is the packet which is sent back after a configuration packet. It does not contain any payload data
    /// </summary>
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
