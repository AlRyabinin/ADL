using AdlpLib.Core.Packet;
using AdlpLib.Core.Packet.Config;
using AdlpLib.Core.Packet.Proxy;
using AdlpLib.Core.Packet.Status;
using Extension.Array;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdlpLib.Core
{
	public class Core
	{
		/// <summary>
		/// Интервал между тиками, мс
		/// </summary>
		const int TIMER_INTERVAL = 10;

		/// <summary>
		/// Инициализировано ли ядро
		/// </summary>
		protected bool Inited { get; private set; } = false;

		/// <summary>
		/// Сетевой интерфейс
		/// </summary>
		private Net Net = null;

		/// <summary>
		/// Таблица устройств
		/// </summary>
		private DeviceBase Devices = new DeviceBase();

		/// <summary>
		/// ТАймер для подсчёта миллисекунд
		/// </summary>
		private System.Timers.Timer Timer = new System.Timers.Timer(TIMER_INTERVAL);

		/// <summary>
		/// Запущено
		/// </summary>
		private bool Running = false;

		/// <summary>
		// Порядковый номер пакета
		/// </summary>
		private ushort Tid = 0;

		/// <summary>
		// Номер системы
		/// </summary>
		private uint SystemId = 0;

		/// <summary>
		// Локальный порт для приёма сообщений
		/// </summary>
		private int LocalPort = 0;

		/// <summary>
		/// Конфигурация обновлена
		/// </summary>
		private bool ConfigurationChanged = false;

		/// <summary>
		/// Запрос конфигурации
		/// </summary>
		private bool ConfigurationRequest = false;

		/// <summary>
		/// Состояние изменено
		/// </summary>
		private bool StatusChanged = false;

		/// <summary>
		/// Запрос обновления ПО
		/// </summary>
		private bool FirmwareRequest = false;

		/// <summary>
		// Счётчик миллисекунд
		/// </summary>
		public long Milliseconds { get; private set; } = 0;

		/// <summary>
		/// Очередь пакетов
		/// </summary>
		private Queue<InputPacket> PacketsQueue = new Queue<InputPacket>();

		/// <summary>
		// Время обновления таблицы устройств
		/// </summary>
		private long LastDeviceUpdatedTableTime = 0;

		/// <summary>
		/// Время последней отправки статуса
		/// </summary>
		private long LastStatusSendedTime = 0;

		private long LastStatusRequestTime = 0;

		/// <summary>
		/// Периодическое обнаружение устройств.
		/// </summary>
		public bool StatusPolling { get; set; } = true;

		/// <summary>
		/// Интервал запросов, мс.
		/// </summary>
		public int StatusPollingPeriod { get; set; } = 5000;

		/// <summary>
		/// Пустой список означает все типы устройств.
		/// </summary>
		public List<string> StatusPollingTypes { get; }
			= new List<string>();

		/// <summary>
		/// Время последней отправки запроса на проксирование
		/// </summary>
		private long LastProxyStatusSendedTime = 0;

		/// <summary>
		/// Название устройства
		/// </summary>
		public string Name { get; private set; }  = "UNK";

		/// <summary>
		/// Список модулей
		/// </summary>
		private List<Module> Modules = new List<Module>();

		/// <summary>
		/// Список модулей
		/// </summary>
		private List<Extension> Extensions = new List<Extension>();

		/// <summary>
		/// Уникальный идентификатор устройства
		/// </summary>
		private Guid Uuid = Guid.Empty;

		/// <summary>
		// Способности блока (BlockCapabilityTime)
		/// </summary>
		private BlockCapability Capability = 0;

		/// <summary>
		/// Номер блока
		/// </summary>
		public byte Block { get; private set; } = 0;

		/// <summary>
		/// Управление потоком
		/// </summary>
		private Thread MainThread = null;

		/// <summary>
		/// Узнать свой адрес и порт
		/// </summary>
		protected IPEndPoint MyEndpoint => new IPEndPoint(Net.IP, LocalPort);

		/// <summary>
		/// Надо ли проксировать?..
		/// </summary>
		private bool IsProxied => LocalPort != Config.ADLPDS_DEFAULT_PORT;

		public Device[] GetDevices() => Devices.GetDevices();

		public int OnlineDevices => Devices.OnlineDevices;

		/// <summary>
		/// Добавить модуль (до инициализации)
		/// </summary>
		public void AddModule(Module Module)
		{
			Modules.Add(Module);
		}

		/// <summary>
		/// Добавить расширение (до инициализации)
		/// </summary>
		public void AddExtension(Extension Extension)
		{
			Extensions.Add(Extension);
		}

		/// <summary>
		/// Получить очередной номер транзакции
		/// </summary>
		/// <returns>номер транзакции</returns>
		public ushort GetNextTid()
		{
			Tid++;

			return Tid;
		}

		/// <summary>
		/// Конструктор
		/// </summary>
		public Core(Net N, Guid Uuid, string Name)
		{
			Net = N;
			Net?.RegisterCore(this);

			this.Uuid = Uuid;
			this.Name = Name;

			Timer.Elapsed += Timer_Elapsed;
		}

		/// <summary>
		/// Инициализация
		/// </summary>
		/// <returns>true, если инициализировано</returns>
		public bool Init()
		{
			for(int i = 0; i < 100; i++)
			{
				int port = Config.ADLPDS_DEFAULT_PORT - i;
				if(Net.BindPort(port))
				{
					LocalPort = port;
					break;
				}
			}

			// Если не удалось привязать...
			if (LocalPort == 0)
				return false;

			// Регистрируем ядра модулей и расширений
			Array.ForEach(Modules.ToArray(), M => M.RegisterCore(this));
			Array.ForEach(Extensions.ToArray(), E => E.RegisterCore(this));

			if (OnInit())
			{
				Timer.Start();
				Running = true;

				MainThread = new Thread(() => { while (Running) { Main(); Thread.Sleep(0); } });
				MainThread.Start();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Освобождение ресурсов
		/// </summary>
		public void Uninit()
		{
			if (Running)
			{
				Running = false;
				MainThread.Join();

				OnUninit();

				Timer.Stop();
			}

			Net.UnbindPort();
		}

		// Задать требование конфигурации
		public void SetConfigurationRequest(bool Value)
		{
			ConfigurationRequest = Value;
		}

		// Задать требование обновления ПО
		public void SetFirmwareRequest(bool Value)
		{
			FirmwareRequest = Value;
		}

		/// <summary>
		/// Задать номер системы
		/// </summary>
		/// <param name="Value"></param>
		public void SetSystemId(uint Value)
		{
			SystemId = Value;
		}

		/// <summary>
		/// Установить, что блок инициализирован
		/// </summary>
		/// <param name="Inited"></param>
		public void SetInited(bool Inited)
		{
			this.Inited = Inited;
		}

		/// <summary>
		/// Задать номер блока
		/// </summary>
		/// <param name="Block">номер блока</param>
		public void SetBlock(int Block)
		{
			if (this.Block != Block)
			{
				this.Block = Convert.ToByte(Block);
			}
		}

		/// <summary>
		/// Сообщить об изменении статуса
		/// </summary>
		public void UpdateStatus()
		{
			StatusChanged = true;
		}

		public int SendGetStatusRequest(IPEndPoint EP = null)
		{
			var request = new GetStatusRequest();

			request.Types.AddRange(StatusPollingTypes);

			if(EP == null)
			{
				EP = new IPEndPoint(
					IPAddress.Any,
					Config.ADLPDS_DEFAULT_PORT);
			}

			return SendRequest(
				EP,
				PacketCode.COMMAND_GET_STATUS,
				request.getPayload());
		}

		/// <summary>
		/// Отправка запроса
		/// </summary>
		/// <returns>Номер транзакции или -1 в случае ошибки</returns>
		public int SendRequest(IPEndPoint EP, PacketCode Code, byte[] Data)
		{
			int Tid = GetNextTid();
			var P = new BasePacket(Convert.ToByte(Code), Data);
			P.Tid = Tid;

			if (Send(EP, P.getPacket()))
				return Tid;
			else
				return -1;
		}

		/// <summary>
		/// Отправка ответа
		/// </summary>
		/// <returns>true, если успешно</returns>
		public bool SendResponse(SenderInfo Info, PacketCode Code, byte[] Data)
		{
			var P = new BasePacket(Convert.ToByte(Code), Data);
			P.Tid = Info.Tid;
			P.Flags |= HeaderFlags.ADLPDS_HEADER_FLAG_RESPONSE;

			return Send(Info.Endpoint, P.getPacket());
		}

		/// <summary>
		/// Отправка пакета UDP
		/// </summary>
		/// <param name="EP">Куда отправить (IP если 0, то широковещательный)</param>
		/// <param name="Data">Данные</param>
		private bool Send(IPEndPoint EP, byte[] Data)
		{
			if ((Net != null) && Net.IsBinded())
			{
				if (EP.Address != IPAddress.Any)
					return Net.Send(EP.Address, EP.Port, Data);
				else
					return Net.Send(EP.Port, Data);
			}
			else
				return false;
		}

		/// <summary>
		/// Основная функция обработки всех событий
		/// </summary>
		private void Main()
		{
			ProcessPackets();
			Process();
			UpdateDeviceTable();

			if(StatusPolling &&
				Milliseconds - LastStatusRequestTime >= StatusPollingPeriod)
			{
				LastStatusRequestTime = Milliseconds;
				SendGetStatusRequest();
			}

			if (IsProxied)
				ProcessProxyStatus();

			if (Inited || ConfigurationRequest)
			{
				SendStatus();
			}

			if(ConfigurationChanged)
			{
				ConfigurationChanged = false;
				OnConfigurationChanged();
			}
			
			Thread.Sleep(1);
		}

		/// <summary>
		/// Очередной тик таймера
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			Milliseconds += TIMER_INTERVAL*2;
		}

		/// <summary>
		/// Приём пакета UDP
		/// </summary>
		/// <param name="ep">Информация об отправителе</param>
		/// <param name="Data">Данные</param>
		/// <returns></returns>
		public bool ReceiveUDP(IPEndPoint ep, byte[] Data)
		{
			try
			{
				PacketsQueue.Enqueue(new InputPacket(ep, Data));
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"ReceiveUDP: {ex}");
			}
			return true;
		}

		/// <summary>
		/// Обработка принятых пакетов
		/// </summary>
		private void ProcessPackets()
		{
			if(PacketsQueue.Count > 0)
			{
				var P = PacketsQueue.Dequeue();

				if(P != null)   
					Receive(P.EndPoint, P.Data);
			}
		}

		/// <summary>
		/// Приём пакета UDP
		/// </summary>
		/// <param name="ep">Информация об отправителе</param>
		/// <param name="Data">Данные</param>
		/// <returns></returns>
		public virtual bool Receive(IPEndPoint ep, byte[] Data)
		{
			if (Data.Length < BasePacket.HeaderSize)
			{
				Debug.WriteLine($"Packet size is too short: {Data.Length} (min: {Packet.BasePacket.HeaderSize})");
				return false;
			}

			var P = new BasePacket(Data);
			if (P.Size + BasePacket.HeaderSize != Data.Length)
			{
				Debug.WriteLine($"Packet size {Data.Length} is not equal declared packet size {Packet.BasePacket.HeaderSize + P.Size}");
				return false;
			}

			if (P.Correct)
			{
				var Info = new SenderInfo(ep, P.Tid);

				return OnPacket(Info, P);
			}
			else
			{
				Debug.WriteLine($"Invalid packet data");
				return false;
			}
		}

		/// <summary>
		/// Отправка запросов прокси
		/// </summary>
		private void ProcessProxyStatus()
		{
			long Diff = Milliseconds - LastProxyStatusSendedTime;

			if (Diff > Config.PROXY_UPDATE_PERIOD)
			{
				LastProxyStatusSendedTime = Milliseconds;

				// Отправить запрос на проксирование
				SendProxyRequest();
			}

			Diff = Milliseconds - LastProxyConfirmedTime;
			if(Diff > Config.DEVICE_TIMEOUT)
			{
				LastProxyConfirmedTime = 0;
				SetProxyConnectionStatus(false);
			}
		}

		/// <summary>
		/// Получить адрес прокси
		/// </summary>
		/// <returns></returns>
		protected virtual IPEndPoint ProxyAddress => new IPEndPoint(Net.IP, Config.ADLPDS_DEFAULT_PORT);

		/// <summary>
		/// Прокси подключено?..
		/// </summary>
		private bool ProxyConnected = false;

		/// <summary>
		/// Последний раз подтверждено проксирование
		/// </summary>
		private long LastProxyConfirmedTime = 0;

		/// <summary>
		/// Отправить запрос на проксирование
		/// </summary>
		private void SendProxyRequest()
		{
			var Cmd = ProxyConnected ? ProxyCommand.PROXY_COMMAND_UPDATE : ProxyCommand.PROXY_COMMAND_ADD;
			var Req = new ProxyRequest(Cmd);

			SendRequest(ProxyAddress, PacketCode.COMMAND_PROXY_CMD, Req.getPacket());
		}

		/// <summary>
		/// Установить статус проксирования
		/// </summary>
		/// <param name="Status"></param>
		private void SetProxyConnectionStatus(bool Status)
		{
			if (ProxyConnected != Status)
			{
				ProxyConnected = Status;
				// TODO: onProxyConnected
				// TODO: onProxyDisconnected
			}
		}

		/// <summary>
		/// Обновить таблицу устройств
		/// </summary>
		private void UpdateDeviceTable()
		{
			long Diff = Milliseconds - LastDeviceUpdatedTableTime;

			if (Diff > Config.DEVICE_TABLE_UPDATE_PERIOD)
			{
				LastDeviceUpdatedTableTime = Milliseconds;

				if (Devices.Update(Milliseconds))
				{
					OnDeviceTableUpdated();
				}
			}
		}

		/// <summary>
		/// Отправка статуса
		/// </summary>
		private void SendStatus()
		{
			long Diff = Milliseconds - LastStatusSendedTime;

			if ((Diff > Config.STATUS_SENDING_PERIOD) || StatusChanged)
			{
				LastStatusSendedTime = Milliseconds;
				StatusChanged = false;

				var Status = GetStatus();
				SendRequest(new IPEndPoint(IPAddress.Any, Config.ADLPDS_DEFAULT_PORT), PacketCode.COMMAND_STATUS, Status.getPacket());
			}
		}

		/// <summary>
		/// Получить информацию об устройстве по IP и порту
		/// </summary>
		/// <param name="info"></param>
		/// <returns></returns>
		public Device GetDeviceByInfo(SenderInfo info)
		{
			return Devices.GetDevice(info.Endpoint);
		}

		/// <summary>
		/// Получить информацию об устройстве по названию и номеру блока
		/// </summary>
		/// <param name="name">Название блока</param>
		/// <param name="block">Блок</param>
		/// <returns></returns>
		public Device GetDeviceByName(string name, int block)
		{
			return Devices.GetDevice(name, block);
		}

		/// <summary>
		/// Обработка проксированных данных
		/// </summary>
		/// <param name="Info">Информация об отправителе</param>
		/// <param name="Status">Пакет с данными</param>
		/// <returns></returns>
		private bool ProcessProxiedData(SenderInfo Info, ProxiedData Data)
		{
			// TODO: В SenderInfo упоминать прокси, через который работает устройство
			var P = Data.Payload;
			var ProxySenderInfo = new SenderInfo(Data.From, P.Tid);

			return OnPacket(ProxySenderInfo, P);
		}

		/// <summary>
		/// Обработаем получение параметров
		/// </summary>
		/// <param name="Info"></param>
		/// <param name="Id"></param>
		/// <returns></returns>
		protected virtual ParamDataValue ProcessGetParamField(ParamDataId Id)
		{
			if (Id.ObjectType == ObjectCode.PARAM_OBJECT_DEVICE)
			{
				switch (Id.Param)
				{
					case ParamCode.DeviceConfiguration:
						return new ParamDataValue(Convert.ToByte(ConfigurationRequest ? 1 : 0));
				}
			}

			foreach (var Module in Modules)
			{
				var Res = Module.ProcessGetParamField(Id);
				if(Res != null)
					return Res;
			}

			foreach (var Extension in Extensions)
			{
				var Res = Extension.ProcessGetParamField(Id);
				if (Res != null)
					return Res;
			}

			return null;
		}

		/// <summary>
		/// Обработаем установку параметров
		/// </summary>
		/// <param name="Info"></param>
		/// <param name="Id"></param>
		/// <param name="Value"></param>
		/// <returns></returns>
		protected virtual bool ProcessSetParamField(ParamDataId Id, ParamDataValue Value)
		{
			if (Id.ObjectType == ObjectCode.PARAM_OBJECT_DEVICE)
			{
				switch (Id.Param)
				{
					case ParamCode.DeviceConfiguration:
						{
							var Req = Value.GetU8Value() != 0;
							ConfigurationRequest = Req;

							if (!Req)
								ConfigurationChanged = true;
						}
						return true;
				}
			}

			foreach(var Module in Modules)
			{
				if (Module.ProcessSetParamField(Id, Value))
					return true;
			}

			foreach (var Extension in Extensions)
			{
				if (Extension.ProcessSetParamField(Id, Value))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Обработаем установку параметров
		/// </summary>
		/// <param name="Info"></param>
		/// <param name="Params"></param>
		/// <returns></returns>
		private bool ProcessSetParam(SenderInfo Info, ParamDataList Params)
		{
			List<ParamDataField> Result = new List<ParamDataField>();
			var Fields = Params.Fields;

			foreach(var F in Fields)
			{
				var Id = F.Id;
				var Value = F.Value;

				// TODO: проверка типов
				bool Res = ProcessSetParamField(Id, Value);
				// Сформировать ответ
				var NewValue = ProcessGetParamField(Id);

				if(NewValue != null)
					Result.Add(new ParamDataField(Id, NewValue));
			}

			var Req = new ParamDataList(Result.ToArray());
			return SendResponse(Info, PacketCode.COMMAND_SET_PARAM, Req.getPacket());
		}

/// <summary>
		/// Обработаем получение параметров
		/// </summary>
		/// <param name="Info"></param>
		/// <param name="Params"></param>
		/// <returns></returns>
		private bool ProcessGetParam(SenderInfo Info, ParamDataRequestList Params)
		{
			List<ParamDataField> Result = new List<ParamDataField>();

			foreach (var Id in Params.Fields)
			{
				var Value = ProcessGetParamField(Id);
				if (Value != null)
					Result.Add(new ParamDataField(Id, Value));
			}

			var Req = new ParamDataList(Result.ToArray());
			return SendResponse(Info, PacketCode.COMMAND_GET_PARAM, Req.getPacket());
		}

		/// <summary>
		/// Обработка запроса на проксирование
		/// </summary>
		/// <returns></returns>
		private bool ProcessProxyCommandResponse(SenderInfo Info, ProxyResponse Response)
		{
			if(Response.Result.HasFlag(ProxyStatus.PROXY_STATUS_ACTIVE))
			{
				LastProxyConfirmedTime = Milliseconds;
				SetProxyConnectionStatus(true);
			}
			else
			{
				SetProxyConnectionStatus(false);
			}

			return true;
		}

		/// <summary>
		/// Обработка поля статуса
		/// </summary>
		/// <param name="Info">Информация об отправителе</param>
		/// <param name="Status">пакет с данными</param>
		/// <returns></returns>
		private bool ProcessStatus(SenderInfo Info, Packet.Status.Status Status)
		{
			if (IsDeviceInteresting(Info, Status))
			{
                var D = Devices.GetDevice(Info.Endpoint, Status.Uuid, Status.Block);
                if (D != null)
				{
					var Changed = D.UpdateDevice(Status);
					D.MarkActive(Milliseconds);

					// Устройство само перечислило себя под другим типом/идентификатором — обновим таблицу
					if (Changed)
						OnDeviceTableUpdated();
				}
				else
				{
					D = new Device(Info.Endpoint, Status, Milliseconds);
					Devices.AddDevice(D);

					OnDeviceTableUpdated();
				}

				ProcessDeviceStatus(Info, Status);
				foreach (var F in Status.Fields)
				{
					ProcessDeviceStatus(Info, F);
				}
			}

			return true;
		}

		/// <summary>
		/// Обработать основное сообщение статуса
		/// </summary>
		/// <param name="Info">Информация об отправителе</param>
		/// <param name="Status">Данные поля статуса</param>
		/// <returns></returns>
		protected virtual bool ProcessDeviceStatus(SenderInfo Info, Packet.Status.Status Status)
		{
			foreach (var Module in Modules)
				Module.ProcessDeviceStatus(Info, Status);

			return true;
		}

		/// <summary>
		/// Обработать поле статуса
		/// </summary>
		/// <param name="Info">Информация об отправителе</param>
		/// <param name="Field">Данные поля статуса</param>
		/// <returns></returns>
		protected virtual bool ProcessDeviceStatus(SenderInfo Info, Packet.Status.StatusField Field)
		{
			foreach (var Module in Modules)
				Module.ProcessDeviceStatus(Info, Field);

			return true;
		}

		//
		// Блок событий
		//

		/// <summary>
		/// Последний этап инициализации [событие для потомков]
		/// </summary>
		/// <returns>true, если инициализировано</returns>
		protected virtual bool OnInit()
		{
			BlockCapability Capabilities = 0;
			foreach (var Module in Modules)
			{
				if (!Module.OnInit())
					return false;

				Capabilities |= Module.GetCapabilities();
			}
			foreach (var Extension in Extensions)
			{
				if (!Extension.OnInit())
					return false;
			}

			Capability = Capabilities;

			return true;
		}

		/// <summary>
		/// Первый этап освобождения ресурсов [событие для потомков]
		/// </summary>
		protected virtual void OnUninit()
		{
			foreach (var Module in Modules)
				Module.OnUninit();

			foreach (var Extension in Extensions)
				Extension.OnUninit();
		}

		/// <summary>
		/// Конфигурация была изменена [событие для потомков]
		/// </summary>
		protected virtual void OnConfigurationChanged()
		{
			foreach (var Module in Modules)
				Module.OnConfigurationChanged();
		}

		/// <summary>
		/// Изменилась таблица устройств
		/// </summary>
		protected virtual void OnDeviceTableUpdated()
		{

		}

		/// <summary>
		/// Получить номер канала в системе ADLDC
		/// </summary>
		/// <param name="Local">Номер канала (1-channels)</param>
		/// <returns>номер канала в системе ADLDC</returns>
		public virtual int GetChannelIndex(int Local)
		{
			return 0;
		}

		/// <summary>
		/// Получить номер локального канала
		/// </summary>
		/// <param name="Channel">номер канала в системе ADLDC</param>
		/// <returns></returns>
		public virtual int GetLocalChannelIndex(int Channel)
		{
			return 0;
		}

		/// <summary>
		/// Обработка пакета [событие для потомков]
		/// </summary>
		/// <param name="Info">Информация об отправителе</param>
		/// <param name="Packet">Пакет</param>
		/// <returns>true, если обработано</returns>
		protected virtual bool OnPacket(SenderInfo Info, BasePacket Packet)
		{
			foreach(var Module in Modules)
			{
				if (Module.OnPacket(Info, Packet))
					return true;
			}

			bool Response = Packet.Flags.HasFlag(HeaderFlags.ADLPDS_HEADER_FLAG_RESPONSE);
			if (!Response)
			{
				switch (Packet.Command)
				{
					case PacketCode.COMMAND_STATUS: return ProcessStatus(Info, new Packet.Status.Status(Packet.Payload));
					case PacketCode.COMMAND_PROXY_DATA: return ProcessProxiedData(Info, new ProxiedData(Packet.Payload));
					case PacketCode.COMMAND_SET_PARAM: return ProcessSetParam(Info, new ParamDataList(Packet.Payload));
					case PacketCode.COMMAND_GET_PARAM: return ProcessGetParam(Info, new ParamDataRequestList(Packet.Payload));

					default: break;
				}
			}
            else
            {
                switch(Packet.Command)
                {
                    case PacketCode.COMMAND_PROXY_CMD:
                        return ProcessProxyCommandResponse(
                            Info, new ProxyResponse(Packet.Payload));

                    case PacketCode.COMMAND_STATUS:
                        return ProcessStatus(
                            Info,
                            new Packet.Status.Status(Packet.Payload));

                    default:
                        break;
                }
            }

            foreach (var Extension in Extensions)
			{
				if (Extension.OnPacket(Info, Packet))
					return true;
			}
			
			return false;
		}

		/// <summary>
		/// Является ли устройство интересным для нас
		/// </summary>
		/// <param name="Info">Информация об отправителе</param>
		/// <param name="Status">Статус</param>
		/// <returns></returns>
		protected virtual bool IsDeviceInteresting(SenderInfo Info, Status Status)
		{
			foreach (var Module in Modules)
			{
				if (Module.IsDeviceInteresting(Info, Status))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Обработка событий [событие для потомков]
		/// </summary>
		protected virtual void Process()
		{
			foreach (var Module in Modules)
				Module.Process();

			foreach (var Extension in Extensions)
				Extension.Process();
		}

		/// <summary>
		/// Получить поля статуса
		/// </summary>
		/// <returns></returns>
		protected virtual List<StatusField> GetStatusFields()
		{
			var Fields = new List<StatusField>();
			foreach(var Module in Modules)
			{
				var ModuleFields = Module.GetStatusFields();
				Fields.AddRange(ModuleFields);
			}

			return Fields;
		}

		/// <summary>
		/// Получить пакет статуса
		/// </summary>
		/// <returns></returns>
		protected virtual Status GetStatus()
		{
			var Fields = GetStatusFields();
			Status.StatusFlags Flags = 0;
			if (ConfigurationRequest)
				Flags |= Status.StatusFlags.STATUS_FLAG_CONFIGURATION_REQUEST;
			if (FirmwareRequest)
				Flags |= Status.StatusFlags.STATUS_FLAG_FIRMWARE_REQUEST;

			return new Status(Uuid, Name, Capability, SystemId, LocalPort, Block, Flags, Fields.ToArray());
		}

		/// <summary>
		/// Получить состояние канала
		/// </summary>
		/// <param name="Local"></param>
		/// <returns></returns>
		public virtual ChannelState GetChannel(int Local)
		{
			return null;
		}

		/// <summary>
		/// Обработка потока
		/// </summary>
		/// <param name="Id"></param>
		/// <param name="Data"></param>
		public virtual void ProcessStream(StreamId Id, byte[] Data)
		{
			foreach (var Module in Modules)
			{
				Module.ProcessStream(Id, Data);
			}
		}

		private class InputPacket
		{
			/// <summary>
			/// Конечная точка
			/// </summary>
			public readonly IPEndPoint EndPoint;

			/// <summary>
			/// Данные
			/// </summary>
			public readonly byte[] Data;

			/// <summary>
			/// Конструктор
			/// </summary>
			/// <param name="EndPoint"></param>
			/// <param name="Data"></param>
			public InputPacket(IPEndPoint EndPoint, byte[] Data)
			{
				this.EndPoint = EndPoint;
				this.Data = Data;
			}
		}

	}
}
