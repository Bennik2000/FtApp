using System;
using System.Collections.Generic;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace TXTCommunication.Fischertechnik.Txt.Command
{
    class CommandUpdateConfig : CommandBase
    {
        // config id from FTX1_CONFIG
        public short ConfigId { get; set; }
        public short ExtensionId { get; set; }
        public FtX1Config Config { get; set; }

        public CommandUpdateConfig()
        {
            CommandId = TxtInterface.CommandIdUpdateConfig;
            Config = new FtX1Config();
        }

        public override byte[] GetByteArray()
        {
            List<byte> bytes = new List<byte>(base.GetByteArray());

            bytes.AddRange(BitConverter.GetBytes(ConfigId));
            bytes.AddRange(BitConverter.GetBytes(ExtensionId));

            bytes.AddRange(Config.GetByteArray());

            return bytes.ToArray();
        }
    }

    class FtX1Config
    {
        private byte[] Dummy { get; } = new byte[4]; // Dummy bytes because of the TX protocol

        // 0=single output O1/O2, 1=motor output M1
        public byte[] Motor { get; } = new byte[TxtInterface.MotorOutputs];

        // Universal input mode, see enum InputMode
        public UniversalInputConfig[] UniversalInputs { get; } = new UniversalInputConfig[TxtInterface.UniversalInputs];

        // 0=normal, 1=inverted (not really used)
        public CounterConfig[] Counters { get; }= new CounterConfig[TxtInterface.Counters];

        // additional motor configuration data (currently not used)
        public short[] MotorConfig { get; }= new short[TxtInterface.MotorOutputs * 4];


        public FtX1Config()
        {
            for (int i = 0; i < UniversalInputs.Length; i++)
            {
                UniversalInputs[i] = new UniversalInputConfig();
            }

            for (int i = 0; i < Counters.Length; i++)
            {
                Counters[i] = new CounterConfig();
            }
        }

        public byte[] GetByteArray()
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            List<byte> bytes = new List<byte>();
            
            bytes.AddRange(Dummy);
            bytes.AddRange(Motor);

            foreach (UniversalInputConfig inputConfig in UniversalInputs)
            {
                bytes.AddRange(inputConfig.GetByteArray());
            }

            foreach (CounterConfig counterConfig in Counters)
            {
                bytes.AddRange(counterConfig.GetByteArray());
            }

            foreach (short s in MotorConfig)
            {
                bytes.AddRange(BitConverter.GetBytes(s));
            }

            return bytes.ToArray();
        }
    }

    class UniversalInputConfig
    {
        public byte Mode { get; set; }
        public byte Digital { get; set; }

        private byte[] Dummy { get; } = new byte[2];

        public byte[] GetByteArray()
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            List<byte> bytes = new List<byte>();

            bytes.Add(Mode);
            bytes.Add(Digital);
            bytes.AddRange(Dummy);

            return bytes.ToArray();
        }   
    }

    class CounterConfig
    {
        public byte Mode { get; set; } // 1=normal, 0=inverted;
        private byte[] Dummy { get; } = new byte[3];

        public byte[] GetByteArray()
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            List<byte> bytes = new List<byte>();

            bytes.Add(Mode);
            bytes.AddRange(Dummy);

            return bytes.ToArray();
        }
    }
}
