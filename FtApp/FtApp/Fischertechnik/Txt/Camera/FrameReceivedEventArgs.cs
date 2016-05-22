using System;

namespace TXTCommunication.Fischertechnik.Txt.Camera
{
    class FrameReceivedEventArgs : EventArgs
    {
        public readonly byte[] FrameData;
        public readonly int DataLength;

        public FrameReceivedEventArgs(byte[] frameData, int dataLength)
        {
            FrameData = frameData;
            DataLength = dataLength;
        }
    }
}
