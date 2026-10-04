using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Packet.Config
{
    public enum ParamDataType
    {
        PARAM_DATA_TYPE_UNDEFINED = 0x00,
        PARAM_DATA_TYPE_BLOCK = 0x01,
        PARAM_DATA_TYPE_RAW = 0x02,
        PARAM_DATA_TYPE_U8 = 0x03,
        PARAM_DATA_TYPE_U16 = 0x04,
        PARAM_DATA_TYPE_U32 = 0x05,
        PARAM_DATA_TYPE_S8 = 0x06,
        PARAM_DATA_TYPE_S16 = 0x07,
        PARAM_DATA_TYPE_S32 = 0x08,
        PARAM_DATA_TYPE_FLOAT = 0x09,
        PARAM_DATA_TYPE_STR = 0x0A
    }
}
