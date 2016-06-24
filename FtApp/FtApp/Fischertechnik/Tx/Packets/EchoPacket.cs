namespace TXCommunication.Packets
{
    /// <summary>
    /// This packet is used to test the connection
    /// </summary>
    class EchoPacket : Packet
    {
        public EchoPacket()
        {
            SendPayloadBytes = false;
            SendTransferAreaIdBytes = false;
            CommandCode = 0x01;
        }
    }
}
