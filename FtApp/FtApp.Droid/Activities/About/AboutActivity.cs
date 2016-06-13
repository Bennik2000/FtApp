using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Text.Method;
using Android.Widget;

namespace FtApp.Droid.Activities.About
{
    [Activity(Label = "AboutActivity", Theme = "@style/FtApp.Base", ScreenOrientation = ScreenOrientation.Portrait)]
    public class AboutActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.ActivityAboutLayout);

            TextView t2 = FindViewById<TextView>(Resource.Id.textViewMoreInformation);
            t2.MovementMethod = LinkMovementMethod.Instance;
        }

        public override void Finish()
        {
            base.Finish();

            OverridePendingTransition(Android.Resource.Animation.FadeIn, Android.Resource.Animation.FadeOut);
        }
    }
}