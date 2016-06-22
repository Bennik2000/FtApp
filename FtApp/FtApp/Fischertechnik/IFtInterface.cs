using FtApp.Fischertechnik;
using FtApp.Fischertechnik.Txt.Events;
using System;

namespace TXTCommunication.Fischertechnik
{
    public interface IFtInterface : IDisposable
    {
        ConnectionStatus Connection { get; set; }

        /// <summary>
        /// This function tries to connect to an interface with the given address.
        /// </summary>
        /// <param name="address">For the TXT Controller it is an ip address and for the TX Controller it is the mac address</param>
        void Connect(string address);

        /// <summary>
        /// This function closes the connection
        /// </summary>
        void Disconnect();

        /// <summary>
        /// This function starts the online mode. Then the inputs can be read and the outputs can be set
        /// </summary>
        void StartOnlineMode();

        /// <summary>
        /// This functions stops the online mode. Then I/O commands should not be sent anymore
        /// </summary>
        void StopOnlineMode();

        /// <summary>
        /// Checks if commands (like set an output value) can be sent
        /// </summary>
        bool CanSendCommand();

        /// <summary>
        /// Reads the version code of the interface firmware
        /// </summary>
        /// <returns>The firmware version</returns>
        string GetInterfaceVersionCode();

        /// <summary>
        /// Reads the name of the interface
        /// </summary>
        /// <returns>The interface name</returns>
        string GetInterfaceName();
        

        /// <summary>
        /// Returns the numberof universal inputs
        /// </summary>
        /// <returns>The number of universal inputs</returns>
        int GetInputCount();

        /// <summary>
        /// Returns the number of output ports. Note: The output ports can either be a single output or two ports together as one motor output
        /// </summary>
        /// <returns>The number of output ports</returns>
        int GetOutputCount();

        /// <summary>
        /// Returns the number of motor ports. Note: The motor ports can either be a single output or two ports together as one motor output
        /// </summary>
        /// <returns>The number of motor ports</returns>
        int GetMotorCount();

        /// <summary>
        /// Returns the maximal output value. The TX Controller and TXT Controller support from 0 to 512
        /// </summary>
        /// <returns>The maximal output value</returns>
        int GetMaxOutputValue();

        /// <summary>
        /// Returns the type of the controller protocol implementation
        /// </summary>
        /// <returns>the type of the interface</returns>
        ControllerType GetControllerType();

        /// <summary>
        /// Reads the controller name without the need of connecting and disconnecting (This is done internally)
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        string RequestControllerName(string address);

        /// <summary>
        /// Checks if the interface at the given address is valid
        /// </summary>
        /// <param name="address">The address to ckeck</param>
        /// <returns>true, when the interface is valid otherwise false</returns>
        bool IsValidInterface(string address);

        /// <summary>
        /// Returns the index of the motor port. Output 5 => M3; Output 2 => M1
        /// </summary>
        /// <param name="outputIndex">The index of the output port</param>
        /// <returns>The index of the motor port</returns>
        int GetMotorIndex(int outputIndex);

        /// <summary>
        /// This function sets a output port to the given value. The value is updated when UpdateValues() is called.
        /// </summary>
        /// <param name="outputIndex">The index of the port.</param>
        /// <param name="value">The value to set between 0 and 512</param>
        /// <param name="extension">The extension where the value should be set</param>
        /// ReSharper disable once UnusedParameter.Global
        void SetOutputValue(int outputIndex, int value, int extension = 0);

        /// <summary>
        /// This function sets a motor to the given speed and direction. The value is updated when UpdateValues() is called.
        /// </summary>
        /// <param name="motorIndex">The index of the motor.</param>
        /// <param name="value">The value to set between 0 and 512</param>
        /// <param name="direction">The direction</param>
        /// <param name="extension">The extension where the value should be set</param>
        /// ReSharper disable once UnusedParameter.Global
        void SetMotorValue(int motorIndex, int value, MotorDirection direction, int extension = 0);

