using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using iFlow.Interfaces;
using iFlow.Utils;

namespace iFlow.DataProvider
{
	[ServiceBehavior(
		InstanceContextMode = InstanceContextMode.PerSession,
		ConcurrencyMode = ConcurrencyMode.Multiple,
		UseSynchronizationContext = false,
		IncludeExceptionDetailInFaults = true
	)]
	public class WcfDataProvider : IDataProvider
	{
		public WcfDataProvider()
		{
			string url = OperationContext.Current.Channel.LocalAddress.Uri.AbsoluteUri;
			Module = Module.Instances.FirstOrDefault(m => m.ServiceHost.BaseAddresses.Any(x => x.AbsoluteUri == url));
			if (Module == null)
				throw new Exception($"Не найден модуль {url}");

			Client = OperationContext.Current.GetCallbackChannel<IDataProviderCallback>();
			IClientChannel channel = (IClientChannel)Client;
			channel.Faulted += Channel_Faulted;
			channel.Closed += Channel_Closed;
		}

		private void Channel_Closed(object sender, EventArgs e)
		{
			Dispose();
		}

		private void Channel_Faulted(object sender, EventArgs e)
		{
			Module.Logger.Add("Связь с клиентом разорвана.");
		}

		/// <summary>
		/// Отладочный конструктор
		/// </summary>
		public WcfDataProvider(Module module, IDataProviderCallback client)
		{
			Module = module;
			Client = client;
		}

		public void Dispose()
		{
			lock (this)
			{
				foreach (IRealtimeSubscription subscripton in Subscriptions)
					subscripton.Dispose();
				Subscriptions.Clear();
			}
		}

		private Module Module { get; }
		private IDataProviderCallback Client { get; }

		public Collection<IRealtimeSubscription> Subscriptions { get; } = new Collection<IRealtimeSubscription>();

		void IService.Ping()
		{
		}

		public TimeSpan GetDefaultUpdateRate()
		{
			return TimeSpan.FromSeconds(1);
		}

		void IDataProvider.Subscribe(Guid subscriptionUid, RealtimeSubscribeTag[] subscribeTags, TimeSpan? updateRate)
		{
			lock (this)
				try
				{
					Module.GlobalTags.CheckTagIds(subscribeTags);

					SortedDictionary<int, IDataSource> tagDataSourceDic = new SortedDictionary<int, IDataSource>
					(
						Module.DataSources
							.SelectMany(ds => ds.DataSource.TagIds.Select(tagId => new { ds.DataSource, TagId = tagId }))
							.ToDictionary(x => x.TagId, x => x.DataSource)
					);

					// Если не указан список тэгов для подписки, то берем все тэго источника данных.
					if (subscribeTags == null)
						subscribeTags = Module.DataSources
							.SelectMany(ds => ds.DataSource.TagIds.Select(tagId => new RealtimeSubscribeTag() { Id = tagId }))
							.ToArray();

					subscribeTags
						.Where(x => !tagDataSourceDic.ContainsKey(x.Id))
						.ThrowExceptionIfAny(x => $"Тэг \"{Module.GlobalTags[x.Id]}\" не относится к источнику данных.");

					var tagGroups = subscribeTags
						.GroupBy(x => tagDataSourceDic[x.Id])
						.ToArray();

					IRealtimeSubscription[] deleteSubscriptions = Subscriptions
						// Удаляем подписки, которые не попадают в группы с новыми источниками данных
						.Where(sub => sub.Uid() == subscriptionUid && !tagGroups.Any(g => g.Key == sub.DataSource()))
						.ToArray();
					foreach (IRealtimeSubscription deleteSubscription in deleteSubscriptions)
					{
						deleteSubscription.Dispose();
						Subscriptions.Remove(deleteSubscription);
					}

					foreach (var tagGroup in tagGroups)
					{
						IRealtimeSubscription subscription = Subscriptions
							.FirstOrDefault(sub => sub.Uid() == subscriptionUid && sub.DataSource() == tagGroup.Key);
						if (subscription == null)
						{
							subscription = tagGroup.Key.RealtimeData.CreateSubscription(updateRate);
							subscription.UserData = new SubscriptionInfo(tagGroup.Key, subscriptionUid);
							subscription.ValuesEvent += Subscription_ValuesEvent;
							Subscriptions.Add(subscription);
						}
						subscription.SubscribeTags = tagGroup.ToArray();
						subscription.UpdateRate = updateRate;
					}
				}
				catch (Exception ex)
				{
					Module.Logger.Add(ex);
					throw;
				}
		}

		void IDataProvider.Unsubscribe(Guid subscriptionUid)
		{
			lock (this)
				try
				{
					IRealtimeSubscription[] deleteSubscriptions = Subscriptions
						.Where(x => x.Uid() == subscriptionUid)
						.ToArray();
					foreach (IRealtimeSubscription deleteSubscription in deleteSubscriptions)
					{
						deleteSubscription.Dispose();
						Subscriptions.Remove(deleteSubscription);
					}
				}
				catch (Exception ex)
				{
					Module.Logger.Add(ex);
					throw;
				}
		}

		void IDataProvider.Write(int tagId, object value)
		{

		}

		private void Subscription_ValuesEvent(object sender, DataReadValue_Id[] values)
		{
			try
			{
				IRealtimeSubscription subscription = (IRealtimeSubscription)sender;
				Client.OnValues(subscription.Uid(), values);
			}
			catch (Exception ex)
			{
				Module.Logger.Add(ex);
				throw;
			}
		}

	}

	internal class SubscriptionInfo
	{
		public SubscriptionInfo(IDataSource dataSource, Guid uid)
			: base()
		{
			DataSource = dataSource;
			Uid = uid;
		}

		public IDataSource DataSource;

		/// <summary>
		/// RealtimeSubscribeSet.Uid
		/// </summary>
		public Guid Uid;
	}


	internal static class SubscriptionInfoExt
	{
		public static Guid Uid(this IRealtimeSubscription subscription)
		{
			return ((SubscriptionInfo)subscription.UserData).Uid;
		}

		public static IDataSource DataSource(this IRealtimeSubscription subscription)
		{
			return ((SubscriptionInfo)subscription.UserData).DataSource;
		}

	}

}

