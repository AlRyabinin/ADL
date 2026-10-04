using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Packet.Config
{
    public enum ObjectCode
    {
        PARAM_OBJECT_DEVICE = 1,           // Параметры блока (IP,NM,GW,номер,MAC,название,пароль и т.д.)
        PARAM_OBJECT_CHANNEL = 2,          // Параметры канала (тип управления, патч, задержка, моторный, кривая, лимит, преднакал и т.д.)
        PARAM_OBJECT_STREAM = 3,           // Параметры потока управления (тип потока, номер потока, стартовый канал вкл/выкл и т.д.)
        PARAM_OBJECT_GROUP = 4,            // Параметры группы (список каналов и т.д.)
        PARAM_OBJECT_PICTURE = 5,          // Параметры картины
        PARAM_OBJECT_PIXELGATE_CHANNEL = 6, // Параметры канала пиксельгейта
        PARAM_OBJECT_CONTROL_BUTTON = 100,  // Параметры кнопки цветной ПУ
        PARAM_OBJECT_GATE_CONVEYOR = 101    // Конвеер гейта
    }
}
