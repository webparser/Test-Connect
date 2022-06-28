using System;
using System.Collections.Generic;
using System.Linq;
using iFlow.Interfaces;

namespace iFlow.DataProtocols
{
	public interface IAbstractRealtimeData : IRealtimeData, IDisposable
	{
		IAbstractDataSource DataSource { get; }
		TimeSpan MinUpdateRate { get; }
		event ExceptionHandler ExceptionEvent;
	}

	public abstract class AbstractRealtimeData<TSubscription> : IAbstractRealtimeData
		where TSubscription : AbstractRealtimeSubscription
	{
		public AbstractRealtimeData(IAbstractDataSource dataSource)
			: base()
		{
			DataSource = dataSource;
		}

		public virtual void Dispose()
		{
			lock (Subscriptions)
				while (Subscriptions.Any())
					Subscriptions.Last().Dispose();
		}

		protected List<TSubscription> Subscriptions { get; } = new List<TSubscription>();

		public IAbstractDataSource DataSource { get; }
		public abstract TimeSpan MinUpdateRate { get; }

		public EventHandler Changed;
		public event ExceptionHandler ExceptionEvent;

		protected abstract TSubscription CreateSubscription_Internal();

		public virtual IRealtimeSubscription CreateSubscription(TimeSpan? updateRate)
		{
			TSubscription subscription = CreateSubscription_Internal();// (TagMappings, updateRate ?? Config.UpdateRate ?? Params.DefaultUpdateRate, deadband);
			lock (Subscriptions)
			{
				subscription.UpdateRate = updateRate;
				subscription.Changed += Subscription_Changed;
				subscription.Disposed += Subscription_Disposed;
				subscription.ExceptionEvent += Subscription_ExceptionEvent;
				Subscriptions.Add(subscription);
			}
			return subscription;
		}

		private void Subscription_Changed(object sender, EventArgs e)
		{
			OnChanged();
		}

		private void Subscription_Disposed(object sender, EventArgs e)
		{
			TSubscription subscription = (TSubscription)sender;
			lock (Subscriptions)
			{
				subscription.Changed -= Subscription_Changed;
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

		public abstract DataReadValue_Id[] Read(int[] tagIds);
		public abstract DataReadValue_Address[] Read(string[] addresses);

		public abstract void Write(DataWriteValue_Id[] tagValues);
        public abstract void Write(DataWriteValue_Address[] values);
	}

}
