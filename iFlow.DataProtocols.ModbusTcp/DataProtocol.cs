using iFlow.DataProtocols;
using iFlow.Interfaces;

public class DataProtocol : AbstractDataProtocol
{
	public override IDataSource CreateDataSource(string configStr, Tag[] tags)
	{
		DataSource dataSource = new DataSource();
		dataSource.Init(configStr, tags);
		return dataSource;
	}

}
