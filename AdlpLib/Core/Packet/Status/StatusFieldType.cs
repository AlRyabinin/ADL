using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Packet.Status
{
    public enum StatusFieldType
    {
		STATUS_FIELD_DEVICE            = 0,
		STATUS_FIELD_EXEC              = 1,
		STATUS_FIELD_CONTROL           = 2,
		STATUS_FIELD_PROXY             = 3,
		STATUS_FIELD_CONFIG            = 4,
		STATUS_FIELD_LOG               = 5,
        STATUS_FIELD_OWNER             = 6,
        STATUS_FIELD_TIMESYNC          = 7,
        STATUS_FIELD_GLOBAL_GROUPS     = 8,
        STATUS_FIELD_EXEC_EXTENDED     = 9,
        STATUS_FIELD_WSO_PAGES_SYNC    = 10,
        STATUS_FIELD_DIM_RACKS_STATUS  = 11,
        STATUS_FIELD_DIM_STREAM_STATUS = 12,
        STATUS_FIELD_DIM_FAN_STATUS    = 13,
        STATUS_FIELD_DIM_COMMON_STATUS = 14
    }
}
