using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core
{
    [Flags]
    public enum BlockCapability
    {
        BLOCKCAP_EXEC = 0x0001,   // Выполнение команд (управление каналами, прямыми или диммируемыми)
        BLOCKCAP_DC = 0x0002,     // Поддерживает управление прямыми каналами
        BLOCKCAP_DIM = 0x0004,    // Поддерживает управление диммируемыми каналами
        BLOCKCAP_CTRL = 0x0010,   // Управляющее устройство (может отдавать команды исполняемым устройствам)
        BLOCKCAP_CFG = 0x0020,    // Устройство может хранить и раздавать конфигурацию
        BLOCKCAP_PROXY = 0x0040   // Умеет переправлять пакеты на другие порты
    }
}
