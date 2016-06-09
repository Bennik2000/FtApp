using FtApp.Fischertechnik.Txt.Events;
using System;
using TXCommunication;
using TXTCommunication.Fischertechnik;
using TXTCommunication.Fischertechnik.Txt;

namespace InterfaceTest
{
    static class Program
    {
        private static IFtInterface _controller;

        static void Main()
        {
            PrintAvailableComPorts();

            _controller = new TxInterface(new SerialSocketAdapter());
            //_controller = new TxtInterface();

            // Hook events
            _controller.Connected += (sender, eventArgs) => Console.WriteLine("Connected");
            _controller.Disconnected += (sender, eventArgs) => Console.WriteLine("Disconnected");
            _controller.OnlineStarted += (sender, eventArgs) => Console.WriteLine("Online mode started");
            _controller.OnlineStopped += (sender, eventArgs) => Console.WriteLine("Online mode stopped");
            _controller.InputValueChanged += ControllerOnInputValueChanged;


            // Connect to the controller
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (_controller is TxtInterface)
            {
                _controller.Connect(TxtInterface.ControllerWifiIp);
            }
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            else if (_controller is TxInterface)
            {
                _controller.Connect("COM10");
            }

            if (_controller.Connection != ConnectionStatus.Connected)
            {
                return;
            }


            // Start the online mode
            _controller.StartOnlineMode();


            // Configure the input ports
            int inputPort = 0;
            _controller.ConfigureInputMode(inputPort++, InputMode.ModeR, true);
            _controller.ConfigureInputMode(inputPort++, InputMode.ModeR, true);
            _controller.ConfigureInputMode(inputPort++, InputMode.ModeUltrasonic, true);
            _controller.ConfigureInputMode(inputPort++, InputMode.ModeR, true);
            _controller.ConfigureInputMode(inputPort++, InputMode.ModeR, true);
            _controller.ConfigureInputMode(inputPort++, InputMode.ModeR, true);
            _controller.ConfigureInputMode(inputPort++, InputMode.ModeR, true);
            _controller.ConfigureInputMode(inputPort, InputMode.ModeUltrasonic, true);

            Console.ReadLine();
            Console.WriteLine("Disconnecting...");

            // Stop the inline mode
            _controller.StopOnlineMode();

            // Disconnect from the interface
            _controller.Disconnect();

            // Don't forget to dispose
            _controller.Dispose();

        }

        private static void PrintAvailableComPorts()
        {
            SerialSocketAdapter adapter = new SerialSocketAdapter();

            adapter.GetAvailableDevices(() => { }, Console.WriteLine, () => { });
        }

        private static void ControllerOnInputValueChanged(object sender, InputValueChangedEventArgs inputValueChangedEventArgs)
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);

            for (int i = 0; i < _controller.GetInputCount(); i++)
            {
                Console.Write("I{0,1} {1, 5}  |", i + 1, _controller.GetInputValue(i));
            }
            Console.WriteLine();
        }
    }
}
