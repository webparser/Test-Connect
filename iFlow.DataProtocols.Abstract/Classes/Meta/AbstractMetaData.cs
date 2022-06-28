using System;
using System.Collections.Generic;
using System.Linq;
using iFlow.Interfaces;

namespace iFlow.DataProtocols
{
	public interface IAbstractMetaData : IMetaData
	{
		IAbstractDataSource DataSource { get; }
		event ExceptionHandler ExceptionEvent;
	}

	public abstract class AbstractMetaData: IAbstractMetaData, IDisposable
	{
		public AbstractMetaData(IAbstractDataSource dataSource)
			: base()
		{
			DataSource = dataSource;
		}

		public virtual void Dispose()
		{
		}

		public IAbstractDataSource DataSource { get; }

		public event ExceptionHandler ExceptionEvent;

		public abstract IMetaItem GetItem(string path);

		protected virtual void OnException(Exception ex)
		{
			ExceptionEvent?.Invoke(this, ex);
		}

	}

}
