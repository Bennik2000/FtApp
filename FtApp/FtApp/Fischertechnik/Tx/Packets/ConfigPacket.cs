using TXTCommunication.Fischertechnik;

namespace TXCommunication.Packets
{
    /// <summary>
    /// This packet updates the configuration of the outputs, inputs and counters
    /// </summary>
    class ConfigPacket : Packet
    {
        public bool[] Motor { get; private set; }
        public UniversalInputConfig[] UniversalInputs { get; private set; }
        public CounterConfig[] Counters { get; private set; }

        public ConfigPacket()
        {
            Motor = new bool[TxInterface.MotorOutputs];
            UniversalInputs = new UniversalInputConfig[TxInterface.UniversalInputs];
            Counters = new CounterConfig[TxInterface.Counters];
            
            CommandCode = 0x05;

            for (int i = 0; i < Motor.Length; i++)
            {
                Motor[i] = false;
            }
            for (int i = 0; i < UniversalInputs.Length; i++)
            {
                UniversalInputs[i] = new UniversalInputConfig();
            }
            for (int i = 0; i < Counters.Length; i++)
            {
                Counters[i] = new CounterConfig();
            }
        }

        protected override byte[] ConstructPayload()
        {
            PayloadBytes = new byte[48];


            // the first 4 bytes configure the output ports. Either motor or single output.
            int packetPosition = 0;
            foreach (bool b in Motor)
            {
                PayloadBytes[packetPosition++] = b ? (byte) 1 : (byte) 0;
            }

            // The next 16 bytes configure the input ports. Every input port uses 2 bytes for configuration. 
            // The first is 0x8 when in digital mode and 0x0 when in analog mode.
            foreach (UniversalInputConfig universalInputConfig in UniversalInputs)
            {
                // Digital|Input Mode
                //    v       v
                //    x000 000x

                byte digitalMask = universalInputConfig.Digital ? (byte)0x80 : (byte)0x00;
                byte inputMask = (byte) universalInputConfig.Mode;

                PayloadBytes[packetPosition++] = (byte) (digitalMask | inputMask);
            }

            // The last 4 bytes configure the counter ports
            foreach (CounterConfig counterConfig in Counters)
            {
                PayloadBytes[packetPosition++] = counterConfig.Mode ? (byte)0x01 : (byte) 0x00;
            }

            return base.ConstructPayload();
        }
    }

    /// <summary>
    /// Holds the input configuration of 1 universal input port.
    /// </summary>
    class UniversalInputConfig
    {
        public InputMode Mode { get; set; } = InputMode.ModeR;
        public bool Digital { get; set; } = true;
    }

    /// <summary>
    /// Holds the configuration of one ounter port
    /// </summary>
    class CounterConfig
    {
        public bool Mode { get; set; } = true;
    }
}
