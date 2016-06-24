using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Text.Method;
using Android.Widget;

namespace FtApp.Droid.Activities.About
{
    /// <summary>
    /// This activity displays the about screen.
    /// </summary>
    [Activity(Label = "AboutActivity", Theme = "@style/FtApp.Base", ScreenOrientation = ScreenOrientation.Portrait)]
    public class AboutActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.ActivityAboutLayout);

            // Configure the TextView that links are clickable
            TextView t2 = FindViewById<TextView>(Resource.Id.textViewMoreInformation);
            t2.MovementMethod = LinkMovementMethod.Instance;
        }

        public override void Finish()
        {
            base.Finish();

            // Override the animation to crossfade
            OverridePendingTransition(Android.Resource.Animation.FadeIn, Android.Resource.Animation.FadeOut);
        }
    }
}