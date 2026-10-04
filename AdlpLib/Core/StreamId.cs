using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core
{
    public enum StreamType
    {
        STREAM_TYPE_NONE   = 0,
        STREAM_TYPE_DMX    = 1,
        STREAM_TYPE_ARTNET = 2,
        STREAM_TYPE_ACN    = 3,
        STREAM_TYPE_DEVICE = 4
    }

    public class StreamId
    {
        /// <summary>
        /// Тип потока
        /// </summary>
        public StreamType Type;

        /// <summary>
        /// Номер потока
        /// </summary>
        public uint Universe;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="type"></param>
        /// <param name="universe"></param>
        public StreamId(StreamType type, uint universe)
        {
            Type = type;
            Universe = universe;
        }

        /// <summary>
        /// Задать через параметр
        /// </summary>
        /// <param name="Value"></param>
        public void Set(uint Value)
        {
            Type = (StreamType)((Value >> 16) & 0xFF);
            Universe = Value & 0xFFFF;
        }

        public uint Get()
        {
            return Universe | (Convert.ToUInt32(Type) << 16);
        }

        public override bool Equals(object obj)
        {
            var O = obj as StreamId;
            if (O == null)
                return false;
            else
                return (O.Type == Type) && (O.Universe == Universe);
        }
    }
}
