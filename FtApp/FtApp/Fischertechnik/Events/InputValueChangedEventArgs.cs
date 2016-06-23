using System;
using System.Collections.Generic;

namespace FtApp.Fischertechnik.Txt.Events
{
    public class InputValueChangedEventArgs : EventArgs
    {
        /// <summary>
        /// This list holds the indexes of all changed input ports
        /// </summary>
        public IList<int> InputPorts { get; }

        public InputValueChangedEventArgs(IList<int> inputPorts)
        {
            InputPorts = inputPorts;
        }
    }
}
