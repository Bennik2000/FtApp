using System;
using System.Collections.Generic;

namespace TXCommunication.Packets
{
    class OutputPacket : Packet
    {
        public ushort[] CounterResetCommandId { get; }
        public byte[] MotorMasterValues { get; }
        public short[] PwmOutputValues { get; }
        public ushort[] MotorDistanceValues { get; }
        public ushort[] MotorCommandId { get; }

        public OutputPacket()
        {
            CounterResetCommandId = new ushort[TxInterface.Counters];
            MotorMasterValues = new byte[TxInterface.MotorOutputs];
            PwmOutputValues = new short[TxInterface.PwmOutputs];
            MotorDistanceValues  =new ushort[TxInterface.MotorOutputs];
            MotorCommandId = new ushort[TxInterface.MotorOutputs];

            CommandCode = 0x02;
            SendTransferAreaIdBytes = false;
        }

        protected override byte[] ConstructPayload()
        {
            List<byte> payloadList = new List<byte>();
            

            foreach (ushort counterReset in CounterResetCommandId)
            {
                payloadList.AddRange(BitConverter.GetBytes(counterReset));
            }
            foreach (byte motorMasterValue in MotorMasterValues)
            {
                payloadList.AddRange(BitConverter.GetBytes(motorMasterValue));
            }
            foreach (short pwmOutputValue in PwmOutputValues)
            {
                payloadList.AddRange(BitConverter.GetBytes(pwmOutputValue));
            }
            foreach (ushort motorDistanceValue in MotorDistanceValues)
            {
                payloadList.AddRange(BitConverter.GetBytes(motorDistanceValue));
            }
            foreach (ushort motorCommandId in MotorCommandId)
            {
                payloadList.AddRange(BitConverter.GetBytes(motorCommandId));
            }

            PayloadBytes = payloadList.ToArray();

            return base.ConstructPayload();
        }
    }
}
