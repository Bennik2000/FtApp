using Android.Content;
using TXTCommunication.Fischertechnik;

namespace FtApp.Droid.Activities.ControllInterface
{
    public interface IFtInterfaceFragment
    {
        void SetFtInterface(IFtInterface ftInterface);
        string GetTitle(Context context);
    }
}