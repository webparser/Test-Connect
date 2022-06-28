using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using iFlow.Interfaces;
using iFlow.Utils;

namespace iFlow.DataProtocols
{
	internal class RealtimeSubscription : AbstractRealtimeSubscription
	{
		public RealtimeSubscription(RealtimeData realtimeData)
			: base(realtimeData)
		{
			OpcSubscription = CreateOpcSubscription();
		}

		public override void Dispose()
		{
			OpcSubscription?.Dispose();
			OpcSubscription = null;
			base.Dispose();
		}

		public new RealtimeData RealtimeData => (RealtimeData)base.RealtimeData;
		private Opc.Da.ISubscription OpcSubscription;

		private Opc.Da.ISubscription CreateOpcSubscription()
		{
			Opc.Da.SubscriptionState opcState = new Opc.Da.SubscriptionState()
			{
				Active = true,
				Name = Guid.NewGuid().ToString(),
				UpdateRate = (int)Math.Round((UpdateRate ?? RealtimeData.MinUpdateRate).TotalMilliseconds),
				// TODO Remove
				KeepAlive = 1000
			};
			Opc.Da.ISubscription subscription = (RealtimeData.DataSource as DataSource).OpcClient.Server.CreateSubscription(opcState);
			try
			{
				subscription.DataChanged += OpcSubscription_DataChanged;
				subscription.SetEnabled(true);
			}
			catch
			{
				subscription.Dispose();
				throw;
			}
			return subscription;
		}

		protected override void SetSubscribeTags(RealtimeSubscribeTag[] subscribeTags)
		{
			try
			{
				if (base.subscribeTags.Any() == true)
				{
					SortedSet<string> oldAddresses = new SortedSet<string>(base.subscribeTags.Select(x => RealtimeData.DataSource.TagAddresses[x.Key]));
					SortedSet<string> newAddresses = new SortedSet<string>(subscribeTags.Select(x => RealtimeData.DataSource.TagAddresses[x.Id]));

					Opc.ItemIdentifier[] removeIdentifiers = oldAddresses
						.Where(x => !newAddresses.Contains(x))
						.Select(x => new Opc.ItemIdentifier(x))
						.ToArray();
					if (removeIdentifiers.Any())
						OpcSubscription.RemoveItems(removeIdentifiers);

					subscribeTags = subscribeTags.Where(x => !oldAddresses.Contains(RealtimeData.DataSource.TagAddresses[x.Id])).ToArray();
				}
				Opc.Da.Item[] opcItems = subscribeTags
					.Select
					(
						x => new Opc.Da.Item()
						{
							Active = true,
							ActiveSpecified = true,
							ClientHandle = x.Id,
							Deadband = x.Deadband ?? default(float),
							DeadbandSpecified = x.Deadband != null,
							//SamplingRate = (int)(x.UpdateRate?.TotalMilliseconds ?? 0),
							//SamplingRateSpecified = x.UpdateRate != null,
							ItemName = RealtimeData.DataSource.TagAddresses[x.Id]
						}
					)
					.ToArray();

				Opc.Da.ItemResult[] results = OpcSubscription.AddItems(opcItems);
				SortedSet<string> resultsIds = results
						.Where(x => x.ResultID == Opc.ResultID.S_OK)
						.Select(x => x.ItemName)
						.ToSortedSet();
				subscribeTags = subscribeTags
					.Where(x => resultsIds.Contains(RealtimeData.DataSource.TagAddresses[x.Id]))
					.ToArray();

				results
					.Where(x => x.ResultID != Opc.ResultID.S_OK)
					.ThrowExceptionIfAny(x => $"OPC-сервер отверг подписку на тэг \"{RealtimeData.DataSource.GlobalTags[(int)x.ClientHandle].Name}\". Причина: {x.ResultID}.");
			}
			finally
			{
				base.SetSubscribeTags(subscribeTags);
			}
		}

		private void OpcSubscription_DataChanged(object subscriptionHandle, object requestHandle, Opc.Da.ItemValueResult[] values)
		{
			Task.Run(() => OpcSubscription_DataChanged_Asyc(values));
		}

		private void OpcSubscription_DataChanged_Asyc(Opc.Da.ItemValueResult[] values)
		{
			try
			{
                DataReadValue_Id[] result = values.Select
				(
					x =>
					{
						int id = (int)x.ClientHandle;
						return new DataReadValue_Id()
						{
							Id = id,
							SystemTime = DateTime.Now,
							DeviceTime = x.TimestampSpecified ? x.Timestamp : (DateTime?)null,
							Quality = x.QualitySpecified ? x.Quality.Convert() : Quality.Good,
							Value = Convert.ChangeType(x.Value, RealtimeData.DataSource.GlobalTags[id].Type)
						};
					}
				).ToArray();

				OnValues(result);
			}
			catch (Exception ex)
			{
				try
				{
					OnException(ex);
				}
				catch { }
			}
		}

	}

}
