using System;
using System.Collections.Generic;

namespace FtApp.Fischertechnik.Txt.Events
{
    public class InputValueChangedEventArgs : EventArgs
    {
        public IList<int> InputPorts { get; }

        public InputValueChangedEventArgs(IList<int> inputPorts)
        {
            InputPorts = inputPorts;
        }
    }
}
