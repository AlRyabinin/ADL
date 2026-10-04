using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core
{
    public enum PacketCode
    {
        COMMAND_GET_PARAM        = 0,
        COMMAND_SET_PARAM        = 1,
        COMMAND_CH_SWITCH        = 2,
        COMMAND_STATUS           = 3,
        COMMAND_PROXY_CMD        = 4,
        COMMAND_PROXY_DATA       = 5,
        COMMAND_FIRMWARE         = 6,
        COMMAND_EVENTS           = 7,
        COMMAND_BACKUP_CMD       = 8,
        COMMAND_BACKUP_DATA      = 9,
        COMMAND_CH_SWITCH_EX     = 10
    }
}
