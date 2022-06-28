using System.ComponentModel;

namespace iFlow.DataProtocols.Modbus
{
	internal enum ErrorCode
	{
		[Description("Принятый код функции не может быть обработан.")]
		IllegalFunction = 01,

		[Description("Адрес данных, указанный в запросе, недоступен.")]
		IllegalDataAddress = 02,

		[Description("Значение, содержащееся в поле данных запроса, является недопустимой величиной.")]
		IllegalDataValue = 03,

		[Description("Невосстанавливаемая ошибка имела место, пока ведомое устройство пыталось выполнить затребованное действие.")]
		SlaveDeviceFailure = 04,

		/// <summary>
		/// Этот ответ предохраняет ведущее устройство от генерации ошибки тайм-аута.
		/// </summary>
		[Description("Ведомое устройство приняло запрос и обрабатывает его, но это требует много времени.")]
		Acknowledge = 05,

		/// <summary>
		/// Ведущее устройство должно повторить сообщение позже, когда ведомое освободится.
		/// </summary>
		[Description("Ведомое устройство занято обработкой команды.")]
		SlaveDeviceBusy = 06,

		/// <summary>
		/// Этот код возвращается для неуспешного программного запроса, использующего функции с номерами 13 или 14. 
		/// Ведущее устройство должно запросить диагностическую информацию или информацию об ошибках от ведомого.
		/// </summary>
		[Description("Ведомое устройство не может выполнить программную функцию, заданную в запросе.")]
		NegativeAcknowledgement = 07,

		/// <summary>
		/// Ведущее устройство может повторить запрос, но обычно в таких случаях требуется ремонт.
		/// </summary>
		[Description("Ведомое устройство при чтении расширенной памяти обнаружило ошибку паритета.")]
		MemoryParityError = 08,

		[Description("Gateway was unable to allocate an internal communication path from the input port to the out port for processing the request.")]
		GatewayPathUnavailable = 10,

		/// <summary>
		/// Usually means that the device is not present on the network. 
		/// </summary>
		[Description("No response was obtained from the target device.")]
		GatewayTargetDeviceFailedToRespond = 11
	}
}
