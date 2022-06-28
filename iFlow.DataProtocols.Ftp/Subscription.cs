using System;
using System.Collections.Generic;
using System.Linq;

namespace iFlow.DataProviders
{
	internal class Subscription : CustomSubscription<TagMapping>
	{
		public Subscription(TagMapping[] tagMappings, TimeSpan updateRate, float? deadband)
			: base(tagMappings, updateRate, deadband)
		{
		}

		public ISubscription HourlySubscription;
		public ISubscription MsgSubscription;
	}

}
