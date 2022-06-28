using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Threading;

using iFlow.DataProvider;
using iFlow.Interfaces;
using iFlow.Shared;
using iFlow.Utils;
using Config = iFlow.DataProvider.Config;

//==================================================================================================================
//====  DataProvider  ====
//==================================================================================================================

public class Module : IModule
{
    public Module()
    {
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
        lock (LockObject)
            Instances.Add(this);
    }

    public void Dispose()
    {
        foreach (DataSourceInfo dataSource in DataSources)
            dataSource.DataSource.Dispose();
        ServiceHost?.Close();
        lock (LockObject)
            Instances.Remove(this);
    }

    public ILogger Logger { get; private set; }
    private readonly static object LockObject = new object();
    internal static List<Module> Instances { get; } = new List<Module>();
    internal ServiceHost ServiceHost { get; private set; }
    internal GlobalTags GlobalTags { get; private set; }

    internal class DataSourceInfo
    {
        public string Name { get; set; }
        public IDataSource DataSource { get; set; }
    }
    internal DataSourceInfo[] DataSources = new DataSourceInfo[0];

    public void Init(SystemConfiguration systemConfiguration, string configStr, ILogger logger)
    {
        Logger = logger;
        Config.Config config = ConfigHelper.Load<Config.Config>(configStr);
        GlobalTags = systemConfiguration.GlobalTags;

        //DataProviderInfo info = dataProviderInfos.FirstOrDefault(x => x.Address == config.Address);
        //if (info == null)
        //	throw new Exception($"В глобальном списке тэгов не найдено соответствие источнику данных \"{config.Address ?? "null"}\"");
        DataSources = CreateDataSources(config.DataSources, systemConfiguration.GlobalTags);

        ServiceHost = new ServiceHost
        (
            typeof(WcfDataProvider),
            new Uri[] { new Uri(config.Address) }
        );
        ServiceHost.AddServiceEndpoint
        (
            typeof(IDataProvider),
            new NetNamedPipeBinding() { MaxReceivedMessageSize = int.MaxValue },
            ""
        );
        ServiceHost.Open();
    }

    private class ProtocolInfo : UidName
    {
        public IDataProtocol Protocol;
    }

    private DataSourceInfo[] CreateDataSources(Config.DataSource[] cfgDataSources, GlobalTags globalTags)
    {
        //Config.DataSource[] duplicates = dataSources
        //	.Where(x => dataSources.Any(y => y != x && IgnoreCase.Equals(x.Name, y.Name, true)))
        //	.ToArray();
        //if (duplicates.Any())
        //{
        //	string[] errors = duplicates
        //		.Select(x => $"Имя источника данных {x.Name} дублируется.")
        //		.Distinct()
        //		.ToArray();
        //	throw new MultiException(errors);
        //}

        Collection<ProtocolInfo> protocolInfos = new Collection<ProtocolInfo>();
        return cfgDataSources
            .Select
            (
                cfgDataSource =>
                {
                    if (string.IsNullOrWhiteSpace(cfgDataSource.Protocol) && cfgDataSource.ProtocolUid == null)
                        throw new Exception($"Для подключаемого протокола базы данных не указаны ни наименование ни Uid");

                    //Guid uid = Guid.Empty;
                    UidName uidName = new UidName()
                    {
                        Uid = cfgDataSource.ProtocolUid, //Guid.TryParse(configProtocol.Uid, out uid) ? (Guid?)uid : null,
                        Name = cfgDataSource.Protocol
                    };


                    ProtocolInfo protocolInfo = protocolInfos.FirstOrDefault(x => x.EqualsTo(uidName.Uid, uidName.Name));
                    if (protocolInfo == null)
                    {
                        string startingClass = nameof(IDataProtocol).Substring(1);
                        protocolInfo = new ProtocolInfo()
                        {
                            Uid = uidName.Uid,
                            Name = uidName.Name,
                            //TODO Release/Debug
#if DebugMode
                            Protocol = new DataProtocol()
#else
							Protocol = (IDataProtocol)ProtocolLoader.Load(uidName, startingClass)
#endif
                        };
                        protocolInfos.Add(protocolInfo);
                    }

                    return new DataSourceInfo
                    {
                        Name = cfgDataSource.Name,
                        DataSource = protocolInfo.Protocol.CreateDataSource(globalTags, cfgDataSource.Config.ToString())
                    };
                }
            )
            .ToArray();
    }

    //==============================================================================================================

    //private static void ValidateTagMappings(IDataTag[] tagMappings)
    //{
    //	if (tagMappings == null)
    //		throw new Exception($"Некорректный таг-маппинг: null");

    //	IDataTag invalidMapping = tagMappings.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Mapping));
    //	if (invalidMapping != null)
    //		throw new Exception($"В таг-маппинге не указан параметр Mapping: {invalidMapping}");

    //	invalidMapping = tagMappings.FirstOrDefault(x => tagMappings.Any(y => x != y && x.Id == y.Id));
    //	if (invalidMapping != null)
    //	{
    //		IEnumerable<string> strings = tagMappings
    //			.Where(x => x.Id == invalidMapping.Id)
    //			.Select(x => x.ToString())
    //			.Take(10);
    //		throw new Exception($"В таг-маппингах дублируется параметр Id:\r\n{string.Join("\r\n", strings)}");
    //	}

    //	invalidMapping = tagMappings.FirstOrDefault(x => tagMappings.Any(y => x != y && x.Mapping == y.Mapping));
    //	if (invalidMapping != null)
    //	{
    //		IEnumerable<string> strings = tagMappings
    //			.Where(x => x.Mapping == invalidMapping.Mapping)
    //			.Select(x => x.ToString())
    //			.Take(10);
    //		throw new Exception($"В таг-маппингах дублируется параметр Mapping: \r\n{string.Join("\r\n", strings)}");
    //	}
    //}

}