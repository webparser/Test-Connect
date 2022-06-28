using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iFlow.DataProviders
{
	internal static class Params
	{
		public const ushort DefaultFtpPort = 21;
		public static readonly TimeSpan DefaultUpdateRate = TimeSpan.FromSeconds(1);

		public static class ReservedTagNames
		{
			public const string Message = "Ftp.Message";
		}
	}

}
