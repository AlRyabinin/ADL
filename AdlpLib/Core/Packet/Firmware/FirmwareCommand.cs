using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Packet.Firmware
{
    public enum FirmwareCommand
    {
        FIRMWARE_COMMAND_DETECT = 0x00, // Определить текущее ПО (param=номер строки)
        FIRMWARE_COMMAND_PREPARE = 0x01, // Подготовить ПО (param=размер ПО)
        FIRMWARE_COMMAND_WRITE = 0x02, // Запись блока   (param=смещение)
        FIRMWARE_COMMAND_CHECK = 0x03, // Проверка ПО    (param=crc16)
        FIRMWARE_COMMAND_FINISH = 0x04, // Завершить      (param=0)
        FIRMWARE_COMMAND_RESET = 0x05, // Сброс          (param=0)
    }
}
