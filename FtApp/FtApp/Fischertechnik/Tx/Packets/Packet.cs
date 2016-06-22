using System;
using System.Collections.Generic;

namespace TXCommunication.Packets
{
    /// <summary>
    /// This is the base class of a packet
    /// </summary>
    public abstract class Packet
    {
        public byte[] PayloadBytes { get; set; } = new byte[0];
        public int CommandCode { get; set; }
        public int From { get; set; } = 0x02;
        public int To { get; set; } = 0x01;
        public short TransactionId { get; set; }
        public short SessionId { get; set; }
        public short TotalLength { get; private set; }
        protected bool SendPayloadBytes { get; set; } = true;
        protected bool SendTransferAreaIdBytes { get; set; } = true;


        // ReSharper disable once VirtualMemberNeverOverriden.Global
        public virtual byte[] GetByteArray()
        {
            ConstructPayload();

            // ReSharper disable once UseObjectOrCollectionInitializer
            List<byte> packet = new List<byte>();
            
            // Packet start (STX)
            packet.Add(0x02);
            packet.Add(0x55);

            // Total length (header + payload); Note: The byte order of the packet length is swapped
            int transferAreaIdLength = SendTransferAreaIdBytes ? 4 : 0;
            int payloadLength = SendPayloadBytes ? PayloadBytes.Length : 0;

            int totalLength = 20 + transferAreaIdLength + payloadLength;
            packet.Add((byte)((totalLength >> 8) & 0x00FF));
            packet.Add((byte)((totalLength >> 0) & 0x00FF));

            // From
            packet.AddRange(BitConverter.GetBytes(From));

            // To
            packet.AddRange(BitConverter.GetBytes(To));

            // Transaction id
            packet.AddRange(BitConverter.GetBytes(TransactionId));

            // Session Id; Note: The session id should always be the same as the previous respionse packet contained
            packet.AddRange(BitConverter.GetBytes(SessionId));

            // Command Code
            packet.AddRange(BitConverter.GetBytes(CommandCode));

            // Number of the following payloads. We only send one payload at once
            packet.AddRange(BitConverter.GetBytes(1));
            
            if (SendTransferAreaIdBytes)
            {
                // Transfer area id
                packet.AddRange(BitConverter.GetBytes(0));
            }
            
            if (SendPayloadBytes)
            {
                // Add payload
                packet.AddRange(PayloadBytes);
            }

            // Checksum; Note: The byte order of the crc is swapped
            short checksum = CalculateCrc(packet.ToArray(), 2, packet.Count);
            packet.Add((byte)((checksum >> 8) & 0x00FF));
            packet.Add((byte)((checksum >> 0) & 0x00FF));

            // End of packet (ETX)
            packet.Add(0x03);

            return packet.ToArray();
        }

        public virtual bool FromByteArray(byte[] data)
        {
            try
            {
                //  Read the total length from the packet; Note: The byte order of the packet length is swapped
                int position = 2;
                TotalLength = (short) ((data[position++] << 8) | (data[position++] << 0));

                // From
                From =
                    (short)
                        ((data[position++] >> 0) | (data[position++] >> 8) | (data[position++] >> 16) |
                         (data[position++] >> 24));

                // To
                To =
                    (short)
                        ((data[position++] >> 0) | (data[position++] >> 8) | (data[position++] >> 16) |
                         (data[position++] >> 24));

                // Transaction Id
                TransactionId = (short) ((data[position++] >> 0) | (data[position++] >> 8));

                // Session Id
                SessionId = (short) ((data[position++] >> 0) | (data[position++] >> 8));

                // Command code
                CommandCode = (short)
                    ((data[position++] >> 0) | (data[position++] >> 8) |
                     (data[position++] >> 16) | (data[position++] >> 24));

                // Skip the number of the following data structures and the transfer area id
                position += 2;

                // Copy the payload
                PayloadBytes = new byte[TotalLength - 20];

                Array.Copy(data, position, PayloadBytes, 0, PayloadBytes.Length);

                //position += PayloadBytes.Length;


                // We do not check the checksum because the algorithm has changed
                //Read the checksum and check
                //short crc = (short)((data[position++] << 8) | (data[position] << 0));

                //short crcToCheck = CalculateCrc(data, 2, data.Length - 3);

                //if (crc != crcToCheck)
                //{
                //    return false;
                //}
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
            catch (OverflowException)
            {
            }

            return true;
        }

        protected virtual byte[] ConstructPayload()
        {
            return PayloadBytes;
        }

        protected short CalculateCrc(byte[] packet, int startIndex, int endIndex)
        {
            short crc = 0;

            for (int i = startIndex; i < endIndex; i++)
            {
                crc += packet[i];
            }

            crc = (short)(~crc + 1);

            return crc;
        }

        public virtual int GetPacketLength()
        {
            return 27;
        }
    }
}
