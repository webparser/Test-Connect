using System;
using System.Xml.Serialization;
using iFlow.Utils;

namespace iFlow.DataProtocols.Xml
{
    public class OpcItem
    {
        [XmlAttribute]
        public string Name { get; set; }
    }

    public class OpcRoot : OpcFolder
    {
    }

    public class OpcServer : OpcFolder
    {
    }

    public class OpcFolder : OpcItem
    {
        [XmlArrayItem(typeof(OpcServer))]
        [XmlArrayItem(typeof(OpcFolder))]
        [XmlArrayItem(typeof(OpcTag))]
        public OpcItem[] Items { get; set; }
    }

    public abstract class OpcTag : OpcItem
    {
        [XmlIgnore]
        public Type Type { get; set; }

        [XmlAttribute("Type")]
        public string TypeStr
        {
            get { return Type.FullName; }
            set
            {
                Type = typeof(bool).Assembly.GetType(value);
                if (Type == null)
                    throw new Exception($"Невозможно восстановить элемент типа \"{value}\"");
            }
        }

        [XmlIgnore]
        public object Value { get; set; }

        [XmlAttribute("Value")]
        public string ValueStr
        {
            get { return Value?.ToString(); }
            set
            {
                if (!Type.IsArray)
                    Value = value.Parse(Type);
            }
        }

    }

}
