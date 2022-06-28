using System;
using System.Collections.Generic;
using System.Linq;
using iFlow.Interfaces;

namespace iFlow.DataProtocols
{
	public class DataSource : AbstractDataSource<Config.Config, string>, IAbstractDataSource
	{
		public DataSource(GlobalTags globalTags)
			: base(globalTags)
		{
		}

		public override void Dispose()
		{
			lock (LockObj)
			{
				((RealtimeData)RealtimeData).Dispose();
				OpcClient.Dispose();
			}
			base.Dispose();
		}

		internal OpcClient OpcClient;
		internal readonly object LockObj = new object();

		protected override IAbstractRealtimeData CreateRealtimeData()
		{
			return new RealtimeData(this);
		}

		protected override IAbstractMetaData CreateMetaData()
		{
			return new MetaData(this);
		}

		public override void LoadConfig(Config.Config config)
		{
			base.LoadConfig(config);
			if (string.IsNullOrWhiteSpace(config.Url))
				throw new Exception($"Не указан адрес OPC-сервера");
			OpcClient = new OpcClient(config.Url);
			// TODO реанимировать
			//RealtimeData.UpdateRate = config.RealtimeData?.UpdateRate;
			//RealtimeData.Deadband = config.RealtimeData?.Deadband;
		}

		protected override Dictionary<string, string> LoadTagAdresses(Config.Config config)
		{
			return config.Tags?
				.ToDictionary(key => key.Name, value => value.Address);
		}

	}

}
