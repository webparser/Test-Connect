using System;
using System.Linq;
using iFlow.Interfaces;

namespace iFlow.DataProtocols
{
	internal class RealtimeData : AbstractRealtimeData<RealtimeSubscription>, IDisposable
	{
		public RealtimeData(DataSource dataSource)
			: base(dataSource)
		{
		}

		public new DataSource DataSource => (DataSource)base.DataSource;

		public override TimeSpan MinUpdateRate => TimeSpan.FromSeconds(1);

		protected override RealtimeSubscription CreateSubscription_Internal()
		{
			return new RealtimeSubscription(this);
		}


		//public override void FreeSubscription(IRealtimeSubscription2 subscription)
		//{
		//	base.FreeSubscription(subscription);
		//	TODO Закрытие OPC-клиента
		//	if (DataSource.OpcClient.Server.Subscriptions.Count == 0)
		//		DataSource.OpcClient.Dispose();
		//}

		public override DataReadValue_Id[] Read(int[] ids)
		{
			DataSource.GlobalTags.CheckTagIds(ids);

			Opc.Da.Item[] opcItems = ids
				.Select
				(
					x => new Opc.Da.Item()
					{
						Active = true,
						ActiveSpecified = true,
						ClientHandle = x,
						ItemName = DataSource.TagAddresses[x], // @"FCX01!FT345B.W3.DINTS(1)"
					}
				)
				.ToArray();
			Opc.Da.ItemValueResult[] results = DataSource.OpcClient.Server.Read(opcItems);

			DateTime now = DateTime.Now;

			return results
				.Select
				(
					x => new DataReadValue_Id()
					{
						Id = (int)x.ClientHandle,
						Quality = x.Quality.Convert(),
						Result = x.ResultID.Convert(),
						DeviceTime = x.TimestampSpecified ? (DateTime?)x.Timestamp : null,
						SystemTime = now,
						Value = x.Value
					}
				)
				.ToArray();
		}

		public override DataReadValue_Address[] Read(string[] addresses)
		{
			Opc.Da.Item[] opcItems = addresses
				.Select
				(
					x => new Opc.Da.Item()
					{
						Active = true,
						ActiveSpecified = true,
						ClientHandle = x,
						ItemName = x, // @"FCX01!FT345B.W3.DINTS(1)"
					}
				)
				.ToArray();
			Opc.Da.ItemValueResult[] results = DataSource.OpcClient.Server.Read(opcItems);

			DateTime now = DateTime.Now;

			return results
				.Select
				(
					x => new DataReadValue_Address()
					{
                        Address = (string)x.ClientHandle,
						Quality = x.Quality.Convert(),
						Result = x.ResultID.Convert(),
						DeviceTime = x.TimestampSpecified ? (DateTime?)x.Timestamp : null,
						SystemTime = now,
						Value = x.Value
					}
				)
				.ToArray();
		}

		public override void Write(DataWriteValue_Id[] values)
		{
			DataSource.GlobalTags.CheckTagIds(values);

			Opc.Da.ItemValue[] opcItems = values
				.Select
				(
					x => new Opc.Da.ItemValue()
					{
						ClientHandle = x.Id,
						ItemName = DataSource.TagAddresses[x.Id], // @"FCX01!FT345B.W3.DINTS(1)"
						Value = x.Value
					}
				)
				.ToArray();
			Opc.IdentifiedResult[] results = DataSource.OpcClient.Server.Write(opcItems);
			//TODO
			//foreach (Opc.IdentifiedResult result in results)
			//{
			//	TagValue tagValue = (TagValue)result.ClientHandle;
			//	tagValue.Result = result.ResultID.Convert();
			//}
		}

		public override void Write(DataWriteValue_Address[] values)
		{
			Opc.Da.ItemValue[] opcItems = values
				.Select
				(
					x => new Opc.Da.ItemValue()
					{
						ClientHandle = x.Address,
						ItemName = x.Address, // @"FCX01!FT345B.W3.DINTS(1)"
						Value = x.Value
					}
				)
				.ToArray();
			Opc.IdentifiedResult[] results = DataSource.OpcClient.Server.Write(opcItems);
			//TODO
			//foreach (Opc.IdentifiedResult result in results)
			//{
			//	TagValue tagValue = (TagValue)result.ClientHandle;
			//	tagValue.Result = result.ResultID.Convert();
			//}
		}

	}

}
