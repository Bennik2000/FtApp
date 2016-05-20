using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;

namespace FtApp.Droid.Activities.SelectDevice
{
    [Activity(Label = "Ft App", MainLauncher = true, Icon = "@drawable/icon", Theme = "@style/FtApp.Base",
        ScreenOrientation = ScreenOrientation.Portrait)]
    public class SelectDeviceActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.SelectDeviceLayout);

            SetupToolbar();
            SetupTxtFragment();
        }

        private void SetupToolbar()
        {
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);

            SetSupportActionBar(toolbar);

            SupportActionBar.Title = Resources.GetString(Resource.String.SelectDeviceActivity_toolbarTitle);
        }

        private void SetupTxtFragment()
        {
            var txtFragment = new TxtFragment();
            var fragmentTransaction = FragmentManager.BeginTransaction();
            fragmentTransaction.Add(Resource.Id.frameLayoutTxtFragment, txtFragment);
            fragmentTransaction.Commit();
        }
    }
}