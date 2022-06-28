using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using iFlow.Interfaces;
using iFlow.Utils;
using iFlow.Utils.Convert;

namespace iFlow.DataProtocols.Modbus
{
	//==============================================================================================================

	internal static class Utils
	{
		/// <summary>
		/// Получение кода функции по адресу регистра и одновременная проверка диапазона запрашиваемых адресов на корректность.
		/// </summary>
		/// <param name="regAbsoluteAddress"></param>
		/// <param name="regLength"></param>
		/// <param name="regRelativeAddress"></param>
		/// <returns></returns>
		public static byte GetFunctionCode(ushort regAbsoluteAddress, ushort regLength)
		{
			ushort regRelativeAddress;
			return GetFunctionCode(regAbsoluteAddress, regLength, out regRelativeAddress);
		}

		/// <summary>
		/// Получение кода функции и относительного адреса регистра по абсолютному адресу и одновременная проверка диапазона
		/// запрашиваемых адресов на корректность.
		/// </summary>
		/// <param name="regAbsoluteAddress"></param>
		/// <param name="regLength"></param>
		/// <param name="regRelativeAddress"></param>
		/// <returns></returns>
		public static byte GetFunctionCode(ushort regAbsoluteAddress, ushort regLength, out ushort regRelativeAddress)
		{
			if (00001 <= regAbsoluteAddress && regAbsoluteAddress + regLength - 1 <= 09999)
			{
				regRelativeAddress = (ushort)(regAbsoluteAddress - 00001);
				return 1;
			}
			if (10001 <= regAbsoluteAddress && regAbsoluteAddress + regLength - 1 <= 19999)
			{
				regRelativeAddress = (ushort)(regAbsoluteAddress - 10001);
				return 2;
			}
			if (30001 <= regAbsoluteAddress && regAbsoluteAddress + regLength - 1 <= 39999)
			{
				regRelativeAddress = (ushort)(regAbsoluteAddress - 30001);
				return 3;
			}
			if (40001 <= regAbsoluteAddress && regAbsoluteAddress + regLength - 1 <= 49999)
			{
				regRelativeAddress = (ushort)(regAbsoluteAddress - 40001);
				return 4;
			}
			throw new Exception($"Диапазон адресов {regAbsoluteAddress}-{regAbsoluteAddress + regLength - 1} в запросе выходит за рамки таблиц адресов.");
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct RequestPacketHeader
		{
			/// <summary>
			/// Id пакета. Мы пишем сюда адрес первого регистра в пакете, значение которого мы считываем.
			/// </summary>
			public ushort TransactionId;

			/// <summary>
			/// Всегда 0x0000, что соответствует протоколу Modbus.
			/// </summary>
			public ushort ProtocolId;

			/// <summary>
			/// Количество байт, которые идут следом (длина пакета без TransactionId, ProtocolId и Length).
			/// </summary>
			public ushort Length;

			/// <summary>
			/// Unit Identifier - идентификатор блока или адрес устройства. Повторяет значение UnitId пакета запроса.
			/// </summary>
			public byte UnitId;

			/// <summary>
			/// Код функции.
			/// </summary>
			public byte FunctionCode;

			/// <summary>
			/// Адрес первого регистра в пакете.
			/// </summary>
			public ushort RegisterAddress;

			/// <summary>
			/// Количество запрашиваемых регистров.
			/// </summary>
			public ushort RegisterCount;

			public static ushort CalcLength()
			{
				return (ushort)(Marshal.SizeOf<RequestPacketHeader>() - 3 * sizeof(ushort));
			}
		}

		/// <summary>
		/// Создание пакета на запрос аналоговых данных. 
		/// </summary>
		/// <param name="unitId">Modbus UnitId</param>
		/// <param name="tags">Список тэгов, чьи данные будут запрошены.</param>
		/// <returns></returns>
		public static byte[] EncodeRequestPacket(ushort transactionId, byte unitId, byte functionCode, ushort registerStart, ushort registerCount)
		{
			RequestPacketHeader request = new RequestPacketHeader()
			{
				TransactionId = transactionId,
				ProtocolId = 0,
				Length = RequestPacketHeader.CalcLength(),
				UnitId = unitId,
				FunctionCode = functionCode,
				RegisterAddress = registerStart,
				RegisterCount = registerCount
			};
			return request.ToBytes(Endianness.BigEndian);
		}

		//==============================================================================================================

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct ResponsePacketHeader
		{
			/// <summary>
			/// Id пакета. Повторяет значение TransactionId пакета запроса.
			/// </summary>
			public ushort TransactionId;

			/// <summary>
			/// Всегда 0x0000, что соответствует протоколу Modbus.
			/// </summary>
			public ushort ProtocolId;

			/// <summary>
			/// Длина пакета в байтах.
			/// </summary>
			public ushort Length;

			/// <summary>
			/// Unit Identifier - идентификатор блока или адрес устройства. Повторяет значение UnitId пакета запроса.
			/// </summary>
			public byte UnitId;

			/// <summary>
			/// Код функции. Повторяет значение FunctionCode пакета запроса, либо дополняется старшим битом 1, что 
			/// означает ошибку в ответе.
			/// </summary>
			public byte FunctionCode;

			/// <summary>
			/// Количество байт далее в пакете, либо код ошибки.
			/// </summary>
			public byte ValuesLength;
		}

		/// <summary>
		/// Попытка декодировать Modbus-пакет (MBAP Header+PDU) в ответе сервера. Поскольку данные от сервера приходят
		/// в несколько заходов (для чтения данных из сокета несколько раз вызывается метод BeginReceive), Modbus-пакет
		/// может попасть на границу между двумя считываниями. Поэтому, если смогли прочесть пакет целиком, возвращаем
		/// true, иначе возвращаем false. Если получили некорректные данные, возвращаем ошибку в параметре error.
		/// Исключение не создаем, чтобы не прервать процесс чтения данных из сокета.
		/// </summary>
		/// <param name="data">Буфер с данными, полученными от сервера.</param>
		/// <param name="offset">Смещение в буфере, начиная с которого расположен пакет.</param>
		/// <param name="registerAddress">Адрес первого Modbus-регистра в пакете.</param>
		/// <param name="registerCount">Количество Modbus-регистров, переданных в пакете.</param>
		/// <param name="segment">Сегмент буфера, содержащий данные пакета (PDU-часть).</param>
		/// <param name="error">Текст ошибки, если сервер вернул ошибку.</param>
		/// <returns>true, если полностью считали пакет. Иначе false.</returns>
		public static bool DecodeResponsePacket(byte[] data, int packetOffset, out ushort firstRegisterAddress,
			out int packetLength, out int bodyOffset, out int bodyLength, out byte errorCode, out string error)
		{
			const string sIncorrect = "Ошибка разбора полученного пакета данных: ";

			firstRegisterAddress = 0;
			packetLength = 0;
			bodyOffset = 0;
			bodyLength = 0;
			errorCode = 0;
			error = null;

			int headerLength = Marshal.SizeOf<ResponsePacketHeader>();

			// Если размер буфера меньше размера MBAP-заголовка пакета, продолжаем принимать данные из сокета
			if (packetOffset + headerLength > data.Length)
				return false;

			// Читаем заголовок пакета
			ResponsePacketHeader packetHeader = data.ToStruct<ResponsePacketHeader>(packetOffset, Endianness.BigEndian);
			firstRegisterAddress = packetHeader.TransactionId;

			// Проверяем корректный ли код функции вернулся в ответе
			IEnumerable<byte> possibleFunctionCodes = Enum.GetValues(typeof(FunctionCode)).OfType<FunctionCode>().Select(x => (byte)x);
			if (!possibleFunctionCodes.Contains((byte)(packetHeader.FunctionCode & 0x7F)))
			{
				packetLength = headerLength;
				error = sIncorrect + $"некорректный код функции: {packetHeader.FunctionCode & 0x7F}";
				return true;
			}

			// Если старший бит установлен в 1, значит в пакете вернулась ошибка.
			if ((packetHeader.FunctionCode & 0x80) != 0)
			{
				packetLength = headerLength;

				errorCode = packetHeader.ValuesLength;
				DescriptionAttribute attribute = ReflectionHelper.GetEnumAttribute<DescriptionAttribute>((ErrorCode)errorCode);
				if (attribute != null)
					error = $"Сервер вернул ошибку: {attribute.Description} ({packetHeader.ValuesLength})";
				else
					error = $"Сервер вернул ошибку {packetHeader.ValuesLength}";
				return true;
			}

			// Если размер буфера меньше длины всего пакета, продолжаем принимать данные из сокета
			bool result = packetOffset + headerLength + packetHeader.ValuesLength <= data.Length;
			if (result)
			{
				packetLength = headerLength + packetHeader.ValuesLength;
				bodyOffset = packetOffset + headerLength;
				bodyLength = packetHeader.ValuesLength;
			}
			return result;
		}


		public static object DecodeDiscreteValue(byte[] data, int index, ModbusDataTag tag)
		{
			switch (Type.GetTypeCode(tag.Type))
			{
				case TypeCode.Boolean:
					int offset = index / 8;
					byte b = data.ToValue<byte>(offset, Endianness.BigEndian);
					return (b & (1 << (index % 8))) != 0;
				default: throw new WrongTypeException(tag.Type);
			}
		}

		public static object DecodeAnalogValue(byte[] data, int offset, ModbusDataTag tag)
		{
			switch (Type.GetTypeCode(tag.Type))
			{
				case TypeCode.Boolean: return data.ToValue<bool>(offset, Endianness.BigEndian);
				case TypeCode.SByte: return data.ToValue<sbyte>(offset, Endianness.BigEndian);
				case TypeCode.Byte: return data.ToValue<byte>(offset, Endianness.BigEndian);
				case TypeCode.Int16: return data.ToValue<short>(offset, Endianness.BigEndian);
				case TypeCode.UInt16: return data.ToValue<ushort>(offset, Endianness.BigEndian);
				case TypeCode.Int32: return data.ToValue<int>(offset, Endianness.BigEndian);
				case TypeCode.UInt32: return data.ToValue<uint>(offset, Endianness.BigEndian);
				case TypeCode.Int64: return data.ToValue<long>(offset, Endianness.BigEndian);
				case TypeCode.UInt64: return data.ToValue<ulong>(offset, Endianness.BigEndian);
				case TypeCode.Single: return data.ToValue<float>(offset, Endianness.BigEndian);
				case TypeCode.Double: return data.ToValue<double>(offset, Endianness.BigEndian);
				default: throw new WrongTypeException(tag.Type);
			}
		}

	}

}
