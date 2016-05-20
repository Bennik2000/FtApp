using System.Collections.Generic;
using System.Linq;

namespace TXTCommunication.Fischertechnik.Txt.Command
{
    class CommandStartOnline : CommandBase
    {
        private byte[] Name { get; set; }

        public CommandStartOnline()
        {
            Name = new byte[64];
            CommandId = TxtInterface.CommandIdStartOnline;
        }

        public override byte[] GetByteArray()
        {
            IList<byte> bytes = new List<byte>(base.GetByteArray());

            foreach (byte b in Name)
            {
                bytes.Add(b);
            }

            return bytes.ToArray();
        }
    }
}
