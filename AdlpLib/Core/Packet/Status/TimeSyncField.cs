using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Packet.Status
{
    internal class TimeSyncField : RawPacket
    {
        /// <summary>
        /// Размер поля
        /// </summary>
        public const int Size = 16;

        public TimeSyncField(byte[] Raw) : base(Raw)
        {

        }

        public TimeSyncField() : base(Size)
        {
            var TS = DateTime.Now;

            Flags = 0;
            Timestamp = Convert.ToUInt32(Convert.ToUInt64(TS.ToFileTime()) & 0xFFFFFFFFUL);

            Year = TS.Year;
            Month = TS.Month;
            Day = TS.Day;
            WeekDay = ((int)TS.DayOfWeek) + 1;

            Hour = TS.Hour;
            Minutes = TS.Minute;
            Seconds = TS.Second;
        }

        public uint Flags
        {
            get { return ReadUInt32(0); }
            set { WriteUInt32(0, value); }
        }

        public uint Timestamp
        {
            get { return ReadUInt32(4); }
            set { WriteUInt32(4, value); }
        }

        public int Year
        {
            get { return ReadUInt16(8); }
            set { WriteUInt16(8, value); }
        }

        public int Month
        {
            get { return ReadByte(10); }
            set { WriteByte(10, value); }
        }

        public int Day
        {
            get { return ReadByte(11); }
            set { WriteByte(11, value); }
        }

        public int WeekDay
        {
            get { return ReadByte(12); }
            set { WriteByte(12, value); }
        }

        public int Hour
        {
            get { return ReadByte(13); }
            set { WriteByte(13, value); }
        }

        public int Minutes
        {
            get { return ReadByte(14); }
            set { WriteByte(14, value); }
        }

        public int Seconds
        {
            get { return ReadByte(15); }
            set { WriteByte(15, value); }
        }
    }
}
