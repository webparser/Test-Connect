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

        [CfgOptional]
        [XmlAttribute]
        public float Deadband { get; set; }
    }

	public class Config
	{
		[XmlAttribute]
		public string Url = null;

		[CfgOptional]
		public Tag[] Tags { get; set; }
	}

}
