using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Packet.Proxy
{
    public enum ProxyCommand
    {
        PROXY_COMMAND_ADD    = 0x01, // Добавить проксируемое устройство
        PROXY_COMMAND_UPDATE = 0x02, // Сброс таймаута
        PROXY_COMMAND_REMOVE = 0x03, // Удалить проксируемое устройство
    }
}
