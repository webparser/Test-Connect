using System;
using System.Xml.Serialization;
using iFlow.Utils;

namespace iFlow.DataProvider.Config
{
	internal class DataSource
	{
		[CfgOptional]
		[XmlAttribute]
		public string Name { get; set; }

		[CfgOptional]
		[XmlAttribute]
		public string Protocol { get; set; }

		[CfgOptional]
		[XmlAttribute]
		public Guid? ProtocolUid { get; set; }

		public NestedConfig Config { get; set; }
	}

	internal class Config
	{
		[XmlAttribute]
		public string Address { get; set; }

		public DataSource[] DataSources { get; set; } = new DataSource[] { };
	}

}