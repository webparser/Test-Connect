using System;
using System.Linq;

using iFlow.DataProviders;

public class DataProvider : AbstractDataProvider, IDataProvider
{
	public TimeSpan MinRequestInterval
	{
		get { return TimeSpan.FromSeconds(1); }
	}

	private int SDataSourceRefCount;
	private SubscriptionDataSource SDataSource;

	public override T GetDataSource<T>(string connectionInfo)
	{
		if (SDataSourceRefCount == 0)
		{
			SubscriptionDataSource sdataSource = new SubscriptionDataSource(connectionInfo, TagMappings);
			SDataSourceRefCount++;
			return (T)(IDataSource)sdataSource;
		}
		return base.GetDataSource<T>(connectionInfo);
	}

}
