using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Packet.Proxy
{
    [Flags]
    public enum ProxyStatus
    {
        PROXY_STATUS_ACTIVE  = 0x01, // Устройство активно
    }
}
