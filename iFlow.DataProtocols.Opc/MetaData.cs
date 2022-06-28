using System;
using System.Linq;

using iFlow.Interfaces;

namespace iFlow.DataProtocols
{
	internal class MetaData : IAbstractMetaData, IDisposable
	{
		public MetaData(DataSource dataSource)
			: base()
		{
			DataSource = dataSource;
			MetaRoot = new MetaRoot(dataSource);
		}

		public void Dispose()
		{
		}

		public IAbstractDataSource DataSource { get; }
		private MetaRoot MetaRoot { get; }
		public event ExceptionHandler ExceptionEvent;

		public IMetaItem GetItem(string path)
		{
			try
			{
				return path == null ? MetaRoot : MetaRoot.GetItem(path.Split(new char[] { '!', '.' }), 0);
			}
			catch (Exception ex)
			{
				OnException(ex);
				throw;
			}
		}

		protected virtual void OnException(Exception ex)
		{
			ExceptionEvent?.Invoke(this, ex);
		}
	}

}
