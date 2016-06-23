using System.Net;
using System.Net.NetworkInformation;

namespace FtApp.Utils
{
    /// <summary>
    /// Provides some helper methods for networking
    /// </summary>
    public static class NetworkUtils
    {
        /// <summary>
        /// Pings an ip address
        /// </summary>
        /// <param name="ip">The address to ping</param>
        /// <returns>true if the connection was valid otherwise false</returns>
        public static bool PingIp(string ip)
        {
            Ping sender = new Ping();
            PingReply result = sender.Send(ip, 500);

            return result?.Status == IPStatus.Success;
        }

        /// <summary>
        /// Checks if it is a valid ip address
        /// </summary>
        /// <param name="ip">The address to check</param>
        /// <returns>true if valid otherwise false</returns>
        public static bool IsValidIpaddress(string ip)
        {
            IPAddress address;
            if (IPAddress.TryParse(ip, out address))
            {
                switch (address.AddressFamily)
                {
                    case System.Net.Sockets.AddressFamily.InterNetwork:
                        return true;
                }
            }
            return false;
        }
    }
}
