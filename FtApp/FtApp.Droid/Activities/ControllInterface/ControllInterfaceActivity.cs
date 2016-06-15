using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using FtApp.Droid.Activities.SelectDevice;
using FtApp.Droid.Native;
using FtApp.Fischertechnik;
using FtApp.Fischertechnik.Simulation;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private TabLayout _tabLayout;
        private ViewPager _viewPager;

        private string _ip;
        private string _controllerName;
        private ControllerType _controllerType;

        private IFtInterface _ftInterface;

        
        private IList<Fragment> _fragments;
        private JoystickFragment _joystickFragment;

        private bool _joystickVisibile;
        
        
        public ControllInterfaceActivity()
        {
            _connectionTaskQueue = new TaskQueue("Connection handler");
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.ActivityControllInterfaceLayout);

            _joystickFragment = FragmentManager.FindFragmentById<JoystickFragment>(Resource.Id.fragmentJoystick);
            _tabLayout = FindViewById<TabLayout>(Resource.Id.controlTxtTabLayout);
            _viewPager = FindViewById<ViewPager>(Resource.Id.controlTxtViewPager);
            

            FragmentManager.BeginTransaction()
                .Hide(_joystickFragment)
                .Commit();


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

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            if (menu.Size() == 0)
            {
                MenuInflater.Inflate(Resource.Menu.ControlInterfaceOptionsMenu, menu);
            }
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.optionsMenuItemJoystick:
                    ToggleJoystickVisibility();
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        public override void OnBackPressed()
        {
            if (_joystickVisibile)
            {
                HideJoystick();
            }
            else
            {
                Finish();
            }
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
            var controlInterfaceTabLayout = FindViewById<TabLayout>(Resource.Id.controlTxtTabLayout);

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
                    title = ((IFtInterfaceFragment)fragment).GetTitle(this);
                }
                
                var tab = controlInterfaceTabLayout.NewTab();

                tab.SetText(title);
                
                controlInterfaceTabLayout.AddTab(tab);
            }

            PagerAdapter adapter = new TabPagerAdapter(SupportFragmentManager, _fragments.ToArray());
            var viewPager = FindViewById<ViewPager>(Resource.Id.controlTxtViewPager);
            viewPager.OffscreenPageLimit = _fragments.Count; // We have to set the OffscreenPageLimit to the tab count. Otherwise the fragments would be restored
            viewPager.Adapter = adapter;

            viewPager.AddOnPageChangeListener(new TabLayout.TabLayoutOnPageChangeListener(controlInterfaceTabLayout));
            viewPager.PageSelected += ViewPagerOnPageSelected;


            controlInterfaceTabLayout.SetOnTabSelectedListener(new TabLayout.ViewPagerOnTabSelectedListener(viewPager));
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
                case ControllerType.Simulate:
                    _ftInterface = new SimulatedFtInterface();
                    break;
                case ControllerType.Unknown:
                    Finish();
                    return;
            }

            _ftInterface.ConnectionLost += FtInterfaceOnConnectionLost;
            _ftInterface.OnlineStarted += FtInterfaceOnOnlineStarted;
            _ftInterface.Connected += FtInterfaceOnConnected;

            foreach (Fragment fragment in _fragments)
            {
                IFtInterfaceFragment interfaceFragment = fragment as IFtInterfaceFragment;
                interfaceFragment?.SetFtInterface(_ftInterface);
            }
            _joystickFragment?.SetFtInterface(_ftInterface);
        }

        private void ShowJoystick()
        {
            _joystickFragment.Activate();

            _joystickVisibile = true;
            _tabLayout.Animate().Alpha(0).SetDuration(200).Start();
            _viewPager.Animate().Alpha(0).SetDuration(200).Start();

            FragmentManager.BeginTransaction()
                .SetCustomAnimations(Resource.Animation.FadeIn, Resource.Animation.FadeOut)
                .Show(_joystickFragment)
                .Commit();
        }

        private void HideJoystick()
        {
            _joystickVisibile = false;
            _tabLayout.Visibility = ViewStates.Visible;
            _tabLayout.Animate().Alpha(1).SetDuration(200).SetListener(new HideOnFinishedAnimationListener(_tabLayout)).Start();

            _viewPager.Visibility = ViewStates.Visible;
            _viewPager.Animate().Alpha(1).SetDuration(200).SetListener(new HideOnFinishedAnimationListener(_viewPager)).Start();

            FragmentManager.BeginTransaction()
                .SetCustomAnimations(Resource.Animation.FadeIn, Resource.Animation.FadeOut)
                .Hide(_joystickFragment)
                .Commit();
        }

        private void ToggleJoystickVisibility()
        {
            if (_joystickVisibile)
            {
                HideJoystick();
            }
            else
            {
                ShowJoystick();
            }
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


        private void FtInterfaceOnConnected(object sender, EventArgs eventArgs)
        {
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