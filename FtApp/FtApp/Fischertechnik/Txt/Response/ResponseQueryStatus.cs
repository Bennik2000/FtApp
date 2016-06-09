using System;
using System.Text;

namespace TXTCommunication.Fischertechnik.Txt.Response
{
    public class ResponseQueryStatus : ResponseBase
    {
        private byte[] _devicename;
        public byte[] Devicename
        {
            get
            {
                // ReSharper disable once ConvertIfStatementToNullCoalescingExpression
                if (_devicename == null)
                {
                    _devicename = new byte[16];
                }
                return _devicename;
            }
            set { _devicename = value; }
        }

        public uint Version { get; set; }


        public ResponseQueryStatus()
        {
            ResponseId = TxtInterface.ResponseIdQueryStatus;
        }

        public string GetDecoratedVersion()
        {
            byte version0 = (byte) (Version >> 0);
            byte version1 = (byte) (Version >> 8);
            byte version2 = (byte) (Version >> 16);
            byte version3 = (byte) (Version >> 24);

            return $"{version3}.{version2}.{version1}.{version0}";
        }

        public string GetControllerName()
        {
            string decoreatedName = Encoding.ASCII.GetString(Devicename, 0, Devicename.Length);
            decoreatedName = decoreatedName.TrimEnd('\0');
            return decoreatedName;
        }

        public override int GetResponseLength()
        {
            return 24;
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



            for (int i = 0; i < Devicename.Length; i++)
            {
                Devicename[i] = bytes[bytesIndex];

                bytesIndex++;
            }

            Version = BitConverter.ToUInt32(bytes, bytesIndex);
        }
    }
}