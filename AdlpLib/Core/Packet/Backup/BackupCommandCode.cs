using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Packet.Backup
{
    public enum BackupCommandCode
    {
        BACKUP_DATA_TYPE_SUPPORT             = 0x00,
        BACKUP_DATA_TYPE_SECTION_COUNT       = 0x01,
        BACKUP_DATA_TYPE_SECTION_BLOCK_COUNT = 0x02,
        BACKUP_DATA_TYPE_SECTION_BLOCK_SIZE  = 0x03,
        BACKUP_DATA_TYPE_SECTION_BLOCK_DATA  = 0x04,
        BACKUP_DATA_TYPE_SECTION_BLOCK_RESET = 0x05,
        BACKUP_DATA_TYPE_SECTION_NAME        = 0x06,
        BACKUP_DATA_TYPE_SECTION_BLOCK_TYPE  = 0x07
    }
}
