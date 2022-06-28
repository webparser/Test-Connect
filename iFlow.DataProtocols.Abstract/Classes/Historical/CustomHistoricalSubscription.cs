using System.Collections.Generic;
using System.Linq;

using iFlow.Interfaces;
using iFlow.Utils;

namespace iFlow.DataProtocols
{
	public partial class CustomHistoricalSubscription : AbstractHistoricalSubscription
	{
		public CustomHistoricalSubscription(ICustomHistoricalData historicalData)
			: base(historicalData)
		{
		}

		public IdDictionary<HistoricalTagState> TagStates { get; private set; } = new IdDictionary<HistoricalTagState>();

		protected override void SetSubscribeTags(IdDictionary<HistoricalSubscribeTag> subscribeTags)
		{
			base.SetSubscribeTags(subscribeTags);

			IEnumerable<KeyValuePair<int, HistoricalTagState>> addStates = subscribeTags
				.Where(x => !TagStates.ContainsKey(x.Key))
				.Select(x => new KeyValuePair<int, HistoricalTagState>(x.Key,
					new HistoricalTagState()
					{
						LastDeviceTime = x.Value.FromTime,
						LastDeviceIndex = x.Value.FromIndex
					}
				));

			TagStates = TagStates
				// Удаляем состояния для тэгов, которых больше нет.
				.Where(x => subscribeTags.ContainsKey(x.Key))
				// Добавляем состояния для новых тэгов.
				.Union(addStates)
				.ToIdDictionary();
		}

	}

}
