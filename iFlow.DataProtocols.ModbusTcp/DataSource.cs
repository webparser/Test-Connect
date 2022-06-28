using System;
using System.Collections.Generic;
using System.Linq;
using iFlow.Interfaces;

namespace iFlow.DataProtocols
{
	public class DataSource : AbstractDataSource<Config.DataSourceConfig, DataTag>
	{
		public DataSource()
			: base()
		{
		}

		public override void Dispose()
		{
			lock (LockObj)
			{
				realtimeData.Dispose();
				OpcClient.Dispose();
			}
			base.Dispose();
		}

		internal OpcClient OpcClient;
		internal readonly object LockObj = new object();

		public override IRealtimeData RealtimeData
		{
			get { return realtimeData; }
		}
		private RealtimeData realtimeData;

		public override IMetaData MetaData
		{
			get { return metaData; }
		}
		private MetaData metaData;

		protected override void Init(Config.Config config)
		{
			OpcClient = new OpcClient(config.Url);
			realtimeData = new RealtimeData(this, config.RealtimeData.DefaultUpdateRate, config.RealtimeData.DefaultDeadband);
			metaData = new MetaData(this);
		}

		protected override Dictionary<string, DataTag> GetConfigTags(Config.Config config)
		{
			return config.Tags == null
				? null
				: config.Tags.ToDictionary(key => key.Name, value => new DataTag(value.Type, value.Address));
		}

		protected override void ValidateConfig(Config.Config config)
		{
			base.ValidateConfig(config);
			if (string.IsNullOrWhiteSpace(config.Url))
				throw new Exception($"Не указан адрес OPC-сервера");
		}

	}

}
