using System;
using TXTCommunication.Fischertechnik.Txt.Response;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace TXTCommunication.Fischertechnik.Txt.Camera
{
    class ResponseCameraFrame : ResponseBase
    {
        public int FramesReady { get; private set; }
        public short FrameWidth { get; private set; }
        public short FrameHeight { get; private set; }
        public int FrameSizeRaw { get; private set; }
        public int FrameSizeCompressed { get; private set; }
        public byte[] FrameData { get; set; }

        public ResponseCameraFrame()
        {
            ResponseId = TxtInterface.DataIdCameraOnlineFrame;
        }

        public override int GetResponseLength()
        {
            return base.GetResponseLength() +
                   sizeof(int) +
                   sizeof(short) +
                   sizeof(short) +
                   sizeof(int) +
                   sizeof(int);
        }
        
        
        public override void FromByteArray(byte[] bytes)
        {
            if (bytes.Length < GetResponseLength())
            {
                throw new InvalidOperationException($"The byte array is to short to read. Length is {bytes.Length} bytes and {GetResponseLength()}bytes are needed");
            }

            int bytesIndex = 0;

            var id = BitConverter.ToUInt32(bytes, bytesIndex);
            bytesIndex += sizeof(uint);
            
            if (id != ResponseId)
            {
                throw new InvalidOperationException($"The byte array does not match the response id. Expected id is {ResponseId} and response id is {id}");
            }

            FramesReady = BitConverter.ToInt32(bytes, bytesIndex);
            bytesIndex += sizeof(int);

            FrameWidth = BitConverter.ToInt16(bytes, bytesIndex);
            bytesIndex += sizeof(short);

            FrameHeight = BitConverter.ToInt16(bytes, bytesIndex);
            bytesIndex += sizeof(short);

            FrameSizeRaw = BitConverter.ToInt32(bytes, bytesIndex);
            bytesIndex += sizeof(int);

            FrameSizeCompressed = BitConverter.ToInt32(bytes, bytesIndex);
        }
    }
}
