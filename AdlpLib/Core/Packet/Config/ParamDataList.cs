using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Packet.Config
{
    internal class ParamDataList : RawPacket
    {
        /// <summary>
        /// Размер поля
        /// </summary>
        public const int HeaderSize = 2;

        public ParamDataList(byte[] Raw) : base(Raw)
        {

        }

        public ParamDataList(ParamDataField[] Fields) : base(HeaderSize + Fields.Sum(Fields => Fields.getLength()))
        {
            this.Flags = 0;
            Count = Fields.Length;

            int Offset = HeaderSize;
            for (int i = 0; i < Fields.Length; i++)
            {
                var F = Fields[i];
                WriteArray(Offset, F.getPacket(), F.getLength());

                Offset += F.getLength();
            }
        }

        /// <summary>
        /// Количество команд
        /// </summary>
        public int Count
        {
            get { return ReadByte(0); }
            set { WriteByte(0, value); }
        }

        /// <summary>
        /// Приоритет команды
        /// </summary>
        public int Flags
        {
            get { return ReadByte(1); }
            set { WriteByte(1, value); }
        }

        public ParamDataField[] Fields
        {
            get
            {
                var Res = new List<ParamDataField>();
                int Offset = HeaderSize;
                var Raw = getPacket();
                for(int i = 0; i < Count; i++)
                {
                    var Size = ParamDataField.GetFieldSize(Raw, Offset);
                    Res.Add(new ParamDataField(ReadArray(Offset, Size)));

                    Offset += Size;
                }

                return Res.ToArray();
            }
        }
    }
}
