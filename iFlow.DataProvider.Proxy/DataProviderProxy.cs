using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using iFlow.Interfaces;
using iFlow.Shared;
using iFlow.Utils;

namespace iFlow.DataProvider
{
    public delegate void ValuesHandler(object sender, Guid uid, DataReadValue_Id[] tagValues);
    public delegate void ErrorHandler(object sender, Exception ex);

    public class DataProviderProxy : Proxy<IDataProvider>, IDataProviderCallback
    {
        public DataProviderProxy(GlobalTags globaltags, string serviceAddress, ILogger logger, System.Threading.SynchronizationContext syncContext = null)
            : base(serviceAddress, logger, syncContext)
        {
            GlobalTags = globaltags;
        }

        public override void Dispose()
        {
            Unsubscribe();
            base.Dispose();
        }

        private GlobalTags GlobalTags;

        private class SubscriptionInfo
        {
            public Guid Uid = new Guid();
            public ValuesHandler ValuesHandler;
            public RealtimeSubscribeTag[] Tags;
            public TimeSpan? UpdateRate;
        }
        private List<SubscriptionInfo> Subscriptions { get; } = new List<SubscriptionInfo>();

        public void Subscribe(Guid uid, ValuesHandler handler, RealtimeSubscribeTag[] tags, TimeSpan? updateRate = null)
        {
            try
            {
                lock (Subscriptions)
                {
                    SubscriptionInfo info = Subscriptions.FirstOrDefault(x => x.Uid == uid);
                    if (info == null)
                    {
                        info = new SubscriptionInfo();
                        Subscriptions.Add(info);
                    }
                    info.ValuesHandler = handler;
                    info.Tags = tags;
                    info.Uid = uid;
                    info.UpdateRate = updateRate;
                    if (IsConnected)
                        Service.Subscribe(info.Uid, info.Tags, info.UpdateRate);
                }
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException)
            {
                Reconnect();
                throw;
            }
        }

        public void Unsubscribe()
        {
            try
            {
                lock (Subscriptions)
                {
                    foreach (SubscriptionInfo info in Subscriptions.ToArray())
                    {
                        Subscriptions.Remove(info);
                        if (IsConnected)
                            Service.Unsubscribe(info.Uid);
                    }
                }
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException)
            {
                Reconnect();
                throw;
            }
        }

        public void Unsubscribe(Guid uid)
        {
            try
            {
                lock (Subscriptions)
                {
                    SubscriptionInfo info = Subscriptions.FirstOrDefault(x => x.Uid == uid);
                    if (info != null)
                    {
                        Subscriptions.Remove(info);
                        if (IsConnected)
                            Service.Unsubscribe(info.Uid);
                    }
                }
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException)
            {
                Reconnect();
                throw;
            }
        }

        protected override void OnOpened(object sender, EventArgs e)
        {
            base.OnOpened(sender, e);
            try
            {
                foreach (SubscriptionInfo info in Subscriptions)
                    Service.Subscribe(info.Uid, info.Tags, info.UpdateRate);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException)
            {
                Logger.Add(ex);
                Reconnect();
                throw;
            }
        }

        public void Write(int tagId, object value)
        {
            GlobalTags.CheckTagId(tagId);

            CheckConnected();
            try
            {
                Service.Write(tagId, value);
            }
            catch (Exception ex) when (ex is CommunicationException || ex is TimeoutException)
            {
                Reconnect();
                throw;
            }
        }

        public void OnValues(Guid subscriptionUid, DataReadValue_Id[] values)
        {
            SyncContext.Post(OnValues_Scheduled, subscriptionUid, values);
        }

        public void OnValues_Scheduled(Guid subscriptionUid, DataReadValue_Id[] values)
        {
            try
            {
                SubscriptionInfo info = null;
                lock (Subscriptions)
                    info = Subscriptions.FirstOrDefault(x => x.Uid == subscriptionUid);
                if (info == null)
                    return;
                info.ValuesHandler(this, subscriptionUid, values);
            }
            catch (Exception ex)
            {
                Logger.Add(ex);
            }
        }

    }

}
