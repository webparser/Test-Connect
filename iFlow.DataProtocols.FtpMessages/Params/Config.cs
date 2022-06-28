namespace iFlow.DataProtocols
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
