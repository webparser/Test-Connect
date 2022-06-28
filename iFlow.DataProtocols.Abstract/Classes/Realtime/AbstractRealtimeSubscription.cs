using System;
using System.Collections.Generic;
using System.Linq;
using iFlow.Interfaces;
using iFlow.Utils;

namespace iFlow.DataProtocols
{
	public abstract class AbstractRealtimeSubscription : IRealtimeSubscription
	{
		public AbstractRealtimeSubscription(IAbstractRealtimeData realtimeData)
			: base()
		{
			RealtimeData = realtimeData;
		}

		public virtual void Dispose()
		{
			Disposed?.Invoke(this, EventArgs.Empty);
		}

		public object UserData { get; set; }

		public IAbstractRealtimeData RealtimeData { get; }

		#region Public Properties
		public virtual RealtimeSubscribeTag[] SubscribeTags
		{
			get { return subscribeTags.Values.ToArray(); }
			set
			{
				try
				{
					lock (this)
					{
						RealtimeData.DataSource.GlobalTags.CheckTagIds(value);
						SortedSet<int> newSubscribeTags = new SortedSet<int>(value.Select(x => x.Id));
						if (subscribeTags.All(x => newSubscribeTags.Contains(x.Key)) && newSubscribeTags.All(x => subscribeTags.ContainsKey(x)))
							return;
						ValidateSubscribeTags(value);
						SetSubscribeTags(value);
					}
					OnChanged();
				}
				catch (Exception ex)
				{
					OnException(ex);
					throw;
				}
			}
		}
		public IdDictionary<RealtimeSubscribeTag> subscribeTags = new IdDictionary<RealtimeSubscribeTag>();

		// ISubscription
		public TimeSpan? UpdateRate
		{
			get { return updateRate; }
			set
			{
				try
				{
					if (updateRate == value)
						return;
					if (value < RealtimeData.MinUpdateRate)
						throw new Exception(
							$"Значение {value} параметра UpdateRate подписки источника данных " +
							$"меньше минимального значения {RealtimeData.MinUpdateRate}, поддерживаемого провайдером {AbstractDataProtocol.ToString()}.");
					updateRate = value;
					OnChanged();
				}
				catch (Exception ex)
				{
					OnException(ex);
					throw;
				}
			}
		}
		private TimeSpan? updateRate;

		#endregion Public Properties

		#region Public Events
		public event EventHandler Disposed;
		public event EventHandler Changed;
		public event ExceptionHandler ExceptionEvent;
		// ISubscription
		public event RealtimeValuesHandler ValuesEvent;
		#endregion Public Events

		protected virtual void OnChanged()
		{
			Changed?.Invoke(this, EventArgs.Empty);
		}

		public void OnException(Exception ex)
		{
			ExceptionEvent?.Invoke(this, ex);
		}

		public void OnValues(DataReadValue_Id[] values)
		{
			ValuesEvent?.Invoke(this, values);
		}

		/// <summary>
		/// Проверка, что список тэгов соответстует тэг-маппингу.
		/// </summary>
		/// <param name="tagIds"></param>
		protected virtual void ValidateSubscribeTags(RealtimeSubscribeTag[] tagSubscriptions)
		{
			if (tagSubscriptions == null)
				return;

			RealtimeData.DataSource.GlobalTags.CheckTagIds(tagSubscriptions);

			SortedSet<int> sortedLocalIds = new SortedSet<int>(RealtimeData.DataSource.TagIds);
			tagSubscriptions
				.Where(x => !sortedLocalIds.Contains(x.Id))
				.ThrowExceptionIfAny(x => $"Подписка на тэг {RealtimeData.DataSource.GlobalTags[x.Id]}, отсутствующий в тэг-листе источника данных.");
		}

		protected virtual void SetSubscribeTags(RealtimeSubscribeTag[] subscribeTags)
		{
			this.subscribeTags = subscribeTags.ToIdDictionary(x => x.Id, x => x);
		}

	}

}
