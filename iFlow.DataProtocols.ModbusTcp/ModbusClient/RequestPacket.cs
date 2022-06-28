using System.Linq;

namespace iFlow.DataProtocols
{
	internal class RequestPacket
	{
		public ushort RegAddress;
		public ushort RegLength;

		public byte[] GetData(byte unitId)
		{
			ushort relativeAddress;
			byte functionCode = Modbus.Utils.GetFunctionCode(RegAddress, RegLength, out relativeAddress);
			// В качестве TransactionId передаем начальный адрес пакета. 
			return Modbus.Utils.EncodeRequestPacket(RegAddress, unitId, functionCode, relativeAddress, RegLength);
		}

		public override string ToString()
		{
			return $"{{Modbus request packet: Address={RegAddress}, Length={RegLength}}}";
		}
	}

	/// <summary>
	/// Данные для запроса. Содержит список всех сформированных пакетов. В каждом пакете - список тэгов, значения 
	/// которых необходимо получить.
	/// </summary>
	internal class Request
	{
		/// <summary>
		/// Список тэгов для запроса. Следить чтобы были в порядке возрастания адреса.
		/// </summary>
		public ModbusDataTag[] Tags;

		/// <summary>
		/// Список пакетов, сформированный на основе тэгов. Следить чтобы были в порядке возрастания адреса.
		/// </summary>
		public RequestPacket[] Packets;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="unitId">UnitId устройства Modbus</param>
		/// <returns></returns>
		public byte[] GetData(byte unitId)
		{
			return Packets.SelectMany(x => x.GetData(unitId)).ToArray();
		}

	}

}

