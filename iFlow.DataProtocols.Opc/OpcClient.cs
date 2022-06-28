using System;
using System.Linq;

namespace iFlow.DataProtocols
{
	public enum MetaFilter
	{
		None = 0,
		Folder = 1,
		Tag = 2,
		All = Folder | Tag
	}

	internal class OpcClient
	{
		public OpcClient(string url)
		{
			Url = url;
		}

		public void Dispose()
		{
			server?.Dispose();
			server = null;
		}

		private readonly string Url;

		public Opc.Da.Server Server
		{
			get
			{
				bool isConnected = false;
				try { isConnected = server != null && server.GetStatus().ServerState != Opc.Da.serverState.running; } catch { }
				if (!isConnected)
				{
					server?.Dispose();
					server = null;

					Opc.URL url = new Opc.URL(Url);
					OpcCom.Factory factory = new OpcCom.Factory();
					server = new Opc.Da.Server(factory, null);
					server.Connect(url, new Opc.ConnectData(new System.Net.NetworkCredential()));
				}
				return server;
			}
		}
		private Opc.Da.Server server;

		//public Opc.Da.BrowseElement GetMetaItem(string path)
		//{
		//	if (path == null)
		//		throw new Exception("Не указан путь");

		//	string[] strings = path.Split(new char[] { '!', '.' });
		//	string itemName = strings.Last();

		//	strings = strings.Take(strings.Length - 1).ToArray();
		//	string parentPath = strings.Length == 0
		//		? null
		//		: strings.Length == 1
		//			? strings.First()
		//			: strings.First() + "!" + string.Join(".", strings.Skip(1));

		//	Opc.ItemIdentifier itemId = parentPath != null ? new Opc.ItemIdentifier(parentPath) : null;
		//	Opc.Da.BrowseFilters filter = new Opc.Da.BrowseFilters()
		//	{
		//		BrowseFilter = Opc.Da.browseFilter.all,
		//		ElementNameFilter = itemName
		//	};
		//	Opc.Da.BrowsePosition position;
		//	try
		//	{
		//		return Server.Browse(itemId, filter, out position).FirstOrDefault();
		//	}
		//	catch (Opc.ResultIDException ex)
		//	{
		//		if (ex.Result == Opc.ResultID.Da.E_UNKNOWN_ITEM_NAME)
		//			return null;
		//		throw;
		//	}
		//}

		public Opc.Da.BrowseElement[] GetMetaChildren(string path, MetaFilter metaFilter = MetaFilter.All)
		{
			Opc.ItemIdentifier itemId = new Opc.ItemIdentifier(path);

			Opc.Da.browseFilter browseFilter = Opc.Da.browseFilter.all;
			switch (metaFilter)
			{
				case MetaFilter.Folder: browseFilter = Opc.Da.browseFilter.branch; break;
				case MetaFilter.Tag: browseFilter = Opc.Da.browseFilter.item; break;
				case MetaFilter.All: browseFilter = Opc.Da.browseFilter.all; break;
				default: return new Opc.Da.BrowseElement[0];
			}
			Opc.Da.BrowseFilters opcFilter = new Opc.Da.BrowseFilters()
			{
				BrowseFilter = browseFilter,
			};
			return Server.Browse(itemId, opcFilter, out Opc.Da.BrowsePosition position);
		}

		public Type GetDataType(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				throw new Exception("Не указан адрес тэга");
			Opc.ItemIdentifier itemId = path != null ? new Opc.ItemIdentifier(path) : null;
			if (itemId == null)
				throw new Exception("Некорректный адрес тэга");
			Opc.Da.ItemPropertyCollection[] properties = Server.GetProperties
			(
				new Opc.ItemIdentifier[] { itemId },
				new Opc.Da.PropertyID[] { Opc.Da.Property.DATATYPE },
				true
			);
			if (!properties.Any())
				throw new Exception($"Ошибка получения свойств тэга \"{path}\"");
			if (properties[0].ResultID.Failed())
				throw new Exception($"Ошибка получения свойств тэга \"{path}\": {properties[0].ResultID.ToString()}");
			return (Type)properties[0][0].Value;
		}

	}

}
