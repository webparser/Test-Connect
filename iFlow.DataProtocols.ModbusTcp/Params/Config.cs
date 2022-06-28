using System;
using System.Xml.Serialization;
using iFlow.Utils;

namespace iFlow.DataProtocols.Config
{
	public class Tag
	{
		[XmlAttribute]
		public string Name { get; set; }

		[XmlAttribute]
		public string Address { get; set; }

		[XmlAttribute]
		public Type Type { get; set; }
	}

	public struct RealtimeData
	{
		[XmlOptional]
		[XmlAttribute]
		public TimeSpan? DefaultUpdateRate { get; set; }

		[XmlOptional]
		[XmlAttribute]
		public float? DefaultDeadband { get; set; }
	}

	public class DataSourceConfig
	{
		[XmlAttribute]
		public string Address = null;

		[XmlOptional]
		[XmlAttribute]
		public ushort? Port = null;

		[XmlOptional]
		[XmlAttribute]
		public byte UnitId = 0;

		[XmlOptional]
		public RealtimeData RealtimeData { get; set; }

		[XmlOptional]
		public Tag[] Tags { get; set; }
	}

}