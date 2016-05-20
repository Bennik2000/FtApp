using FtApp.Fischertechnik.Txt.Events;
using System;
using System.Collections.Generic;
using System.Timers;
using TXTCommunication.Fischertechnik.Txt.Camera;
using TXTCommunication.Fischertechnik.Txt.Command;
using TXTCommunication.Fischertechnik.Txt.Response;
using Timer = System.Timers.Timer;

namespace TXTCommunication.Fischertechnik.Txt
{
    class TxtInterface : FtInterface
    {
        #region Constants

        public const int DefaultUpdateInterval = 50;

        public const string ControllerUsbIp = "192.168.7.2";
        public const string ControllerBluetoothIp = "192.168.9.2";
        public const string ControllerWifiIp = "192.168.8.2";
        public const int ControllerIpPort = 65000;
        public const int ControllerCameraIpPort = 65001;

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
        
        private readonly TxtExtension _masterInterface;

        private TxtCommunication TxtCommunication { get; set; }
        public TxtCameraCommunication TxtCamera { get; private set; }

        public int UpdateInterval { get; set; } = DefaultUpdateInterval;

        private bool _configurationChanged;
        private bool _soundChanged;
        private bool _soundPlaying;

        private int _soundPlayIndex;
        private int _configurationIndex;

        private Timer _updateValuesTimer;

        public TxtInterface()
        {
            Connection = ConnectionStatus.NotConnected;
            _masterInterface = new TxtExtension(0);
        }
        
        public override void Connect(string ip)
        {
            if (Connection == ConnectionStatus.Connected || Connection == ConnectionStatus.Online)
            {
                throw new InvalidOperationException("Already connected to an interface");
            }
            
            TxtCommunication = new TxtCommunication(ip);
            TxtCamera = new TxtCameraCommunication(TxtCommunication);
            
            TxtCommunication.OpenConnection();

            Connection = TxtCommunication.Connected ? ConnectionStatus.Connected : ConnectionStatus.NotConnected;

            _masterInterface.ResetValues();

            Connected?.Invoke(this, new EventArgs());
        }

        public override void Disconnect()
        {
            ThrowWhenNotConnected();

            TxtCommunication.CloseConnection();
            Connection = ConnectionStatus.NotConnected;

            Disconnected?.Invoke(this, new EventArgs());

            TxtCommunication.Dispose();
            TxtCommunication = null;

            TxtCamera.Dispose();
            TxtCamera = null;

            _masterInterface.ResetValues();
        }

        public override void StartOnlineMode()
        {
            if (Connection == ConnectionStatus.Online)
            {
                throw new InvalidOperationException("Already connected to an interface");
            }
            ThrowWhenNotConnected();

            _updateValuesTimer = new Timer(UpdateInterval);
            _updateValuesTimer.Elapsed += UpdateValuesTimerTick;
            _updateValuesTimer.AutoReset = true;

            _soundPlayIndex = 0;
            _configurationIndex = 0;

            var responseStartOnline = new ResponseStartOnline();
            TxtCommunication.SendCommand(new CommandStartOnline(), responseStartOnline);

            Connection = ConnectionStatus.Online;
            
            _updateValuesTimer.Start();

            OnlineStarted?.Invoke(this, new EventArgs());

            List<int> inputPorts = new List<int>();
            for (int i = 0; i < UniversalInputs; i++)
            {
                inputPorts.Add(i);
            }
            InputValueChangedEventArgs eventArgs = new InputValueChangedEventArgs(inputPorts);

            InputValueChanged?.Invoke(this, eventArgs);
        }

        public override void StopOnlineMode()
        {
            if (Connection == ConnectionStatus.Connected)
            {
                throw new InvalidOperationException("Interface is not in online mode");
            }
            ThrowWhenNotConnected();
            
            _updateValuesTimer.Stop();
            _updateValuesTimer.Close();
            _updateValuesTimer = null;

            StopAllOutputs();

            var responseStopOnline = new ResponseStopOnline();
            TxtCommunication.SendCommand(new CommandStopOnline(), responseStopOnline);
            
            OnlineStopped?.Invoke(this, new EventArgs());

            Connection = ConnectionStatus.Connected;
        }

        public override string GetInterfaceVersionCode()
        {
            ThrowWhenNotConnected();
            

            var responseQueryStatus = new ResponseQueryStatus();
            TxtCommunication.SendCommand(new CommandQueryStatus(), responseQueryStatus);

            return responseQueryStatus.GetDecoratedVersion();
        }

