using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

using iFlow.Utils;

namespace iFlow.DataProviders
{
	//public delegate void DataHandler<T>(string dataSourceName, string serverName, T[] tags) where T : FtpData;

 //   internal class FtpProvider
 //   {
 //       public FtpProvider(string dataSourceName, Config.FtpServerConfig config) : base()
 //       {
 //           DataSourceName = dataSourceName;
 //           Config = config;
 //       }

 //       private readonly string DataSourceName;
 //       private readonly Config.FtpServerConfig Config;
 //       private HourlyProvider HourlyProv;
 //       private MsgProvider MsgProv;

 //       public void Subscribe(DataHandler<TagData> dataHandler, int? lastId = null)
 //       {
 //           if (HourlyProv == null)
 //               HourlyProv = new HourlyProvider(DataSourceName, Config);
 //           HourlyProv.Subscribe(dataHandler, lastId);
 //       }

 //       public void Subscribe(DataHandler<MsgData> dataHandler, int? lastId = null)
 //       {
 //           if (MsgProv == null)
 //               MsgProv = new MsgProvider(DataSourceName, Config);
 //           MsgProv.Subscribe(dataHandler, lastId);
 //       }

 //       public void Unsubscribe(DataHandler<TagData> dataHandler)
 //       {
 //           if (HourlyProv == null)
 //               throw new Exception("");
 //           HourlyProv.Unsubscribe(dataHandler);
 //           if (!HourlyProv.Subscriptions.Any())
 //               HourlyProv = null;
 //       }
 //       public void Unsubscribe(DataHandler<MsgData> dataHandler)
 //       {
 //           if (MsgProv == null)
 //               throw new Exception("");
 //           MsgProv.Unsubscribe(dataHandler);
 //           if (!MsgProv.Subscriptions.Any())
 //               MsgProv = null;
 //       }

 //       //==============================================================================================================
 //       //==  DataManager  ==
 //       //==============================================================================================================

        

 //       //==============================================================================================================
 //       //==  AbstractProvider  ==
 //       //==============================================================================================================

 //       private abstract class AbstractProvider<T> where T : FtpData
 //       {
 //           public AbstractProvider(string dataSourceName, Config.FtpServerConfig config)
 //               : base()
 //           {
 //               DataSourceName = dataSourceName;
 //               Config = config;

 //               Subscriptions = new Collection<Subscription<T>>();
 //               FtpService = UnitTest.FtpService ?? new FtpService();

 //               tmrUpdate = (IFtpTimer)(UnitTest.UpdateTimerType ?? typeof(FtpTimer)).GetConstructor(new Type[0]).Invoke(new object[0]);
 //               tmrUpdate.Elapsed += tmrUpdate_Elapsed;
 //           }

 //           private readonly AbstractFtpService FtpService;
 //           private readonly string DataSourceName;
 //           private readonly IFtpTimer tmrUpdate;
 //           public Collection<Subscription<T>> Subscriptions;
            

 //           public void Subscribe(DataHandler<T> dataHandler, int? lastId = null)
 //           {
 //               Subscription<T> subscription = new Subscription<T>()
 //               {
 //                   DataHandler = dataHandler,
 //                   LastId = lastId,
 //                   LastTime = DateTime.MinValue
 //               };
 //               Subscriptions.Add(subscription);
 //               GetHistory(subscription);

 //               tmrUpdate.Start();
 //           }

 //           public void Unsubscribe(DataHandler<T> dataHandler)
 //           {
 //               Subscription<T> subscription = Subscriptions.FirstOrDefault(x => x.DataHandler == dataHandler);
 //               if (subscription == null)
 //                   throw new Exception();
 //               Subscriptions.Remove(subscription);
 //               if (!Subscriptions.Any())
 //                   tmrUpdate.Stop();
 //           }

 //           private string[] ExtractCsv(string fileName, byte[] data)
 //           {
 //               string csvName = Path.ChangeExtension(Path.GetFileName(fileName), ".csv");
 //               string dataStr = ZipUtils.DecompressFromFile(data, Encoding.GetEncoding(1251), csvName);
 //               return dataStr.Split(new string[] { "\r\n" }, StringSplitOptions.None).ToArray();
 //           }

 //           protected abstract T[] Parse(DateTime fileDate, string[] lines);

 //           /// <summary>
 //           /// Запрос размера файла на Ftp-сервере. Размер может запрашиваться тремя способами, в зависимости от того, реализует
 //           /// Ftp-сервер те или иные возможности или нет: 1 - непосредственный запрос размера файла (Ftp-команда "SIZE {FileName}"),
 //           /// 2 - запрос списка файлов, с получением полной информации по каждому файлу, в том числе размера, 3 - скачивание файла
 //           /// и замер количества полученных данных.
 //           /// </summary>
 //           /// <param name="fileName"></param>
 //           /// <param name="data"></param>
 //           /// <returns></returns>
 //           protected long GetFileSize(string fileName, out byte[] data)
 //           {
 //               data = null;

