//using System;
//using System.Linq;
//using iFlow.Interfaces;
//using iFlow.Utils;

//namespace iFlow.DataProtocols
//{
//    internal abstract class MetaItem
//    {
//        public MetaItem(MetaFolder parent, FakeOpcClient opcClient, OpcItem opcElement)
//        {
//            this.parent = parent;
//            OpcClient = opcClient;
//            OpcElement = opcElement;
//        }

//        protected readonly FakeOpcClient OpcClient;
//        protected readonly OpcItem OpcElement;

//        public IMetaFolder Parent
//        {
//            get { return parent; }
//        }
//        public MetaFolder parent;

//        public string Name
//        {
//            get { return OpcElement.Name; }
//        }

//        public string Address
//        {
//            get { return OpcElement.Address; }
//        }
//    }

//    internal class MetaRoot : MetaFolder, IMetaRoot
//    {
//        public MetaRoot(MetaFolder parent, FakeOpcClient opcClient, OpcFolder opcElement)
//            : base(parent, opcClient, opcElement)
//        {

//        }

//        public override T[] GetItems<T>()
//        {
//            return OpcClient.Root.Items
//                .Select(x => new MetaDevice(this, OpcClient, (OpcFolder)x))
//                .OfType<T>()
//                .ToArray();
//        }
//    }

//    internal class MetaDevice : MetaFolder, IMetaDevice
//    {
//        public MetaDevice(MetaFolder parent, FakeOpcClient opcClient, OpcFolder opcElement)
//            : base(parent, opcClient, opcElement)
//        {

//        }

//    }

//    internal class MetaFolder : MetaItem, IMetaFolder
//    {
//        public MetaFolder(MetaFolder parent, FakeOpcClient opcClient, OpcFolder opcElement)
//            : base(parent, opcClient, opcElement)
//        {

//        }

//        public virtual T[] GetItems<T>()
//            where T : IMetaItem
//        {
//            MetaFilter filter = MetaFilter.All;
//            if (typeof(T) == typeof(IMetaFolder))
//                filter = MetaFilter.Folder;
//            else if (typeof(T) == typeof(IMetaTag))
//                filter = MetaFilter.Tag;
//            else if (typeof(T) == typeof(IMetaItem))
//                filter = MetaFilter.Tag;
//            else if (typeof(T) == typeof(IMetaDevice))
//                filter = MetaFilter.Folder;
//            else
//                throw new WrongTypeException(typeof(T));

//            //return OpcClient.ById.Values
//            //    .Select(x=>
//            //    {
//            //        switch (x)
//            //        {
//            //            case OpcServer opcServer: return typeof(T) == typeof( IMetaDevice)?new MetaDevice();
//            //        }
//            //    })
//            //    .Select(x => x is OpcTag ? (MetaItem)new MetaTag(this, OpcClient, (OpcTag)x) : new MetaFolder(this, OpcClient, (OpcFolder)x))
//            //    .OfType<T>()
//            //    .ToArray();
//        }

//    }

//    internal class MetaTag : MetaItem, IMetaTag
//    {
//        public MetaTag(MetaFolder parent, FakeOpcClient opcClient, OpcTag opcElement)
//            : base(parent, opcClient, opcElement)
//        {

//        }

//        public Type DataType
//        {
//            get
//            {
//                if (dataType == null)
//                    dataType = OpcClient.ByName[OpcElement.Address].Type;
//                return dataType;
//            }
//        }
//        private Type dataType;

//    }


//}
