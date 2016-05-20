
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Support.V7.App;
using TXTCommunication.Fischertechnik;
using TXTCommunication.Fischertechnik.Txt;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace FtApp.Droid.Activities.ControlTxt
{
    [Activity(Label = "Ft App", Icon = "@drawable/icon", Theme = "@style/FtApp.Base")]
    public class ControlTxtActivity : AppCompatActivity
    {
        public const string IpAdressExtraDataId = "IpAdress";

        private string _ip;
        private FtInterface _ftInterface;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.ControlTxtLayout);

            _ip = GetIpAdressFromIntent();

            SetupFtInterface();
            SetupToolbar();
            SetupTabs();
        }

        protected override void OnStart()
        {
            base.OnStart();
            ConnectToFtInterface();
        }
        
        protected override void OnPause()
        {
            base.OnPause();
            DisconnectFromInterface();
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

            Fragment[] fragments =
            {
                new InputFragment(_ftInterface),
                new OutputFragment(_ftInterface)
            };


            foreach (Fragment fragment in fragments)
            {
                string title = string.Empty;
                
                if (fragment is InputFragment)
                {
                    title = GetText(Resource.String.ControlTxtActivity_tabInputTitle);
                }
                else if (fragment is OutputFragment)
                {
                    title = GetText(Resource.String.ControlTxtActivity_tabOutputTitle);
                }

                controlTxtTabLayout.AddTab(controlTxtTabLayout.NewTab().SetText(title));
            }

            
            ViewPager viewPager = FindViewById<ViewPager>(Resource.Id.controlTxtViewPager);
            PagerAdapter adapter = new TabPagerAdapter(SupportFragmentManager, fragments);
            viewPager.Adapter = adapter;

            viewPager.AddOnPageChangeListener(new TabLayout.TabLayoutOnPageChangeListener(controlTxtTabLayout));

            controlTxtTabLayout.SetOnTabSelectedListener(new TabLayout.ViewPagerOnTabSelectedListener(viewPager));
        }

        private void SetupFtInterface()
        {
            _ftInterface = new TxtInterface();
        }

        private void ConnectToFtInterface()
        {
            _ftInterface.Connect(_ip);

            _ftInterface.StartOnlineMode();
        }

        private void DisconnectFromInterface()
        {
            _ftInterface.StopOnlineMode();
            _ftInterface.Disconnect();
        }


        private string GetIpAdressFromIntent()
        {
            Bundle extras = Intent.Extras;
            if (extras != null)
            {
                return extras.GetString(IpAdressExtraDataId);
            }
            return string.Empty;
        }


        private class TabPagerAdapter : FragmentPagerAdapter
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