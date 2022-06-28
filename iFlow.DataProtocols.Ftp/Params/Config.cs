using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

using iFlow.Utils;

namespace iFlow.DataProviders
{
	public class Config : CustomConfig
	{
		public string Address;
		public ushort? Port;
		/// <summary>Логин для входа на Ftp-сервер</summary>
		public string UserName;
		/// <summary>Пароль для входа на Ftp-сервер</summary>
		public string Password;
	}
}
