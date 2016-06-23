using FtApp.Fischertechnik;

namespace FtApp.Droid.Activities.SelectDevice
{
    public class InterfaceViewModel
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public bool ControllerNameLaoding { get; set; }
        public ControllerType ControllerType { get; set; }
    }
}