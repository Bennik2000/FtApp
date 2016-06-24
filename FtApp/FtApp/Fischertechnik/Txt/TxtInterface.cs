using FtApp.Fischertechnik;
using FtApp.Fischertechnik.Txt.Events;
using FtApp.Utils;
using System;
using System.Collections.Generic;
using System.Timers;
using System.Linq;
using TXTCommunication.Fischertechnik.Txt.Camera;
using TXTCommunication.Fischertechnik.Txt.Command;
using TXTCommunication.Fischertechnik.Txt.Response;
using Timer = System.Timers.Timer;
// ReSharper disable InconsistentNaming

namespace TXTCommunication.Fischertechnik.Txt
{
    class TxtInterface : IFtInterface
    {
#if DEBUG
        public bool IsDebugEnabled { get; set; } = true;
#else
        public bool IsDebugEnabled { get; set; } = false;
#endif

        #region Constants

        public const int DefaultUpdateInterval = 50;

        public const string ControllerUsbIp = "192.168.7.2";
        public const string ControllerBluetoothIp = "192.168.9.2";
        public const string ControllerWifiIp = "192.168.8.2";
        public const int ControllerIpPort = 65000;
        public const int ControllerCameraIpPort = 65001;
        public const int TcpTimeout = 2000;

        public const int PwmMaxValue = 512;

        public const int PwmOutputs = 8;
        public const int MotorOutputs = 4;
        public const int Counters = 4;
        public const int UniversalInputs = 8;
        public const int IrChannels = 4;


#region Command Ids
        public const uint CommandIdQueryStatus = 0xDC21219A;
        public const uint CommandIdStartOnline = 0x163FF61D;
        public const uint CommandIdUpdateConfig = 0x060EF27E;
        public const uint CommandIdExchangeData = 0xCC3597BA;
        public const uint CommandIdExchangeDataCmpr = 0xFBC56F98;
        public const uint CommandIdStopOnline = 0x9BE5082C;
        public const uint CommandIdStartCameraOnline = 0x882A40A6;
        public const uint CommandIdStopCameraOnline = 0x17C31F2F;

        public const uint AcknowledgeIdCameraOnlineFrame = 0xADA09FBA;
#endregion
        
#region Response Ids
        public const uint ResponseIdQueryStatus = 0xBAC9723E;
        public const uint ResponseIdStartOnline = 0xCA689F75;
        public const uint ResponseIdUpdateConfig = 0x9689A68C;
        public const uint ResponseIdExchangeData = 0x4EEFAC41;
        public const uint ResponseIdExchangeDataCmpr = 0x6F3B54E6;
        public const uint ResponseIdStopOnline = 0xFBF600D2;
        public const uint ResponseIdStartCameraOnline = 0xCF41B24E;
        public const uint ResponseIdStopCameraOnline = 0x4B3C1EB6;

        public const uint DataIdCameraOnlineFrame = 0xBDC2D7A1;
#endregion
#endregion
        
        private readonly FtExtension _masterInterface;

        private TxtCommunication TxtCommunication { get; set; }
        public TxtCameraCommunication TxtCamera { get; private set; }
        
        private Timer _updateValuesTimer;

        public int UpdateInterval { get; set; } = DefaultUpdateInterval;

        public string Ip { get; private set; }

        private bool _configurationChanged;
        private bool _soundChanged;
        private bool _soundPlaying;

        private int _soundPlayIndex;
        private int _configurationIndex;
        
        public TxtInterface()
        {
            Connection = ConnectionStatus.NotConnected;
            _masterInterface = new FtExtension(0);
        }

        public ConnectionStatus Connection { get; set; }

        public void Connect(string ip)
        {
            LogMessage($"Connecting to {ip}");
            Ip = ip;
            if (Connection == ConnectionStatus.Connected || Connection == ConnectionStatus.Online)
            {
                throw new InvalidOperationException("Already connected to an interface");
            }
            
            TxtCommunication?.Dispose();

            TxtCommunication = new TxtCommunication(this);
            TxtCamera = new TxtCameraCommunication(TxtCommunication);

            try
            {
                TxtCommunication.OpenConnection();
                Connection = TxtCommunication.Connected ? ConnectionStatus.Connected : ConnectionStatus.NotConnected;
            }
            catch (Exception e)
            {
                LogMessage($"Exception while connecting: {e.Message}");
                Connection = ConnectionStatus.Invalid;
                HandleException(e);
            }


            _masterInterface.ResetValues();

            LogMessage("Connected");
            _connected?.Invoke(this, new EventArgs());
        }

