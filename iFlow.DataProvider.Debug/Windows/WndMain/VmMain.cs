using System;
using System.Linq;
using System.Windows.Threading;
using iFlow.Interfaces;
using iFlow.ServiceDispatcher;
using iFlow.Utils;
using iFlow.Wpf;

namespace iFlow.DataProvider
{
    public class VmMain : VmBase
    {
        public VmMain(Dispatcher dispatcher)
        {
            module = new Module();

            AddedHandler addedHandler = (s, message) => dispatcher.BeginInvoke
             (
                 new Action(() =>
                 {
                     Log += message;
                     OnPropertyChanged(nameof(Log));
                 }),
                 DispatcherPriority.Background
             );

            if (CommandLineHelper.ParamExists("debug"))
                Logger.DebugAdded += addedHandler;
            else
                Logger.Added += addedHandler;

            ServiceDispatcher = new ServiceDispatcherProxy(ServiceShared.CommunicationParams.sServiceDispatcherAddress, Logger.Default);
            ServiceDispatcher.Opened += ServiceDispatcher_Opened;
            ServiceDispatcher.Closed += ServiceDispatcher_Closed;
            ServiceDispatcher.Faulted += ServiceDispatcher_Faulted;
            ServiceDispatcher.Start();
        }

        private Module module;
        private ServiceDispatcherProxy ServiceDispatcher;
        private IRealtimeSubscription Subscription;

        public VmTag[] VmTags
        {
            get => vmTags?.Values.ToArray();
        }
        private IdDictionary<VmTag> vmTags;

        public string Log { get; private set; }

        private void ServiceDispatcher_Opened(object sender, EventArgs e)
        {
            ServiceConfiguration serviceConfiguration = ServiceDispatcher.GetServiceConfiguration("iFlow.DataProvider");
            SystemConfiguration systemConfiguration = ServiceDispatcher.GetSystemConfiguration();

            try
            {
                module.Init(systemConfiguration, serviceConfiguration.Modules.First(x => x.Name == "iFlow.DataProvider").Config, Logger.Default);
                IRealtimeData realtimeData = module.DataSources.First().DataSource.RealtimeData;

                realtimeData.Read(new int[] { 1 });
                if (realtimeData is IFakeRealtimeData fakeData)
                {
                    vmTags = module.GlobalTags
                        .Select
                        (
                            (x, index) =>
                            {
                                VmTag vmTag = null;
                                switch (x.Type.GetSimplified())
                                {
                                    case SimpleType.Bool:
                                        vmTag = (VmTag)new VmBoolTag(index + 1, x)
                                        {
                                            Value = (bool)fakeData.GetValue(x.Id),
                                            IsRandom = fakeData.GetIsRandom(x.Id)
                                        };
                                        break;
                                    case SimpleType.Int:
                                        vmTag = (VmTag)new VmIntTag(index + 1, x)
                                        {
                                            Value = (int)fakeData.GetValue(x.Id),
                                            MinValue = (int)fakeData.GetMinValue(x.Id),
                                            MaxValue = (int)fakeData.GetMaxValue(x.Id),
                                            IsRandom = fakeData.GetIsRandom(x.Id)
                                        };
                                        ((VmIntTag)vmTag).MinChanged += (s, ee) => { VmIntTag t = (VmIntTag)s; fakeData.SetMinValue(t.Tag.Id, t.MinValue); };
                                        ((VmIntTag)vmTag).MaxChanged += (s, ee) => { VmIntTag t = (VmIntTag)s; fakeData.SetMaxValue(t.Tag.Id, t.MaxValue); };
                                        break;
                                    case SimpleType.Float:
                                        vmTag = (VmTag)new VmFloatTag(index + 1, x)
                                        {
                                            Value = (double)fakeData.GetValue(x.Id),
                                            MinValue = (double)fakeData.GetMinValue(x.Id),
                                            MaxValue = (double)fakeData.GetMaxValue(x.Id),
                                            IsRandom = fakeData.GetIsRandom(x.Id)
                                        };
                                        ((VmFloatTag)vmTag).MinChanged += (s, ee) => { VmFloatTag t = (VmFloatTag)s; fakeData.SetMinValue(t.Tag.Id, t.MinValue); };
                                        ((VmFloatTag)vmTag).MaxChanged += (s, ee) => { VmFloatTag t = (VmFloatTag)s; fakeData.SetMaxValue(t.Tag.Id, t.MaxValue); };
                                        break;
                                    case SimpleType.String:
                                        vmTag = (VmTag)new VmStringTag(index + 1, x)
                                        {
                                            Value = (string)fakeData.GetValue(x.Id),
                                            IsRandom = fakeData.GetIsRandom(x.Id)
                                        };
                                        break;
                                    default: throw new WrongTypeException(x.Type.GetSimplified());
                                }
                                vmTag.Changed += VmTag_Change;
                                vmTag.IsRandomChanged += (s, ee) => { VmTag t = (VmFloatTag)s; fakeData.SetIsRandom(t.Tag.Id, t.IsRandom); };
                                return vmTag;
                            }
                        )
                        .ToIdDictionary(x => x.Tag.Id, x => x);
                    OnPropertyChanged(nameof(VmTags));

                    Subscription = realtimeData.CreateSubscription(TimeSpan.FromSeconds(1));
                    Subscription.SubscribeTags = vmTags.Select(x => new RealtimeSubscribeTag() { Id = x.Key }).ToArray();
                    Subscription.ValuesEvent += Subscription_ValuesEvent;
                }
            }
            catch (Exception ex)
            {
                WndError.Show(ex);
            }
        }

        private void VmTag_Change(object sender, EventArgs e)
        {
            VmTag vmTag = (VmTag)sender;
            ((IFakeRealtimeData)module.DataSources.First().DataSource.RealtimeData).SetValue(vmTag.Tag.Id, vmTag.Value);
        }

        private void Subscription_ValuesEvent(object sender, DataReadValue_Id[] values)
        {
            foreach (DataReadValue_Id value in values)
                vmTags[value.Id].Update(value.Value);
        }

        private void ServiceDispatcher_Closed(object sender, EventArgs e)
        {
            vmTags = null;
            OnPropertyChanged(nameof(VmTags));
            Subscription?.Dispose();
            module?.Dispose();
        }

        private void ServiceDispatcher_Faulted(object sender, EventArgs e)
        {
            vmTags = null;
            OnPropertyChanged(nameof(VmTags));
            Subscription?.Dispose();
            module?.Dispose();
        }

    }

}
