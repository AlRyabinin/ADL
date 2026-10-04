using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core
{
    public class ChannelState
    {
        /// <summary>
        /// Имя канала
        /// </summary>
        public string Name { get; private set; } = "";

        /// <summary>
        /// Номер канала
        /// </summary>
        public int Index { get; private set; } = 0;

        /// <summary>
        /// Требуемый приоритет
        /// </summary>
        public int Priority { get; protected set; } = 0;

        /// <summary>
        /// Флаги
        /// </summary>
        protected ChannelFlags Status = 0;

        /// <summary>
        /// Является ли канал настроенным
        /// </summary>
        public bool Configured => Index != 0;

        /// <summary>
        /// Является ли канал настроенным
        /// </summary>
        public bool Available
        {
            get { return Status.HasFlag(ChannelFlags.CHSTATUS_AVAILABLE); }
            set
            {
                if (value)
                    Status |= ChannelFlags.CHSTATUS_AVAILABLE;
                else
                    Status &= ~ChannelFlags.CHSTATUS_AVAILABLE;
            }
        }

        /// <summary>
        /// Включён ли канал
        /// </summary>
        public bool On
        {
            get { return Status.HasFlag(ChannelFlags.CHSTATUS_ON); }
            set
            {
                if (value)
                    Flags |= ChannelFlags.CHSTATUS_ON;
                else
                    Flags &= ~ChannelFlags.CHSTATUS_ON;
            }
        }

        /// <summary>
        /// Флаги
        /// </summary>
        private OwnFlags InternalFlags = 0;

        private const ChannelFlags FlagChangeMask = ChannelFlags.CHSTATUS_CTRL | ChannelFlags.CHSTATUS_ERROR | ChannelFlags.CHSTATUS_ON | ChannelFlags.CHSTATUS_PARTIAL | ChannelFlags.CHSTATUS_WAIT;

        /// <summary>
        /// Флаги канала
        /// </summary>
        public ChannelFlags Flags
        {
            get
            {
                if (Status.HasFlag(ChannelFlags.CHSTATUS_AVAILABLE))
                {
                    var F = Status;

                    if (InternalFlags.HasFlag(OwnFlags.CHANNEL_FLAG_WAIT_RESPONSE))
                        F |= ChannelFlags.CHSTATUS_WAIT;

                    return F;
                }
                else
                    return 0;
            }

            set
            {
                var Diff = (value ^ Status) & FlagChangeMask;
                if (Diff != 0)
                    SetOwnFlag(OwnFlags.CHANNEL_FLAG_STATE_UPDATED);

                Status = value;
            }
        }

        public ChannelState()
        {
            Reset();
        }

        /// <summary>
        /// Сброс состояния
        /// </summary>
        protected virtual void Reset()
        {
            InternalFlags = OwnFlags.CHANNEL_FLAG_PRIORITY_CONTROL;
            Flags = 0;
            Priority = 0;
        }

        /// <summary>
        /// Установить номер канала
        /// </summary>
        /// <param name="Index">Номер</param>
        public void SetIndex(int Index)
        {
            this.Index = Index;
        }

        /// <summary>
        /// Установить название канала
        /// </summary>
        /// <param name="Index">Номер</param>
        public void SetName(string Name)
        {
            this.Name = Name;
        }

        /// <summary>
        /// Установить внутренний флаг
        /// </summary>
        /// <param name="Flag">внутренний флаг</param>
        protected void SetOwnFlag(OwnFlags Flag, bool Value)
        {
            if (Value)
                SetOwnFlag(Flag);
            else
                ResetOwnFlag(Flag);
        }

        /// <summary>
        /// Установить внутренний флаг
        /// </summary>
        /// <param name="Flag">внутренний флаг</param>
        protected void SetOwnFlag(OwnFlags Flag)
        {
            InternalFlags |= Flag;
        }

        /// <summary>
        /// Установить внутренний флаг
        /// </summary>
        /// <param name="Flag">внутренний флаг</param>
        protected void ResetOwnFlag(OwnFlags Flag)
        {
            InternalFlags &= ~Flag;
        }

        /// <summary>
        /// Проверить внутренний флаг
        /// </summary>
        /// <param name="Flag">внутренний флаг</param>
        /// <returns>true, если флаг установлен</returns>
        protected bool GetOwnFlag(OwnFlags Flag)
        {
            return InternalFlags.HasFlag(Flag);
        }

        /// <summary>
        /// Можно ли контролировать канал
        /// </summary>
        /// <param name="MyPriority"></param>
        /// <returns></returns>
        public virtual bool IsControllable() => false;

        /// <summary>
        /// Проверить внутренний флаг с его сбросом
        /// </summary>
        /// <param name="Flag">внутренний флаг</param>
        /// <returns>true, если флаг установлен</returns>
        protected bool GetOwnFlagWithReset(OwnFlags Flag)
        {
            if (InternalFlags.HasFlag(Flag))
            {
                ResetOwnFlag(Flag);
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Установить режим управления через DMX
        /// </summary>
        public void SetDMXMode(bool Value)
        {
            if (Value)
            {
                Flags |= ChannelFlags.CHSTATUS_CTRL;
                ResetOwnFlag(OwnFlags.CHANNEL_FLAG_PRIORITY_CONTROL);
            }
            else
            {
                Flags &= ~ChannelFlags.CHSTATUS_CTRL;
                SetOwnFlag(OwnFlags.CHANNEL_FLAG_PRIORITY_CONTROL);
            }
        }

        /// <summary>
        /// Есть ли контроль приоритетов
        /// </summary>
        public bool HasPriorityControl => InternalFlags.HasFlag(OwnFlags.CHANNEL_FLAG_PRIORITY_CONTROL);

        /// <summary>
        /// Является ли канал управляемым через DMX
        /// </summary>
        /// <returns></returns>
        public bool DMXMode => Status.HasFlag(ChannelFlags.CHSTATUS_CTRL);

        /// <summary>
        /// Изменилось ли состояние канала
        /// </summary>
        /// <returns></returns>
        public bool IsStatusChanged() => GetOwnFlagWithReset(OwnFlags.CHANNEL_FLAG_STATE_UPDATED);

        /// <summary>
        /// Установить свойство, является ли диммируемым каналом
        /// </summary>
        public void SetDimmerCapable(bool Value) => SetOwnFlag(OwnFlags.CHANNEL_FLAG_DIMMER_CAPABLE, Value);

        /// <summary>
        /// Является ли канал диммируемым
        /// </summary>
        /// <returns></returns>
        public bool DimmerCapable => GetOwnFlag(OwnFlags.CHANNEL_FLAG_DIMMER_CAPABLE);

        /// <summary>
        /// Отметить, что конфигурация канала была изменена
        /// </summary>
        public void MarkConfigurationChanged() => SetOwnFlag(OwnFlags.CHANNEL_FLAG_CONFIG_UPDATED);

        /// <summary>
        /// Была ли конфигурация изменена
        /// </summary>
        /// <returns></returns>
        public bool IsConfigurationChanged() => GetOwnFlagWithReset(OwnFlags.CHANNEL_FLAG_CONFIG_UPDATED);


        /// <summary>
        /// Внутренние флаги
        /// </summary>
        [Flags]

        protected enum OwnFlags
        {
            CHANNEL_FLAG_TIMEOUTED = 0x01,  // Нет ответа от управляющего устройства
            CHANNEL_FLAG_CONTROL_LOST = 0x02,  // Потерян контроль над каналом (нет более ни одного устройства, которое бы им управляли)
            CHANNEL_FLAG_WAIT_RESPONSE = 0x04, // Ожидание ответа
            CHANNEL_FLAG_LOCAL = 0x08,  // Локальное управление
            CHANNEL_FLAG_CONFIG_UPDATED = 0x10,  // Конфигурация была изменена
            CHANNEL_FLAG_STATE_UPDATED = 0x20,  // Состояние было изменено
            CHANNEL_FLAG_DIMMER_CAPABLE = 0x40,  // Канал умеет диммироваться
            CHANNEL_FLAG_PRIORITY_CONTROL = 0x80,  // Канал участвует в системе приоритетов
            CHANNEL_FLAG_WIDE_DMX = 0x100, // Канал использует 16 бит DMX
        }
    }
}
