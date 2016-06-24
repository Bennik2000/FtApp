using System;
using System.Collections.Generic;
using System.Threading;
using FtApp.Fischertechnik.Txt.Events;
using TXTCommunication.Fischertechnik;

namespace FtApp.Fischertechnik.Simulation
{
    /// <summary>
    /// This class simulates an interface. You can use it to test the UI without connecting to a real interface.
    /// </summary>
    class SimulatedFtInterface : IFtInterface
    {
        public ConnectionStatus Connection { get; set; }

        public void Connect(string address)
        {
            Thread.Sleep(500);
            Connected?.Invoke(this, EventArgs.Empty);
        }

        public void Disconnect()
        {
            Thread.Sleep(100);
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        public void StartOnlineMode()
        {
            Thread.Sleep(300);
            OnlineStarted?.Invoke(this, EventArgs.Empty);

            // When the online mode "started" then we reset all input values
            List<int> inputPorts = new List<int>();
            for (int i = 0; i < GetInputCount(); i++)
            {
                inputPorts.Add(i);
            }
            InputValueChanged?.Invoke(this, new InputValueChangedEventArgs(inputPorts));
        }

        public void StopOnlineMode()
        {
            Thread.Sleep(100);
            OnlineStopped?.Invoke(this, EventArgs.Empty);
        }

        public bool CanSendCommand()
        {
            return true;
        }

        public string GetInterfaceVersionCode()
        {
            return "1.0";
        }

        public string GetInterfaceName()
        {
            return "Simulated Interface";
        }

        public int GetInputCount()
        {
            return 8;
        }

        public int GetOutputCount()
        {
            return 8;
        }

        public int GetMotorCount()
        {
            return 4;
        }

        public int GetMaxOutputValue()
        {
            return 512;
        }

        public ControllerType GetControllerType()
        {
            return ControllerType.Simulate;
        }

        public string RequestControllerName(string address)
        {
            return "Simulated Interface";
        }

        public bool IsValidInterface(string address)
        {
            return true;
        }

        public int GetMotorIndex(int outputIndex)
        {
            return (int)Math.Ceiling((double)outputIndex / 2);
        }

        public void SetOutputValue(int outputIndex, int value, int extension = 0)
        {
        }

        public void SetMotorValue(int motorIndex, int value, MotorDirection direction, int extension = 0)
        {
        }

        public void ConfigureOutputMode(int outputIndex, bool isMotor, int extension = 0)
        {
        }

        public int GetInputValue(int index, int extension = 0)
        {
            return 1;
        }

        public void ConfigureInputMode(int inputIndex, InputMode inputMode, bool digital, int extension = 0)
        {
        }

        public void Dispose()
        {
        }

        public event InputValueChangedEventHandler InputValueChanged;
        public event ConnectedEventHandler Connected;
        public event ConnectionLostEventHandler ConnectionLost;
        public event DisconnectedEventHandler Disconnected;
        public event OnlineStartedEventHandler OnlineStarted;
        public event OnlineStoppedEventHandler OnlineStopped;
    }
}
