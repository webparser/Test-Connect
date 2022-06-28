using System;
using System.Collections.Generic;
using System.Linq;
using iFlow.Interfaces;

namespace iFlow.DataProtocols
{
	public interface IAbstractHistoricalData : IHistoricalData, IDisposable
	{
		IAbstractDataSource DataSource { get; }
		event ExceptionHandler ExceptionEvent;
	}

	public abstract class AbstractHistoricalData<TSubscription> : IAbstractHistoricalData
		where TSubscription : AbstractHistoricalSubscription
	{
		public AbstractHistoricalData(IAbstractDataSource DataSource)
			: base()
		{
		}

		public virtual void Dispose()
		{
			lock (Subscriptions)
				while (Subscriptions.Any())
					Subscriptions.Last().Dispose();
		}

		public IAbstractDataSource DataSource { get; }
		protected readonly List<TSubscription> Subscriptions = new List<TSubscription>();

		public EventHandler Changed;
		public event ExceptionHandler ExceptionEvent;

		public float? Deadband
		{
			get { return deadband; }
			set
			{
				if (deadband == value)
					return;
				lock (this)
					deadband = value;
				OnChanged();
			}
		}
		private float? deadband;

		protected abstract TSubscription InternalCreateSubscription();

		public virtual IHistoricalSubscription2 CreateSubscription()
		{
			TSubscription subscription = InternalCreateSubscription();// (TagMappings, updateRate ?? Config.UpdateRate ?? Params.DefaultUpdateRate, deadband);
			lock (Subscriptions)
			{
				Subscriptions.Add(subscription);
				subscription.Disposed += Subscription_Disposed;
				subscription.ExceptionEvent += Subscription_ExceptionEvent;
			}
			return subscription;
		}

		private void Subscription_Disposed(object sender, EventArgs e)
		{
			TSubscription subscription = (TSubscription)sender;
			lock (Subscriptions)
			{
				subscription.Disposed -= Subscription_Disposed;
				subscription.ExceptionEvent -= Subscription_ExceptionEvent;
				Subscriptions.Remove(subscription);
			}
		}

		protected virtual void Subscription_ExceptionEvent(object sender, Exception ex, string comments = null)
		{
			TSubscription subscription = (TSubscription)sender;
			string tagNames = string.Join(", ", subscription.SubscribeTags
				.Take(10)
				.Select(x => DataSource.GlobalTags[x.Id].Name));
			if (subscription.SubscribeTags.Count() > 10)
				tagNames += ", ...";
			OnException(ex, $"Ошибка в подписке на тэги \"{tagNames}\". " + (comments ?? ""));
		}

		protected virtual void OnChanged()
		{
			Changed?.Invoke(this, EventArgs.Empty);
		}

		protected virtual void OnException(Exception ex, string comments = null)
		{
			ExceptionEvent?.Invoke(this, ex, comments);
		}

	}

}
