using System;
using System.IO.Ports;
using System.Threading;
using TXCommunication;

namespace InterfaceTest
{
    class SerialSocketAdapter : IRfcommAdapter
    {
        private SerialPort _serialPort;
        public bool Opened { get; private set; }
        public bool DumpToConsole { get; set; }

        public void OpenConnection(string address)
        {
            if (!Opened)
            {
                _serialPort = new SerialPort(address);
                _serialPort.Open();

                Opened = true;
            }
        }

        public void CloseConnection()
        {
            if (Opened)
            {
                _serialPort?.Close();
            }
        }

        public void Write(byte[] bytes)
        {
            if (DumpToConsole)
            {
                Console.WriteLine("Write:");
                for (int i = 0; i < bytes.Length; i++)
                {
                    Console.Write(BitConverter.ToString(new[] { bytes[i] }) + " ");

                    if ((i + 1) % 16 == 0)
                    {
                        Console.WriteLine();
                    }
                }
                Console.WriteLine();
            }

            _serialPort.Write(bytes, 0, bytes.Length);
        }

        public void GetAvailableDevices(SerialSearchStarted started, SerialSearchFound found, SerialSearchFinished finished)
        {
            started();

            foreach (string portName in SerialPort.GetPortNames())
            {
                found(portName);
            }

            finished();
        }

        public bool IsAvaliable(string address)
        {
            return true;
        }

        public byte[] Read(int count)
        {
            // We have to wait until all bytes are received. I do not know why but we have to :)
            // (Normally the SerialPort.Read() method blocks until all bytes arrived)
            while (_serialPort.BytesToRead < count)
            {
                Thread.Sleep(10);
            }

            byte[] bytes = new byte[count];

            _serialPort.Read(bytes, 0, count);


            if (DumpToConsole)
            {
                Console.WriteLine("Read:");
                for (int i = 0; i < bytes.Length; i++)
                {
                    Console.Write(BitConverter.ToString(new[] { bytes[i] }) + " ");

                    if ((i + 1) % 16 == 0)
                    {
                        Console.WriteLine();
                    }
                }
                Console.WriteLine();
            }

            return bytes;
        }

        public void Dispose()
        {
            _serialPort.Dispose();
        }
    }
}
