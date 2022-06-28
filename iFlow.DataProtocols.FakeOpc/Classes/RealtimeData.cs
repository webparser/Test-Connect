using System;
using System.Linq;

using iFlow.Interfaces;
using iFlow.Utils;

namespace iFlow.DataProtocols
{
    public partial class RealtimeData : CustomRealtimeData<CustomRealtimeSubscription>, IFakeRealtimeData
    {
        public RealtimeData(DataSource dataSource)
            : base(dataSource)
        {
            DataSource = dataSource;
        }

        public new DataSource DataSource { get; }
        public override TimeSpan MinUpdateRate { get { return TimeSpan.FromSeconds(1); } }

        protected override CustomRealtimeSubscription CreateSubscription_Internal()
        {
            return new CustomRealtimeSubscription(this);
        }

        private OpcTag GetOpcTag(int tagId)
        {
            DataSource.GlobalTags.CheckTagId(tagId);
            string tagAddress = DataSource.TagAddresses[tagId];
            OpcTag opcTag = (OpcTag)DataSource.OpcClient.Get(tagAddress);
            return opcTag;
        }

        public override DataReadValue_Id[] Read(int[] ids)
        {
            DataSource.GlobalTags.CheckTagIds(ids);

            DataReadValue_Id[] result = ids
                .Select
                (
                    x => new DataReadValue_Id()
                    {
                        Id = x,
                        DeviceTime = DateTime.Now,
                        SystemTime = DateTime.Now,
                        Quality = Quality.Good,
                        Result = OperationResult.Ok,
                        Value = GetOpcTag(x).Value
                    }
                )
                .ToArray();
            return result;
        }

        public override DataReadValue_Address[] Read(string[] addresses)
        {
            return addresses
                .Select
                (
                    x => x == null ? null : new DataReadValue_Address()
                    {
                        Address = x,
                        DeviceTime = DateTime.Now,
                        SystemTime = DateTime.Now,
                        Quality = Quality.Good,
                        Result = OperationResult.Ok,
                        Value = ((OpcTag)DataSource.OpcClient.Get(x)).Value
                    }
                )
                .ToArray();
        }

        public override void Write(DataWriteValue_Id[] tagValues)
        {
        }

        public override void Write(DataWriteValue_Address[] tagValues)
        {
        }

        // IFakeRealtimeData
        public bool GetIsRandom(int tagId)
        {
            return GetOpcTag(tagId).IsRandom;
        }

        public void SetIsRandom(int tagId, bool value)
        {
            GetOpcTag(tagId).IsRandom = value;
        }

        private void CheckRangeMatch(int tagId)
        {
            Type type = DataSource.GlobalTags[tagId].Type;
            if (type != typeof(int) && type != typeof(double) && type != typeof(DateTime))
                throw new Exception($"Тип тэга \"{DataSource.GlobalTags[tagId].Name}\" не имеет минимальных и максимальных значений.");
        }

        public object GetValue(int tagId)
        {
            OpcTag tag = GetOpcTag(tagId);
            return tag.Value;
        }

        public void SetValue(int tagId, object value)
        {
            GetOpcTag(tagId).Value = value;
        }

        public object GetMinValue(int tagId)
        {
            switch (GetOpcTag(tagId))
            {
                case IntOpcTag intTag: return intTag.MinValue;
                case FloatOpcTag floatTag: return floatTag.MinValue;
                default: throw new WrongTypeException(GetOpcTag(tagId));
            }
        }

        public void SetMinValue(int tagId, object value)
        {
            switch (GetOpcTag(tagId))
            {
                case IntOpcTag intTag: typeof(int).CheckMatch(value); intTag.MinValue = (int)value; break;
                case FloatOpcTag floatTag: typeof(double).CheckMatch(value); floatTag.MinValue = (int)value; break;
                default: throw new WrongTypeException(GetOpcTag(tagId));
            }
        }

        public object GetMaxValue(int tagId)
        {
            switch (GetOpcTag(tagId))
            {
                case IntOpcTag intTag: return intTag.MaxValue;
                case FloatOpcTag floatTag: return floatTag.MaxValue;
                default: throw new WrongTypeException(GetOpcTag(tagId));
            }
        }

        public void SetMaxValue(int tagId, object value)
        {
            switch (GetOpcTag(tagId))
            {
                case IntOpcTag intTag: typeof(int).CheckMatch(value); intTag.MaxValue = (int)value; break;
                case FloatOpcTag floatTag: typeof(double).CheckMatch(value); floatTag.MaxValue = (int)value; break;
                default: throw new WrongTypeException(GetOpcTag(tagId));
            }
        }

    }

}