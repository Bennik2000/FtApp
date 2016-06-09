using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Android.Widget;
using FtApp.Droid.Native;
using FtApp.Fischertechnik;
using TXCommunication;
using TXTCommunication.Fischertechnik;
using TXTCommunication.Fischertechnik.Txt;
using TXTCommunication.Utils;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace FtApp.Droid.Activities.ControllInterface
{
    [Activity(Label = "Ft App", Icon = "@drawable/icon", Theme = "@style/FtApp.Base", ScreenOrientation = ScreenOrientation.Portrait)]
    public class ControllInterfaceActivity : AppCompatActivity
    {
        public const string AdressExtraDataId = "Adress";
        public const string ControllerNameExtraDataId = "ControllerName";
        public const string ControllerTypeExtraDataId = "ControllerType";

        private readonly TaskQueue _connectionTaskQueue;

        private ProgressDialog _connectingDialog;
        private AlertDialog _notAvailableDialog;

        private string _ip;
        private string _controllerName;
        private ControllerType _controllerType;

        private IFtInterface _ftInterface;

        
        private IList<Fragment> _fragments;
        
        public ControllInterfaceActivity()
        {
            _connectionTaskQueue = new TaskQueue("Connection handler");
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.ControllInterfaceLayout);

            ExtractExtraData();

            InitializeDialogs();
            SetupToolbar();
            SetupTabs();
        }

        protected override void OnStart()
        {
            base.OnStart();
            SetupFtInterface();
            ConnectToFtInterface();
        }
        
        protected override void OnPause()
        {
            DisconnectFromInterface();

            try
            {
                _connectingDialog.Dismiss();
            }
            catch (Exception) { }

            try
            {
                _notAvailableDialog.Dismiss();
            }
            catch (Exception) { }


            base.OnPause();
        }
        
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _ftInterface.Dispose();
        }

        private void ExtractExtraData()
        {
            Bundle extras = Intent.Extras;

            _controllerType = extras != null ? (ControllerType)extras.GetInt(ControllerTypeExtraDataId) : ControllerType.Unknown;
            _controllerName = extras != null ? extras.GetString(ControllerNameExtraDataId) : string.Empty;
            _ip = extras != null ? extras.GetString(AdressExtraDataId) : string.Empty;
        }

        private void InitializeDialogs()
        {
            _connectingDialog = new ProgressDialog(this);

            _connectingDialog.SetTitle(GetString(Resource.String.ControlTxtActivity_interfaceConnectingTitle));
            _connectingDialog.SetMessage(GetString(Resource.String.ControlTxtActivity_interfaceConnecting));
            _connectingDialog.SetCancelable(false);
            _connectingDialog.Indeterminate = true;

            var notAvailableBuilder = new AlertDialog.Builder(this, Resource.Style.AlertDialogStyle);

            notAvailableBuilder.SetTitle(GetString(Resource.String.ControlTxtActivity_interfaceNotAvaliableTitle));
            notAvailableBuilder.SetMessage(GetString(Resource.String.ControlTxtActivity_interfaceNotAvaliable));
            notAvailableBuilder.SetCancelable(false);
            notAvailableBuilder.SetPositiveButton(Resource.String.ControlTxtActivity_interfaceNotAvaliablePositive,
                delegate { Finish(); });

            _notAvailableDialog = notAvailableBuilder.Create();
        }

        private void SetupToolbar()
        {
            var toolbar = FindViewById<Toolbar>(Resource.Id.controlTxtToolbar);
            
            SetSupportActionBar(toolbar);
            
            //SupportActionBar.Title = Resources.GetString(Resource.String.ControlTxtActivity_toolbarTitle);
            SupportActionBar.Title = _controllerName;
        }

        private void SetupTabs()
        {
            var controlTxtTabLayout = FindViewById<TabLayout>(Resource.Id.controlTxtTabLayout);

            _fragments = new List<Fragment>
            {
                new InputFragment(),
                new OutputFragment()
            };


            switch (_controllerType)
            {
                case ControllerType.Tx:
                    break;
                case ControllerType.Txt:
                    _fragments.Add(new CameraFragment());
                    break;
                case ControllerType.Unknown:
                    break;
            }


            foreach (Fragment fragment in _fragments)
            {
                string title = string.Empty;

                if (fragment is IFtInterfaceFragment)
                {
                    title = ((IFtInterfaceFragment) fragment).GetTitle(this);
                }

                controlTxtTabLayout.AddTab(controlTxtTabLayout.NewTab().SetText(title));
            }


            ViewPager viewPager = FindViewById<ViewPager>(Resource.Id.controlTxtViewPager);
            PagerAdapter adapter = new TabPagerAdapter(SupportFragmentManager, _fragments.ToArray());
            viewPager.OffscreenPageLimit = _fragments.Count; // We have to set the OffscreenPageLimit because otherwise the fragments would be restored
            viewPager.Adapter = adapter;

            viewPager.AddOnPageChangeListener(new TabLayout.TabLayoutOnPageChangeListener(controlTxtTabLayout));
            viewPager.PageSelected += ViewPagerOnPageSelected;

            controlTxtTabLayout.SetOnTabSelectedListener(new TabLayout.ViewPagerOnTabSelectedListener(viewPager));
        }

        private void ViewPagerOnPageSelected(object sender, ViewPager.PageSelectedEventArgs pageSelectedEventArgs)
        {
            // Control the camera preview of the TXT Controller.
            // When the camera fragment is not visible we stop decoding the jpeg stream to save ressources
            var fragment = _fragments[pageSelectedEventArgs.Position];

            if (fragment is CameraFragment)
            {
                ((CameraFragment) fragment).DisplayFrames = true;
            }
            else
            {
                var cameraFragment = _fragments.FirstOrDefault(f => f is CameraFragment) as CameraFragment;

                if (cameraFragment != null)
                {
                    cameraFragment.DisplayFrames = false;
                }
            }
        }

        private void SetupFtInterface()
        {
            switch (_controllerType)
            {
                case ControllerType.Tx:
                    _ftInterface = new TxInterface(new BluetoothAdapter(this));
                    break;
                case ControllerType.Txt:
                    _ftInterface = new TxtInterface();
                    break;
                case ControllerType.Unknown:
                    Finish();
                    return;
            }

            _ftInterface.ConnectionLost += FtInterfaceOnConnectionLost;
            _ftInterface.OnlineStarted += FtInterfaceOnOnlineStarted;

            foreach (Fragment fragment in _fragments)
            {
                IFtInterfaceFragment interfaceFragment = fragment as IFtInterfaceFragment;
                interfaceFragment?.SetFtInterface(_ftInterface);
            }
        }

        private void FtInterfaceOnOnlineStarted(object sender, EventArgs eventArgs)
        {
            if (_connectingDialog.IsShowing)
            {
                try
                {
                    _connectingDialog.Dismiss();
                }
                catch (Exception)
                {
                }
            }
        }


        private void ConnectToFtInterface()
        {
            _connectingDialog.Show();

            _connectionTaskQueue.DoWorkInQueue(() =>
            {
                _ftInterface.Connect(_ip);
                _ftInterface.StartOnlineMode();
            }, false);
        }

        private void DisconnectFromInterface()
        {
            _connectionTaskQueue.DoWorkInQueue(() =>
            {
                if (_ftInterface.CanSendCommand())
                {
                    _ftInterface.StopOnlineMode();
                    _ftInterface.Disconnect();
                }
            }, true);
        }


        private void FtInterfaceOnConnectionLost(object sender, EventArgs eventArgs)
        {
            Finish();
            RunOnUiThread(() => { Toast.MakeText(this, GetString(Resource.String.ControlTxtActivity_interfaceConnectionLostMessage), ToastLength.Short).Show(); });
        }

        private class TabPagerAdapter : FragmentStatePagerAdapter
        {
            private readonly Fragment[] _fragments;

            public TabPagerAdapter(FragmentManager fragmentManager, Fragment[] fragments) : base(fragmentManager)
            {
                _fragments = fragments;
            }

            public override int Count => _fragments.Length;

            public override Fragment GetItem(int position)
            {
                return _fragments[position];
            }
        }
    }
}