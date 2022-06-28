using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using iFlow.Interfaces;

namespace iFlow.DataProtocols
{
	internal partial class ModbusClient : IDisposable
	{
		public ModbusClient(string address, ushort port, byte unitId) : base()
		{
			Address = address;
			Port = port;
			UnitId = unitId;
		}

		public void Dispose()
		{
			lock (SocketLock)
			{
				Socket?.Close();
				Socket = null;
			}

			AsyncMutex.Close();
			//CancellationTokenSource.Cancel();
			//CancellationTokenSource.Dispose();
		}

		private readonly string Address;
		private readonly ushort Port;
		private readonly byte UnitId;

		private Socket Socket;
		private readonly Mutex AsyncMutex = new Mutex();
		private readonly object SocketLock = new object();
		protected readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

		private void ValidateConfig(Config.DataSourceConfig config)
		{
			if (string.IsNullOrWhiteSpace(config.Address))
				throw new Exception($"Не указан адрес Modbus-сервера");
			if (config.UnitId == 0)
				throw new Exception($"Не указан UnitId");
		}

		private bool WaitEvent(AbstractAsyncInfo asyncInfo, TimeSpan timeout, string timeoutMessage)
		{
			WaitHandle[] waitHandles = new WaitHandle[]
			{
				asyncInfo.Event,
				CancellationTokenSource.Token.WaitHandle
			};
			switch (WaitHandle.WaitAny(waitHandles, timeout))
			{
				// Сработало событие asyncInfo.Event. Закончили операцию соединения/передачи/получения. Не факт, что успешно.
				case 0:
					// Если  произошла ошибка в Callback-методе.
					if (asyncInfo.Exception != null)
					{
						Disconnect();
						throw asyncInfo.Exception;
					}
					// Если уничтожили сокет, значит приложение закрывается, поэтому отменяем обмен данными, возвращаем false.
					// Иначе, возвращаем true.
					lock (SocketLock)
						return Socket != null;
				// Сработало событие CancellationTokenSource.Token.WaitHandle. Закрыли сокет. Операция отменена.
				case 1:
					Disconnect();
					return false;
				case WaitHandle.WaitTimeout:
					Disconnect();
					throw new Exception(timeoutMessage);
				default:
					throw new Exception("Неизвестный результат выполнения WaitHandle.WaitAny");
			}
		}

		private bool Connect()
		{
			AbstractAsyncInfo asyncInfo = null;
			try
			{
				lock (SocketLock)
				{
					if (Socket != null)
					{
						if (Socket.Connected)
							return true;
					}
					else
					{
						Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
						Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, 1);
					}

					asyncInfo = new ConnectAsyncInfo(Socket);
					// Используем BeginConnect, чтобы установить тайм-аут соединения. Обычный Connect не поддерживает тайм-ауты.
					Socket.BeginConnect(Address, Port, ConnectCallback, asyncInfo);
				}

				if (!WaitEvent(asyncInfo, Params.Socket.ConnectTimeout, "Таймаут соединения"))
					return false;
			}
			finally
			{
				asyncInfo?.Dispose();
			}

			lock (SocketLock)
			{
				if (Socket == null)
					return false;
				if (Socket.Connected)
				{
					//tmrDisconnect.Stop();
					//tmrDisconnect.Start();
				}
				else
				{
					Disconnect();
					return false;
				}
			}
			return true;
		}

		private void ConnectCallback(IAsyncResult asyncResult)
		{
			ConnectAsyncInfo connectInfo = (ConnectAsyncInfo)asyncResult.AsyncState;
			try
			{
				lock (SocketLock)
				{
					// Проверяем не закрыли ли уже сокет.
					if (connectInfo.Socket.GetCleanedUp())
						return;
					connectInfo.Socket.EndConnect(asyncResult);
				}
			}
			catch (Exception ex)
			{
				connectInfo.Exception = ex;
			}
			finally
			{
				connectInfo.Event.Set();
			}
		}

		private void Disconnect()
		{
			//tmrDisconnect.Stop();
			lock (SocketLock)
			{
				Socket?.Close();
				Socket = null;
			}
		}

