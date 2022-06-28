
namespace iFlow.DataProtocols.Modbus
{
	internal enum FunctionCode
	{
		ReadDiscreteOutput = 1,
		ReadDiscreteInput = 2,
		ReadAnalogOutput = 3,
		ReadAnalogInput = 4,
		WriteDiscreteOutput = 5,
		WriteAnalogOutput = 6,
		WriteMultipleDiscreteOutputs = 15,
		WriteMultipleAnalogOutputs = 16,
		//ReadWriteMultipleRegister = 23
	}
}
