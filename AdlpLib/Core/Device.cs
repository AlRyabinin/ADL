using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core
{
    /// <summary>
    /// Информация об удалённом устройстве
    /// </summary>
    public class Device
    {
        public Guid Uuid { get; private set; } = Guid.Empty;

        /// <summary>
        /// Обозначение типа устройства
        /// </summary>
        public string Name { get; private set; } = null;

        /// <summary>
        /// Последняя активность (мс от начала работы текущего блока)
        /// </summary>
        public long LastActivity { get; private set; } = 0;

        /// <summary>
        /// Флаги
        /// </summary>
        public uint Flags { get; private set; } = 0;

        /// <summary>
        /// Конечная точка
        /// </summary>
        public IPEndPoint Endpoint { get; private set; } = null;

        /// <summary>
        /// Номер блока
        /// </summary>
        public int Block { get; private set; } = 0;

        /// <summary>
        /// Способности
        /// </summary>
        public BlockCapability Capability { get; private set; } = 0;

        /// <summary>
        /// Приоритет
        /// </summary>
        public int Priority { get; private set; } = 0;

        /// <summary>
        /// Базовый канал
        /// </summary>
        public int BaseChannel { get; private set; } = 0;

        /// <summary>
        /// Количество каналов
        /// </summary>
        public int Channels { get; private set; } = 0;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="Endpoint">Отправитель</param>
        /// <param name="Status">Статус</param>
        /// <param name="Time">Текущее время</param>
        public Device(IPEndPoint Endpoint, Packet.Status.Status Status, long Time)
        {
            this.Endpoint = Endpoint;
            Capability = Status.Capability;
            Block = Status.Block;
            LastActivity = Time;
            Uuid = Status.Uuid;
            Name = Status.Type;
        }

        /// <summary>
        /// Просрочено устройство?
        /// </summary>
        /// <param name="Time">Текущее время</param>
        /// <returns>true, если более неактивно</returns>
        public bool IsTimeouted(long Time) => (Time - LastActivity) > Config.DEVICE_TIMEOUT;

        /// <summary>
        /// Это оно же?
        /// </summary>
        /// <param name="Endpoint">Адрес и порт отправителя</param>
        /// <returns>true, если это то же</returns>
        public bool CheckAddress(IPEndPoint Endpoint) => Endpoint.Equals(this.Endpoint);

        /// <summary>
        /// Сообщить об активности
        /// </summary>
        /// <param name="Time">Текущее время</param>
        public void MarkActive(long Time)
        {
            LastActivity = Time;
        }

        /// <summary>
        /// Актуализировать состояние устройства по данным поля статуса.
        /// Устройство сообщает свой идентификатор (UUID) и тип в каждом статусе,
        /// поэтому если они изменились на стороне устройства, обновляем кэш.
        /// </summary>
        /// <param name="Status">Данные статуса</param>
        /// <returns>true, если идентичность устройства изменилась (UUID или тип)</returns>
        public bool UpdateDevice(Packet.Status.Status Status)
        {
            bool Changed = false;

            if (Uuid != Status.Uuid)
            {
                Uuid = Status.Uuid;
                Changed = true;
            }

            if (!string.Equals(Name, Status.Type, StringComparison.Ordinal))
            {
                Name = Status.Type;
                Changed = true;
            }

            return Changed;
        }
    }
}
