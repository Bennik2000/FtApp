using Android.Content;

namespace FtApp.Droid.Activities.ControlInterface
{
    // This interface provides a method to get the title of a fragment
    public interface ITitledFragment
    {
        string GetTitle(Context context);
    }
}