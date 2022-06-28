using System;
using System.Linq;
using iFlow.Interfaces;

namespace iFlow.DataProtocols
{
	public class RealtimeDataSource : CustomRealtimeDataSource<Config, ModbusTagMapping, RealtimeSubscription>
	{
		public RealtimeDataSource(string configStr, IDataTag[] tagMappings)
			: base(configStr, tagMappings, Params.DefaultUpdateRate, Params.MinUpdateRate)
		{
			ModbusClient = new ModbusClient(Config.Address, Config.Port ?? Params.Socket.DefaultModbusPort, Config.UnitId);
		}

		public override void Dispose()
		{
			base.Dispose();
		}

		private readonly ModbusClient ModbusClient;

		protected override RealtimeSubscription InternalCreateSubscription(TimeSpan? updateRate = null, float? deadband = null)
		{
			return new RealtimeSubscription(TagMappings, updateRate ?? DefaultUpdateRate, Params.MinUpdateRate, deadband);
		}

		/// <summary>
		/// Проверка, что таг-маппинг не null, не пуст, не дублируются параметры Id и Mapping
		/// </summary>
		/// <param name="tagMappings"></param>
		protected override void ValidateTagMappings(ModbusTagMapping[] tagMappings)
		{
			base.ValidateTagMappings(tagMappings);

			ushort address;
			DataTag invalidMapping = tagMappings.FirstOrDefault(x => !ushort.TryParse(x.Address, out address));
			if (invalidMapping != null)
				throw new Exception($"В таг-маппингах некорректно указан Modbus-адрес регистра: {invalidMapping}");

			tagMappings = tagMappings.OrderBy(x => x.ByteAddress).ToArray();
			for (int i = 1; i < tagMappings.Length; i++)
				if (tagMappings[i - 1].ByteAddress + tagMappings[i - 1].ByteLength > tagMappings[i].ByteAddress)
					throw new Exception($"В таг-маппинге пересекаются адресные пространства: {tagMappings[i - 1]} и {tagMappings[i]}");
		}

		protected override ModbusTagMapping CreateTag(IDataTag tag)
		{
			return new ModbusTagMapping(tag.Id, tag.Type, tag.Address);
		}

		protected override TagValue[] GetValues(ModbusTagMapping[] tags)
		{
			return ModbusClient.GetValues(tags);
		}

		public override void Read(TagValue[] tagValues)
		{
		}

		public override void Write(TagValue[] tagValues)
		{
		}

	}

}