        public override string GetInterfaceName()
        {
            return "TXT";
        }

        public override int GetInputCount()
        {
            return UniversalInputs;
        }

        public override int GetOutputCount()
        {
            return PwmOutputs;
        }

        public override int GetMotorCount()
        {
            return MotorOutputs;
        }

        public override int GetMaxOutputValue()
        {
            return PwmMaxValue;
        }

        public override int GetMotorIndex(int outputIndex)
        {
            return (int) Math.Ceiling((double) outputIndex/2);
        }

        public override void SetOutputValue(int outputIndex, int value, int extension = 0)
        {
            if (extension > 0)
            {
                throw new NotImplementedException("More than one extension is not implemented right now");
            }
            ThrowWhenNotConnected();
            
            _masterInterface.SetOutputValue(outputIndex, value);
        }

        public override void SetMotorValue(int motorIndex, int value, MotorDirection direction, int extension = 0)
        {
            if (extension > 0)
            {
                throw new NotImplementedException("More than one extension is not implemented right now");
            }
            ThrowWhenNotConnected();

            _masterInterface.SetMotorDirection(motorIndex, direction);
            _masterInterface.SetMotorValue(motorIndex, value);
        }

        public override void ConfigureOutputMode(int outputIndex, bool isMotor, int extension = 0)
        {
            if (extension > 0)
            {
                throw new NotImplementedException("More than one extension is not implemented right now");
            }
            ThrowWhenNotConnected();

            _masterInterface.SetOutputMode(outputIndex, isMotor);
            _configurationChanged = true;
        }

        public override int GetInputValue(int index, int extension = 0)
        {
            if (extension > 0)
            {
                throw new NotImplementedException("More than one extension is not implemented right now");
            }
            ThrowWhenNotConnected();

            return _masterInterface.GetInputValue(index);
        }
        
        public override void ConfigureInputMode(int inputIndex, InputMode inputMode, bool digital, int extension = 0)
        {
            if (extension > 0)
            {
                throw new NotImplementedException("More than one extension is not implemented right now");
            }
            ThrowWhenNotConnected();

            _masterInterface.SetInputMode(inputIndex, inputMode, digital);

            _configurationChanged = true;
        }

        public override event InputValueChangedEventHandler InputValueChanged;
        
        public delegate void SoundPlaybackFinishedEventHandler(object sender, EventArgs e);
        public event SoundPlaybackFinishedEventHandler SoundPlaybackFinished;

        public override event ConnectedEventHandler Connected;
        public override event DisconnectedEventHandler Disconnected;
        public override event OnlineStartedEventHandler OnlineStarted;
        public override event OnlineStoppedEventHandler OnlineStopped;

        public override void Dispose()
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



            TxtCommunication.SendCommand(commandExchangeData, responseExchangeData);


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
                InputValueChanged?.Invoke(this, eventArgs);
            }


            if (responseExchangeData.SoundCommandId != _soundPlayIndex - 1 && _soundPlaying)
            {
                _soundPlaying = false;

                // Fire an event when the sound playback has finished
                SoundPlaybackFinished?.Invoke(this, new EventArgs());
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
                commandUpdateConfig.Config.Motor[0] = _masterInterface.OutputModes[i]
                    ? (byte)1
                    : (byte)0;
            }
            
            
            TxtCommunication.SendCommand(commandUpdateConfig, responseUpdateConfig);

            _configurationChanged = false;
        }

        private void ThrowWhenNotConnected()
        {
            if (Connection == ConnectionStatus.NotConnected || TxtCommunication == null || _masterInterface == null)
            {
                throw new InvalidOperationException("Not connected to an interface");
            }
        }

        private void UpdateValuesTimerTick(object sender, ElapsedEventArgs eventArgs)
        {
            _updateValuesTimer.Stop();

            UpdateValues();

            _updateValuesTimer.Start();
        }

        public static string IsControllerAvailable(string ip)
        {
            string versionCode = null;
            TxtInterface txt = new TxtInterface();
            try
            {
                txt.Connect(ip);

                versionCode = txt.GetInterfaceVersionCode();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                if (txt.Connection != ConnectionStatus.NotConnected)
                {
                    txt.Disconnect();
                }
                txt.Dispose();
                
            }

            return versionCode;
        }
    }
}
