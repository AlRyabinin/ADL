using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Packet.Firmware
{
    public enum FirmwareResponseCode
    {
        FIRMWARE_RESPONSE_OK = 0,              // Хорошо
        FIRMWARE_RESPONSE_INVALID_ARG = 1,     // Некорректный параметр
        FIRMWARE_RESPONSE_INVALID_STATE = 2,   // Код команды неверный
        FIRMWARE_RESPONSE_INVALID_COMMAND = 3, // Код команды неверный
        FIRMWARE_RESPONSE_ERROR = 4            // Какая-то ошибка в выполнении
    }
}
