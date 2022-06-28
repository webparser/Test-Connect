//using System;
//using iFlow.Utils;

//namespace iFlow.DataProtocols
//{
//    public abstract class TagState
//    {
//        public object Value { get => GetValue(); set => SetValue(value); }
//        public DateTime ValueTime { get; set; }
//        public virtual bool IsRandom { get; set; } = true;

//        protected abstract object GetValue();
//        protected abstract void SetValue(object value);
//    }

//    public abstract class TagState<T> : TagState
//    {
//        public new T Value { get; set; }
//        private T SaveValue;

//        public override bool IsRandom
//        {
//            get { return base.IsRandom; }
//            set
//            {
//                if (value)
//                    SaveValue = Value;
//                base.IsRandom = value;
//                if (!value)
//                    Value = SaveValue;
//            }
//        }

//        protected override object GetValue()
//        {
//            return Value;
//        }

//        protected override void SetValue(object value)
//        {
//            typeof(T).CheckMatch(value);
//            Value = (T)value;
//        }
//    }

//    public class BoolTagState : TagState<bool>
//    {
//    }

//    public class IntTagState : TagState<int>
//    {
//        public int MinValue { get; set; }
//        public int MaxValue { get; set; } = 100;
//    }

//    public class FloatTagState : TagState<double>
//    {
//        public double MinValue { get; set; }
//        public double MaxValue { get; set; } = 100.0;
//    }

//    public class StringTagState : TagState<string>
//    {
//    }

//}
