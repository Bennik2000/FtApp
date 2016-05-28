using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Android.Widget;
using FtApp.Utils;
using System;
using System.Linq;
using TXTCommunication.Fischertechnik;
using TXTCommunication.Fischertechnik.Txt;
using TXTCommunication.Utils;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace FtApp.Droid.Activities.ControlTxt
{
    [Activity(Label = "Ft App", Icon = "@drawable/icon", Theme = "@style/FtApp.Base")]
    public class ControlTxtActivity : AppCompatActivity
    {
        public const string IpAdressExtraDataId = "IpAdress";

        private readonly TaskQueue _connectionTaskQueue;

        private ProgressDialog _connectingDialog;
        private AlertDialog _notAvailableDialog;

        private string _ip;

        private IFtInterface _ftInterface;
        
        private Fragment[] _fragments;
        
        public ControlTxtActivity()
        {
            _connectionTaskQueue = new TaskQueue("Connection handler");
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.ControlTxtLayout);

            
            Bundle extras = Intent.Extras;
            _ip = extras != null ? extras.GetString(IpAdressExtraDataId) : string.Empty;

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

        private void SetupToolbar()
        {
            var toolbar = FindViewById<Toolbar>(Resource.Id.controlTxtToolbar);
            
            SetSupportActionBar(toolbar);
            
            SupportActionBar.Title = Resources.GetString(Resource.String.ControlTxtActivity_toolbarTitle);
        }

        private void SetupTabs()
        {
            var controlTxtTabLayout = FindViewById<TabLayout>(Resource.Id.controlTxtTabLayout);

            _fragments = new Fragment[]
            {
                new InputFragment(),
                new OutputFragment(),
                new CameraFragment()
            };


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
            PagerAdapter adapter = new TabPagerAdapter(SupportFragmentManager, _fragments);
            viewPager.OffscreenPageLimit = _fragments.Length; // We have to set the OffscreenPageLimit because otherwise the fragments would be restored
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
            _ftInterface = new TxtInterface();
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
                catch (Exception) { }
            }
        }


        private void ConnectToFtInterface()
        {
            _connectingDialog.Show();

            _connectionTaskQueue.DoWorkInQueue(() =>
            {
                if (NetworkUtils.PingIp(_ip))
                {
                    _ftInterface.Connect(_ip);
                    _ftInterface.StartOnlineMode();
                }
                else
                {
                    RunOnUiThread(() =>
                    {
                        _notAvailableDialog.Show();
                    });
                }
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
            RunOnUiThread(() =>
            {
                Toast.MakeText(this, GetString(Resource.String.ControlTxtActivity_interfaceConnectionLostMessage), ToastLength.Short).Show();
            });
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