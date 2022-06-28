using System;

namespace iFlow.DataProtocols
{
	//public class DataTag
	//{
	//	//public MappingTag(int id, Type type, string mapping)
	//	public DataTag(Type type, string address)
	//		: base()
	//	{
	//		Type = type;
	//		Address = address;
	//	}

	//	public Type Type { get; }

	//	public int Length
	//	{
	//		get
	//		{
	//			switch (Type.GetTypeCode(Type))
	//			{
	//				case TypeCode.Boolean: return sizeof(byte);
	//				case TypeCode.SByte: return sizeof(sbyte);
	//				case TypeCode.Byte: return sizeof(byte);
	//				case TypeCode.Int16: return sizeof(short);
	//				case TypeCode.UInt16: return sizeof(ushort);
	//				case TypeCode.Int32: return sizeof(int);
	//				case TypeCode.UInt32: return sizeof(uint);
	//				case TypeCode.Int64: return sizeof(long);
	//				case TypeCode.UInt64: return sizeof(ulong);
	//				case TypeCode.Single: return sizeof(float);
	//				case TypeCode.Double: return sizeof(double);
	//				default: return length;
	//			}
	//		}
	//		set { length = value; }
	//	}
	//	private int length;

	//	public string Address { get; }

	//	public override string ToString()
	//	{
	//		return $"{{DataTag: Type={(Type != null ? Type.Name : "null")}, Address={Address}}}";
	//	}

	//}

}