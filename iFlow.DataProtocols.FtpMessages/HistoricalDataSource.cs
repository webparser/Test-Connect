using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iFlow.Interfaces;
using iFlow.Utils;

namespace iFlow.DataProtocols
{
    public class HistoricalDataSource : CustomHistoricalData<DataTag, Subscription>
    {
        private const string sFtpDir = "//JEROS/USERS/APPF/JLOG/MSG";
        private const string sFilePrefix = "Msg_";

        public HistoricalDataSource(string configStr, IDataTag[] tagMappings)
            : base(configStr, tagMappings, Params.DefaultUpdateRate, Params.MinUpdateRate)
        {
            MessageTagMapping = TagMappings.FirstOrDefault(x => x.Address== Params.ReservedTagNames.Message);
            if (MessageTagMapping == null)
                throw new Exception("");
            FtpClient = new CustomFtpClient(Config.Address, Config.Port ?? Params.DefaultFtpPort, sFtpDir, Config.UserName, Config.Password);
        }

        private readonly DataTag MessageTagMapping;
        private readonly CustomFtpClient FtpClient;

        private struct ContentInfo
        {
            public DateTime? LastTime;
            public int LastIndex;
        }
        private class FileInfo
        {
            public FtpFileInfo Ftp = null;
            public ContentInfo Content;
        }
        private FileInfo[] FileList = new FileInfo[0];

        protected override Subscription InternalCreateSubscription(TimeSpan? updateRate = null, float? deadband = null)
        {
            return new Subscription(TagMappings, updateRate ?? DefaultUpdateRate, Params.MinUpdateRate, deadband);
        }

        private IEnumerable<FileInfo> QuickJoin(IEnumerable<FileInfo> fileList, IEnumerable<FtpFileInfo> ftpFileList)
        {
            IEnumerator<FileInfo> fileEnum = fileList.GetEnumerator();
            FileInfo fileInfo = fileEnum.MoveNext() ? fileEnum.Current : null;
            IEnumerator<FtpFileInfo> ftpFileEnum = ftpFileList.OrderBy(x => x.Date).GetEnumerator();
            FtpFileInfo ftpFileInfo = ftpFileEnum.MoveNext() ? ftpFileEnum.Current : null;
            while (fileInfo != null || ftpFileInfo != null)
            {
                if (fileInfo != null)
                {
                    if (ftpFileInfo != null)
                    {
                        switch (fileInfo.Ftp.Date.Date.CompareTo(ftpFileInfo.Date.Date))
                        {
                            case -1:
                                //  Файл был, но исчез. Удаляем инфу о нем.
                                fileInfo = fileEnum.MoveNext() ? fileEnum.Current : null;
                                break;
                            case 0:
                                yield return fileInfo;
                                fileInfo = fileEnum.MoveNext() ? fileEnum.Current : null;
                                ftpFileInfo = ftpFileEnum.MoveNext() ? ftpFileEnum.Current : null;
                                break;
                            case 1:
                                // Появился новый файл.
                                yield return new FileInfo()
                                {
                                    Ftp = ftpFileInfo
                                };
                                ftpFileInfo = ftpFileEnum.MoveNext() ? ftpFileEnum.Current : null;
                                break;
                            default:
                                throw new Exception("");
                        }

                    }
                    // else
                    //   Файл был, но исчез. Удаляем инфу о нем.
                }
                else
                {
                    yield return new FileInfo()
                    {
                        Ftp = ftpFileInfo
                    };
                    ftpFileInfo = ftpFileEnum.MoveNext() ? ftpFileEnum.Current : null;
                }
            }
        }

        private FileInfo FirstForDownload(TagRequest[] tagRequests, bool includeBounds)
        {
            TagRequest minTagRequest = tagRequests
                .OrderBy(x => x, TagRequest.Comparer)
                .First();

            foreach (FileInfo fileInfo in FileList)
            {
                if (fileInfo.Content.LastTime != null)
                    if (minTagRequest.FromTime == fileInfo.Content.LastTime.Value)
                    {
                        if (includeBounds ? minTagRequest.FromIndex <= fileInfo.Content.LastIndex : minTagRequest.FromIndex < fileInfo.Content.LastIndex)
                            return fileInfo;
                    }
                    else
                    {
                        if (minTagRequest.FromTime < fileInfo.Content.LastTime.Value)
                            return fileInfo;
                    }
                else if (minTagRequest.FromTime.Date <= fileInfo.Ftp.Date.Date)
                    return fileInfo;
            }
            if (minTagRequest.FromTime < DateTime.Now)
                return FileList.LastOrDefault();
            return null;
        }

