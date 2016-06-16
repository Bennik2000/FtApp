using System;
using System.Threading;
using TXTCommunication.Fischertechnik;

namespace FtApp.Fischertechnik.Simulation
{
    class SimulatedFtInterface : IFtInterface
    {
        public ConnectionStatus Connection { get; set; }

        public void Connect(string adress)
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

        public string RequestControllerName(string adress)
        {
            return "Simulated Interface";
        }

        public bool IsValidInterface(string adress)
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
