using System;
using System.Linq;
using System.Text;

namespace iFlow.DataProviders
{
    public class SubscriptionDataSource : CustomSubscriptionDataSource<Config, DataTag, Subscription>
	{
		private const string sFtpDir = "//JEROS/USERS/APPF/JLOG/LOG";
		private const string sFilePrefix = "Hourly_";

		public SubscriptionDataSource(string configStr, MappingTag[] tagMappings)
			: base(configStr, tagMappings, Params.DefaultUpdateRate, Params.MinUpdateRate)
		{
			FtpClient = new CustomFtpClient(Config.Address, Config.Port ?? Params.DefaultFtpPort, sFtpDir, Config.UserName, Config.Password);
		}

		private readonly CustomFtpClient FtpClient;

		private FtpFileInfo LastFileInfo;
		private int LastLineIndex;

		protected override Subscription InternalCreateSubscription(TimeSpan? updateRate = null, float? deadband = null)
		{
			return new Subscription(TagMappings, updateRate ?? DefaultUpdateRate, Params.MinUpdateRate, deadband);
		}

		protected override TimeSpan? GetRequestTimerInterval()
		{
			return base.GetRequestTimerInterval();
		}

		protected override TagValue[] GetValues(TagMapping[] tags)
		{
			if (!FtpClient.Ping())
				throw new Exception("");

			FtpFileInfo tmpFileInfo;
			lock (LockObj)
				tmpFileInfo = LastFileInfo;

			byte[] data = null;
			if (tmpFileInfo == null)
			{
				tmpFileInfo = FtpClient.DownloadFileList()
					.OrderBy(x => x.Date)
					.LastOrDefault();
				if (tmpFileInfo == null)
					return null;
				LastLineIndex = 0;
			}
			else
			{
				long size = FtpClient.GetFileSize(tmpFileInfo.Name, out data);
				if (size == tmpFileInfo.Size.Value)
				{
					FtpFileInfo tmpFileInfo2 = FtpClient.DownloadFileList()
						.OrderBy(x => x.Date)
						.LastOrDefault();
					if (tmpFileInfo2 == null)
						throw new Exception("");
					if (tmpFileInfo.Date >= tmpFileInfo2.Date)
						return new TagValue[0];

					tmpFileInfo = tmpFileInfo2;
					data = null;
					LastLineIndex = 0;
				}
				else
					tmpFileInfo.Size = size;
			}
			if (data == null)
				data = FtpClient.DownloadFile(tmpFileInfo.Name);

			string[] csv = ZipUtils
				.DecompressFromFile(data, Encoding.GetEncoding(1251))
				.Split(new string[] { "\r\n" }, StringSplitOptions.None);
			Parse(csv, tmpFileInfo.Date);

			lock (LockObj)
				LastFileInfo = tmpFileInfo;

			//return new TagValueContainer();
			return null;
		}

		private TagValue[] Parse(string[] lines, DateTime fileDate)
		{
			if (lines == null)
				throw new Exception("lines == null");
			if (lines.Length < 2)
				throw new Exception("Некорректный формат данных: количество строк меньше 2");

			// Пропускаем столбец с датой
			string[] tagNames = lines[0].Split(',').Skip(1).ToArray();

			// две строки заголовка
			lines = lines.Skip(2).ToArray();

			if (!lines.Any())
				return new TagValue[0];

			string[] items = lines.Last().Split(',');
			DateTime time = DateTime.ParseExact(items[0], "yyyy/MM/dd HH:mm:ss", null);

			// Пропускаем столбец с датой
			string[] values = items.Skip(1).ToArray();
			if (values.Length != tagNames.Length)
				throw new Exception("Некорректный формат данных: количесто значений не совпадает с количеством наименований тэгов");

			DateTime now = DateTime.Now;
			return values.Select
			(
				(value, index) =>
				{
					TagMapping tagMapping = TagMappings.FirstOrDefault(x => x.Mapping == tagNames[index]);
					if (tagMapping == null)
						throw new Exception("");
					return new TagValue()
					{
						Id = tagMapping.Id,
						RowId = FtpUtils.ComposeIndex(fileDate, lines.Length - 1),
						DeviceTime = time,
						LocalTime = now,
						Value = value
					};
				}
			).ToArray();
		}

	}

}