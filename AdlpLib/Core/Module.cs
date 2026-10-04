using AdlpLib.Core.Packet.Config;
using AdlpLib.Core.Packet.Status;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core
{
    public class Module
    {
        protected Core AdlpCore = null;


        protected long Milliseconds => AdlpCore.Milliseconds;

        public Module()
        {

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
        /// Является ли устройство интересным для нас
        /// </summary>
        /// <param name="Info">Информация об отправителе</param>
        /// <param name="Status">Статус</param>
        /// <returns></returns>
        public virtual bool IsDeviceInteresting(SenderInfo Info, Status Status)
        {
            return false;
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
        /// Получить возможности модуля
        /// </summary>
        /// <returns></returns>
        public virtual BlockCapability GetCapabilities()
        {
            return 0;
        }

        /// <summary>
        /// Обработать основное сообщение статуса
        /// </summary>
        /// <param name="Info">Информация об отправителе</param>
        /// <param name="Status">Данные поля статуса</param>
        /// <returns></returns>
        public virtual bool ProcessDeviceStatus(SenderInfo Info, Status Status)
        {
            return true;
        }

        /// <summary>
        /// Обработать поле статуса
        /// </summary>
        /// <param name="Info">Информация об отправителе</param>
        /// <param name="Field">Данные поля статуса</param>
        /// <returns></returns>
        public virtual bool ProcessDeviceStatus(SenderInfo Info, StatusField Field)
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

        /// <summary>
        /// Конфигурация была изменена [событие для потомков]
        /// </summary>
        public virtual void OnConfigurationChanged()
        {

        }

        /// <summary>
        /// Получить поля статуса
        /// </summary>
        /// <returns></returns>
        public virtual List<StatusField> GetStatusFields()
        {
            return new List<StatusField>();
        }

        /// <summary>
        /// Обработка потока
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="Data"></param>
        public virtual void ProcessStream(StreamId Id, byte[] Data)
        {

        }
    }
}