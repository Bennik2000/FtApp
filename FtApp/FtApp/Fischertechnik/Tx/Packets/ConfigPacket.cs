using TXTCommunication.Fischertechnik;

namespace TXCommunication.Packets
{
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


            int packetPosition = 0;
            foreach (bool b in Motor)
            {
                PayloadBytes[packetPosition++] = b ? (byte) 1 : (byte) 0;
            }

            foreach (UniversalInputConfig universalInputConfig in UniversalInputs)
            {
                // Digital|Input Mode
                //    v       v
                //    x000 000x

                byte digitalMask = universalInputConfig.Digital ? (byte)0x80 : (byte)0x00;
                byte inputMask = (byte) universalInputConfig.Mode;

                PayloadBytes[packetPosition++] = (byte) (digitalMask | inputMask);
            }

            foreach (CounterConfig counterConfig in Counters)
            {
                PayloadBytes[packetPosition++] = counterConfig.Mode ? (byte)0x01 : (byte) 0x00;
            }

            return base.ConstructPayload();
        }
    }


    class UniversalInputConfig
    {
        public InputMode Mode { get; set; } = InputMode.ModeR;
        public bool Digital { get; set; } = true;
    }

    class CounterConfig
    {
        public bool Mode { get; set; } = true;
    }
}
