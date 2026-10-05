using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Packet.Backup
{
    public class BackupData : RawPacket
    {
        public const int HeaderSize = 10;

        public BackupData(byte[] Raw) : base(Raw)
        {
        }

        public BackupData(BackupCommandCode Code, ushort Section, ushort Block, BackupErrorCode Error, byte[] Data) : base(HeaderSize + (Data != null ? Data.Length : 0))
        {
            this.Code = Code;
            this.Section = Section;
            this.Block = Block;
            this.Error = Error;
            this.Size = Data != null ? (ushort)Data.Length : (ushort)0;

            if (Data != null && Data.Length > 0)
                WriteArray(HeaderSize, Data, Data.Length);
        }

        public BackupCommandCode Code
        {
            get { return (BackupCommandCode)ReadUInt16(0); }
            set { WriteUInt16(0, Convert.ToInt32(value)); }
        }

        public ushort Section
        {
            get { return ReadUInt16(2); }
            set { WriteUInt16(2, value); }
        }

        public ushort Block
        {
            get { return ReadUInt16(4); }
            set { WriteUInt16(4, value); }
        }

        public BackupErrorCode Error
        {
            get { return (BackupErrorCode)ReadUInt16(6); }
            set { WriteUInt16(6, Convert.ToInt32(value)); }
        }

        public ushort Size
        {
            get { return ReadUInt16(8); }
            set { WriteUInt16(8, value); }
        }

        public byte[] Data
        {
            get { return Size > 0 ? ReadArray(HeaderSize, Size) : new byte[0]; }
        }

        /// <summary>
        /// Малое значение ответа (SUPPORT, SECTION_COUNT, BLOCK_COUNT, BLOCK_SIZE, BLOCK_TYPE).
        /// В протоколе маленькие значения могут передаваться в поле error (size=0),
        /// либо в данных (size=2) — вычитываем оба варианта.
        /// </summary>
        public ushort DataValue
        {
            get
            {
                if (Size >= 2)
                    return ReadUInt16(HeaderSize);
                return (ushort)Error;
            }
        }

        /// <summary>
        /// Тип секции, как есть (для BACKUP_DATA_TYPE_SECTION_BLOCK_TYPE).
        /// </summary>
        public BackupSectionType SectionType
        {
            get { return (BackupSectionType)DataValue; }
        }

        /// <summary>
        /// Имя секции, как есть (для BACKUP_DATA_TYPE_SECTION_NAME)
        /// </summary>
        public string SectionName
        {
            get { return ReadANSIString(HeaderSize, Size); }
        }
    }
}
