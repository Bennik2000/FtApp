using System;

namespace FtApp.Utils
{
    /// <summary>
    /// Provides some helper methods for common calculations
    /// </summary>
    public static class MathUtils
    {
        private const double ToDegreesMultiplier = 180.0/Math.PI;
        private const double ToRadiansMultiplier = Math.PI/180.0;

        /// <summary>
        /// Converts radians to degrees
        /// </summary>
        /// <param name="angle">The angle in radians</param>
        /// <returns>The angle in degrees</returns>
        public static double ToDegrees(double angle)
        {
            return angle*ToDegreesMultiplier;
        }

        /// <summary>
        /// Converts degrees to radians
        /// </summary>
        /// <param name="angle">The angle in degrees</param>
        /// <returns>The angle in radians</returns>
        public static double ToRadians(double angle)
        {
            return angle * ToRadiansMultiplier;
        }
    }
}
