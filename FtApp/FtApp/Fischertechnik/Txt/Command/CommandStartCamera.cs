using System;
using System.Collections.Generic;

namespace TXTCommunication.Fischertechnik.Txt.Command
{
    internal class CommandStartCamera : CommandBase
    {
        // Tested resolutions / frame rates for the ft-camera are 320x240@30fps and 640x480@15fps
        public int Width { get; set; } = 320;
        public int Height { get; set; } = 240;
        public int Framerate { get; set; } = 20;
        public int Powerlinefreq { get; set; } = 0; // 0=auto, 1=50Hz, 2=60Hz

        public CommandStartCamera()
        {
            CommandId = TxtInterface.CommandIdStartCameraOnline;
        }

        public override byte[] GetByteArray()
        {
            List<byte> bytes = new List<byte>(base.GetByteArray());

            bytes.AddRange(BitConverter.GetBytes(Width));
            bytes.AddRange(BitConverter.GetBytes(Height));
            bytes.AddRange(BitConverter.GetBytes(Framerate));
            bytes.AddRange(BitConverter.GetBytes(Powerlinefreq));

            return bytes.ToArray();
        }
    }
}
