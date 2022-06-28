using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace iFlow.DataProviders
{
	internal partial class SubscriptionDataSource : CustomSubscriptionDataSource<Config, TagMapping, Subscription>
	{

		public SubscriptionDataSource(string configStr, TagMapping[] tagMappings) : base(configStr, tagMappings, Params.DefaultUpdateRate)
		{
		}

		private HourlyChildDataSource HourlyDataSource;
		private MsgChildDataSource MsgDataSource;

		protected override Subscription InternalCreateSubscription(TimeSpan? updateRate = default(TimeSpan?), float? deadband = null)
		{
			Subscription subscription = new Subscription(TagMappings, DefaultUpdateRate, DefaultDeadband);
			subscription.Changed += Subscription_Changed;
			subscription.Disposed += Subscription_Disposed;

			if (HourlyDataSource == null)
				HourlyDataSource = new HourlyChildDataSource(Config, TagMappings);
			ISubscription hourlySubscription = HourlyDataSource.CreateSubscription(updateRate, deadband);
			hourlySubscription.UserData = subscription;
			subscription.HourlySubscription = hourlySubscription;

			if (MsgDataSource == null)
				MsgDataSource = new MsgChildDataSource(Config, TagMappings);
			ISubscription msgSubscription = MsgDataSource.CreateSubscription(updateRate, deadband);
			msgSubscription.UserData = subscription;
			subscription.MsgSubscription = msgSubscription;

			return subscription;
		}

		private void Subscription_Changed(object sender, EventArgs e)
		{
			Subscription subscription = (Subscription)sender;

			TagSubscription[] hourlyTags = subscription.TagStates
				.Where(x => x.Mapping.Mapping != Params.FtpMessageTag)
				.Select(x => x.Subscription)
				.ToArray();
			subscription.HourlySubscription.Change(hourlyTags);

			TagSubscription[] msgTags = subscription.TagStates
				.Where(x => x.Mapping.Mapping == Params.FtpMessageTag)
				.Select(x => x.Subscription)
				.ToArray();
			subscription.MsgSubscription.Change(msgTags);
		}

		private void Subscription_Disposed(object sender, EventArgs e)
		{
			Subscription subscription = (Subscription)sender;

			subscription.HourlySubscription.Dispose();
			subscription.MsgSubscription.Dispose();

			if (!Subscriptions.Any())
			{
				HourlyDataSource.Dispose();
				HourlyDataSource = null;

				MsgDataSource.Dispose();
				MsgDataSource = null;
			}
		}

	}

}