 //               // Сначала пытаемся получить размер файла Ftp-командой SIZE
 //               if (Features.SupportFileSize)
 //               {
 //                   long? size;
 //                   if (FtpService.GetFileSize(Config.Server, GetFtpPath(fileName), Config.UserName, Config.Password, out size))
 //                       return size.Value;
 //                   else
 //                       // Ftp-сервер не поддерживает данный тип запроса. Больше не будем его использовать.
 //                       Features.SupportFileSize = false;
 //               }

 //               // Затем пробуем получить размер файла скачав содержимое каталога с подробностями по каждому файлу.
 //               if (Features.SupportFileSizeInList)
 //               {
 //                   // Определем что нам выгоднее по скорости, скачать содержимое каталога файлов или скачать непосредственно файл.
 //                   if (ListDownloadSpeedometer.Duration < FileDownloadSpeedometer.Duration)
 //                   {
 //                       // Начинаем замер длительности скачивания содержимого каталога.
 //                       ListDownloadSpeedometer.Start();
 //                       try
 //                       {
 //                           FtpFileInfo[] ftpList = FtpService.GetFileList(Config.Server, GetFtpPath(fileName), Config.UserName, Config.Password, FilePrefix);
 //                           // Вычленяем информацию по нашему искомому файлу
 //                           FtpFileInfo ftpFile = ftpList.FirstOrDefault(x => string.Compare(x.Name, fileName, true) == 0);
 //                           // Опс! А файла-то и нет в каталоге! Какая-то ошибка программы!
 //                           if (ftpFile == null)
 //                               throw new Exception("");
 //                           if (ftpFile.Size != null)
 //                               return ftpFile.Size.Value;
 //                           // Ftp-сервер не поддерживает данный тип запроса. Больше не будем его использовать.
 //                           Features.SupportFileSizeInList = false;
 //                       }
 //                       finally
 //                       {
 //                           ListDownloadSpeedometer.Stop();
 //                       }
 //                   }
 //               }

 //               // Первые два способа не прошли. Скачиваем сам файл и замеряем количество полученных данных.
 //               FileDownloadSpeedometer.Start();
 //               try
 //               {
 //                   data = FtpService.GetFile(Config.Server, GetFtpPath(fileName), Config.UserName, Config.Password);
 //                   return data.Length;
 //               }
 //               finally
 //               {
 //                   FileDownloadSpeedometer.Stop();
 //               }
 //           }

 //           protected void GetHistory(Subscription<T> tempSubscription = null)
 //           {
 //               Subscription<T>[] allSubscriptions = Subscriptions.ToArray();
 //               if (tempSubscription != null)
 //                   allSubscriptions = allSubscriptions.Union(new Subscription<T>[] { tempSubscription }).ToArray();

 //               FtpFileInfo[] ftpFiles;

 //               if ((Subscriptions.GetMinDate().Date == DateTime.Now.Date) && (Subscriptions.GetMinDate().Date == LastFileInfo.Date.Date))
 //               {
 //                   // Не знаем текущую длину файла, поэтому ставим null
 //                   ftpFiles = new FtpFileInfo[] { new FtpFileInfo(LastFileInfo.Name, LastFileInfo.Date, null) };
 //               }
 //               else
 //               {
 //                   Logger.Add("GetHistory: Begin GetFileList");
 //                   ListDownloadSpeedometer.Start();
 //                   try
 //                   {
 //                       ftpFiles = FtpService
 //                           .GetFileList(Config.Server, FtpDir, Config.UserName, Config.Password, FilePrefix)
 //                           .Where(x => x.Date.Date >= allSubscriptions.GetMinDate().Date)
 //                           .ToArray();
 //                   }
 //                   finally
 //                   {
 //                       ListDownloadSpeedometer.Stop();
 //                   }
 //                   Logger.Add("GetHistory: End GetFileList: {0}", ftpFiles.Length);
 //               }

 //               foreach (FtpFileInfo ftpFile in ftpFiles)
 //               {
 //                   byte[] data = null;
 //                   if (ftpFile.Name == LastFileInfo.Name)
 //                   {
 //                       if (ftpFile.Size == null)
 //                           ftpFile.Size = GetFileSize(ftpFile.Name, out data);
 //                       if (ftpFile.Size == LastFileInfo.Size)
 //                           return;
 //                   }

 //                   if (data == null)
 //                   {
 //                       FileDownloadSpeedometer.Start();
 //                       try
 //                       {
 //                           data = FtpService.GetFile(Config.Server, GetFtpPath(ftpFile.Name), Config.UserName, Config.Password);
 //                       }
 //                       finally
 //                       {
 //                           FileDownloadSpeedometer.Stop();
 //                       }
 //                   }

 //                   if (LastFileInfo.Date.Date == ftpFile.Date.Date)
 //                       LastFileInfo.Size = data.Length;

