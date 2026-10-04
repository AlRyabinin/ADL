using AdlpLib.Core.Packet.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core.Modules.Types
{
    public class ConfiguredDevice
    {
        /// <summary>
        /// Информация об устройстве
        /// </summary>
        public readonly Device Device;

        /// <summary>
        /// Уникальный идентификатор устройства
        /// </summary>
        protected Guid Uuid;

        /// <summary>
        /// Куда отправлять запросы
        /// </summary>
        public IPEndPoint Endpoint => Device.Endpoint;

        /// <summary>
        /// Текущий запрос конфигурирования
        /// </summary>
        private ConfigurationRequest Request = null;

        /// <summary>
        /// Повторный запрос конфигурирования
        /// </summary>
        private ConfigurationRequest RepeatRequest = null;

        /// <summary>
        /// Список запросов на конфигурирование...
        /// </summary>
        private Queue<ConfigurationRequest> ConfigurationRequests = new Queue<ConfigurationRequest>();

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="Device">Информация об устройстве</param>
        /// <param name="Uuid">Уникальный идентификатор</param>
        public ConfiguredDevice(Device Device, Guid Uuid, ConfigurationRequest[] Requests)
        {
            this.Device = Device;
            this.Uuid = Uuid;

            foreach (var Request in Requests)
                ConfigurationRequests.Enqueue(Request);
        }

        /// <summary>
        /// Проверка устройства
        /// </summary>
        /// <param name="EP"></param>
        /// <returns></returns>
        public bool Check(IPEndPoint Endpoint) => Device.CheckAddress(Endpoint);

        /// <summary>
        /// Проверка устройства
        /// </summary>
        /// <param name="EP"></param>
        /// <returns></returns>
        public bool Check(Guid UUID) => Uuid.Equals(UUID);

        /// <summary>
        /// Проверка устройства
        /// </summary>
        /// <param name="EP"></param>
        /// <returns></returns>
        public bool Check(IPEndPoint Endpoint, Guid UUID)
        {
            return Uuid.Equals(UUID);
        }

        /// <summary>
        /// Обработать ответ от устройства
        /// </summary>
        /// <param name="Params"></param>
        virtual internal void ProcessResponse(ParamDataList Params)
        {
            if (Request != null)
            {
                Request.ResultOk();
            }
        }

        /// <summary>
        /// Функция обработки стстауса, проверки таймаутов и т.д.
        /// </summary>
        virtual public void Process()
        {
            // Идёт передача конфигурации...
            if (Request != null)
            {
                if (Request.Completed)
                {
                    // Хорошо, запрос закончен...
                    if (Request.Ok)
                    {
                        // Освобождаем очередь
                        Request = null;
                    }
                    else
                    {
                        // Можно выдать его ещё через некоторое время...
                        // TODO...
                    }
                }
                else if (Request.IsTimeouted(4000))
                {
                    // Повторный запрос
                    RepeatRequest = Request;
                    Request = null;
                }
            }
        }

        /// <summary>
        /// Закончен ли процесс конфигурации
        /// </summary>
        /// <returns></returns>
        virtual public bool IsCompleted()
        {
            return Request == null && ConfigurationRequests.Count == 0 && RepeatRequest == null;
        }

        /// <summary>
        /// Есть ли параметры для отправки
        /// </summary>
        /// <returns></returns>
        virtual public bool HasParamsToSend()
        {
            return RepeatRequest != null || Request == null && ConfigurationRequests.Count > 0;
        }

        /// <summary>
        /// Получить список параметров для отправки
        /// </summary>
        /// <returns></returns>
        virtual public ConfigurationRequest GetNextRequest()
        {
            if (RepeatRequest != null)
            {
                RepeatRequest.Start();
                Request = RepeatRequest;
                RepeatRequest = null;

                return Request;
            }
            else
            {
                var Req = ConfigurationRequests.Dequeue();
                Request = Req;

                return Req;
            }
        }
    }
}
