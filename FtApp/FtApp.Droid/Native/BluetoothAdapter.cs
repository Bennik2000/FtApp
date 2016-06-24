using Android.Bluetooth;
using Android.Content;
using Java.Util;
using System;
using TXCommunication;

namespace FtApp.Droid.Native
{
    /// <summary>
    /// This class implements the IRfcommAdapter interface. It is used to have a serial connection over bluetooth
    /// </summary>
    class BluetoothAdapter : IRfcommAdapter
    {
        private Context Context { get; }

        private static readonly UUID RfCommUuid = UUID.FromString("00001101-0000-1000-8000-00805F9B34FB");
        
        private BluetoothDevice _bluetoothDevice;
        private BluetoothSocket _bluetoothSocket;

        private readonly Android.Bluetooth.BluetoothAdapter _bluetoothAdapter;
        private BroadcastReceiver _bluetoothBroadcastRecevicer;


        public BluetoothAdapter(Context context)
        {
            Context = context;

            _bluetoothAdapter = Android.Bluetooth.BluetoothAdapter.DefaultAdapter;
        }

        public void OpenConnection(string address)
        {
            if (!_bluetoothAdapter.IsEnabled)
            {
                throw new InvalidOperationException("The bluetooth adapter is not enabled");
            }
            if (!Android.Bluetooth.BluetoothAdapter.CheckBluetoothAddress(address))
            {
                throw new InvalidOperationException("The given address is not valid");
            }
            
            _bluetoothDevice = _bluetoothAdapter.GetRemoteDevice(address);
            _bluetoothSocket = _bluetoothDevice?.CreateInsecureRfcommSocketToServiceRecord(RfCommUuid);
            
            _bluetoothSocket?.Connect();
        }

        public void CloseConnection()
        {
            // Close the I/O streams
            _bluetoothSocket?.InputStream?.Close();
            _bluetoothSocket?.OutputStream?.Close();

            // Cose the socket connection
            _bluetoothSocket?.Close();
        }

        public void Write(byte[] bytes)
        {
            if (_bluetoothSocket != null && _bluetoothSocket.IsConnected)
            {
                // Write the bytes to the socket
                _bluetoothSocket.OutputStream.Write(bytes, 0, bytes.Length);
            }
        }

        public byte[] Read(int count)
        {
            if (_bluetoothSocket != null && _bluetoothSocket.IsConnected)
            {
                // Read the arrived bytes
                byte[] bytes = new byte[count];

                if (_bluetoothSocket != null && _bluetoothSocket.IsConnected)
                {
                    _bluetoothSocket.InputStream.Read(bytes, 0, bytes.Length);
                }

                return bytes;
            }

            return new byte[count];
        }

        public void CancelSearch()
        {
            // Cancel the bluetooth discovery
            if (_bluetoothAdapter != null && _bluetoothAdapter.IsDiscovering)
            {
                _bluetoothAdapter.CancelDiscovery();
            }
        }

        public bool IsAvaliable(string address)
        {
            // Checks if a bluetooth adress is valid
            return Android.Bluetooth.BluetoothAdapter.CheckBluetoothAddress(address);
        }


        public void SearchAvailableDevices(SerialSearchStarted started, SerialSearchFound found, SerialSearchFinished finished)
        {
            // Search for bluetooth devices
            _bluetoothBroadcastRecevicer = new BluetoothBroadcastReceiver(started, found, finished);

            IntentFilter deviceFoundFilter = new IntentFilter(BluetoothDevice.ActionFound);
            Context.RegisterReceiver(_bluetoothBroadcastRecevicer, deviceFoundFilter);

            IntentFilter discoveryFinishedFilter = new IntentFilter(Android.Bluetooth.BluetoothAdapter.ActionDiscoveryFinished);
            Context.RegisterReceiver(_bluetoothBroadcastRecevicer, discoveryFinishedFilter);

            IntentFilter discoveryStartedFilter = new IntentFilter(Android.Bluetooth.BluetoothAdapter.ActionDiscoveryStarted);
            Context.RegisterReceiver(_bluetoothBroadcastRecevicer, discoveryStartedFilter);

            _bluetoothAdapter.StartDiscovery();
        }
        

        public void Dispose()
        {
            if (_bluetoothSocket != null)
            {
                if (_bluetoothSocket.IsConnected)
                {
                    _bluetoothSocket.InputStream.Close();
                    _bluetoothSocket.OutputStream.Close();
                    _bluetoothSocket.Close();
                }

                _bluetoothSocket.Dispose();
            }


            _bluetoothAdapter?.CancelDiscovery();
        }



        private class BluetoothBroadcastReceiver : BroadcastReceiver
        {
            private readonly SerialSearchStarted _startedDelegate;
            private readonly SerialSearchFound _foundDelegate;
            private readonly SerialSearchFinished _finishedDelegate;


            public BluetoothBroadcastReceiver(SerialSearchStarted started, SerialSearchFound found, SerialSearchFinished finished)
            {
                _startedDelegate = started;
                _foundDelegate = found;
                _finishedDelegate = finished;
            }

            public override void OnReceive(Context context, Intent intent)
            {
                string action = intent.Action;

                switch (action)
                {
                    case BluetoothDevice.ActionFound:
                        BluetoothDevice device = ReadBluetoothDeviceFromIntent(intent);
                        _foundDelegate(device.Address);
                        break;

                    case Android.Bluetooth.BluetoothAdapter.ActionDiscoveryStarted:
                        _startedDelegate();
                        break;

                    case Android.Bluetooth.BluetoothAdapter.ActionDiscoveryFinished:
                        _finishedDelegate();
                        context.UnregisterReceiver(this);
                        break;
                }
            }

            private BluetoothDevice ReadBluetoothDeviceFromIntent(Intent intent)
            {
                return intent.GetParcelableExtra(BluetoothDevice.ExtraDevice) as BluetoothDevice;
            }
        }
    }
}