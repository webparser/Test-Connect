using System;

namespace iFlow.DataProtocols
{
    internal static class Params
	{
		public static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(1);

		public static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(500);
		public static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(100);
		public static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(100);

		public static readonly TimeSpan DefaultUpdateRate = TimeSpan.FromSeconds(1);
	}
}
