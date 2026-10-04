using AdlpLib.Core.Packet.Config;
using AdlpLib.Core.Packet.Status;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core
{
    public class Extension
    {
        /// <summary>
        /// Имя расширения
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Ссылка на ядро
        /// </summary>
        protected Core AdlpCore = null;

        public Extension(string Name)
        {
            this.Name = Name;
        }

        /// <summary>
        /// Зарегистрировать ядро
        /// </summary>
        /// <param name="AdlpCore"></param>
        public void RegisterCore(Core AdlpCore)
        {
            this.AdlpCore = AdlpCore;
        }

        /// <summary>
        /// Обработка пакета [событие ядра]
        /// </summary>
        /// <param name="Info">Информация об отправителе</param>
        /// <param name="Packet">Пакет</param>
        /// <returns>true, если обработано</returns>
        public virtual bool OnPacket(SenderInfo Info, Packet.BasePacket Packet)
        {
            return false;
        }

        /// <summary>
        /// Обработка событий [событие ядра]
        /// </summary>
        public virtual void Process()
        {

        }

        /// <summary>
        /// Обработка базовых параметров [событие ядра]
        /// </summary>
        /// <param name="Info"></param>
        /// <param name="Id"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public virtual bool ProcessSetParamField(ParamDataId Id, ParamDataValue Value)
        {
            return false;
        }

        /// <summary>
        /// Обработаем получение параметров [событие ядра]
        /// </summary>
        /// <param name="Info"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        public virtual ParamDataValue ProcessGetParamField(ParamDataId Id)
        {
            return null;
        }

        /// <summary>
        /// Последний этап инициализации [событие для потомков]
        /// </summary>
        /// <returns>true, если инициализировано</returns>
        public virtual bool OnInit()
        {
            return true;
        }

        /// <summary>
        /// Первый этап освобождения ресурсов [событие для потомков]
        /// </summary>
        public virtual void OnUninit()
        {

        }
    }
}