        public void Disconnect()
        {
            LogMessage("Disconnecting");
            ThrowWhenNotConnected();

            try
            {
                TxtCommunication.CloseConnection();
                Connection = ConnectionStatus.NotConnected;
            }
            catch (Exception e)
            {
                LogMessage($"Exception while disconnecting: {e.Message}");
                Connection = ConnectionStatus.Invalid;
                HandleException(e);
            }

            _disconnected?.Invoke(this, new EventArgs());

            TxtCommunication.Dispose();
            TxtCommunication = null;

            TxtCamera.Dispose();
            TxtCamera = null;

            LogMessage("Disconnected");
            _masterInterface.ResetValues();
        }

        public void StartOnlineMode()
        {
            LogMessage("Starting online mode");
            if (Connection == ConnectionStatus.Online)
            {
                throw new InvalidOperationException("Already in online mode");
            }
            ThrowWhenNotConnected();

            _updateValuesTimer = new Timer(UpdateInterval);
            _updateValuesTimer.Elapsed += UpdateValuesTimerTick;
            _updateValuesTimer.AutoReset = true;

            _soundPlayIndex = 0;
            _configurationIndex = 0;


            var responseStartOnline = new ResponseStartOnline();

            try
            {
                TxtCommunication.SendCommand(new CommandStartOnline(), responseStartOnline);
                Connection = ConnectionStatus.Online;

                _updateValuesTimer.Start();

                _onlineStarted?.Invoke(this, new EventArgs());

                List<int> inputPorts = new List<int>();
                for (int i = 0; i < UniversalInputs; i++)
                {
                    inputPorts.Add(i);
                }
                InputValueChangedEventArgs eventArgs = new InputValueChangedEventArgs(inputPorts);

                _inputValueChanged?.Invoke(this, eventArgs);
            }
            catch (Exception e)
            {
                LogMessage($"Exception while starting online mode: {e.Message}");
                Connection = ConnectionStatus.Invalid;
                HandleException(e);
            }
            LogMessage("Online mode started");
        }

        public void StopOnlineMode()
        {
            LogMessage("Stopping online mode");
            if (Connection == ConnectionStatus.Connected)
            {
                throw new InvalidOperationException("Interface is not in online mode");
            }
            ThrowWhenNotConnected();
            
            _updateValuesTimer.Stop();
            _updateValuesTimer.Close();
            _updateValuesTimer = null;

            StopAllOutputs();

            try
            {
                var responseStopOnline = new ResponseStopOnline();

                TxtCommunication.SendCommand(new CommandStopOnline(), responseStopOnline);

                _onlineStopped?.Invoke(this, new EventArgs());


                Connection = ConnectionStatus.Connected;
            }
            catch (Exception e)
            {
                LogMessage($"Exception while stopping online mode: {e.Message}");
                HandleException(e);
            }
            LogMessage("Online mode stopped");
        }

        public bool CanSendCommand()
        {
            return Connection == ConnectionStatus.Online || Connection == ConnectionStatus.Connected;
        }

        public string GetInterfaceVersionCode()
        {
            ThrowWhenNotConnected();

            try
            {
                var responseQueryStatus = new ResponseQueryStatus();
                TxtCommunication.SendCommand(new CommandQueryStatus(), responseQueryStatus);
                return responseQueryStatus.GetDecoratedVersion();
            }
            catch (Exception e)
            {
                HandleException(e);
            }
            return string.Empty;
        }

        public string GetInterfaceName()
        {
            ThrowWhenNotConnected();

            try
            {
                var responseQueryStatus = new ResponseQueryStatus();
                TxtCommunication.SendCommand(new CommandQueryStatus(), responseQueryStatus);
                return responseQueryStatus.GetControllerName();
            }
            catch (Exception e)
            {
                HandleException(e);
            }
            return string.Empty;
        }

        public bool IsInterfaceReachable(string address)
        {
            return NetworkUtils.PingIp(address);
        }

        public int GetInputCount()
        {
            return UniversalInputs;
        }

        public int GetOutputCount()
        {
            return PwmOutputs;
        }

        public int GetMotorCount()
        {
            return MotorOutputs;
        }

        public int GetMaxOutputValue()
        {
            return PwmMaxValue;
        }

        public ControllerType GetControllerType() => ControllerType.Txt;

        public string RequestControllerName(string address)
        {
            if (TxtCommunication == null)
            {
                TxtCommunication = new TxtCommunication(this);
            }

            return TxtCommunication.RequestControllerName(address);
        }

        public bool IsValidInterface(string address)
        {
            if (TxtCommunication == null)
            {
                TxtCommunication = new TxtCommunication(this);
            }

            return TxtCommunication.IsValidInterface(address);
        }

        public int GetMotorIndex(int outputIndex)
        {
            return (int) Math.Ceiling((double) outputIndex/2);
        }

        public void SetOutputValue(int outputIndex, int value, int extension = 0)
        {
            if (extension > 0)
            {
                throw new NotImplementedException("More than one extension is not implemented right now");
            }
            ThrowWhenNotConnected();
            
            _masterInterface.SetOutputValue(outputIndex, value);
        }

