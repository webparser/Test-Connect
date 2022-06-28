using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iFlow.DataProtocols
{
	internal static class Params
	{
		public static class Socket
		{
			public static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(1);

			public static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(500);
			public static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(100);
			public static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(100);

			public const ushort DefaultModbusPort = 502;
		}

		/// <summary>
		/// Максимальное количество аналоговых регистров, которое можно запросить в одном пакете. Ограничение протокола.
		/// </summary>
		public const ushort MaxAnalogRegisterCountInPacket = 125;

		public static readonly TimeSpan DefaultUpdateRate = TimeSpan.FromSeconds(1);
		public static readonly TimeSpan MinUpdateRate = TimeSpan.FromSeconds(1);
	}
}
