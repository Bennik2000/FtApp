namespace FtApp.Droid.Activities.ControlInterface
{
    class JoystickConfiguration
    {
        public enum JoystickModes
        {
            Single = 0,
            Syncron = 1
        }

        public JoystickModes JoystickMode { get; set; }

        public int[] MotorIndexes { get; set; } = new int[2];
    }
}