		/// <summary>
		/// Запросить значение тэгов. Если запрошенные данные будут успешно получены, функция вернет список значений. 
		/// Если во время запроса будет закрыто соединение (например, получена команда на закрытие приложения), 
		/// функция вернет null. Если во время выполнения запроса произойдет ошибка, то будет создано исключение.
		/// </summary>
		/// <param name="tagIds">Список идентификаторов тэгов, чьи значения необходимо получить. Идентификаторы 
		/// должны быть предварительно описаны в тэг-маппинге.</param>
		/// <returns></returns>
		public ReadValue[] Read(ModbusDataTag[] tags)
		{
			try
			{
				// Блокируем все параллельные запросы через данное соединение
				AsyncMutex.WaitOne();

				if (!Connect())
					return null;

				Request request = PrepareRequest(tags);
				if (!Send(request.GetData(UnitId)))
					return null;

				Response response = new Response();
				if (!Receive(request.Packets.Length, response))
					return null;

				if (response.Packets.Count != request.Packets.Length)
					throw new Exception($"Количество полученных пакетов - {response.Packets.Count}, " +
						$"не соответствует количеству пакетов в запросе - {request.Packets.Length}");

				return ParseResponse(request, response);
			}
			catch (ObjectDisposedException)
			{
				// Уничтожили объект. Скорее всего, в процесе выгрузки приложения.
				return null;
			}
			finally
			{
				if (!AsyncMutex.SafeWaitHandle.IsClosed)
					AsyncMutex.ReleaseMutex();
			}
		}

		private bool Send(byte[] data)
		{
			using (RequestAsyncInfo sendInfo = new RequestAsyncInfo(Socket, data.Length))
			{
				lock (SocketLock)
				{
					if (Socket == null)
						return false;
					Socket.BeginSend(data, 0, data.Length, SocketFlags.None, SendCallback, sendInfo);
					//tmrDisconnect.Stop();
					//tmrDisconnect.Start();
				}
				WaitEvent(sendInfo, Params.Socket.SendTimeout, "Таймаут передачи данных");
			}
			return true;
		}

		private void SendCallback(IAsyncResult asyncResult)
		{
			RequestAsyncInfo sendInfo = (RequestAsyncInfo)asyncResult.AsyncState;
			try
			{
				int sentCount = 0;

				lock (SocketLock)
				{
					// Проверяем не закрыли ли уже сокет.
					if (sendInfo.Socket.GetCleanedUp())
						return;
					sentCount = sendInfo.Socket.EndSend(asyncResult);
				}
				if (sentCount != sendInfo.ByteCount)
					throw new Exception($"Ошибка передачи данных: количество переданных данных - {sentCount}, не соответствует запросу - {sendInfo.ByteCount}");
			}
			catch (Exception ex)
			{
				sendInfo.Exception = ex;
			}
			finally
			{
				sendInfo.Event.Set();
			}
		}

		private bool Receive(int packetCount, Response response)
		{
			using (ResponseAsyncInfo receiveInfo = new ResponseAsyncInfo(Socket, packetCount, response))
			{
				lock (SocketLock)
				{
					if (Socket == null)
						return false;
					Socket.BeginReceive(receiveInfo.ReceiveBuffer, 0, receiveInfo.ReceiveBuffer.Length, SocketFlags.None, ReceiveCallback, receiveInfo);
				}
				return WaitEvent(receiveInfo, Params.Socket.ReceiveTimeout, "Таймаут получения данных");
			}
		}

		private void ReceiveCallback(IAsyncResult asyncResult)
		{
			ResponseAsyncInfo receiveInfo = (ResponseAsyncInfo)asyncResult.AsyncState;
			try
			{
				int receivedCount = 0;
				lock (SocketLock)
				{
					// Проверяем не закрыли ли уже сокет.
					if (receiveInfo.Socket.GetCleanedUp())
						return;
					receivedCount = receiveInfo.Socket.EndReceive(asyncResult);
				}
				int offset = receiveInfo.ParseBuffer.Length;
				Array.Resize(ref receiveInfo.ParseBuffer, offset + receivedCount);
				Array.Copy(receiveInfo.ReceiveBuffer, 0, receiveInfo.ParseBuffer, offset, receivedCount);

				AbstractResponsePacket lastPacket = receiveInfo.Response.Packets.LastOrDefault();
				while (true)
				{
					ushort regAddress;
					int packetLength;
					int dataOffset;
					int dataLength;
					byte errorCode;
					string error;

					int packetOffset = lastPacket != null ? lastPacket.Offset + lastPacket.Length : 0;
					if (!Modbus.Utils.DecodeResponsePacket(receiveInfo.ParseBuffer, packetOffset, out regAddress,
							out packetLength, out dataOffset, out dataLength, out errorCode, out error))
						break;

					if (error == null)
						lastPacket = new ResponseDataPacket()
						{
							RegAddress = regAddress,
							Offset = packetOffset,
							Length = packetLength,
							Data = receiveInfo.ParseBuffer,
							DataOffset = dataOffset,
							DataLength = dataLength
						};
					else
						lastPacket = new ResponseErrorPacket()
						{
							RegAddress = regAddress,
							Offset = packetOffset,
							Length = packetLength,
							ErrorCode = errorCode,
							Error = error
						};
					receiveInfo.Response.Packets.Add(lastPacket);
				}
				if (receiveInfo.Response.Packets.Count == receiveInfo.RequestPacketCount)
					receiveInfo.Event.Set();
				else
					Socket.BeginReceive(receiveInfo.ReceiveBuffer, 0, receiveInfo.ReceiveBuffer.Length, SocketFlags.None, ReceiveCallback, receiveInfo);
			}
			catch (Exception ex)
			{
				receiveInfo.Exception = ex;
				receiveInfo.Event.Set();
			}
		}

