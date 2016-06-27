using Android.Content;
using FtApp.Fischertechnik;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using TXTCommunication.Fischertechnik;
using TXTCommunication.Fischertechnik.Txt;
using BluetoothAdapter = Android.Bluetooth.BluetoothAdapter;

namespace FtApp.Droid.Activities.SelectDevice
{
    class InterfaceSearcher
    {
        public const string ValidateTxNameExpression = "ROBO[- ]TX[- ]{0,1}[0-9]{0,3}";

        internal delegate void InterfaceFoundEventHandler(object sender, InterfaceFoundEventArgs eventArgs);
        internal event InterfaceFoundEventHandler InterfaceFound;

        internal delegate void SearchStartedEventHandler(object sender, EventArgs eventArgs);
        internal event SearchStartedEventHandler SearchStarted;

        internal delegate void SearchFinishedEventHandler(object sender, EventArgs eventArgs);
        internal event SearchFinishedEventHandler SearchFinished;

        private readonly ManualResetEvent _waitForFinishedResetEvent;

        private bool _searching;

        private Context Context { get; set; }

        private Native.BluetoothAdapter SerialAdapter { get; set; }

        private IList<string> PossibleIpaddresses { get; set; }


        public InterfaceSearcher(Context context)
        {
            Context = context;

            SerialAdapter = new Native.BluetoothAdapter(context);

            PossibleIpaddresses = new List<string>
            {
                TxtInterface.ControllerWifiIp,
                //TxtInterface.ControllerBluetoothIp,
                //TxtInterface.ControllerUsbIp
            };

            _waitForFinishedResetEvent = new ManualResetEvent(false);
        }

        ~InterfaceSearcher()
        {
            SerialAdapter?.Dispose();
            SerialAdapter = null;
        }

        public void SearchForInterfaces()
        {
            _searching = true;
            SearchStarted?.Invoke(this, EventArgs.Empty);

            SearchTxt();
            SearchTx();
        }

        public void CancelSearchForInterfaces()
        {
            SerialAdapter.CancelSearch();

            _searching = false;
            _waitForFinishedResetEvent.Set();
        }

        public void WaitForSearchFinished()
        {
            if (_searching)
            {
                _waitForFinishedResetEvent.WaitOne();
                _waitForFinishedResetEvent.Reset();
            }
        }

        private void SearchTxt()
        {
            Thread.Sleep(100);
            IFtInterface txtInterface = new TxtInterface();

            foreach (string ipaddress in PossibleIpaddresses)
            {
                if (txtInterface.IsValidInterface(ipaddress))
                {
                    InterfaceFound?.Invoke(this, new InterfaceFoundEventArgs(ipaddress, "TXT Controller", ControllerType.Txt));
                }
            }
        }

        private void SearchTx()
        {
            if (BluetoothAdapter.DefaultAdapter != null && BluetoothAdapter.DefaultAdapter.IsEnabled)
            {
                SerialAdapter.SearchAvailableDevices(() => { }, adress =>
                {
                    string deviceName = BluetoothAdapter.DefaultAdapter.GetRemoteDevice(adress).Name;

                    if (deviceName != null)
                    {
                        // Connecting to a bluetooth device while discovery is not possible (or very slow). Therefore we only check for a match in the device name.
                        Regex regex = new Regex(ValidateTxNameExpression);
                        if (regex.IsMatch(deviceName))
                        {
                            InterfaceFound?.Invoke(this,
                                new InterfaceFoundEventArgs(adress, deviceName, ControllerType.Tx));
                        }
                    }
                }, () =>
                {
                    SearchFinished?.Invoke(this, EventArgs.Empty);

                    _searching = false;
                    _waitForFinishedResetEvent.Set();
                });
            }
            else
            {
                _searching = false;
                _waitForFinishedResetEvent.Set();
            }
        }

        
        public class InterfaceFoundEventArgs : EventArgs
        {
            public string Address { get; set; }
            public string Name { get; set; }
            public ControllerType ControllerType { get; set; }

            public InterfaceFoundEventArgs(string address, string name, ControllerType type)
            {
                Address = address;
                Name = name;
                ControllerType = type;
            }
        }
    }
}