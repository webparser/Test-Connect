using System;
using System.IO;
using System.Linq;
using System.Text;

using iFlow.Utils;

namespace iFlow.DataProviders
{
	internal abstract class ChildDataSource : CustomSubscriptionDataSource<Config, TagMapping, ChildSubscription>
	{
		public ChildDataSource(Config config, TagMapping[] tagMappings)
			: base(config, tagMappings, Params.DefaultUpdateRate)
		{
			FtpClient = new YokoFtpClient(Config.Address, Config.Port ?? Params.DefaultFtpPort, FtpDir, Config.UserName, Config.Password);
		}

		protected abstract string FtpDir { get; }
		protected abstract string FilePrefix { get; }

		protected YokoFtpClient FtpClient { get; }

		protected override ChildSubscription InternalCreateSubscription(TimeSpan? updateRate = default(TimeSpan?), float? deadband = null)
		{
			return new ChildSubscription(TagMappings, TimeSpan.FromSeconds(0), deadband);
		}

		protected void GetHistory()
		{
			FtpFileInfo[] ftpFiles;

			DateTime now = DateTime.Now;
			if (now.Date == LastFileInfo.Date.Date)
			{
				ftpFiles = new FtpFileInfo[]
				{
				// Не знаем текущую длину файла, поэтому ставим null
					new FtpFileInfo(LastFileInfo.Name, LastFileInfo.Date, null)
				};
			}
			else
			{
				ftpFiles = FtpClient.DownloadFileList()
					//.GetFileList(Config.Server, FtpDir, Config.UserName, Config.Password, FilePrefix)
					.Where(x => x.Date.Date >= now.Date)
					.ToArray();
			}

			foreach (FtpFileInfo ftpFile in ftpFiles)
			{
				byte[] data = null;
				if (ftpFile.Name == LastFileInfo.Name)
				{
					if (ftpFile.Size == null)
						ftpFile.Size = FtpClient.GetFileSize(ftpFile.Name, out data);
					if (ftpFile.Size == LastFileInfo.Size)
						return;
				}

				if (data == null)
				{
					FileDownloadSpeedometer.Start();
					try
					{
						data = FtpService.GetFile(Config.Server, GetFtpPath(ftpFile.Name), Config.UserName, Config.Password);
					}
					finally
					{
						FileDownloadSpeedometer.Stop();
					}
				}

				if (LastFileInfo.Date.Date == ftpFile.Date.Date)
					LastFileInfo.Size = data.Length;

				string[] lines = ExtractCsv(ftpFile.Name, data);
				T[] parsedData = Parse(ftpFile.Date, lines);

				foreach (Subscription<T> subscription in allSubscriptions)
				{
					T[] subData = null;
					if (subscription.LastId != null)
						subData = parsedData.Where(x => x.ControllerId > subscription.LastId.Value).ToArray();
					else
						subData = parsedData.Where(x => x.Time > subscription.LastTime).ToArray();
					if (subData.Any())
						subscription.DataHandler(DataSourceName, Config.Name, subData);
					if (subData.Any())
						subscription.LastId = subData.Max(x => x.ControllerId);
				}
			}

			FtpFileInfo last = ftpFiles.LastOrDefault();
			if (last != null)
				if ((LastFileInfo.Name == null) || (last.Date.Date > LastFileInfo.Date.Date))
					LastFileInfo = last;
		}

	}
}