		/// <summary>
		/// Метод по Id запрашиваемых тэгов выбирает элементы маппинга, по маппингам вычисляет адресные пространства
		/// Modbus-регистров, данные которых необходимо получить, объединяет адресные пространства регистров в области,
		/// примыкающие друг к другу без зазоров (чтобы в одном пакете получить данные нескольких тэгов), затем разбивает
		/// полученне адресные пространства на пакеты размером по 125 двухбайтовых регистров (ограничение протокола
		/// Modbus, в одном пакете можно получить данные максимум 125 регистров).
		/// </summary>
		/// <param name="tagIds"></param>
		/// <returns></returns>
		private Request PrepareRequest(ModbusTagMapping[] tags)
		{
			Request request = new Request()
			{
				// Сортируем в порядке возрастания адресов, чтобы скомбинировать тэги, чьи адресные пространства примыкают без зазоров.
				TagMappings = tags.OrderBy(x => x.ByteAddress).ToArray()
			};

			// Список пакетов запроса
			List<RequestPacket> packets = new List<RequestPacket>();

			// Функция создания пакетов
			Action<ushort, ushort> createPackets = new Action<ushort, ushort>((regAddress, regLength) =>
			{
				// Нарезаем пакеты по 125 регистров (максимум).
				while (regLength > 0)
				{
					RequestPacket packet = new RequestPacket()
					{
						RegAddress = regAddress,
						RegLength = Math.Min(regLength, Params.MaxAnalogRegisterCountInPacket)
					};
					packets.Add(packet);

					regAddress += Params.MaxAnalogRegisterCountInPacket;
					regLength -= Math.Min(regLength, Params.MaxAnalogRegisterCountInPacket);
				}
			});

			ushort packetStart = 0;
			ushort packetLength = 0;
			for (int i = 0; i < request.TagMappings.Length; i++)
			{
				ModbusTagMapping currentMapping = request.TagMappings[i];

				// Проверка адресного пространства и соответствия типу
				byte functionCode = Modbus.Utils.GetFunctionCode(currentMapping.RegAddress, currentMapping.RegLength);
				if (functionCode == 1 || functionCode == 2)
					if (currentMapping.Type != typeof(bool))
						throw new Exception($"Попытка получить {currentMapping} в диапазоне [1-9999, 10001-19999] дискретных данных.");

				// Если это первый тэг
				if (i == 0)
				{
					packetStart = currentMapping.RegAddress;
					packetLength = currentMapping.RegLength;
				}
				// Если между адресными пространствами предыдущего и текущего тэгов есть зазор, то заканчиваем
				// формирование очередного пакета.
				else if (currentMapping.RegAddress != packetStart + packetLength)
				{
					createPackets(packetStart, packetLength);
					packetStart = currentMapping.RegAddress;
					packetLength = currentMapping.RegLength;
				}
				// иначе увеличиваем адресное пространство тэгов, идущих без зазора.
				else
					packetLength += currentMapping.RegLength;
			}
			// Для данных последнего тэга (последних тэгов, если они примыкающие) условие RegisterAddress != packetStart + packetSize
			// не срабатывает. Поэтому, их добавляем отдельно.
			if (packetLength > 0)
				createPackets(packetStart, packetLength);

			request.Packets = packets.ToArray();
			return request;
		}

		private byte[] MergeResponsePackets(ResponseDataPacket[] dataPackets, int start)
		{
			// Отбираем смежные пакеты, начиная со start
			dataPackets = dataPackets
				.Where((x, i) => i < start ? false : (i == start ? true : dataPackets[i].RegAddress == dataPackets[i - 1].RegAddress + dataPackets[i - 1].RegLength))
				.ToArray();

			// Создаем буфер для размещения данных примыкающих пакетов.
			byte[] buffer = new byte[dataPackets.Sum(x => x.DataLength)];

			int offset = 0;
			foreach (ResponseDataPacket packet in dataPackets)
			{
				Array.Copy(packet.Data, packet.DataOffset, buffer, offset, packet.DataLength);
				offset += packet.DataLength;
			}
			return buffer;
		}

