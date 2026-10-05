using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Packet.Backup
{
    public enum BackupSectionType
    {
        BACKUP_DATA_SECTION_UNKNOWN    = 0x00,
        BACKUP_DATA_SECTION_DEVICE     = 0x01,
        BACKUP_DATA_SECTION_CHANNELS   = 0x02,
        BACKUP_DATA_SECTION_GROUPS     = 0x03,
        BACKUP_DATA_SECTION_STREAMS    = 0x04,
        BACKUP_DATA_SECTION_CONTROL    = 0x05,
        BACKUP_DATA_SECTION_EXEC       = 0x06,
        BACKUP_DATA_SECTION_ADLP       = 0x07, // Настр. исп. устройства
        BACKUP_DATA_SECTION_CONTROL_CH = 0x08, // Управление
        BACKUP_DATA_SECTION_EXEC_CH    = 0x09, // Исп. устройства
        BACKUP_DATA_SECTION_RACKS      = 0x0A, // Полки
        BACKUP_DATA_SECTION_PICTURES   = 0x0B, // Картины
        BACKUP_DATA_SECTION_CONTROL_BT = 0x0C, // Кнопки
        BACKUP_DATA_SECTION_GATE_CH    = 0x0D, // Канал гейта
        BACKUP_DATA_SECTION_HARDWARE   = 0x0E, // Аппаратные настройки
        BACKUP_DATA_SECTION_INDIGO_CH  = 0x0F, // Канал гейта индиго
        BACKUP_DATA_SECTION_RAW        = 0xFF
    }
}