using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Modules.Types
{
    public class ConfigurationRequest
    {
        /// <summary>
        /// Поля запроса
        /// </summary>
        public readonly Packet.Config.ParamDataField[] Fields;

        /// <summary>
        /// Время отправки
        /// </summary>
        public DateTime Sended;

        /// <summary>
        /// Номер транзакции
        /// </summary>
        public int Tid = -1;

        /// <summary>
        /// Был ли завершён запрос
        /// </summary>
        public bool Completed { get; private set; } = false;

        /// <summary>
        /// Успешно ли выполнено
        /// </summary>
        public bool Ok { get; private set; } = false;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="Fields"></param>
        public ConfigurationRequest(Packet.Config.ParamDataField[] Fields)
        {
            this.Fields = Fields;

            Sended = DateTime.Now;
        }

        /// <summary>
        /// Сообщить об ошибке
        /// </summary>
        /// <param name="Text"></param>
        public void ResultError(string Text)
        {
            Completed = true;
        }

        /// <summary>
        /// Сообщить об успешном ответе
        /// </summary>
        /// <param name="Text"></param>
        public void ResultOk()
        {
            Ok = true;
            Completed = true;
        }

        /// <summary>
        /// Сброс
        /// </summary>
        public void Start()
        {
            Sended = DateTime.Now;
            Tid = -1;
            Completed = false;
            Ok = false;
        }

        /// <summary>
        /// Сколько время прошло с момента отправки
        /// </summary>
        /// <param name="Milliseconds"></param>
        /// <returns></returns>
        public bool IsTimeouted(int Milliseconds)
        {
            return (DateTime.Now - Sended).TotalMilliseconds > Milliseconds;
        }
    }
}
