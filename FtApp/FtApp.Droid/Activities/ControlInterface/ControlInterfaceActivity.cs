using Android.App;
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
using Java.Lang;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TXCommunication;
using TXTCommunication.Fischertechnik;
using TXTCommunication.Fischertechnik.Txt;
using TXTCommunication.Utils;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using Exception = System.Exception;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace FtApp.Droid.Activities.ControlInterface
{
    /// <summary>
    /// This activity controls the interface. It hosts all the needed fragments
    /// </summary>
    [Activity(Label = "Ft App", Icon = "@drawable/icon", Theme = "@style/FtApp.Base"/*, ScreenOrientation = ScreenOrientation.Portrait*/)]
    public class ControlInterfaceActivity : AppCompatActivity
    {
        private const string JoystickVisibleDataId = "JoystickVisible";
        private const string ActiveTabDataId = "ActiveTab";

        public const string AddressExtraDataId = "Address";
        public const string ControllerNameExtraDataId = "ControllerName";
        public const string ControllerTypeExtraDataId = "ControllerType";

        private readonly TaskQueue _connectionTaskQueue;

        private ProgressDialog _connectingDialog;
        private AlertDialog _notAvailableDialog;

        private TabLayout _tabLayout;
        private ViewPager _viewPager;


        private IList<Fragment> _fragments;
        private JoystickFragment _joystickFragment;

        private bool _joystickVisibile;
        private bool _isActivityInFocus;
        private bool _eventsHooked;

        public ControlInterfaceActivity()
        {
            _connectionTaskQueue = new TaskQueue("Connection handler");

            FtInterfaceInstanceProvider.InstanceChanged += FtInterfaceInstanceProviderOnInstanceChanged;

            // We have to use the FtInterfaceCameraProxy one time to call the static constructor
            bool b = FtInterfaceCameraProxy.FirstFrame;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.ActivityControllInterfaceLayout);

            _joystickFragment = FragmentManager.FindFragmentById<JoystickFragment>(Resource.Id.fragmentJoystick);
            _tabLayout = FindViewById<TabLayout>(Resource.Id.controlTxtTabLayout);
            _viewPager = FindViewById<ViewPager>(Resource.Id.controlTxtViewPager);


            ExtractExtraData();
            InitializeDialogs();


            SetupToolbar();
            SetupTabs();

            if (savedInstanceState == null)
            {
                // When this is the initial startup where we hide the joystick fragment
                HideJoystick();
            }
            else
            {
                RetreiveSavedState(savedInstanceState);
            }
        }

        protected override void OnStart()
        {
            base.OnStart();

            // Hook the events
            HookEvents();

            if (FtInterfaceInstanceProvider.Instance == null)
            {
                // Setup the interface if we do not have an instance already
                SetupFtInterface();
            }

            // On start we conenct to the interface
            ConnectToFtInterface();
        }

        protected override void OnPause()
        {
            // Dismiss the dialogs that we do not get a window leaked exception
            try
            {
                _connectingDialog.Dismiss();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception) { }

            try
            {
                _notAvailableDialog.Dismiss();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception) { }


            base.OnPause();
        }

        protected override void OnStop()
        {
            base.OnStop();

            if (!_isActivityInFocus)
            {
                // When this activity is not in focus we disconnect. It is not in focus when it is closed
                DisconnectFromInterface();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // Cleanup the task queue
            _connectionTaskQueue?.Dispose();

            // Unhook the events
            UnhookEvents();
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);

            // We save the state of the joystick visibility and the current aactive tab
            outState.PutBoolean(JoystickVisibleDataId, _joystickVisibile);
            outState.PutInt(ActiveTabDataId, _tabLayout.SelectedTabPosition);
        }


        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            _isActivityInFocus = hasFocus;
        }


        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            if (menu.Size() == 0)
            {
                // If we did not already inflate the menu we have to inflate it now
                MenuInflater.Inflate(Resource.Menu.ControlInterfaceOptionsMenu, menu);
            }
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.optionsMenuItemJoystick:
                    // Toggle the joystick visibility whe the joystick button has been selected
                    ToggleJoystickVisibility();
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        public override void OnBackPressed()
        {
            if (_joystickVisibile)
            {
                // When the joystick is visible we hide it
                HideJoystick();
            }
            else
            {
                // when the joystick is not visible we close the activity
                Finish();
            }
        }

        private void RetreiveSavedState(Bundle savedInstanceState)
        {
            // Load the saved state. We load the vsibility of the joystick and the current active tab
            if (savedInstanceState.GetBoolean(JoystickVisibleDataId))
            {
                ShowJoystick();
            }
            else
            {
                HideJoystick();
            }

            _viewPager.SetCurrentItem(savedInstanceState.GetInt(ActiveTabDataId, 0), false);
        }


        private void ExtractExtraData()
        {
            // We extract the parameters. The parameters are the controller type, the controller name and the address.
            Bundle extras = Intent.Extras;

            FtInterfaceInstanceProvider.ControllerType = extras != null ? (ControllerType)extras.GetInt(ControllerTypeExtraDataId) : ControllerType.Unknown;
            FtInterfaceInstanceProvider.ControllerName = extras != null ? extras.GetString(ControllerNameExtraDataId) : string.Empty;
            FtInterfaceInstanceProvider.Ip = extras != null ? extras.GetString(AddressExtraDataId) : string.Empty;
        }

        private void InitializeDialogs()
        {
            _connectingDialog = new ProgressDialog(this);

            _connectingDialog.SetTitle(GetString(Resource.String.ControlInterfaceActivity_interfaceConnectingTitle));
            _connectingDialog.SetMessage(GetString(Resource.String.ControlInterfaceActivity_interfaceConnecting));
            _connectingDialog.SetCancelable(false);
            _connectingDialog.Indeterminate = true;

            var notAvailableBuilder = new AlertDialog.Builder(this, Resource.Style.AlertDialogStyle);

            notAvailableBuilder.SetTitle(GetString(Resource.String.ControlInterfaceActivity_interfaceNotAvaliableTitle));
            notAvailableBuilder.SetMessage(GetString(Resource.String.ControlInterfaceActivity_interfaceNotAvaliable));
            notAvailableBuilder.SetCancelable(false);
            notAvailableBuilder.SetPositiveButton(Resource.String.ControlInterfaceActivity_interfaceNotAvaliablePositive,
                delegate { Finish(); });

            _notAvailableDialog = notAvailableBuilder.Create();
        }

        private void SetupToolbar()
        {
            var toolbar = FindViewById<Toolbar>(Resource.Id.controlTxtToolbar);

            SetSupportActionBar(toolbar);

            SupportActionBar.Title = FtInterfaceInstanceProvider.ControllerName;
        }

        private void SetupTabs()
        {
            _fragments = new List<Fragment>
            {
                new InputFragment(),
                new OutputFragment()
            };

            // Depending on the interface type we add the camera fragment or not
            switch (FtInterfaceInstanceProvider.ControllerType)
            {
                case ControllerType.Tx:
                    break;
                case ControllerType.Txt:
                    _fragments.Add(new CameraFragment());
                    break;
                case ControllerType.Unknown:
                    break;
            }


            // For every fragment we add it to the tab layout
            foreach (Fragment fragment in _fragments)
            {
                string title = string.Empty;

                if (fragment is ITitledFragment)
                {
                    title = ((ITitledFragment)fragment).GetTitle(this);
                }

                var tab = _tabLayout.NewTab();

                tab.SetText(title);

                _tabLayout.AddTab(tab);
            }

            PagerAdapter adapter = new TabPagerAdapter(SupportFragmentManager, _fragments.ToArray());
            _viewPager.Adapter = adapter;
            _viewPager.OffscreenPageLimit = _fragments.Count; // We have to set the OffscreenPageLimit to the tab count. Otherwise the fragments would be restored
            _viewPager.AddOnPageChangeListener(new TabLayout.TabLayoutOnPageChangeListener(_tabLayout));


            _tabLayout.SetOnTabSelectedListener(new TabLayout.ViewPagerOnTabSelectedListener(_viewPager));
        }


        private void SetupFtInterface()
        {
            // Depending on the controller type we intantiate the instance
            switch (FtInterfaceInstanceProvider.ControllerType)
            {
                case ControllerType.Tx:
                    FtInterfaceInstanceProvider.Instance = new TxInterface(new BluetoothAdapter(this));
                    break;
                case ControllerType.Txt:
                    FtInterfaceInstanceProvider.Instance = new TxtInterface();
                    break;
                case ControllerType.Simulate:
                    FtInterfaceInstanceProvider.Instance = new SimulatedFtInterface();
                    break;
                case ControllerType.Unknown:
                    Finish();
                    return;
            }
        }

        private void ShowJoystick()
        {
            // We show the joystick with an animation
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
            // We hide the joystick with an animation
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
            // Depending on the current visibility we hide or show the joystick
            if (_joystickVisibile)
            {
                HideJoystick();
            }
            else
            {
                ShowJoystick();
            }
        }


        private void FtInterfaceInstanceProviderOnInstanceChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            _eventsHooked = false;

            // When the instance has changed we hook the events
            HookEvents();
        }

        private void HookEvents()
        {
            // Hook the needed events
            if (FtInterfaceInstanceProvider.Instance != null && !_eventsHooked)
            {
                FtInterfaceInstanceProvider.Instance.ConnectionLost += FtInterfaceOnConnectionLost;
                FtInterfaceInstanceProvider.Instance.OnlineStarted += FtInterfaceOnOnlineStarted;
                FtInterfaceInstanceProvider.Instance.OnlineStopped += InstanceOnOnlineStopped;
                FtInterfaceInstanceProvider.Instance.Connected += FtInterfaceOnConnected;

                _eventsHooked = true;
            }
        }


        private void UnhookEvents()
        {
            // Unhook the hooked events
            if (FtInterfaceInstanceProvider.Instance != null)
            {
                FtInterfaceInstanceProvider.Instance.ConnectionLost -= FtInterfaceOnConnectionLost;
                FtInterfaceInstanceProvider.Instance.OnlineStarted -= FtInterfaceOnOnlineStarted;
                FtInterfaceInstanceProvider.Instance.Connected -= FtInterfaceOnConnected;

                _eventsHooked = false;
            }
        }


        private void FtInterfaceOnConnected(object sender, EventArgs eventArgs)
        {
            // When the interface is conencted we start the camera stream
            FtInterfaceCameraProxy.StartCameraStream();
        }

        private void FtInterfaceOnOnlineStarted(object sender, EventArgs eventArgs)
        {
            if (_connectingDialog.IsShowing)
            {
                try
                {
                    // We hide the connection dialog when the online mode has started
                    _connectingDialog.Dismiss();
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch (Exception)
                {
                }
            }
        }

        private void InstanceOnOnlineStopped(object sender, EventArgs eventArgs)
        {
            // When the online mode has stopped we also stop the camera stream
            FtInterfaceCameraProxy.StopCameraStream();
        }

        private void ConnectToFtInterface()
        {
            if (FtInterfaceInstanceProvider.Instance.Connection == ConnectionStatus.NotConnected)
            {
                // Show the connecting dialog
                _connectingDialog.Show();

                // The connecting process is done on a separate thread
                _connectionTaskQueue.DoWorkInQueue(() =>
                {
                    FtInterfaceInstanceProvider.Instance?.Connect(FtInterfaceInstanceProvider.Ip);
                    FtInterfaceInstanceProvider.Instance?.StartOnlineMode();
                }, false);
            }
        }

        private void DisconnectFromInterface()
        {
            // Do the disconnection on a separate thread
            _connectionTaskQueue.DoWorkInQueue(() =>
            {
                if (FtInterfaceInstanceProvider.Instance != null && FtInterfaceInstanceProvider.Instance.CanSendCommand())
                {
                    // Stop the online mode and disconnect
                    FtInterfaceInstanceProvider.Instance.StopOnlineMode();
                    FtInterfaceInstanceProvider.Instance.Disconnect();

                    // Cleanup the instance
                    FtInterfaceInstanceProvider.Instance.Dispose();
                    FtInterfaceInstanceProvider.Instance = null;
                }
            }, true);
        }


        private void FtInterfaceOnConnectionLost(object sender, EventArgs eventArgs)
        {
            // When the connectin has been lost we close the activity and display a toast
            Finish();
            RunOnUiThread(() => 
            {
                Toast.MakeText(this,
                    GetString(Resource.String.ControlInterfaceActivity_interfaceConnectionLostMessage),
                    ToastLength.Short).Show();
            });
        }

        private class TabPagerAdapter : FragmentPagerAdapter
        {
            private readonly Fragment[] _fragments;

            public TabPagerAdapter(FragmentManager fragmentManager, Fragment[] fragments) : base(fragmentManager)
            {
                _fragments = fragments;
            }

            public override int Count => _fragments.Length;


            public override IParcelable SaveState()
            {
                // We do not save the state because we don't need it
                return null;
            }

            public override void RestoreState(IParcelable state, ClassLoader loader)
            {
                // We have to do nothing to restore the fragments. Otherwise the camera stream is buggy
            }

            public override Fragment GetItem(int position)
            {
                return _fragments[position];
            }
        }
    }
}