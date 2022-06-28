using System;

namespace iFlow.DataProviders
{
	public class ChildSubscription: CustomSubscription<TagMapping>
	{
		public ChildSubscription(TagMapping[] tagMappings, TimeSpan updateRate, float? deadband)
			: base(tagMappings, updateRate, deadband)
		{
		}
	}
}
