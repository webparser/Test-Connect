using System;
using iFlow.Utils;

namespace iFlow.DataProtocols
{
	public partial class RealtimeSubscription : CustomRealtimeSubscription<ModbusDataTag>
	{
		public RealtimeSubscription(IdDictionary<ModbusDataTag> tags, TimeSpan updateRate, TimeSpan minUpdateRate, float deadband)
			: base(tags, updateRate, minUpdateRate, deadband)
		{
		}

	}

}
