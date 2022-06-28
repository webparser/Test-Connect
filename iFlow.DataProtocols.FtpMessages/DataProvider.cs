using iFlow.DataProtocols;
using iFlow.Interfaces;

public class DataProtocol : AbstractDataProtocol, IDataProtocol
{
	private int SDataSourceRefCount;
	private HistoricalDataSource HistoricalDataSource;

	public override IHistoricalDataSource GetHistoricalDataSource(string connectionInfo, IDataTag[] tagMappings)
	{
		if (SDataSourceRefCount == 0)
		{
			HistoricalDataSource = new HistoricalDataSource(connectionInfo, tagMappings);
			SDataSourceRefCount++;
		}
		return HistoricalDataSource;
	}

}
