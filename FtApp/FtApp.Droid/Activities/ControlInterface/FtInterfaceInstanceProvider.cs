using System.ComponentModel;
using FtApp.Fischertechnik;
using TXTCommunication.Fischertechnik;

namespace FtApp.Droid.Activities.ControlInterface
{
    internal static class FtInterfaceInstanceProvider
    {
        private static IFtInterface _instance;
        public static IFtInterface Instance
        {
            get { return _instance; }
            set
            {
                if (_instance != value)
                {
                    _instance = value;
                    InstanceChanged?.Invoke(null, new PropertyChangedEventArgs("Instance"));
                }
                
            }
        }

        public static string Ip { get; set; }
        public static string ControllerName { get; set; }
        public static ControllerType ControllerType { get; set; }

        public static event PropertyChangedEventHandler InstanceChanged;
    }
}