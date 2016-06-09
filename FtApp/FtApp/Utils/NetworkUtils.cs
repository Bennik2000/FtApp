using System.Net;
using System.Net.NetworkInformation;

namespace FtApp.Utils
{
    public static class NetworkUtils
    {
        public static bool PingIp(string ip)
        {
            Ping sender = new Ping();
            PingReply result = sender.Send(ip, 500);

            return result?.Status == IPStatus.Success;
        }

        public static bool IsValidIpAdress(string ip)
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
