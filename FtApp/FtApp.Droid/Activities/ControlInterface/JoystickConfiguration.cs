namespace FtApp.Droid.Activities.ControlInterface
{
    /// <summary>
    /// Thie joystick configuration holds the configuration values of the joystick
    /// </summary>
    class JoystickConfiguration
    {
        public enum JoystickModes
        {
            Single = 0,
            Syncron = 1
        }

        /// <summary>
        /// The mode of the joystick
        /// </summary>
        public JoystickModes JoystickMode { get; set; }

        /// <summary>
        /// The motor indexes of the joystick. Mostly 2
        /// </summary>
        public int[] MotorIndexes { get; set; } = new int[2];
    }
}