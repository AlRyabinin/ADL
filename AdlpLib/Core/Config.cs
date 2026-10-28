using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core
{
    internal class Config
    {
		internal const int CONTROL_BLOCK_MAX_OWNER_BLOCKS = 64;

		internal const int LOCAL_PRIORITY = 255;

		internal const int NOTWORK_PRIORITY = 254;

		internal const int MAX_PRIORITY = 254;

		internal const int MAX_BLOCK_INDEX = 254;

		internal const int BLOCK_CHANNELS = 24;

		internal const int MAX_DEVICE_COUNT = 32;

		internal const int DEVICE_TABLE_UPDATE_PERIOD = 100;

		internal const int MAX_PACKET_SIZE = 1600;

		internal const int STATUS_SENDING_PERIOD = 1000;

		internal const int CHANNEL_UPDATE_PERIOD = 100;

		internal const int ADLPDS_DEFAULT_PORT = 20000;

		internal const int DEVICE_TIMEOUT = 20000;

		internal const int STREAM_TIMEOUT = 4000;

		internal const int CHANNEL_TIMEOUT = 6000;

		internal const int CHANNEL_WAIT_TIMEOUT = 4000;

		internal const int PRIORITY_GUARD_TIME = 4000;

		internal const int PRIORITY_TIMEOUT = 10000;

		internal const int PROXY_UPDATE_PERIOD = 4000;

		internal const int GROUP_INIT_TIMEOUT = 10000;

		internal const int CONFIG_SAVE_TIMEOUT = 5000;

		internal const int MAX_MODULE_COUNT = 6;

		internal const int MAX_EXTENSION_COUNT = 4;

		internal const int MAX_STATUS_SIZE = 1300;

		internal const int MAX_STATUS_FIELD_SIZE = 512;

		internal const int EXEC_BLOCK_MAX_CHANNELS = 96;

		internal const int CONTROL_BLOCK_MAX_CHANNELS = 96;

		internal const int MAX_GROUP_CHANNEL_COUNT = 100;

		internal const int MAX_EVENT_COUNT = 32;

		internal const int MAX_CHANNEL_SWITCH_COUNT = 96;

		internal const int MAX_CHANNEL_SWITCH_EX_COUNT = 96;

		internal const int MAX_PROXIED_DEVICE_COUNT = 128;

		internal const int MAX_BACKUP_DATA_SIZE = 1300;

		internal const int MAX_RACK_COUNT = 24;

		internal const int MAX_STREAM_COUNT = 3;

		internal const int MAX_FAN_COUNT = 16;

		internal const int MAX_RACK_CHANNEL_COUNT = 6;

		internal const int MAX_STREAM_ID_COUNT = 10;

		internal const int RESERVABLE_PRIORITY_PERIOD = 1000;

		internal const int TIME_TO_SEND_CONFIG_STATUS = 25000;

		internal const int MAX_PICTURE_NAME_SIZE = 32;

		internal const int LINE_NAME_SIZE = 6;

		internal const int DEVICE_TYPE_NAME_SIZE = 8;

		internal const int CHANNEL_NAME_SIZE = 32;

		internal const int MAX_PARAM_TEXT_SIZE = 32;

		internal const int MAX_BACKUP_BLOCK_SIZE = 1300;

		internal const int PROTOCOL_VERSION = 0;
	}
}
