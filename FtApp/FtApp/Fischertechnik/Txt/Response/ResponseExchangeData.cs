using System;
using System.Collections.Generic;

namespace TXTCommunication.Fischertechnik.Txt.Response
{
    class ResponseExchangeData : ResponseBase
    {
        public short[] UniversalInputs { get; }
        public short[] CounterInput { get; }
        public short[] CounterValue { get; }
        public short[] CounterCommandId { get; }
        public short[] MotorCommandId { get; }

        public ushort SoundCommandId { get; set; }
        
        public ResponseExchangeDataIr[] Ir { get; }

        public ResponseExchangeData()
        {
            ResponseId = TxtInterface.ResponseIdExchangeData;

            UniversalInputs = new short[8];
            CounterInput = new short[4];
            CounterValue = new short[4];
            CounterCommandId = new short[4];
            MotorCommandId = new short[4];
            Ir = new ResponseExchangeDataIr[5];
        }

        public override int GetResponseLength()
        {
            return base.GetResponseLength() +
                   UniversalInputs.Length*sizeof(short) +
                   CounterInput.Length*sizeof(short) +
                   CounterValue.Length*sizeof(short) +
                   CounterCommandId.Length*sizeof(short) +
                   MotorCommandId.Length*sizeof(short) +
                   sizeof(ushort) +
                   ResponseExchangeDataIr.GetByteLength()*Ir.Length + 1;
        }

        public override void FromByteArray(byte[] bytes)
        {
            if (bytes.Length < GetResponseLength())
            {
                throw new InvalidOperationException($"The byte array is to short to read. Length is {bytes.Length} bytes and 24 bytes are needed");
            }

            int bytesIndex = 0;

            var id = BitConverter.ToUInt32(bytes, bytesIndex);

            bytesIndex += 4;


            if (id != ResponseId)
            {
                throw new InvalidOperationException($"The byte array does not match the response id. Expected id is {ResponseId} and response id is {id}");
            }


            for (int i = 0; i < UniversalInputs.Length; i++)
            {
                UniversalInputs[i] = BitConverter.ToInt16(bytes, bytesIndex);

                bytesIndex += sizeof(short);
            }

            for (int i = 0; i < CounterInput.Length; i++)
            {
                CounterInput[i] = BitConverter.ToInt16(bytes, bytesIndex);

                bytesIndex += sizeof(short);
            }

            for (int i = 0; i < CounterValue.Length; i++)
            {
                CounterValue[i] = BitConverter.ToInt16(bytes, bytesIndex);

                bytesIndex += sizeof(short);
            }

            for (int i = 0; i < CounterCommandId.Length; i++)
            {
                CounterCommandId[i] = BitConverter.ToInt16(bytes, bytesIndex);

                bytesIndex += sizeof(short);
            }

            for (int i = 0; i < MotorCommandId.Length; i++)
            {
                MotorCommandId[i] = BitConverter.ToInt16(bytes, bytesIndex);

                bytesIndex += sizeof(short);
            }
            

            SoundCommandId = BitConverter.ToUInt16(bytes, bytesIndex);


            for (int i = 0; i < Ir.Length; i++)
            {
                Ir[i] = new ResponseExchangeDataIr();
                Ir[i].FromBytes(bytes, bytesIndex);

                bytesIndex += ResponseExchangeDataIr.GetByteLength();
            }
        }
    }

    class ResponseExchangeDataIr
    {
        public byte IrLeftX { get; set; }
        public byte IrLeftY { get; set; }
        public byte IrRightX { get; set; }
        public byte IrRightY { get; set; }
        public byte IrBits { get; set; }

        public static int GetByteLength()
        {
            return sizeof(byte) +
                   sizeof(byte) +
                   sizeof(byte) +
                   sizeof(byte) +
                   sizeof(byte);
        }

        public byte[] GetBytes()
        {
            List<byte> bytes = new List<byte> {IrLeftX, IrLeftY, IrRightX, IrRightY, IrBits};
            
            return bytes.ToArray();
        }

        public void FromBytes(byte[] bytes, int offset)
        {
            int bytesIndex = offset;

            IrLeftX = bytes[bytesIndex++];
            IrLeftY = bytes[bytesIndex++];
            IrRightX = bytes[bytesIndex++];
            IrRightY = bytes[bytesIndex++];
            IrBits = bytes[bytesIndex];
        }
    }
}