        /// <summary>
        /// Configures a given output port. If <paramref name="isMotor"/> is true, the output ports are set to motor ports
        /// </summary>
        /// <param name="outputIndex">The index of the output port</param>
        /// <param name="isMotor">Specifies whether the output port is a motor</param>
        /// <param name="extension">The extension where the value should be set</param>
        /// ReSharper disable once UnusedParameter.Global
        void ConfigureOutputMode(int outputIndex, bool isMotor, int extension = 0);

        /// <summary>
        /// Returns the value of an input port.
        /// </summary>
        /// <param name="index">The index of the input port</param>
        /// <param name="extension">The extension where the value should be read</param>
        /// ReSharper disable once UnusedParameter.Global
        int GetInputValue(int index, int extension = 0);

        /// <summary>
        /// Configures the input mode of a given port.
        /// </summary>
        /// <param name="inputIndex">The index of the input port</param>
        /// <param name="inputMode">The input mode of the port</param>
        /// <param name="digital">Specifies whether the port is digital or analog</param>
        /// <param name="extension">The extension where the value should be set</param>
        /// ReSharper disable once UnusedParameter.Global
        void ConfigureInputMode(int inputIndex, InputMode inputMode, bool digital, int extension = 0);

        /// <summary>
        /// This event is fired when the input valus have changed
        /// </summary>
        event InputValueChangedEventHandler InputValueChanged;

        /// <summary>
        /// This event is fired when the interface is connected successfully
        /// </summary>
        event ConnectedEventHandler Connected;

        /// <summary>
        /// This event is fired when the connection to the interface is lost
        /// </summary>
        event ConnectionLostEventHandler ConnectionLost;

        /// <summary>
        /// This event is fired when the interface is disconnected
        /// </summary>
        event DisconnectedEventHandler Disconnected;

        /// <summary>
        /// This event is fired when the online mode started
        /// </summary>
        event OnlineStartedEventHandler OnlineStarted;

        /// <summary>
        /// This event is fired when the online mode stopped
        /// </summary>
        event OnlineStoppedEventHandler OnlineStopped;
    }


    public enum MotorDirection
    {
        Left,
        Right
    }

    /// <summary>
    /// This enumeration contains the connection states
    /// </summary>
    public enum ConnectionStatus
    {
        /// <summary>
        /// Connected means that the connection is established but not in online mode
        /// </summary>
        Connected,

        /// <summary>
        /// The online mode is enabled
        /// </summary>
        Online,

        /// <summary>
        /// No interface is connected
        /// </summary>
        NotConnected,

        /// <summary>
        /// The connection is invalid most likely because of an timeout
        /// </summary>
        Invalid
    }

    /// <summary>
    /// These are the input modes which the TX Controller and the TXT Controller use
    /// </summary>
    public enum InputMode
    {
        /// <summary>
        /// ModeU measures the voltage. Often used for the trail sensor (digital) and color sensor (analog)
        /// </summary>
        ModeU = 0,

        /// <summary>
        /// ModeR measures the resistance. This mode is used for simple digital switches or an analog NTC
        /// </summary>
        ModeR,
        ModeR2,

        /// <summary>
        /// ModeUltrasonic is a special mode. It is used for an ultrasonic sensor
        /// </summary>
        ModeUltrasonic,
        ModeInvalid
    }

    public delegate void InputValueChangedEventHandler(object sender, InputValueChangedEventArgs e);
    public delegate void ConnectedEventHandler(object sender, EventArgs e);
    public delegate void ConnectionLostEventHandler(object sender, EventArgs e);
    public delegate void DisconnectedEventHandler(object sender, EventArgs e);
    public delegate void OnlineStartedEventHandler(object sender, EventArgs e);
    public delegate void OnlineStoppedEventHandler(object sender, EventArgs e);
}
