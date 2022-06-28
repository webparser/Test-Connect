using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.FtpClient;
using System.Net.NetworkInformation;

namespace iFlow.DataProtocols
{
	public class CustomFtpClient
	{
		public CustomFtpClient(string host, ushort port, string dir, string login, string password)
			: base()
		{
			Host = host;
			Port = port;
			Dir = dir;
			Login = login;
			Password = password;
		}

		private readonly string Host;
		private readonly ushort Port;
		private readonly string Dir;
		private readonly string Login;
		private readonly string Password;

		private readonly ServerFeatures Features = new ServerFeatures();
		private readonly Speedometer FileDownloadSpeedometer = new Speedometer();
		private readonly Speedometer ListDownloadSpeedometer = new Speedometer();

		public bool Ping()
		{
			using (Ping ping = new Ping())
				return ping.Send(Host).Status == IPStatus.Success;
		}

		private string CombinePath(params string[] par)
		{
			// Слэши переводим в обратные слэши, готовим к Path.Combine
			par = par.Select(x => x.Replace("//", "$$").Replace('/', '\\')).ToArray();
			// Производим Path.Combine и восстанавливаем слэши
			return Path.Combine(par).Replace('\\', '/').Replace("$$", "//").Replace("///", "//");
		}

		private string ExpandPath(string fileName)
		{
			return CombinePath(Dir, fileName.ToUpper());
		}

		private DateTime? ExtractTime(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				return null;

			string[] fileParts = fileName.Split(new char[] { '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
			if (fileParts.Length != 3)
				return null;

			if (!DateTime.TryParseExact(fileParts[1], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fileTime))
				return null;

			return fileTime;
		}

		private FtpClient CreateClient()
		{
			return new FtpClient()
			{
				Host = this.Host,
				Port = this.Port,
				Credentials = new NetworkCredential(Login, Password)
			};
		}

		/// <summary>
		/// Считывает список файлов из контроллера по Ftp-протоколу, выбирает из них только файлы с именами
		/// в формате Msg_yyyyMMdd.zip или Hourly_yyyyMMdd.zip.
		/// </summary>
		/// <returns></returns>
		public FtpFileInfo[] DownloadFileList()
		{
			// Начинаем замер длительности скачивания содержимого каталога.
			ListDownloadSpeedometer.Start();
			try
			{
				using (FtpClient ftpClient = CreateClient())
				{
					ftpClient.SetWorkingDirectory(Dir);

					return ftpClient.GetListing()
						.Where(x => string.Compare(Path.GetExtension(x.Name), ".zip", true) == 0)
						.Select
						(
							x =>
							{
								DateTime? fileTime = ExtractTime(x.Name);
								// Нас интересуют только файлы с датой в составе имени.
								if (fileTime != null)
									// Парсер возвращает 0 в качестве длины файла, если длину не удалось определить.
									return new FtpFileInfo(x.Name, fileTime.Value, x.Size != 0 ? (long?)x.Size : null);
								return null;
							}
						)
						.Where(x => x != null)
						.ToArray();
				}
			}
			finally
			{
				ListDownloadSpeedometer.Stop();
			}
		}

		/// <summary>
		/// Метод скачивает с ftp-сервера файл и возвращает результат в виде массива байт.
		/// </summary>
		/// <param name="host"></param>
		/// <param name="path"></param>
		/// <param name="login"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		public byte[] DownloadFile(string fileName)
		{
			FileDownloadSpeedometer.Start();
			try
			{
				using (FtpClient ftpClient = CreateClient())
				using (Stream ftpStream = ftpClient.OpenRead(ExpandPath(fileName)))
				using (MemoryStream memoryStream = new MemoryStream())
				{
					ftpStream.CopyTo(memoryStream);
					return memoryStream.ToArray();
				}
			}
			finally
			{
				FileDownloadSpeedometer.Stop();
			}
		}

		private bool GetFileSize(string fileName, out long? size)
		{
			using (FtpClient ftpClient = CreateClient())
			{
				size = ftpClient.GetFileSize(ExpandPath(fileName));
				if (size <= 0)
					size = null;
				return size != null;
			}
		}

		/// <summary>
		/// Запрос размера файла на Ftp-сервере. Размер может запрашиваться тремя способами, в зависимости от того, реализует
		/// Ftp-сервер те или иные возможности или нет: 1 - непосредственный запрос размера файла (Ftp-команда "SIZE {FileName}"),
		/// 2 - запрос списка файлов, с получением полной информации по каждому файлу, в том числе размера, 3 - скачивание файла
		/// и замер количества полученных байт.
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public long GetFileSize(string fileName, out FtpFileInfo[] fileList, out byte[] data)
		{
			fileList = null;
			data = null;

			// Сначала пытаемся получить размер файла Ftp-командой SIZE
			if (Features.SupportFileSize)
			{
				if (GetFileSize(fileName, out long? size))
					return size.Value;
				else
					// Ftp-сервер не поддерживает данный тип запроса. Больше не будем его использовать.
					Features.SupportFileSize = false;
			}

			// Затем пробуем получить размер файла скачав содержимое каталога с подробностями по каждому файлу.
			if (Features.SupportFileSizeInList)
			{
				// Определем что нам выгоднее по скорости, скачать содержимое каталога файлов или скачать непосредственно файл.
				if (ListDownloadSpeedometer.Duration < FileDownloadSpeedometer.Duration)
				{
					fileList = DownloadFileList();
					// Вычленяем информацию по нашему искомому файлу
					FtpFileInfo ftpFile = fileList.FirstOrDefault(x => string.Compare(x.Name, fileName, true) == 0);
					// Опс! А файла-то и нет в каталоге! Какая-то ошибка программы!
					if (ftpFile == null)
						throw new Exception("");
					if (ftpFile.Size != null)
						return ftpFile.Size.Value;
					// Ftp-сервер не поддерживает данный тип запроса. Больше не будем его использовать.
					Features.SupportFileSizeInList = false;
				}
			}

			// Первые два способа не прошли. Скачиваем сам файл и замеряем количество полученных данных.
			data = DownloadFile(fileName);
			return data.Length;
		}
	}

}
