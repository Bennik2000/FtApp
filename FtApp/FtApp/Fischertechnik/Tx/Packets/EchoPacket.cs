namespace TXCommunication.Packets
{
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
