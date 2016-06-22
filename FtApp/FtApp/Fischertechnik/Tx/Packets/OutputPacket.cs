using System;
using System.Collections.Generic;

namespace TXCommunication.Packets
{
    /// <summary>
    /// This packet is sent to set the output values.
    /// </summary>
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
            
            // The first 8 bytes contain the reset commands for every counter.
            // If the reset command is set, the related counter value will be resetted
            foreach (ushort counterReset in CounterResetCommandId)
            {
                payloadList.AddRange(BitConverter.GetBytes(counterReset));
            }

            // The next bytes set the motor master. If one master is set, another motor can be configured to
            // be syncron
            foreach (byte motorMasterValue in MotorMasterValues)
            {
                payloadList.AddRange(BitConverter.GetBytes(motorMasterValue));
            }

            // These bytes contain the raw PWM values of each port. The values must be between 1 and 512
            foreach (short pwmOutputValue in PwmOutputValues)
            {
                payloadList.AddRange(BitConverter.GetBytes(pwmOutputValue));
            }

            // These bytes set the distance of one motor.
            foreach (ushort motorDistanceValue in MotorDistanceValues)
            {
                payloadList.AddRange(BitConverter.GetBytes(motorDistanceValue));
            }

            // The last bytes set the motor command id
            foreach (ushort motorCommandId in MotorCommandId)
            {
                payloadList.AddRange(BitConverter.GetBytes(motorCommandId));
            }

            PayloadBytes = payloadList.ToArray();

            return base.ConstructPayload();
        }
    }
}
