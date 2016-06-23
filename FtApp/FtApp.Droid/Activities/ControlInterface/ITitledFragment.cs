using Android.Content;

namespace FtApp.Droid.Activities.ControlInterface
{
    /// <summary>
    /// This interface provides a method to get the title of a fragment
    /// </summary>
    public interface ITitledFragment
    {
        string GetTitle(Context context);
    }
}