using FtApp.Fischertechnik;

namespace FtApp.Droid.Activities.SelectDevice
{
    public class InterfaceViewModel
    {
        public string Adress { get; set; }
        public string Name { get; set; }
        public bool ControllerNameLaoding { get; set; }
        public ControllerType ControllerType { get; set; }
    }
}