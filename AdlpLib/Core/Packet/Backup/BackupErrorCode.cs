using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Packet.Backup
{
    public enum BackupErrorCode
    {
        BACKUP_DATA_ERROR_CODE_OK          = 0x0000,
        BACKUP_DATA_ERROR_CODE_INV_CODE    = 0xFFFF,
        BACKUP_DATA_ERROR_CODE_INV_SECTION = 0xFFFE,
        BACKUP_DATA_ERROR_CODE_INV_BLOCK   = 0xFFFD,
        BACKUP_DATA_ERROR_CODE_INV_SIZE    = 0xFFFC,
        BACKUP_DATA_ERROR_CODE_ERROR       = 0xFFFB
    }
}
