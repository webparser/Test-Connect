using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using iFlow.Interfaces;

public abstract class AbstractDataProtocol : IDataProtocol
{
	public virtual void Dispose()
	{
	}

	public event ExceptionHandler ExceptionEvent;

	public virtual TimeSpan MinRequestInterval
	{
		get { return TimeSpan.FromSeconds(1); }
	}

	public abstract IDataSource CreateDataSource(GlobalTags globalTags, string configStr);

	protected virtual void DataSource_ExceptionEvent(object sender, Exception ex)
	{
		OnException(ex);
	}

	protected virtual void OnException(Exception ex)
	{
		ExceptionEvent?.Invoke(this, ex);
	}

	public new static string ToString()
	{
		Assembly currentAssembly = Assembly.GetAssembly(typeof(AbstractDataProtocol));
		GuidAttribute guidAttr = currentAssembly.GetCustomAttributes<GuidAttribute>().FirstOrDefault();
		string result = "Guid=" + guidAttr.Value;

		AssemblyProductAttribute productAttr = currentAssembly.GetCustomAttributes<AssemblyProductAttribute>().FirstOrDefault();
		if (productAttr != null)
			result = "Name=" + productAttr.Product + ", " + result;

		return $"{{DataProtocol: {result}}}";
	}

}