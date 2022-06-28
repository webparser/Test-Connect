using System;
using System.Collections.Generic;
using System.Linq;
using iFlow.Interfaces;

namespace iFlow.DataProtocols
{
	public class DataSource : AbstractDataSource<Config.Config, string>
	{
		public DataSource(GlobalTags globaltags)
			: base(globaltags)
		{
		}

		public override void Dispose()
		{
			lock (this)
			{
				((RealtimeData)RealtimeData).Dispose();
				((MetaData)MetaData).Dispose();
				OpcClient?.Dispose();
			}
			base.Dispose();
		}

		internal FakeOpcClient OpcClient;

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
			if (string.IsNullOrWhiteSpace(config.Url))
				throw new Exception($"Не указан адрес OPC-сервера");
			base.LoadConfig(config);
			OpcClient = new FakeOpcClient(config.Tags);
			// TODO реанимировать
			//RealtimeData.UpdateRate = config.RealtimeData?.UpdateRate;
			//RealtimeData.Deadband = config.RealtimeData?.Deadband;
		}

		protected override Dictionary<string, string> LoadTagAdresses(Config.Config config)
		{
            if (config.Tags == null)
                return new Dictionary<string, string>();
            return config.Tags.ToDictionary(x => x.Name, x => x.Address );
		}

	}

}
