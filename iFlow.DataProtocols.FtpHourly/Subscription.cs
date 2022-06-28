using System;
using iFlow.DataProtocols;

namespace iFlow.DataProviders
{
    public partial class Subscription : CustomRealtimeSubscription<DataTag>
	{
		public Subscription(DataTag[] tags, TimeSpan updateRate, TimeSpan minUpdateRate, float? deadband)
			: base(tags, updateRate, minUpdateRate, deadband)
		{
		}

	}

}
