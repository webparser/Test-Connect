using iFlow.DataProtocols;
using iFlow.DataProtocols.Config;
using iFlow.Interfaces;
using iFlow.Utils;

public class DataProtocol : AbstractDataProtocol
{
	public override IDataSource CreateDataSource(GlobalTags globalTags, string configStr)
	{
		DataSource dataSource = new DataSource(globalTags);
		Config config = ConfigHelper.Load<Config>(configStr);
		dataSource.LoadConfig(config);
		return dataSource;
	}

}