        public void SetMotorValue(int motorIndex, int value, MotorDirection direction, int extension = 0)
        {
            if (extension > 0)
            {
                throw new NotImplementedException("More than one extension is not implemented right now");
            }
            ThrowWhenNotConnected();

            _masterInterface.SetMotorDirection(motorIndex, direction);
            _masterInterface.SetMotorValue(motorIndex, value);
        }

        public void ConfigureOutputMode(int outputIndex, bool isMotor, int extension = 0)
        {
            if (extension > 0)
            {
                throw new NotImplementedException("More than one extension is not implemented right now");
            }
            ThrowWhenNotConnected();

            _masterInterface.SetOutputMode(outputIndex, isMotor);
            _configurationChanged = true;
        }

        public int GetInputValue(int index, int extension = 0)
        {
            if (extension > 0)
            {
                throw new NotImplementedException("More than one extension is not implemented right now");
            }
            ThrowWhenNotConnected();

            return _masterInterface.GetInputValue(index);
        }
        
        public void ConfigureInputMode(int inputIndex, InputMode inputMode, bool digital, int extension = 0)
        {
            if (extension > 0)
            {
                throw new NotImplementedException("More than one extension is not implemented right now");
            }
            ThrowWhenNotConnected();

            _masterInterface.SetInputMode(inputIndex, inputMode, digital);

            _configurationChanged = true;
        }
        
        
        public delegate void SoundPlaybackFinishedEventHandler(object sender, EventArgs e);

        // ReSharper disable once UnusedParameter.Global
        public void PlaySound(ushort soundIndex, ushort repeatCount, int extension = 0)
        {
            if (extension > 0)
            {
                throw new NotImplementedException("More than one extension is not implemented right now");
            }
            ThrowWhenNotConnected();

            _masterInterface.SoundIndex = soundIndex;
            _masterInterface.SountRepeatCount = repeatCount;

            _soundChanged = true;
        }

        public void UpdateValues()
        {
            ThrowWhenNotConnected();

            if (_configurationChanged)
            {
                UpdateConfiguration();
                _configurationChanged = false;
            }


            var commandExchangeData = new CommandExchangeData();
            var responseExchangeData = new ResponseExchangeData();


            for (int i = 0; i < _masterInterface.OutputValues.Length; i++)
            {
                commandExchangeData.PwmOutputValues[i] = (short)_masterInterface.OutputValues[i];
            }

            
            commandExchangeData.SoundCommandId = (ushort)_soundPlayIndex;
            

            if (_soundChanged)
            {
                commandExchangeData.SoundCommandId = (ushort)++_soundPlayIndex;
                commandExchangeData.SoundIndex = _masterInterface.SoundIndex;
                commandExchangeData.SoundRepeat = _masterInterface.SountRepeatCount;
                _soundChanged = false;
                _soundPlaying = true;
            }


            try
            {
                TxtCommunication.SendCommand(commandExchangeData, responseExchangeData);
            }
            catch (Exception e)
            {
                HandleException(e);
                return;
            }


            IList<int> valueChanged = new List<int>();

            for (int i = 0; i < responseExchangeData.UniversalInputs.Length; i++)
            {
                var newInputValue = responseExchangeData.UniversalInputs[i];

                if (_masterInterface.GetInputValue(i) != newInputValue)
                {
                    _masterInterface.SetInputValue(i, newInputValue);
                    
                    valueChanged.Add(i);
                }
            }

            if (valueChanged.Count > 0)
            {
                // Fire an event when an input value has changed
                InputValueChangedEventArgs eventArgs = new InputValueChangedEventArgs(valueChanged);
                _inputValueChanged?.Invoke(this, eventArgs);
            }


            if (responseExchangeData.SoundCommandId != _soundPlayIndex - 1 && _soundPlaying)
            {
                _soundPlaying = false;

                // Fire an event when the sound playback has finished
                _soundPlaybackFinished?.Invoke(this, new EventArgs());
            }
        }

        private void StopAllOutputs()
        {
            for (int i = 0; i < PwmOutputs; i++)
            {
                if (!_masterInterface.OutputModes[i / 2])
                {
                    _masterInterface.SetOutputValue(i, 0);
                }
            }
            for (int i = 0; i < MotorOutputs; i++)
            {
                if (_masterInterface.OutputModes[i/2])
                {
                    _masterInterface.SetMotorValue(i, 0);
                }
            }

            UpdateValues();
        }

