using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iFlow.DataProviders
{
	internal class HourlyChildDataSource : ChildDataSource
	{
		public HourlyChildDataSource(Config config, TagMapping[] tagMappings)
			: base(config, tagMappings)
		{
		}

		protected override string FtpDir { get { return "//JEROS/USERS/APPF/JLOG/LOG"; } }
		protected override string FilePrefix { get { return "Hourly_"; } }

		protected override ChildSubscription InternalCreateSubscription(TimeSpan? updateRate = default(TimeSpan?), float? deadband = null)
		{
			return new ChildSubscription(TagMappings, TimeSpan.FromSeconds(0), deadband);
		}

		private TagData[] Parse(DateTime fileDate, string[] lines)
		{
			if (lines == null)
				throw new Exception("lines == null");
			if (lines.Length < 2)
				throw new Exception("Некорректный формат данных");

			// Пропускаем столбец с датой
			string[] tagNames = lines[0].Split(',').Skip(1).ToArray();

			// две строки заголовка
			lines = lines.Skip(2).ToArray();

			return lines.Where(x => !string.IsNullOrWhiteSpace(x)).SelectMany
			(
				line =>
				{
					string[] items = line.Split(',');
					DateTime time = DateTime.ParseExact(items[0], "yyyy/MM/dd HH:mm:ss", null);

					// Пропускаем столбец с датой
					string[] values = items.Skip(1).ToArray();
					if (values.Length != tagNames.Length)
						throw new Exception("Некорректный формат данных");

					return values.Select
					(
						(value, index) => new TagData()
						{
							ControllerId = Utils.ComposeIndex(fileDate, index),
							TagName = tagNames[index],
							Time = time,
							Value = value
						}
					);
				}
			).ToArray();
		}

	}

}
