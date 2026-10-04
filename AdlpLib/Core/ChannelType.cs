using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core
{
    public enum ChannelType
    {
        CHTYPE_NONE    = 0x0000,  // Канал отключён
	    CHTYPE_CHANNEL = 0x0001,  // Отдельный канал
		CHTYPE_GROUP   = 0x0002,  // Группа
        CHTYPE_PICTURE = 0x0003,  // Картина локальная
    }
}
