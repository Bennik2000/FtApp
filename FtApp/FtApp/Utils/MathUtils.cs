using System;
using System.Collections.Generic;
using System.Text;

namespace FtApp.Utils
{
    public static class MathUtils
    {
        private const double ToDegreesMultiplier = 180.0/Math.PI;
        private const double ToRadiansMultiplier = Math.PI/180.0;

        public static double ToDegrees(double angle)
        {
            return angle*ToDegreesMultiplier;
        }
        public static double ToRadians(double angle)
        {
            return angle * ToRadiansMultiplier;
        }
    }
}
