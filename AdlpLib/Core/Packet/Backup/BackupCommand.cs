using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Packet.Backup
{
    public class BackupCommand : RawPacket
    {
        public const int Size = 6;

        public BackupCommand(byte[] Raw) : base(Raw)
        {
        }

        public BackupCommand(BackupCommandCode Code, ushort Section, ushort Block) : base(Size)
        {
            this.Code = Code;
            this.Section = Section;
            this.Block = Block;
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
    }
}
