using System;
using System.Collections.Generic;
using System.Linq;

using iFlow.Interfaces;
using iFlow.Utils;

namespace iFlow.DataProtocols
{
	public abstract class AbstractHistoricalSubscription : IHistoricalSubscription2
	{
		public AbstractHistoricalSubscription(IAbstractHistoricalData historicalData)
			: base()
		{
			HistoricalData = historicalData;
		}

		public virtual void Dispose()
		{
			Disposed?.Invoke(this, EventArgs.Empty);
		}

		public IAbstractHistoricalData HistoricalData { get; }

		#region Public Properties
		public virtual HistoricalSubscribeTag[] SubscribeTags
		{
			get { return subscribeTags.Select(x => x.Value).ToArray(); }
			set
			{
				try
				{
					lock (this)
					{
						IdDictionary<HistoricalSubscribeTag> newSubscribeTags = value.ToIdDictionary(x => x.Id, x => x);
						if (subscribeTags.All(x => newSubscribeTags.ContainsKey(x.Key)) && newSubscribeTags.All(x => subscribeTags.ContainsKey(x.Key)))
							return;
						ValidateSubscribeTags(value);
						SetSubscribeTags(newSubscribeTags);
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
		public IdDictionary<HistoricalSubscribeTag> subscribeTags;

		// ISubscription
		public float? Deadband
		{
			get { return deadband; }
			set
			{
				try
				{
					if (deadband == value)
						return;
					deadband = value;
					OnChanged();
				}
				catch (Exception ex)
				{
					OnException(ex);
				}
			}
		}
		private float? deadband;
		#endregion Public Properties		

		#region Public Events
		public event EventHandler Disposed;
		public event EventHandler Changed;
		public event ExceptionHandler ExceptionEvent;
		// ISubscription
		public event HistoricalValuesHandler ValuesEvent;
		#endregion Public Events

		protected void OnChanged()
		{
			Changed?.Invoke(this, EventArgs.Empty);
		}

		public void OnValues(TagValueSet[] values)
		{
			ValuesEvent?.Invoke(this, values);
		}

		public void OnException(Exception ex)
		{
			ExceptionEvent?.Invoke(this, ex);
		}

		/// <summary>
		/// Проверка, что список тэгов не соответстует тэг-маппингу.
		/// </summary>
		/// <param name="tagIds"></param>
		protected virtual void ValidateSubscribeTags(HistoricalSubscribeTag[] tagSubscriptions)
		{
			if (tagSubscriptions == null)
				return;

			HistoricalData.DataSource.GlobalTags.CheckTagIds(tagSubscriptions);

			SortedSet<int> sortedLocalIds = new SortedSet<int>(HistoricalData.DataSource.TagIds);
			tagSubscriptions
				.Where(x => !sortedLocalIds.Contains(x.Id))
				.ThrowExceptionIfAny(x => $"Подписка на тэг {HistoricalData.DataSource.GlobalTags[x.Id]}, отсутствующий в тэг-листе источника данных.");

			DateTime now = DateTime.Now;
			tagSubscriptions
				.Where(x => x.FromTime > now)
				.ThrowExceptionIfAny(x => $"Время начала выборки {x.FromTime} тэга {HistoricalData.DataSource.GlobalTags[x.Id]} превышает текущее время {now}.");
		}

		protected virtual void SetSubscribeTags(IdDictionary<HistoricalSubscribeTag> subscribeTags)
		{
			this.subscribeTags = subscribeTags;
		}

	}

}