		private IEnumerable<ReadValue> ParseValues(Request request, byte[] data, int regAddress)
		{
			// Двухбайтовое значение
			DateTime now = DateTime.Now;
			int byteAddress = regAddress * 2;

			ModbusDataTag tmpMapping = request.Tags.FirstOrDefault(x => x.RegAddress == regAddress);
			if (tmpMapping == null)
				throw new Exception($"Данные, полученные от контроллера не соответствуют запросу.");
			int index = Array.IndexOf(request.Tags, tmpMapping);

			bool isDiscrete = new byte[] { 1, 2 }.Contains(Modbus.Utils.GetFunctionCode(tmpMapping.RegAddress, tmpMapping.RegLength));

			// Читаем данные тэгов
			for (int i = index; i < request.Tags.Length; i++)
			{
				ModbusDataTag tagMapping = request.Tags[i];

				if (isDiscrete)
				{
					if ((tagMapping.RegAddress - regAddress + 8) / 8 > data.Length)
					{
						//if ((request.TagMappings[i - 1].RegAddress - regAddress + 7) / 8 != data.Length)
						//	throw new Exception($"Данные, полученные от контроллера не соответствуют запросу.");
						yield break;
					}
				}
				else
				{
					if (tagMapping.ByteAddress > byteAddress + data.Length)
					{
						if (request.Tags[i - 1].ByteAddress + request.Tags[i - 1].ByteLength != byteAddress + data.Length)
							throw new Exception($"Данные, полученные от контроллера не соответствуют запросу.");
						yield break;
					}
					// Если длина очередного тэга выходит за пределы буфера
					if (tagMapping.ByteAddress + tagMapping.ByteLength > byteAddress + data.Length)
						throw new Exception($"Данные, полученные от контроллера меньше чем в запросе.");
				}

				yield return new ReadValue()
				{
					Id = tagMapping.Id,
					SystemTime = now,
					Value = isDiscrete
						? Modbus.Utils.DecodeDiscreteValue(data, tagMapping.RegAddress - regAddress, tagMapping)
						: Modbus.Utils.DecodeAnalogValue(data, tagMapping.ByteAddress - byteAddress, tagMapping),
					Quality = Quality.Good
				};
			}
		}

		private ReadValue[] ParseResponse(Request request, Response response)
		{
			// Если в ответе сервера есть хоть один пакет, содержащий ошибку, то создаем исключение с этой ошибкой.
			ResponseErrorPacket errorPacket = (ResponseErrorPacket)response.Packets.FirstOrDefault(x => x is ResponseErrorPacket);
			if (errorPacket != null)
			{
				RequestPacket requestPacket = request.Packets.FirstOrDefault(x => x.RegAddress == errorPacket.RegAddress);
				throw new Exception(errorPacket.Error + " " + (requestPacket != null ? requestPacket.ToString() : ""));
			}

			// Сортируем пакеты по возрастанию адресов.
			ResponseDataPacket[] dataPackets = response.Packets
				.OfType<ResponseDataPacket>()
				.OrderBy(x => x.RegAddress)
				.ToArray();

			// Случилось невероятное. Полученные области пересекаются.
			for (int i = 1; i < dataPackets.Length; i++)
				if (dataPackets[i - 1].RegAddress + dataPackets[i - 1].RegLength > dataPackets[i].RegAddress)
					throw new Exception($"В ответе области адресов пересекаются: {dataPackets[i - 1]} и {dataPackets[i]}");

			// Находим начальные индексы областей, в которых находятся смежные пакеты
			return Enumerable.Range(0, dataPackets.Length)
				.Where(i => i == 0 ? true : dataPackets[i].RegAddress != dataPackets[i - 1].RegAddress + dataPackets[i - 1].RegLength)
				.Select
				(
					x => new
					{
						Data = MergeResponsePackets(dataPackets, x),
						RegAddress = dataPackets[x].RegAddress
					}
				)
				.SelectMany(x => ParseValues(request, x.Data, x.RegAddress))
				.ToArray();
		}

	}

	internal static class SocketExt
	{
		/// <summary>
		/// Непубличное свойство CleanedUp == true, если сокет уже закрыт.
		/// </summary>
		/// <param name="socket"></param>
		/// <returns></returns>
		public static bool GetCleanedUp(this Socket socket)
		{
			PropertyInfo cleanedUpProperty = socket.GetType().GetProperty("CleanedUp", BindingFlags.Instance | BindingFlags.NonPublic);
			return (bool)cleanedUpProperty.GetValue(socket);
		}
	}

}
