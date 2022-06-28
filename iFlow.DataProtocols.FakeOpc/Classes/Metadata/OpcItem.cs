using System;
using System.Linq;
using iFlow.Interfaces;
using iFlow.Utils;

namespace iFlow.DataProtocols
{
    public class OpcItem: IMetaItem
    {
        public OpcItem(OpcItem parent, string name)
            : base()
        {
            Parent = parent;
            Name = name;
        }

        public OpcItem Parent { get; }
        public string Name { get; }
        public string Address { get => (Parent == null ? "" : Parent.Address + "/") + Name; }

        IMetaFolder IMetaItem.Parent
        {
            get => Parent as IMetaFolder;
        }
    }

    public class OpcFolder : OpcItem, IMetaFolder
    {
        public OpcFolder(OpcItem parent, string name)
            : base(parent, name)
        {
        }

        public OpcItem[] Items { get; set; }

        public T[] GetItems<T>()
            where T : IMetaItem
        {
            return Items.OfType<T>().ToArray();
        }

    }


    public class OpcServer : OpcFolder
    {
        public OpcServer(OpcItem parent, string name)
            : base(parent, name)
        {
        }
    }

    public class OpcRoot : OpcFolder
    {
        public OpcRoot()
            : base(null, null)
        {
        }
    }

    public abstract class OpcTag : OpcItem, IMetaTag
    {
        public OpcTag(OpcItem parent, Type type, string name)
            : base(parent, name)
        {
            Type = type;
        }

        public Type Type { get; }
        public object Value { get => GetValue(); set => SetValue(value); }
        public DateTime ValueTime { get; set; }
        public bool IsRandom { get; set; } = true;

        protected abstract object GetValue();
        protected abstract void SetValue(object value);
    }

    public abstract class OpcTag<T> : OpcTag
    {
        public OpcTag(OpcItem parent, Type type, string name)
            : base(parent, type, name)
        {
        }

        protected static Random Rnd = new Random();

        public T RndValue { get; set; }
        public new T Value { get; set; }

        protected abstract T GenValue();

        protected override object GetValue()
        {
            if (IsRandom)
            {
                DateTime now = DateTime.Now;
                if (ValueTime + TimeSpan.FromSeconds(4.9) < now)
                {
                    RndValue = GenValue();
                    ValueTime = now;
                }
                return RndValue;
            }
            return Value;
        }

        protected override void SetValue(object value)
        {
            typeof(T).CheckMatch(value);
            Value = (T)value;
        }
    }

    public class BoolOpcTag : OpcTag<bool>
    {
        public BoolOpcTag(OpcItem parent, Type type, string name)
            : base(parent, type, name)
        {
        }

        protected override bool GenValue()
        {
            return Rnd.Next(100) > 50;
        }
    }

    public class IntOpcTag : OpcTag<int>
    {
        public IntOpcTag(OpcItem parent, Type type, string name)
            : base(parent, type, name)
        {
        }

        public int MinValue { get; set; }
        public int MaxValue { get; set; } = 100;

        protected override int GenValue()
        {
            DateTime now = DateTime.Now;
            double x = GetHashCode() + (now.Minute * 60 + now.Second + now.Millisecond / 1000.0) / 30.0;
            return (int)((Math.Sin(x) + 1) * (MaxValue + MinValue) / 2);
        }
    }

    public class FloatOpcTag : OpcTag<double>
    {
        public FloatOpcTag(OpcItem parent, Type type, string name)
            : base(parent, type, name)
        {
        }

        public double MinValue { get; set; }
        public double MaxValue { get; set; } = 100.0;

        protected override double GenValue()
        {
            DateTime now = DateTime.Now;
            double x = GetHashCode() + (now.Minute * 60 + now.Second + now.Millisecond / 1000.0) / 30.0;
            return (int)((Math.Sin(x) + 1) * (MaxValue + MinValue) / 2);
        }
    }

    public class StringOpcTag : OpcTag<string>
    {
        public StringOpcTag(OpcItem parent, Type type, string name)
            : base(parent, type, name)
        {
        }

        protected override string GenValue()
        {
            return "Сообщение: " + new string((char)Rnd.Next((byte)'a', (byte)'z' + 1), Rnd.Next(20));
        }
    }

}
