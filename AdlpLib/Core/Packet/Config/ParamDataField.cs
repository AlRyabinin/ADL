using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extension.Packet;

namespace AdlpLib.Core.Packet.Config
{
    public class ParamDataField : RawPacket
    {
        /// <summary>
        /// Размер поля
        /// </summary>
        public const int Size = 4;

        public ParamDataField(byte[] Raw) : base(Raw)
        {

        }

        public ParamDataField(ParamDataId Id, ParamDataValue Value) : base(ParamDataId.Size + Value.getLength())
        {
            this.Id = Id;
            WriteArray(ParamDataId.Size, Value.getPacket(), Value.getLength());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static int GetFieldSize(byte[] Raw, long Offset)
        {
            return ParamDataValue.GetFieldSize(Raw, Offset + ParamDataId.Size) + ParamDataId.Size;
        }

        /// <summary>
        /// Идентификатор объекта
        /// </summary>
        public ParamDataId Id
        {
            get { return new ParamDataId(ReadArray(0, ParamDataId.Size)); }
            set { WriteArray(0, value.getPacket(), ParamDataId.Size); }
        }

        /// <summary>
        /// Значение объекта
        /// </summary>
        public ParamDataValue Value
        {
            get { return new ParamDataValue(ReadArray(ParamDataId.Size, getLength() - ParamDataId.Size)); }
        }
    }
}
