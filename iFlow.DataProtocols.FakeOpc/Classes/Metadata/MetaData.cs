using System;

using iFlow.Interfaces;

namespace iFlow.DataProtocols
{
    internal class MetaData : IAbstractMetaData, IDisposable
    {
        public MetaData(DataSource dataSource)
            : base()
        {
            DataSource = dataSource;
        }

        public void Dispose()
        {
            DataSource.OpcClient.Dispose();
        }

        IAbstractDataSource IAbstractMetaData.DataSource
        {
            get => DataSource;
        }
        public DataSource DataSource { get; }

        public event ExceptionHandler ExceptionEvent;

        public IMetaItem GetItem(string address)
        {
            try
            {
                return DataSource.OpcClient.Get(address);
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
