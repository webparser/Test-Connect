using System;
using System.Collections.Generic;
using System.Linq;

using iFlow.Interfaces;
using iFlow.Utils;

namespace iFlow.DataProtocols
{
	public partial class CustomRealtimeSubscription : AbstractRealtimeSubscription
	{
		public CustomRealtimeSubscription(ICustomRealtimeData realtimeData)
			: base(realtimeData)
		{
		}

		public DateTime RequestTime = DateTime.MinValue;
		public IdDictionary<RealtimeTagState> TagStates { get; private set; } = new IdDictionary<RealtimeTagState>();

		protected override void SetSubscribeTags(RealtimeSubscribeTag[] subscribeTags)
		{
			base.SetSubscribeTags(subscribeTags);

			IEnumerable<KeyValuePair<int, RealtimeTagState>> addStates = subscribeTags
				.Where(x => !TagStates.ContainsKey(x.Id))
				.Select(x => new KeyValuePair<int, RealtimeTagState>(x.Id, new RealtimeTagState()));

			TagStates = TagStates
				// Удаляем состояния для тэгов, которых больше нет.
				.Where(x => base.subscribeTags.ContainsKey(x.Key))
				// Добавляем состояния для новых тэгов.
				.Union(addStates)
				.ToIdDictionary();
		}

	}

}
