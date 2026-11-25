using System;

namespace AdlpLib.Configuration.Application
{
    /// <summary>
    /// Multicast-адрес уже занят другим устройством. Возникает, когда БД отклонила запись:
    /// сработал триггер (SIGNAL 45000, MULTICAST_ADDRESS_ALREADY_IN_USE)
    /// или нарушен уникальный индекс по адресу.
    /// </summary>
    public sealed class MulticastAddressInUseException : InvalidOperationException
    {
        public string Address { get; }

        /// <summary>Id устройства-владельца адреса, если удалось определить.</summary>
        public long? OwnerDeviceId { get; }

        public MulticastAddressInUseException(string address, long? ownerDeviceId, Exception? inner = null)
            : base(BuildMessage(address, ownerDeviceId), inner)
        {
            Address = address ?? string.Empty;
            OwnerDeviceId = ownerDeviceId;
        }

        private static string BuildMessage(string address, long? ownerDeviceId)
        {
            return ownerDeviceId.HasValue
                ? $"Multicast-адрес {address} уже назначен устройству Id={ownerDeviceId.Value}."
                : $"Multicast-адрес {address} уже используется другим устройством.";
        }
    }
}