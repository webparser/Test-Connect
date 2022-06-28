using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iFlow.Utils;

namespace iFlow.DataProviders
{
	internal class MsgChildDataSource : ChildDataSource
	{
		public MsgChildDataSource(Config config, TagMapping[] tagMappings)
			: base(config, tagMappings)
		{
			MessageMapping = tagMappings.FirstOrDefault(x => x.Mapping == Params.ReservedTagNames.Message);
		}

		protected override string FtpDir { get { return "//JEROS/USERS/APPF/JLOG/MSG"; } }
		protected override string FilePrefix { get { return "Msg_"; } }

		private TagMapping MessageMapping;

		private FtpFileInfo LastFileInfo;
		private int LastLineInfo;
		private Queue<TagValue[]> TagValueBuffer = new Queue<TagValue[]>();

		protected override TimeSpan? GetTimerInterval()
		{
			if (TagValueBuffer.Any())
				return TimeSpan.Zero;
			else
				return DefaultUpdateRate LastRequestTime
		}

		protected override TagValue[] GetValues(TagMapping[] tags)
		{
			if (TagValueBuffer.Any())
				return TagValueBuffer.Dequeue();

			if (!FtpClient.Ping())
				throw new Exception("");

			byte[] data = null;
			if (LastFileInfo == null)
			{
				LastFileInfo = FtpClient.DownloadFileList()
					.OrderBy(x => x.Date)
					.LastOrDefault();
				if (LastFileInfo == null)
					return new TagValue[0];
			}
			else
			{
				long size = FtpClient.GetFileSize(LastFileInfo.Name, out data);
				if (size == LastFileInfo.Size.Value)
					return new TagValue[0];
			}
			if (data == null)
				data = FtpClient.DownloadFile(LastFileInfo.Name);
			string[] csv = ZipUtils
				.DecompressFromFile(data, Encoding.GetEncoding(1251))
				.Split(new string[] { "\r\n" }, StringSplitOptions.None);
			Parse(csv);

			if (TagValueBuffer.Any())
				return TagValueBuffer.Dequeue();
			return new TagValue[0];
		}


		private void Parse(string[] lines)
		{
			if (lines == null)
				throw new Exception("lines == null");
			if (MessageMapping == null)
				throw new Exception("");

			DateTime now = DateTime.Now;

			foreach (string line in lines)
			{
				string[] values = line.Split(',');
				if (values.Length != 3)
					throw new Exception("Некорректный формат данных");

				TagValue tagValue = new TagValue()
				{
					Id = MessageMapping.Id,
					RowId = Utils.ComposeIndex(LastFileInfo.Date, LastLineInfo++),
					DeviceTime = DateTime.ParseExact(values[0], "yyyy/MM/dd HH:mm:ss.fff", null),
					LocalTime = now,
					Value = values[1] + "\u0000" + values[2]
				};
				TagValueBuffer.Enqueue(new TagValue[] { tagValue });
			}
		}

	}

}