        protected override TagValueSet[] GetValuesChunk(TagRequest[] tagRequests, bool includeBounds)
        {
            //base.GetValuesChunk();
            if (!tagRequests.Any())
                throw new Exception("Некорректный запрос данных. Список запрашиваемых тэгов пуст.");

            if (!FtpClient.Ping())
                throw new Exception("");

            // Если список файлов пуст (еще не считывали), считываем его.
            if (!FileList.Any())
            {
                FtpFileInfo[] ftpFileList = FtpClient.DownloadFileList();
                if (!ftpFileList.Any())
                    return new TagValueSet[0];
                FileList = QuickJoin(FileList, ftpFileList).ToArray();
            }

            // Берем первый файл, удовлетворяющий ограничению времени.
            FileInfo fileInfo = FirstForDownload(tagRequests, includeBounds);
            if (fileInfo == null)
                return new TagValueSet[0];

            byte[] data = null;
            // Если запрашиваются данные последнего файла в списке, то проверяем, не появились ли свежие данные.
            // В противном случае, просто считываем содержимое нужного файла. Файлы за предыдущие даты уже не меняются.
            if (fileInfo == FileList.Last())
            {
                FtpFileInfo[] tmpFileList;
                // Считываем размер файла. В зависимости от того, какие методы поддерживает Ftp-сервер, длина файла может
                // быть получена разными способами:
                // - прямым запросом длины файла (Ftp-команда SIZE)
                // - получением списка файлов с подробностями по каждому файлу (Ftp-команда LIST)
                // - считыванием содержимого файла
                // В зависимости от сработавшего метода, получаем побочные данные: список файлов или полное содержимое 
                // интересующего файла.
                long size = FtpClient.GetFileSize(fileInfo.Ftp.Name, out tmpFileList, out data);
                // Если размер файла не изменился
                if (size == (fileInfo.Ftp.Size ?? 0))
                {
                    // Проверяем, может, уже создан новый файл.
                    if (tmpFileList != null)
                        FileList = QuickJoin(FileList, tmpFileList).ToArray();
                    else
                        FileList = QuickJoin(FileList, FtpClient.DownloadFileList()).ToArray();
                    fileInfo = FirstForDownload(tagRequests, includeBounds);
                    if (fileInfo == null)
                        return new TagValueSet[0];
                    data = null;
                }
                else
                {
                    fileInfo.Ftp.Size = size;
                    fileInfo.Content.LastTime = null;
                }
            }

            if (data == null)
                data = FtpClient.DownloadFile(fileInfo.Ftp.Name);

            string[] csv = ZipUtils
                .DecompressFromFile(data, Encoding.GetEncoding(1251))
                .Split(new string[] { "\r\n" }, StringSplitOptions.None)
                .ToArray();

            TagValueSet[] result;
            try
            {
                result = Parse(csv, tagRequests);
            }
            catch (Exception ex)
            {
                throw new Exception($"Файл: {fileInfo.Ftp.Name}", ex);
            }

            if (result.Any())
            {
                fileInfo.Content.LastTime = result.Max(x => x.Values.Max(y => y.DeviceTime));
                fileInfo.Content.LastIndex = result.Max(x => x.Values.Max(y => y.DeviceIndex));
            }
            return result;
        }

        private TagValueSet[] Parse(string[] lines, TagRequest[] tagRequests)
        {
            if (lines == null)
                throw new Exception("Некоррекный вызов метода: lines == null");
            if (MessageTagMapping == null)
                throw new Exception($"Не инициализирован маппинг тэга \"{Params.ReservedTagNames.Message}\"");

            TagRequest messageRequest = tagRequests.FirstOrDefault(x => x.TagMapping.Id == MessageTagMapping.Id);

            DateTime deviceTime = DateTime.MinValue;
            int deviceIndex = 0;

            List<TagValueSet.ValueInfo> values = new List<TagValueSet.ValueInfo>();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                string[] parts = line.Split(new char[] { ',' }, 3);
                if (parts.Length != 3)
                    throw new Exception($"Некорректный формат данных. Строка {i + 1}");

                DateTime newDeviceTime = DateTime.ParseExact(parts[0], "yyyy/MM/dd HH:mm:ss.fff", null);
                deviceIndex = newDeviceTime != deviceTime ? 0 : deviceIndex + 1;
                deviceTime = newDeviceTime;

                if (messageRequest != null)
                    if (deviceTime >= messageRequest.FromTime)
                        if ((deviceTime > messageRequest.FromTime) || (deviceIndex > messageRequest.FromIndex))
                        {
                            TagValueSet.ValueInfo value = new TagValueSet.ValueInfo()
                            {
                                DeviceTime = deviceTime,
                                DeviceIndex = deviceIndex,
                                Quality = Quality.Good,
                                Value = parts[1] + "\u0000" + parts[2]
                            };
                            values.Add(value);
                        }
            }

            DateTime now = DateTime.Now;
            return tagRequests.Select
            (
                x => new TagValueSet(x.TagMapping.Id)
                {
                    SystemTime = now,
                    Values = x.TagMapping.Id == MessageTagMapping.Id ? values.ToArray() : null,
                    //LastDeviceTime = deviceTime,
                    //LastDeviceIndex = deviceIndex,
                }
            ).ToArray();
        }

    }

}