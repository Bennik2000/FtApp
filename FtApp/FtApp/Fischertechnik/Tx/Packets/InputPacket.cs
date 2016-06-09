using System;

namespace TXCommunication.Packets
{
    class InputPacket : Packet
    {
        public ushort[] UniversalInputs { get; set; }
        public ushort[] CounterInput { get; set; }
        public ushort[] CounterValue { get; set; }
        public ushort DisplayButtonLeft { get; set; }
        public ushort DisplayButtonRight { get; set; }



        public InputPacket()
        {
            UniversalInputs = new ushort[TxInterface.UniversalInputs];
            CounterInput = new ushort[TxInterface.Counters];
            CounterValue = new ushort[TxInterface.Counters];
            
            CommandCode = 0x66;
        }

        public override int GetPacketLength()
        {
            return 16*4 + 15;
        }

        public override bool FromByteArray(byte[] data)
        {
            bool baseReturn = base.FromByteArray(data);

            int packetPosition = 6;

            for (int i = 0; i < UniversalInputs.Length; i++)
            {
                UniversalInputs[i] = BitConverter.ToUInt16(PayloadBytes, packetPosition);
                packetPosition += sizeof(ushort);
            }


            for (int i = 0; i < CounterInput.Length; i++)
            {
                CounterInput[i] = PayloadBytes[packetPosition];
                packetPosition += sizeof(char);
            }


            for (int i = 0; i < CounterValue.Length; i++)
            {
                CounterValue[i] = BitConverter.ToUInt16(PayloadBytes, packetPosition);
                packetPosition += sizeof(ushort);
            }


            return baseReturn;
        }
    }
}
