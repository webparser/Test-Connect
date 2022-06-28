using System;

namespace iFlow.DataProtocols
{
	public partial class Subscription : CustomHistoricalSubscription<DataTag>
	{
		public Subscription(DataTag[] tagMappings, TimeSpan updateRate, TimeSpan minUpdateRate, float? deadband)
			: base(tagMappings, updateRate, minUpdateRate, deadband)
		{
		}

	}

}
