using Extension.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Packet.Config
{
    public class ParamDataId : RawPacket
    {
        /// <summary>
        /// Размер поля
        /// </summary>
        public const int Size = 6;

        public ParamDataId(byte[] Raw) : base(Raw)
        {

        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="ObjectType">Тип объекта</param>
        /// <param name="Flags">Опции</param>
        /// <param name="Index">Номер объекта</param>
        /// <param name="Param">Номер параметра</param>
        public ParamDataId(ObjectCode ObjectType, int Flags, int Index, ParamCode Param) : base(Size)
        {
            this.ObjectType = ObjectType;
            this.Flags = Flags;
            this.Index = Index;
            this.Param = Param;
        }

        /// <summary>
        /// Тип объекта
        /// </summary>
        public ObjectCode ObjectType
        {
            get { return (ObjectCode)ReadByte(0); }
            set { WriteByte(0, Convert.ToInt32(value)); }
        }

        /// <summary>
        /// Опции
        /// </summary>
        public int Flags
        {
            get { return ReadByte(1); }
            set { WriteByte(1, value); }
        }

        /// <summary>
        /// Номер объекта
        /// </summary>
        public int Index
        {
            get { return ReadUInt16(2); }
            set { WriteUInt16(2, value); }
        }

        /// <summary>
        /// Номер параметра
        /// </summary>
        public ParamCode Param
        {
            get { return (ParamCode)ReadUInt16(4); }
            set { WriteUInt16(4, Convert.ToInt32(value)); }
        }
    }
}
