using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using iFlow.Interfaces;
using iFlow.Utils;

namespace iFlow.DataProtocols
{
	public interface ICustomHistoricalData : IAbstractHistoricalData
	{
	}

	public abstract class CustomHistoricalData<TSubscription> : AbstractHistoricalData<TSubscription>, ICustomHistoricalData
		where TSubscription : CustomHistoricalSubscription
	{
		public CustomHistoricalData(IAbstractDataSource dataSource)
			: base(dataSource)
		{
		}

		// TODO Asign UpdateRate
		private TimeSpan UpdateRate { get; }

		private Timer RequestTimer;
		// TODO Disconnect Timer
		/// <summary>
		/// Таймер закрытия соединения. Если в течение Params.SessionTimeout не было запросов, соединения можно закрыть.
		/// </summary>
		//private readonly System.Timers.Timer tmrDisconnect;

		protected DateTime LastRequestTime = DateTime.MinValue;

		protected class TagRequest
		{
			public int Id;
			public DateTime FromTime;
			public int FromIndex;

			public static IComparer<TagRequest> Comparer
			{
				get
				{
					return Comparer<TagRequest>.Create((x, y) =>
					{
						if (x.FromTime != y.FromTime)
							return x.FromTime.CompareTo(y.FromTime);
						return x.FromIndex.CompareTo(y.FromIndex);
					});
				}
			}
		}

		protected abstract TagValueSet[] GetValuesChunk(TagRequest[] tagRequests, bool includeBounds);

		protected override void OnChanged()
		{
			SetTimer();
		}

		protected virtual TimeSpan? GetRequestTimerInterval()
		{
			lock (this)
			{
				if (Subscriptions.Any(x => x.TagStates.Any(y => !y.Value.OnlyFreshData)))
					return TimeSpan.Zero;

				IEnumerable<DateTime> times = Subscriptions
					.SelectMany
					(
						sub => sub.SubscribeTags.Select
						(
							tag => sub.TagStates[tag.Id].LastRequestTime + UpdateRate
						)
					);
				if (!times.Any())
					return null;

				DateTime minTime = times.Min();
				minTime = minTime > LastRequestTime + UpdateRate ? minTime : LastRequestTime + UpdateRate;

				DateTime now = DateTime.Now;
				if (minTime > now)
					return minTime - now;
				return TimeSpan.Zero;
			}
		}

		private void SetTimer()
		{
			RequestTimer?.Dispose();
			RequestTimer = null;

			TimeSpan? interval = GetRequestTimerInterval();
			if (interval != null)
				RequestTimer = new Timer(Timer_Tick, null, interval.Value, TimeSpan.FromMilliseconds(-1));
		}

		//private bool IsValueChanged(HistoricalTagState tagState, float? subDeadBand, object value)
		private bool IsValueChanged(TSubscription subscription, HistoricalSubscribeTag subscribeTag, object value)
		{
			HistoricalTagState tagState = subscription.TagStates[subscribeTag.Id];
			if (!DataSource.GlobalTags[subscribeTag.Id].Type.IsNumeric())
			{
				bool result = !Equals(tagState.LastValue, value);
				if (result)
					tagState.LastValue = value;
				return result;
			}

			float? deadBand = subscribeTag.Deadband ?? subscription.Deadband ?? Deadband;
			if ((deadBand == null) || (tagState.LastValue == null))
			{
				tagState.LastValue = value;
				return true;
			}
			checked
			{
				double lastValue = (double)Convert.ChangeType(tagState.LastValue, typeof(double));
				value = Convert.ChangeType(value, typeof(double));
				if (Math.Abs(lastValue - (double)value) > deadBand.Value)
				{
					tagState.LastValue = value;
					return true;
				}
			}
			return false;
		}

		private void ProcessRequest(KeyValuePair<int, HistoricalTagState>[] tagStates)
		{
			if (!tagStates.Any())
				return;

			IdDictionary<HistoricalTagState[]> tagStateGroups = tagStates
				.GroupBy(x => x.Key)
				.Select(x => new KeyValuePair<int, HistoricalTagState[]>(x.Key, x.Select(y => y.Value).ToArray()))
				.ToIdDictionary();

			TagRequest[] requests = tagStateGroups
				.Select
				(
					x =>
					{
						var minTagState = x.Value
							.OrderBy(y => y, HistoricalTagState.Comparer)
							.First();
						return new TagRequest()
						{
							Id = x.Key,
							FromTime = minTagState.LastDeviceTime,
							FromIndex = minTagState.LastDeviceIndex
						};
					}
				)
				.ToArray();

			TagValueSet[] valueSets = GetValuesChunk(requests, false);

			foreach (TagValueSet valueSet in valueSets)
			{
				if (valueSet.Values.Any())
				{
					var maxValue = valueSet.Values
						.OrderBy(x => x, TagValueSet.ValueComparer)
						.Last();
					foreach (HistoricalTagState tagState in tagStateGroups[valueSet.Id])
					{
						tagState.LastDeviceTime = maxValue.DeviceTime;
						tagState.LastDeviceIndex = maxValue.DeviceIndex;
					}
				}
			}

			if (valueSets.Any(x => x.Values?.Any() == true))
				NotifySubscribers(valueSets);
			else
				foreach (var tagState in tagStates)
					tagState.Value.OnlyFreshData = true;
		}

		private void NotifySubscribers(TagValueSet[] valueSets)
		{
			lock (this)
			{
				foreach (TSubscription subscription in Subscriptions)
				{
					TagValueSet[] notifyValueSets = valueSets
						.Select
						(
							valueSet =>
							{
								if (valueSet.Values == null)
									return null;

								if (!subscription.subscribeTags.TryGetValue(valueSet.Id, out HistoricalSubscribeTag tag))
									return null;
								if (!subscription.TagStates.TryGetValue(valueSet.Id, out HistoricalTagState tagState))
									return null;

								var values = valueSet.Values
									.Where(x => IsValueChanged(subscription, tag, x.Value))
									.ToArray();
								if (!values.Any())
									return null;

								return new TagValueSet()
								{
									Id = valueSet.Id,
									SystemTime = valueSet.SystemTime,									
									Values = values
								};
							}
						)
						.Where(x => x != null)
						.ToArray();
					if (notifyValueSets.Any())
						subscription.OnValues(notifyValueSets);
				}
			}
		}

		private void Timer_Tick(object st)
		{
			RequestTimer?.Dispose();

			try
			{
				DateTime now = DateTime.Now;
				TimeSpan delta = TimeSpan.FromMilliseconds(Math.Min(500, UpdateRate.TotalMilliseconds / 10));

				KeyValuePair<int, HistoricalTagState>[] requestStates;
				lock (this)
				{
					// Все тэги, чье время запроса уже наступило
					requestStates = Subscriptions
						.SelectMany
						(
							sub => sub.TagStates
								.Where
								(
									tag =>
									{
										if (!tag.Value.OnlyFreshData)
											return false;
										if (tag.Value.LastRequestTime + UpdateRate - delta < now)
										{
											if (tag.Value.LastRequestTime == DateTime.MinValue)
											{
												tag.Value.LastRequestTime = now;
											}
											else
											{
												double d = Math.Truncate((now - tag.Value.LastRequestTime).TotalMilliseconds / UpdateRate.TotalMilliseconds);
												tag.Value.LastRequestTime += TimeSpan.FromMilliseconds((d + 1) * UpdateRate.TotalMilliseconds);
											}
											return true;
										}
										return false;
									}
								)
						)
						.ToArray();
				}
				ProcessRequest(requestStates);

				lock (this)
				{
					// Все тэги, чье время запроса уже наступило
					requestStates = Subscriptions
						.SelectMany(sub => sub.TagStates.Where(x => !x.Value.OnlyFreshData))
						.ToArray();
				}
				ProcessRequest(requestStates);
			}
			catch (Exception ex)
			{
				OnException(ex);
			}
			finally
			{
				SetTimer();
			}
		}

		//private void tmrDisconnect_Elapsed(object sender, EventArgs e)
		//{
		//	tmrDisconnect.Stop();
		//	lock (SocketLock)
		//	{
		//		Socket?.Close();
		//		Socket = null;
		//	}
		//}

	}

}