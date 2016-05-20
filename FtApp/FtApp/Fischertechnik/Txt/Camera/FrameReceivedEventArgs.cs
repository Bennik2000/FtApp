using System;

namespace TXTCommunication.Fischertechnik.Txt.Camera
{
    class FrameReceivedEventArgs : EventArgs
    {
        public readonly ResponseCameraFrame ResponseFrame;

        public FrameReceivedEventArgs(ResponseCameraFrame responseFrame)
        {
            ResponseFrame = responseFrame;
        }
    }
}
