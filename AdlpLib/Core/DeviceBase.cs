using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AdlpLib.Core
{
    /// <summary>
    /// База данных устройств
    /// </summary>
    class DeviceBase
    {
        /// <summary>
        /// Список устройств
        /// </summary>
        private List<Device> Devices = new List<Device>();

        /// <summary>
        /// Получить устройство по адресу
        /// </summary>
        /// <param name="Endpoint"></param>
        /// <returns></returns>
        public Device GetDevice(IPEndPoint Endpoint)
        {
            foreach(var D in Devices)
            {
                if (D.CheckAddress(Endpoint))
                    return D;
            }

            return null;
        }

        /// <summary>
        /// Получить информацию об устройстве по названию и номеру блока
        /// </summary>
        /// <param name="name">Название блока</param>
        /// <param name="block">Блок</param>
        /// <returns></returns>
        public Device GetDevice(string name, int block)
        {
            foreach (var D in Devices)
            {
                if ((D.Name == name) && (D.Block == block))
                    return D;
            }

            return null;
        }

        public Device[] GetDevices() => Devices.ToArray();

        public int OnlineDevices => Devices.Count;

        /// <summary>
        /// Добавить устройство
        /// </summary>
        /// <param name="D"></param>
        public void AddDevice(Device D)
        {
            Devices.Add(D);
        }

        /// <summary>
        /// Обновить таблицу устройств
        /// </summary>
        /// <returns>true, если таблица изменилась</returns>
        public bool Update(long Time)
        {
            var Temp = new List<Device>();

            foreach(var D in Devices)
            {
                if (D.IsTimeouted(Time))
                    Temp.Add(D);
            }

            foreach (var D in Temp)
                Devices.Remove(D);

            return Temp.Count > 0;
        }
    }
}
