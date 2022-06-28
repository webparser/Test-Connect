using System;
using System.IO;
using System.Linq;

using iFlow.Utils;

namespace iFlow.DataProviders
{
    //public interface ISlowDataManager
    //{
    //    void GetTagHistory(DataHandler<TagData> dataHandler, int lastId);
    //    void Subscribe(DataHandler<TagData> dataHandler, int? lastId = null);
    //    void Unsubscribe(DataHandler<TagData> dataHandler);
    //    void GetMsgHistory(DataHandler<MsgData> dataHandler, int lastId);
    //    void Subscribe(DataHandler<MsgData> dataHandler, int? lastId = null);
    //    void Unsubscribe(DataHandler<MsgData> dataHandler);
    //}

    //public class SlowDataManager : ISlowDataManager
    //{
    //    public SlowDataManager() : base()
    //    {
    //    }

    //    private FtpProvider[] FtpProviders;

    //    public void Start()
    //    {
    //        try
    //        {
    //            //Config config = new Config();
    //            //Config.DataSourceConfig dataSource = new Config.DataSourceConfig()
    //            //{
    //            //    Name = "DataSource1",
    //            //    FtpCommon = new Config.FtpCommonConfig()
    //            //    {
    //            //        UserName = "stardom",
    //            //        Password = "YOKOGAWA",
    //            //        HourlyEnabled = true,
    //            //        MsgEnabled = true
    //            //    }
    //            //};
    //            //config.DataSources.Add(dataSource);

    //            //Config.FtpServerConfig ftpServer = new Config.FtpServerConfig()
    //            //{
    //            //    Name = "FCX01",
    //            //    Server = "10.26.8.225"
    //            //};
    //            //dataSource.FtpServers.Add(ftpServer);
    //            //Config.Save(@"d:\Ftp.Config.xml", config);

    //            Config config = Config.Load(@"d:\Ftp.Config.xml");
    //            Logger.LogDir = config.System.LogDir;

    //            FtpProviders = config.DataSources
    //                .SelectMany(x => x.FtpServers.Select(y => new FtpProvider(x.Name, y)))
    //                .ToArray();
    //            Logger.Add("Служба запущена");
    //        }
    //        catch (Exception ex)
    //        {
    //            Logger.Add(ex);
    //        }
    //    }

    //    public void Stop()
    //    {
    //        try
    //        {
    //            Logger.Add("Служба остановлена");
    //        }
    //        catch (Exception ex)
    //        {
    //            Logger.Add(ex);
    //        }
    //    }

    //    private void tmrStartup_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    //    {
    //        try
    //        {
    //        }
    //        catch (Exception ex)
    //        {
    //            Logger.Add(ex);
    //        }
    //    }

    //    public void GetTagHistory(DataHandler<TagData> dataHandler, int lastId)
    //    {

    //    }

    //    public void Subscribe(DataHandler<TagData> dataHandler, int? lastId = null)
    //    {
    //        foreach (FtpProvider propvider in FtpProviders)
    //            propvider.Subscribe(dataHandler, lastId);
    //    }

    //    public void Unsubscribe(DataHandler<TagData> dataHandler)
    //    {
    //        foreach (FtpProvider propvider in FtpProviders)
    //            propvider.Unsubscribe(dataHandler);
    //    }

    //    public void GetMsgHistory(DataHandler<MsgData> dataHandler, int lastId)
    //    {
    //    }

    //    public void Subscribe(DataHandler<MsgData> dataHandler, int? lastId = null)
    //    {
    //        foreach (FtpProvider propvider in FtpProviders)
    //            propvider.Subscribe(dataHandler, lastId);
    //    }

    //    public void Unsubscribe(DataHandler<MsgData> dataHandler)
    //    {
    //        foreach (FtpProvider propvider in FtpProviders)
    //            propvider.Unsubscribe(dataHandler);
    //    }

    //}

}
