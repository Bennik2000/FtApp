using Android.App;
using Android.Content;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using Uri = Android.Net.Uri;

namespace FtApp.Droid.Activities.AppRating
{
    // Adapted from: http://stackoverflow.com/a/14514701/4563449
    /// <summary>
    /// Displays a rating reminder dialog
    /// </summary>
    class RatingDialog
    {
        public static int LaunchesUntilPrompt = 5;
        public static int AskIntervall = 5;

        private static Dialog _rateDialog;


        /// <summary>
        /// Displays a dialog every 5th call
        /// </summary>
        /// <param name="context"></param>
        public static void RequestRatingReminder(Context context)
        {
            ISharedPreferences sharedPreferences = context.GetSharedPreferences(typeof(RatingDialog).FullName, 0);

            string preferenceKeyDontShowAgain = context.GetString(Resource.String.RateReminderDialog_doNotShowAgainKey);

            // Check if we want to show it
            if (sharedPreferences.GetBoolean(preferenceKeyDontShowAgain, false))
            {
                return;
            }

            ISharedPreferencesEditor editor = sharedPreferences.Edit();

            if (editor != null)
            {
                string preferenceKeyLaunchCount = context.GetString(Resource.String.RateReminderDialog_launchCountKey);

                long launchCount = sharedPreferences.GetLong(preferenceKeyLaunchCount, 0);
                editor.PutLong(preferenceKeyLaunchCount, launchCount + 1);

                // Displays the rating reminder dialog when we have enough launch counts
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
            rateReminderDialogBuilder.SetMessage(context.GetString(Resource.String.RateReminderDialog_message));
            rateReminderDialogBuilder.SetCancelable(true);

            rateReminderDialogBuilder.SetPositiveButton(Resource.String.RateReminderDialog_positive,
                delegate
                {
                    // When the positive button has been pressed: open the app store
                    OpenAppStore(context);

                    // Also do not open the dialog again
                    SetDoNotShowAgain(context, true);
                    
                    _rateDialog?.Dismiss();
                });

            rateReminderDialogBuilder.SetNeutralButton(Resource.String.RateReminderDialog_neutral,
                delegate
                {
                    _rateDialog?.Dismiss();
                });

            rateReminderDialogBuilder.SetNegativeButton(Resource.String.RateReminderDialog_negative,
                delegate
                {
                    // Do not open the dialog again
                    SetDoNotShowAgain(context, true);
                    _rateDialog?.Dismiss();
                });

            rateReminderDialogBuilder.SetCancelable(false);

            // Create and show the dialog
            _rateDialog = rateReminderDialogBuilder.Create();
            _rateDialog.Show();
        }

        private static void OpenAppStore(Context context)
        {
            // Start a new activity with the appstore uri
            context.StartActivity(new Intent(Intent.ActionView, Uri.Parse("market://details?id=" + context.ApplicationContext.PackageName)));
        }

        private static void SetDoNotShowAgain(Context context, bool showAgain)
        {
            ISharedPreferences sharedPreferences = context.GetSharedPreferences(typeof(RatingDialog).FullName, 0);

            string preferenceKey = context.GetString(Resource.String.RateReminderDialog_doNotShowAgainKey);
            

            ISharedPreferencesEditor editor = sharedPreferences.Edit();

            if (editor != null)
            {
                editor.PutBoolean(preferenceKey, showAgain);
                editor.Commit();
            }
        }
    }
}