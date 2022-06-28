using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using iFlow.Interfaces;
using iFlow.Utils;

namespace iFlow.DataProtocols
{
    public interface ICustomRealtimeData : IAbstractRealtimeData
	{
	}

	public abstract class CustomRealtimeData<TSubscription> : AbstractRealtimeData<TSubscription>, ICustomRealtimeData
		where TSubscription : CustomRealtimeSubscription
	{
		public CustomRealtimeData(IAbstractDataSource dataSource)
			: base(dataSource)
		{
		}

		private Timer RequestTimer;
		// TODO Disconnect Timer
		/// <summary>
		/// Таймер закрытия соединения. Если в течение Params.SessionTimeout не было запросов, соединения можно закрыть.
		/// </summary>
		//private readonly System.Timers.Timer tmrDisconnect;

		protected override void OnChanged()
		{
			SetTimer();
		}

		protected virtual TimeSpan? GetRequestTimerInterval()
		{
			DateTime now = DateTime.Now;
			//long nowMsecs = (long)(now - DateTime.MinValue).TotalMilliseconds;

			lock (Subscriptions)
			{
				IEnumerable<DateTime> requestTimes = Subscriptions.Select(sub => sub.RequestTime);
				//.Select
				//(
				//	sub =>
				//	{
				//		lock (sub)
				//		{
				//			//TimeSpan updateRate = (sub.UpdateRate ?? MinUpdateRate) > MinUpdateRate ? sub.UpdateRate.Value : MinUpdateRate;
				//			//long updateMsecs = (long)updateRate.TotalMilliseconds;
				//			if (sub.RequestTime<=now )
				//				return sub.RequestTime;

				//			{
				//				return sub.SubscribeTags.Select(tag => sub.TagStates[tag.Id].LastRequestTime + updateRate);
				//			}
				//		}
				//	}
				//);
				if (!requestTimes.Any())
					return null;

				DateTime minTime = requestTimes.Min();
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

		private bool IsValueChanged(TSubscription subscription, RealtimeSubscribeTag subscribeTag, object value)
		{
			RealtimeTagState tagState = subscription.TagStates[subscribeTag.Id];
			if (!DataSource.GlobalTags[subscribeTag.Id].Type.IsNumeric())
			{
				bool result = !Equals(tagState.LastValue, value);
				if (result)
					tagState.LastValue = value;
				return result;
			}

			float? deadBand;
			lock (this)
				deadBand = subscribeTag.Deadband;
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

		private void Timer_Tick(object state)
		{
			RequestTimer?.Dispose();
			try
			{
				DateTime now = DateTime.Now;
				TimeSpan delta = TimeSpan.FromMilliseconds(Math.Min(500, MinUpdateRate.TotalMilliseconds / 10));

				TSubscription[] requestSubscriptions;
				lock (Subscriptions)
				{
					// Все тэги, чье время запроса уже наступило
					requestSubscriptions = Subscriptions
						.Where(sub => sub.RequestTime < now + delta)
						.ToArray();
				}

				int[] requestTagIds = requestSubscriptions
					.SelectMany(sub => sub.SubscribeTags.Select(tag => tag.Id))
					// выбираем уникальные Tag Id, потому как на один тэг могут быть подписаны несколько клиентов
					.Distinct()
					.ToArray();
				if (!requestTagIds.Any())
					return;

                DataReadValue_Id[] values = Read(requestTagIds)
					.Select((x, index) => new DataReadValue_Id()
					{
						Id = requestTagIds[index],
						SystemTime = x.SystemTime,
						DeviceTime = x.DeviceTime,
						Quality = x.Quality,
						Result = x.Result,
						Value = x.Value
					})
					.ToArray();

				foreach (TSubscription subscription in requestSubscriptions)
				{
                    DataReadValue_Id[] notifyValues = values
						.Select
						(
							value =>
							{
								// Если подписка успела измениться
								if (!subscription.subscribeTags.TryGetValue(value.Id, out RealtimeSubscribeTag tag))
									return null;

								if (!subscription.TagStates.TryGetValue(value.Id, out RealtimeTagState tagState))
									return null;

								if (!IsValueChanged(subscription, tag, value.Value))
									return null;
								return value;
							}
						)
						.Where(x => x != null)
						.ToArray();

					// Даже если ни одно значение не изменилось, дергаем клиента, чтобы известить, что произошел опрос значений.
					subscription.OnValues(notifyValues);
				}

				foreach (TSubscription sub in requestSubscriptions)
				{
					double updateRate = (sub.UpdateRate ?? MinUpdateRate).TotalMilliseconds;
					DateTime maxTime = sub.RequestTime + delta > now ? sub.RequestTime + delta : now;
					double nextMsec = Math.Truncate((maxTime - DateTime.MinValue).TotalMilliseconds / updateRate + 1) * updateRate;
					sub.RequestTime = DateTime.MinValue + TimeSpan.FromMilliseconds(nextMsec);
				}
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