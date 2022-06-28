using System;
using System.Collections.Generic;
using System.Linq;
using iFlow.Interfaces;

namespace iFlow.DataProtocols
{
	/// =======================================================================================================<summary>
	/// 
	/// </summary>======================================================================================================
	internal abstract class MetaItem : IMetaItem
	{
		public MetaItem(DataSource dataSource, MetaFolder parent, string name)
		{
			DataSource = dataSource;
			Parent = parent;
			Name = name;
		}

		protected DataSource DataSource { get; }
		public IMetaFolder Parent { get; }
		public string Name { get; }

		public string Address
		{
			get
			{
				if (this is MetaRoot)
					return null;
				if (this is MetaDevice)
					return Name;
				return $"{Parent.Address}{(Parent is IMetaDevice ? "!" : ".")}{Name}";
			}
		}

		public bool Exists
		{
			get { return true; }
		}
	}

	/// =======================================================================================================<summary>
	/// 
	/// </summary>======================================================================================================
	internal class MetaFolder : MetaItem, IMetaFolder
	{
		public MetaFolder(DataSource dataSource, MetaFolder parent, string name)
			: base(dataSource, parent, name)
		{

		}

		private MetaFolder[] FoldersCache;
		private DateTime FoldersExpirationTime;
		private MetaTag[] TagsCache;
		private DateTime TagsExpirationTime;

		public IMetaItem GetItem(string[] path, int index)
		{
			IMetaItem[] items = GetItems<IMetaItem>();
			IMetaItem item = items.FirstOrDefault(x => x.Name == path[index]);
			if (item == null)
				throw new Exception($"Не найден OPC элемент \"{string.Join(".", path)}\"");
			return index < path.Length - 1 ? ((MetaFolder)item).GetItem(path, index + 1) : item;
		}

		public virtual T[] GetItems<T>() where T : IMetaItem
		{
			bool needFolders = typeof(T).IsAssignableFrom(typeof(IMetaFolder)) || typeof(IMetaFolder).IsAssignableFrom(typeof(T));
			bool refreshFoldersCache = needFolders && (FoldersCache == null || DateTime.Now > FoldersExpirationTime);
			if (refreshFoldersCache)
			{
				IEnumerable<Opc.Da.BrowseElement> opcFolders = DataSource.OpcClient.GetMetaChildren(Address, MetaFilter.Folder);
				IEnumerable<MetaFolder> existsFolders =
					FoldersCache == null ? new MetaFolder[0] : FoldersCache.Where(folder => opcFolders.Any(opcFolder => opcFolder.Name == folder.Name));
				IEnumerable<MetaFolder> newFolders =
					(FoldersCache == null ? opcFolders : opcFolders.Where(opcFolder => !FoldersCache.Any(folder => folder.Name == opcFolder.Name)))
					.Select(x => this is MetaRoot ? new MetaDevice(DataSource, this, x.Name) : new MetaFolder(DataSource, this, x.Name));
				FoldersCache = existsFolders.Concat(newFolders)
					.OrderBy(x => x.Name)
					.ToArray();
				FoldersExpirationTime = DateTime.Now + Params.CacheExpirationInterval;
			}

			bool needTags = typeof(T).IsAssignableFrom(typeof(IMetaTag)) || typeof(IMetaTag).IsAssignableFrom(typeof(T));
			bool refreshTagsCache = needTags && (TagsCache == null || DateTime.Now > TagsExpirationTime);
			if (refreshTagsCache)
			{
				IEnumerable<Opc.Da.BrowseElement> opcTags = DataSource.OpcClient.GetMetaChildren(Address, MetaFilter.Tag);
				IEnumerable<MetaTag> existsTags =
					TagsCache == null ? new MetaTag[0] : TagsCache.Where(Tag => opcTags.Any(opcTag => opcTag.Name == Tag.Name));
				IEnumerable<MetaTag> newTags =
					(TagsCache == null ? opcTags : opcTags.Where(opcTag => !TagsCache.Any(Tag => Tag.Name == opcTag.Name)))
					.Select(x => new MetaTag(DataSource, this, x.Name));
				TagsCache = existsTags.Concat(newTags)
					.OrderBy(x => x.Name)
					.ToArray();
				TagsExpirationTime = DateTime.Now + Params.CacheExpirationInterval;
			}
			return (FoldersCache ?? new MetaFolder[0]).OfType<T>().Concat((TagsCache ?? new MetaTag[0]).OfType<T>()).ToArray();
		}

	}

	/// =======================================================================================================<summary>
	/// 
	/// </summary>======================================================================================================
	internal class MetaRoot : MetaFolder, IMetaRoot
	{
		public MetaRoot(DataSource dataSource)
			: base(dataSource, null, null)
		{
		}

	}

	/// =======================================================================================================<summary>
	/// 
	/// </summary>======================================================================================================
	internal class MetaDevice : MetaFolder, IMetaDevice
	{
		public MetaDevice(DataSource dataSource, MetaFolder parent, string name)
			: base(dataSource, parent, name)
		{
		}

	}

	/// =======================================================================================================<summary>
	/// 
	/// </summary>======================================================================================================
	internal class MetaTag : MetaItem, IMetaTag
	{
		public MetaTag(DataSource dataSource, MetaFolder parent, string name)
			: base(dataSource, parent, name)
		{

		}

		public Type Type
		{
			get
			{
				if (dataType == null)
					dataType = DataSource.OpcClient.GetDataType(Address);
				return dataType;
			}
		}
		private Type dataType;

	}


}
