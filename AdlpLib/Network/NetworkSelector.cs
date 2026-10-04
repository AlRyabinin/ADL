using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

using Extension.IP;

namespace Network
{
    public static class NetworkSelector
    {
        /// <summary>
        /// Список сетевых интерфейсов
        /// </summary>
        /// <returns></returns>
        public static List<NetworkInterface> getInterfaces()
        {
            List<NetworkInterface> Interfaces = new List<NetworkInterface>();
            IPAddress[] localIP = Dns.GetHostAddresses(Dns.GetHostName());

            Interfaces.Add(new NetworkInterface(IPAddress.Any, IPAddress.Any, "Автоматически"));

            foreach (IPAddress A in localIP)
            {
                if(A.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    Interfaces.Add(new NetworkInterface(A, IPAddress.Any, A.ToString()));
            }

            return Interfaces;
        }


        /// <summary>
        /// Дефолтный интерфейс
        /// </summary>
        /// <returns></returns>
        public static NetworkInterface getDefaultInterface()
        {
            return new NetworkInterface(IPAddress.Any, IPAddress.Any, "Автоматически");
        }

        /// <summary>
        /// Интерфейс по IP
        /// </summary>
        /// <returns></returns>
        public static NetworkInterface getInterface(string IP, string Mask)
        {
            return new NetworkInterface(IPAddress.Parse(IP), IPAddress.Parse(Mask), IP);
        }

        /// <summary>
        /// Есть ли подобный интерфейс
        /// </summary>
        /// <param name="IP"></param>
        /// <returns></returns>
        public static bool HasSuchInterface(string IP)
        {
            var Ifs = getInterfaces();

            foreach (var I in Ifs)
            {
                if (I.Address.ToString().CompareTo(IP) == 0) return true;
            }

            return false;
        }
    }
}
