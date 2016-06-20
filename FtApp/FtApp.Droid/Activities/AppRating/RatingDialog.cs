using Android.App;
using Android.Content;
using Java.Lang;
using Uri = Android.Net.Uri;
using AlertDialog = Android.Support.V7.App.AlertDialog;

namespace FtApp.Droid.Activities.AppRating
{
    // Adapted from: http://stackoverflow.com/a/14514701/4563449
    class RatingDialog
    {
        public static int LaunchesUntilPrompt = 5;
        public static int AskIntervall = 5;

        private static Dialog _rateDialog;


        public static void RequestRatingReminder(Context context)
        {
            ISharedPreferences sharedPreferences = context.GetSharedPreferences(typeof(RatingDialog).FullName, 0);

            string preferenceKeyDontShowAgain = context.GetString(Resource.String.RateReminderDialog_DoNotShowAgainKey);


            if (sharedPreferences.GetBoolean(preferenceKeyDontShowAgain, false))
            {
                return;
            }

            ISharedPreferencesEditor editor = sharedPreferences.Edit();

            if (editor != null)
            {
                string preferenceKeyLaunchCount = context.GetString(Resource.String.RateReminderDialog_LaunchCountKey);

                long launchCount = sharedPreferences.GetLong(preferenceKeyLaunchCount, 0);
                editor.PutLong(preferenceKeyLaunchCount, launchCount + 1);

               
                if (launchCount >= LaunchesUntilPrompt && (launchCount + 1) % (AskIntervall)== 0)
                {
                    ShowRateDialog(context);
                }

                editor.Commit();
            }
        }

        private static void ShowRateDialog(Context context)
        {
            var rateReminderDialogBuilder = new AlertDialog.Builder(context, Resource.Style.AlertDialogStyle);

            rateReminderDialogBuilder.SetTitle(context.GetString(Resource.String.RateReminderDialog_title));
            rateReminderDialogBuilder.SetMessage(context.GetString(Resource.String.RateReminderDialog_Message));
            rateReminderDialogBuilder.SetCancelable(true);

            rateReminderDialogBuilder.SetPositiveButton(Resource.String.RateReminderDialog_Positive,
                delegate
                {
                    OpenAppStore(context);
                    SetDoNotShowAgain(context, true);
                    _rateDialog?.Dismiss();
                });

            rateReminderDialogBuilder.SetNeutralButton(Resource.String.RateReminderDialog_Neutral,
                delegate
                {
                    _rateDialog?.Dismiss();
                });

            rateReminderDialogBuilder.SetNegativeButton(Resource.String.RateReminderDialog_Negative,
                delegate
                {
                    SetDoNotShowAgain(context, true);
                    _rateDialog?.Dismiss();
                });


            _rateDialog = rateReminderDialogBuilder.Create();
            _rateDialog.Show();
        }

        private static void OpenAppStore(Context context)
        {
            context.StartActivity(new Intent(Intent.ActionView, Uri.Parse("market://details?id=" + context.ApplicationContext.PackageName)));
        }

        private static void SetDoNotShowAgain(Context context, bool showAgain)
        {
            ISharedPreferences sharedPreferences = context.GetSharedPreferences(typeof(RatingDialog).FullName, 0);

            string preferenceKey = context.GetString(Resource.String.RateReminderDialog_DoNotShowAgainKey);
            

            ISharedPreferencesEditor editor = sharedPreferences.Edit();

            if (editor != null)
            {
                editor.PutBoolean(preferenceKey, showAgain);
                editor.Commit();
            }
        }
    }
}