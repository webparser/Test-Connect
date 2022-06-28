using System;
using iFlow.Utils;

namespace iFlow.DataProtocols
{
	public class ModbusDataTag : DataTag
	{
		public ModbusDataTag(Type type, string mapping)
			: base(type, mapping)
		{
			RegAddress = ushort.Parse(mapping);
		}

		/// <summary>
		///  Modbus-адрес регистра (первого регистра, если размер данных превышает размер одного регистра).
		/// </summary>
		public ushort RegAddress { get; private set; }

		/// <summary>
		/// Количество регистров, необходимое для размещения типа данных.
		/// </summary>
		public ushort RegLength
		{
			get
			{
				switch (Type.GetTypeCode(Type))
				{
					case TypeCode.Boolean: return 1;
					case TypeCode.SByte: return sizeof(sbyte) / 2;
					case TypeCode.Byte: return 1;
					case TypeCode.Int16: return sizeof(short) / 2;
					case TypeCode.UInt16: return sizeof(ushort) / 2;
					case TypeCode.Int32: return sizeof(int) / 2;
					case TypeCode.UInt32: return sizeof(uint) / 2;
					case TypeCode.Int64: return sizeof(long) / 2;
					case TypeCode.UInt64: return sizeof(ulong) / 2;
					case TypeCode.Single: return sizeof(float) / 2;
					case TypeCode.Double: return sizeof(double) / 2;
					default: throw new WrongTypeException(Type);
				}
			}
		}

		/// <summary>
		/// Адрес размещения в пересчете на байты. Расчитывается как адрес modbus-регистра, указанный в свойстве Adress,
		/// умноженный на размерность регистра (два байта).
		/// </summary>
		public int ByteAddress { get { return RegAddress * 2; } }

		/// <summary>
		/// Физическая длина значения в байтах.
		/// </summary>
		public int ByteLength { get { return RegLength * 2; } }

		public override string ToString()
		{
			return $"{{ModbusTagMapping: Type={Type.Name}, Address={RegAddress}}}";
		}
	}
}
