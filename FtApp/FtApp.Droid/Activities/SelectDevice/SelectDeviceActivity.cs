using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using FtApp.Droid.Activities.ControllInterface;
using System.Collections.Generic;
using FtApp.Droid.Activities.About;
using FtApp.Droid.Activities.AppRating;
using FtApp.Droid.Activities.Help;
using FtApp.Fischertechnik;
using BluetoothAdapter = Android.Bluetooth.BluetoothAdapter;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace FtApp.Droid.Activities.SelectDevice
{
    [Activity(Label = "Ft App", MainLauncher = true, Icon = "@drawable/icon", Theme = "@style/FtApp.Base",
        ScreenOrientation = ScreenOrientation.Portrait)]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SelectDeviceActivity : AppCompatActivity
    {
        private const int EnableBluetoothRequestId = 1;

        private ListView _listViewDevices;
        private FoundDevicesListAdapter _foundDevicesListAdapter;
        private ProgressBar _progressBarScanning;
        private LinearLayout _layoutListEmpty;

        private InterfaceSearchAsyncTask _interfaceSearchAsyncTask;

        private readonly List<InterfaceViewModel> _foundDevices;


        public SelectDeviceActivity()
        {
            _foundDevices = new List<InterfaceViewModel>();
        }


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.ActivitySelectDeviceLayout);

            _foundDevices.Clear();

            _listViewDevices = FindViewById<ListView>(Resource.Id.devicesListView);
            _progressBarScanning = FindViewById<ProgressBar>(Resource.Id.progressBarScanning);
            _layoutListEmpty = FindViewById<LinearLayout>(Resource.Id.layoutInterfaceListEmpty);

            _layoutListEmpty.Visibility = ViewStates.Gone;


            if (savedInstanceState == null)
            {
                RatingDialog.RequestRatingReminder(this);
            }

            SetupToolbar();
            SetupListView();
        }

        protected override void OnStart()
        {
            base.OnStart();
             _foundDevices.Clear();
            SearchForInterfaces();
        }

        protected override void OnPause()
        {
            base.OnPause();
            _interfaceSearchAsyncTask?.CancelSearch();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == EnableBluetoothRequestId)
            {
                if (resultCode == Result.Ok)
                {
                    SearchForInterfaces();
                }
                if (resultCode == Result.Canceled)
                {
                    Toast.MakeText(this, Resource.String.SelectDeviceActivity_bluetoothHasToBeEnabled, ToastLength.Short).Show();
                }
            }
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            if (menu.Size() == 0)
            {
                MenuInflater.Inflate(Resource.Menu.SelectDeviceOptionsMenu, menu);
            }
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.optionsMenuItemSimulate:
                    OpenControlActivity(".", GetString(Resource.String.ControlTxtActivity_SimulatedInterfaceName), ControllerType.Simulate);
                    return true;

                case Resource.Id.optionsMenuItemAbout:
                    OpenAboutActivity();
                    return true;

                case Resource.Id.optionsMenuItemHelp:
                    OpenHelpActivity();
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        private void SetupListView()
        {
            _listViewDevices.Divider = null;
            _listViewDevices.DividerHeight = 0;
            _listViewDevices.ItemClick += ListViewDevicesOnItemClick;

            _foundDevicesListAdapter = new FoundDevicesListAdapter(this, _foundDevices);
            _listViewDevices.Adapter = _foundDevicesListAdapter;
        }
        
        private void SetupToolbar()
        {
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);

            SetSupportActionBar(toolbar);

            SupportActionBar.Title = Resources.GetString(Resource.String.SelectDeviceActivity_toolbarTitle);
        }

        
        private void ListViewDevicesOnItemClick(object sender, AdapterView.ItemClickEventArgs itemClickEventArgs)
        {
            if (_foundDevices.Count > itemClickEventArgs.Id)
            {
                var clickedItem = _foundDevices[(int)itemClickEventArgs.Id];
                
                OpenControlActivity(clickedItem.Adress, clickedItem.Name, clickedItem.ControllerType);
            }
        }


        private void OpenControlActivity(string address, string name, ControllerType type)
        {
            _interfaceSearchAsyncTask?.CancelSearch();

            // Open the control activity and pass the extra data
            Intent intent = new Intent(this, typeof(ControllInterfaceActivity));

            intent.PutExtra(ControllInterfaceActivity.AdressExtraDataId, address);
            intent.PutExtra(ControllInterfaceActivity.ControllerNameExtraDataId, name);
            intent.PutExtra(ControllInterfaceActivity.ControllerTypeExtraDataId, (int)type);

            StartActivity(intent);
        }

        private void OpenAboutActivity()
        {
            _interfaceSearchAsyncTask?.CancelSearch();
            
            Intent intent = new Intent(this, typeof(AboutActivity));
            StartActivity(intent);
            OverridePendingTransition(Android.Resource.Animation.FadeIn, Android.Resource.Animation.FadeOut);
        }

        private void OpenHelpActivity()
        {
            _interfaceSearchAsyncTask?.CancelSearch();
            
            Intent intent = new Intent(this, typeof(HelpActivity));
            StartActivity(intent);
            OverridePendingTransition(Android.Resource.Animation.FadeIn, Android.Resource.Animation.FadeOut);

        }

        private void SearchForInterfaces()
        {
            if (BluetoothAdapter.DefaultAdapter.IsEnabled)
            {
                _interfaceSearchAsyncTask = new InterfaceSearchAsyncTask(this);

                _interfaceSearchAsyncTask.ProgressUpdated += InterfaceSearchAsyncTaskOnProgressUpdated;
                _interfaceSearchAsyncTask.SearchFinished += InterfaceSearchAsyncTaskOnSearchFinished;

                _interfaceSearchAsyncTask.Execute(string.Empty);


                HideEmptyStateImage();
                _progressBarScanning.Visibility = ViewStates.Visible;

                _foundDevices.Clear();

                _foundDevicesListAdapter.NotifyDataSetChanged();

            }
            else
            {
                SwitchOnBluetooth();
            }
        }


        private void InterfaceSearchAsyncTaskOnSearchFinished(object sender, InterfaceSearchAsyncTask.SearchFinishedEventArgs eventArgs)
        {
            if (_foundDevices.Count > 0)
            {
                HideEmptyStateImage();
            }
            else
            {
                ShowEmptyStateImage();
            }

            _progressBarScanning.Visibility = ViewStates.Invisible;
            _foundDevicesListAdapter.NotifyDataSetChanged();
        }

        private void InterfaceSearchAsyncTaskOnProgressUpdated(object sender, InterfaceSearchAsyncTask.ProgressUpdatedEventArgs eventArgs)
        {
            RunOnUiThread(() =>
            {
                _foundDevices.Add(eventArgs.Interface);
                _foundDevicesListAdapter.NotifyDataSetChanged();
            });
        }


        private void SwitchOnBluetooth()
        {
            Intent enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
            StartActivityForResult(enableBtIntent, EnableBluetoothRequestId);
        }

        private void ShowEmptyStateImage()
        {
            _layoutListEmpty.Alpha = 0;
            _layoutListEmpty.Visibility = ViewStates.Visible;

            _layoutListEmpty.Animate().Alpha(1).SetDuration(225).Start();
        }

        private void HideEmptyStateImage()
        {
            _layoutListEmpty.Alpha = 1;

            _layoutListEmpty.Animate().Alpha(0).SetDuration(225).SetListener(new HideOnFinishedAnimationListener(_layoutListEmpty)).Start();
        }

    }
}