        private void UpdateConfiguration()
        {
            ThrowWhenNotConnected();


            // ReSharper disable once UseObjectOrCollectionInitializer
            var commandUpdateConfig = new CommandUpdateConfig();
            var responseUpdateConfig = new ResponseUpdateConfig();

            // Increase the configuration id to notify that the configuration has changed
            commandUpdateConfig.ConfigId = (short)++_configurationIndex;


            for (int i = 0; i < _masterInterface.InputModes.Length; i++)
            {
                commandUpdateConfig.Config.UniversalInputs[i].Mode = (byte)_masterInterface.InputModes[i];
            }
            for (int i = 0; i < _masterInterface.InputIsDigital.Length; i++)
            {
                commandUpdateConfig.Config.UniversalInputs[i].Digital = _masterInterface.InputIsDigital[i]
                    ? (byte) 1
                    : (byte) 0;
            }

            for (int i = 0; i < _masterInterface.OutputModes.Length; i++)
            {
                commandUpdateConfig.Config.Motor[i] = _masterInterface.OutputModes[i]
                    ? (byte)1
                    : (byte)0;
            }

            try
            {
                TxtCommunication.SendCommand(commandUpdateConfig, responseUpdateConfig);
            }
            catch (Exception e)
            {
                HandleException(e);
            }

            _configurationChanged = false;
        }

        private void ThrowWhenNotConnected()
        {
            if (Connection == ConnectionStatus.NotConnected || TxtCommunication == null || _masterInterface == null)
            {
                throw new InvalidOperationException("Not connected to an interface");
            }
            if (Connection == ConnectionStatus.Invalid)
            {
                throw new InvalidOperationException("The connection is invalid");
            }
        }

        private void HandleException(Exception exception)
        {
            Connection = ConnectionStatus.Invalid;
            
            _connectionLost?.Invoke(this, new EventArgs());
        }

        internal void LogMessage(string message)
        {
            if (IsDebugEnabled)
            {
                Console.WriteLine(message);
            }
        }

        private void UpdateValuesTimerTick(object sender, ElapsedEventArgs eventArgs)
        {
            _updateValuesTimer.Stop();

            UpdateValues();

            _updateValuesTimer.Start();
        }



        private event SoundPlaybackFinishedEventHandler _soundPlaybackFinished;
        public event SoundPlaybackFinishedEventHandler SoundPlaybackFinished
        {
            add
            {
                if (_soundPlaybackFinished == null || !_soundPlaybackFinished.GetInvocationList().Contains(value))
                {
                    _soundPlaybackFinished += value;
                }
            }
            remove
            {
                _soundPlaybackFinished -= value;
            }
        }

        private event InputValueChangedEventHandler _inputValueChanged;
        public event InputValueChangedEventHandler InputValueChanged
        {
            add
            {
                if (_inputValueChanged == null || !_inputValueChanged.GetInvocationList().Contains(value))
                {
                    _inputValueChanged += value;
                }
            }
            remove
            {
                _inputValueChanged -= value;
            }
        }

        private event ConnectedEventHandler _connected;
        public event ConnectedEventHandler Connected
        {
            add
            {
                if (_connected == null || !_connected.GetInvocationList().Contains(value))
                {
                    _connected += value;
                }
            }
            remove
            {
                _connected -= value;
            }
        }

        private event ConnectionLostEventHandler _connectionLost;
        public event ConnectionLostEventHandler ConnectionLost
        {
            add
            {
                if (_connectionLost == null || !_connectionLost.GetInvocationList().Contains(value))
                {
                    _connectionLost += value;
                }
            }
            remove
            {
                _connectionLost -= value;
            }
        }

        private event DisconnectedEventHandler _disconnected;
        public event DisconnectedEventHandler Disconnected
        {
            add
            {
                if (_disconnected == null || !_disconnected.GetInvocationList().Contains(value))
                {
                    _disconnected += value;
                }
            }
            remove
            {
                _disconnected -= value;
            }
        }

        private event OnlineStartedEventHandler _onlineStarted;
        public event OnlineStartedEventHandler OnlineStarted
        {
            add
            {
                if (_onlineStarted == null || !_onlineStarted.GetInvocationList().Contains(value))
                {
                    _onlineStarted += value;
                }
            }
            remove
            {
                _onlineStarted -= value;
            }
        }

        private event OnlineStoppedEventHandler _onlineStopped;
        public event OnlineStoppedEventHandler OnlineStopped
        {
            add
            {
                if (_onlineStopped == null || !_onlineStopped.GetInvocationList().Contains(value))
                {
                    _onlineStopped += value;
                }
            }
            remove
            {
                _onlineStopped -= value;
            }
        }


        public void Dispose()
        {
            Connection = ConnectionStatus.NotConnected;

            if (TxtCommunication != null)
            {
                TxtCommunication.Dispose();
                TxtCommunication = null;
            }

            if (TxtCamera != null)
            {
                TxtCamera.Dispose();
                TxtCamera = null;
            }

            _masterInterface.ResetValues();
        }

    }
}
