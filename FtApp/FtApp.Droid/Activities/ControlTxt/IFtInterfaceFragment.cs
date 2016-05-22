using Android.Content;
using TXTCommunication.Fischertechnik;

namespace FtApp.Droid.Activities.ControlTxt
{
    public interface IFtInterfaceFragment
    {
        void SetFtInterface(FtInterface ftInterface);
        string GetTitle(Context context);
    }
}