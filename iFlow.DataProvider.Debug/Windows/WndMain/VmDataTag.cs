using System;
using iFlow.Interfaces;
using iFlow.Utils;
using iFlow.Wpf;

namespace iFlow.DataProvider
{
    public class VmTag : VmBase
    {
        public VmTag(int index, Tag tag)
            : base()
        {
            Index = index;
            Tag = tag;
        }

        public int Index { get; }
        public Tag Tag { get; }
        public EventHandler Changed;
        public EventHandler IsRandomChanged;

        public object Value
        {
            get => value;
            set
            {
                this.value = value;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
        private object value;

        public bool IsRandom
        {
            get => isRandom;
            set
            {
                isRandom = value;
                OnPropertyChanged(nameof(IsRandom));
                IsRandomChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        private bool isRandom;

        public void Update(object value)
        {
            bool isMatch = value == null
                ? Tag.Type.IsNullable()
                : Tag.Type == value.GetType() || Nullable.GetUnderlyingType(Tag.Type) == value.GetType();
            if (!isMatch)
                throw new Exception($"Тип значения \"{value ?? "<null>"}\" не сопадает с типом тэга \"{Tag.Name}\"");

            if (this.value == value)
                return;
            this.value = value;
            OnPropertyChanged(nameof(Value));
        }
    }

    public class VmValueTag<T> : VmTag
    {
        public VmValueTag(int index, Tag tag)
            : base(index, tag)
        {
        }

        public new T Value
        {
            get => (T)base.Value;
            set => base.Value = value;
        }
    }

    public class VmMinMaxTag<T> : VmValueTag<T>
    {
        public VmMinMaxTag(int index, Tag tag)
            : base(index, tag)
        {
        }

        public EventHandler MinChanged;
        public EventHandler MaxChanged;

        public T MinValue
        {
            get => minValue;
            set { minValue = value; MinChanged?.Invoke(this, EventArgs.Empty); }
        }
        private T minValue;

        public T MaxValue
        {
            get => maxValue;
            set { maxValue = value; MaxChanged?.Invoke(this, EventArgs.Empty); }
        }
        private T maxValue;
    }

    public class VmBoolTag : VmValueTag<bool>
    {
        public VmBoolTag(int index, Tag tag)
            : base(index, tag)
        {
        }

        public new bool Value
        {
            get => (bool)base.Value;
            set => base.Value = value;
        }
    }

    public class VmIntTag : VmMinMaxTag<int>
    {
        public VmIntTag(int index, Tag tag)
            : base(index, tag)
        {
        }

        //public new int Value
        //{
        //    get => (int)(double)base.Value;
        //    set => base.Value = value;
        //}
    }

    public class VmFloatTag : VmMinMaxTag<double>
    {
        public VmFloatTag(int index, Tag tag)
            : base(index, tag)
        {
        }

        //public new double Value
        //{
        //    get
        //    {
        //        try
        //        {
        //            return (double)base.Value;
        //        }
        //        catch (Exception ex)
        //        {
        //            return 0;
        //        }
        //    }
        //    set => base.Value = value;
        //}
    }

    public class VmStringTag : VmValueTag<string>
    {
        public VmStringTag(int index, Tag tag)
            : base(index, tag)
        {
        }

        public new string Value
        {
            get => base.Value;
            set => base.Value = value;
        }
    }
}
