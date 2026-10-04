using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Network
{
    public class NetworkInterface
    {
        /// <summary>
        /// IP-адрес
        /// </summary>
        public IPAddress Address;

        /// <summary>
        /// Название
        /// </summary>
        public string Name;

        public NetworkInterface(IPAddress A, IPAddress Mask, string N)
        {
            Address = A;
            Name = N;
        }

        public override string ToString()
        {
            if (Address == null)
                return "Non-exists interface";

            if (Address.Equals(IPAddress.Any))
                return "Automatic";
            else
                return Address.ToString();
        }
    }
}
