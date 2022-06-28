using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using iFlow.Interfaces;
using iFlow.Utils;

namespace iFlow.DataProtocols
{
    public enum MetaFilter
    {
        Folder = 1,
        Tag = 2,
        All = Folder | Tag
    }

    internal class FakeOpcClient : IDisposable
    {
        private const string sOpcXml = "Opc.Xml";

        public OpcRoot Root { get; private set; }

        public FakeOpcClient(string opcXmlPath)
        {
            //string opcXmlPath = PathHelper.AppendExecutingAssemblyDir(sOpcXml);

            OpcItem XmlToItem(OpcItem parent, Xml.OpcItem xmlItem)
            {
                OpcItem opcItem = null;
                switch (xmlItem)
                {
                    case Xml.OpcRoot xmlRoot: opcItem = new OpcRoot(); break;
                    case Xml.OpcServer xmlServer: opcItem = new OpcServer(parent, xmlServer.Name); break;
                    case Xml.OpcFolder xmlFolder: opcItem = new OpcServer(parent, xmlFolder.Name); break;
                    case Xml.OpcTag xmlTag: opcItem = new OpcServer(parent, xmlTag.Name); break;
                    default: throw new WrongTypeException(xmlItem);
                }
                if (opcItem is OpcFolder opcFolder)
                    opcFolder.Items = ((Xml.OpcFolder)xmlItem).Items.Select(x => XmlToItem(opcItem, x)).ToArray();
                return opcItem;
            }

            if (File.Exists(opcXmlPath))
            {
                Xml.OpcRoot xmlRoot = SerializeHelper.DeserializeFromFile<Xml.OpcRoot>(opcXmlPath);
                Root = (OpcRoot)XmlToItem(null, xmlRoot);
            }
        }

        public FakeOpcClient(Config.Tag[] tags)
        {
            OpcFolder GetFolder(OpcFolder opcFolder, IEnumerable<string> path)
            {
                if (!path.Any())
                    return opcFolder;
                OpcItem opcItem = opcFolder.Items?.FirstOrDefault(x => x.Name == path.First());
                if (opcItem == null)
                {
                    opcItem = new OpcFolder(opcFolder, path.First());
                    opcFolder.Items = (opcFolder.Items ?? new OpcItem[0])
                        .Concat(opcItem)
                        .OrderBy(x => x.Name)
                        .ToArray();
                }
                else if (!(opcItem is OpcFolder))
                    throw new Exception($"Ошибка структуры GlobalTags. \"{opcItem.Name}\" - и папка и не папка.");
                return GetFolder((OpcFolder)opcItem, path.Skip(1));
            }

            Root = new OpcRoot();
            foreach (Config.Tag tag in tags)
            {
                string pathStr = tag.Address.Split('!').Last();
                string[] path = pathStr.Split(".");

                // Кроме послдеднего элемента - самого тэга.
                OpcFolder opcFolder = GetFolder(Root, path.Take(path.Length - 1));

                OpcTag opcTag;
                switch (tag.Type.GetSimplified())
                {
                    case SimpleType.Bool: opcTag = new BoolOpcTag(opcFolder, tag.Type, path.Last()); break;
                    case SimpleType.Int: opcTag = new IntOpcTag(opcFolder, tag.Type, path.Last()); break;
                    case SimpleType.Float: opcTag = new FloatOpcTag(opcFolder, tag.Type, path.Last()); break;
                    case SimpleType.String: opcTag = new StringOpcTag(opcFolder, tag.Type, path.Last()); break;
                    default: throw new WrongTypeException(tag.Type);
                }
                opcFolder.Items = (opcFolder.Items ?? new OpcItem[0])
                    .Concat(opcTag)
                    .OrderBy(x => x.Name)
                    .ToArray();
            }
        }

        //public IEnumerable<OpcItem> Enum()
        //{
        //    return Enum(Root);
        //}

        //private IEnumerable<OpcItem> Enum(OpcFolder opcFolder)
        //{
        //    yield return opcFolder;
        //    foreach (OpcFolder child in opcFolder.Items.OfType<OpcFolder>())
        //        foreach (OpcItem descendant in Enum(child))
        //            yield return descendant;
        //    foreach (OpcTag child in opcFolder.Items.OfType<OpcTag>())
        //        yield return child;
        //}

        public void Dispose()
        {
            Root = null;
        }

        public OpcItem Get(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new Exception("Не указан адрес тэга.");
            OpcItem opcItem = Root;

            path = path.Split('!').Last();
            foreach (string step in path.Split('.'))
            {
                if (!(opcItem is OpcFolder opcFolder))
                    throw new Exception($"{opcItem.Name} не является папкой.");
                opcItem = opcFolder.Items.FirstOrDefault(x => string.Compare(x.Name, step, true) == 0);
            }
            if (opcItem == null)
                throw new Exception($"не найден элемент {path}.");
            return opcItem;
        }

        public OpcItem GetMetaItem(string path)
        {
            return Get(path);
        }

        //public OpcItem[] GetMetaChildren(string path, MetaFilter metaFilters)
        //{
        //    IEnumerable<OpcItem> items = Enum()
        //        .OfType<OpcFolder>()
        //        .FirstOrDefault(x => x.Address == path)?.Items;
        //    if (items == null)
        //        return null;

        //    switch (metaFilters)
        //    {
        //        case MetaFilter.Folder: return items.OfType<OpcFolder>().ToArray();
        //        case MetaFilter.Tag: return items.OfType<OpcTag>().ToArray();
        //        default: return items.ToArray();
        //    }
        //}

        //public Type GetDataType(string path)
        //{
        //    return Get(path).Type;
        //}

    }

}