 //                   string[] lines = ExtractCsv(ftpFile.Name, data);
 //                   T[] parsedData = Parse(ftpFile.Date, lines);

 //                   foreach (Subscription<T> subscription in allSubscriptions)
 //                   {
 //                       T[] subData = null;
 //                       if (subscription.LastId != null)
 //                           subData = parsedData.Where(x => x.ControllerId > subscription.LastId.Value).ToArray();
 //                       else
 //                           subData = parsedData.Where(x => x.Time > subscription.LastTime).ToArray();
 //                       if (subData.Any())
 //                           subscription.DataHandler(DataSourceName, Config.Name, subData);
 //                       if (subData.Any())
 //                           subscription.LastId = subData.Max(x => x.ControllerId);
 //                   }
 //               }

 //               FtpFileInfo last = ftpFiles.LastOrDefault();
 //               if (last != null)
 //                   if ((LastFileInfo.Name == null) || (last.Date.Date > LastFileInfo.Date.Date))
 //                       LastFileInfo = last;
 //           }

 //           private void tmrUpdate_Elapsed(object sender, EventArgs e)
 //           {
 //               tmrUpdate.Stop();
 //               try
 //               {
 //                   CheckUpdate();
 //               }
 //               catch (Exception ex)
 //               {
 //                   Logger.Add(ex);
 //               }
 //               finally
 //               {
 //                   tmrUpdate.Start();
 //               }
 //           }

 //           protected virtual void CheckUpdate()
 //           {
 //               GetHistory();
 //           }

 //       }

 //       //==============================================================================================================
 //       //==  HourlyProvider  ==
 //       //==============================================================================================================

 //       private delegate void TagDataHandler(TagData[] data);

 //       /// <summary>
 //       /// 
 //       /// </summary>
 //       private class HourlyProvider : AbstractProvider<TagData>
 //       {
 //           public HourlyProvider(string dataSourceName, Config.FtpServerConfig config)
 //               : base(dataSourceName, config)
 //           {
 //           }

 //           protected override string FtpDir { get { return "//JEROS/USERS/APPF/JLOG/LOG"; } }
 //           protected override string FilePrefix { get { return "Hourly_"; } }

 //           protected override TagData[] Parse(DateTime fileDate, string[] lines)
 //           {
 //               if (lines == null)
 //                   throw new Exception("lines == null");
 //               if (lines.Length < 2)
 //                   throw new Exception("Некорректный формат данных");

 //               // Пропускаем столбец с датой
 //               string[] tagNames = lines[0].Split(',').Skip(1).ToArray();

 //               // две строки заголовка
 //               lines = lines.Skip(2).ToArray();

 //               return lines.Where(x => !string.IsNullOrWhiteSpace(x)).SelectMany
 //               (
 //                   line =>
 //                   {
 //                       string[] items = line.Split(',');
 //                       DateTime time = DateTime.ParseExact(items[0], "yyyy/MM/dd HH:mm:ss", null);

 //                       // Пропускаем столбец с датой
 //                       string[] values = items.Skip(1).ToArray();
 //                       if (values.Length != tagNames.Length)
 //                           throw new Exception("Некорректный формат данных");

 //                       return values.Select
 //                       (
 //                           (value, index) => new TagData()
 //                           {
 //                               ControllerId = Utils.ComposeIndex(fileDate, index),
 //                               TagName = tagNames[index],
 //                               Time = time,
 //                               Value = value
 //                           }
 //                       );
 //                   }
 //               ).ToArray();
 //           }

 //       }

 //       //==============================================================================================================
 //       //==  MsgProvider  ==
 //       //==============================================================================================================

 //       /// <summary>
 //       /// 
 //       /// </summary>
 //       private class MsgProvider : AbstractProvider<MsgData>
 //       {
 //           public MsgProvider(string dataSourceName, Config.FtpServerConfig config)
 //               : base(dataSourceName, config)
 //           {
 //           }

 //           protected override string FtpDir { get { return "//JEROS/USERS/APPF/JLOG/MSG"; } }
 //           protected override string FilePrefix { get { return "Msg_"; } }

 //           protected override MsgData[] Parse(DateTime fileDate, string[] lines)
 //           {
 //               if (lines == null)
 //                   throw new Exception("lines == null");

 //               return lines.Select
 //               (
 //                   (line, index) =>
 //                   {
 //                       string[] values = line.Split(',');
 //                       if (values.Length != 3)
 //                           throw new Exception("Некорректный формат данных");

 //                       return new MsgData()
 //                       {
 //                           ControllerId = Utils.ComposeIndex(fileDate, index),
 //                           Time = DateTime.ParseExact(values[0], "yyyy/MM/dd HH:mm:ss.fff", null),
 //                           Code = values[1],
 //                           Text = values[2]
 //                       };
 //                   }
 //               ).ToArray();
 //           }

 //       }

 //   }

}
