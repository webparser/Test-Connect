using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using iFlow.Utils;

namespace iFlow.Shared
{
	internal static class ProtocolLoader
	{
		/// <summary>
		/// Поиск сборки, содержащей дата-провайдер dataProtocolUid и инициализация дата-провайдера из нее.
		/// Поиск производится в папке, в которой находится основной хост-процесс (GUI-программа или сервис), либо
		/// в текущей рабочей папке.
		/// </summary>
		/// <param name="uidName"></param>
		/// <param name="className"></param>
		/// <returns></returns>
		public static object Load(UidName uidName, string className)
		{
			// Находим все сборки дата-провайдеров по шаблону имени файла iFlow.DbProtocols.*.dll
			string programDir = Path.GetDirectoryName(AssemblyHelper.GetAssemblyInfo().Location);
			IEnumerable<string> protocolDllPaths = Directory.GetFiles(programDir, "*.dll", SearchOption.AllDirectories);
			if (Environment.CurrentDirectory != programDir)
			{
				string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				if (!Environment.CurrentDirectory.StartsWith(winDir, StringComparison.CurrentCultureIgnoreCase))
					protocolDllPaths = protocolDllPaths.Union(Directory.GetFiles(Environment.CurrentDirectory, "*.dll", SearchOption.AllDirectories));
			}

			string protocolDllPath = protocolDllPaths.FirstOrDefault
			(
				x =>
				{
					// Проверка сборки на наличие запрашиваемого дата-провайдера, без блокирования файла сборки.
					AssemblyInfo assemblyInfo = AssemblyHelper.GetAssemblyInfo(x);
					bool result = uidName.EqualsTo(assemblyInfo?.Guid, assemblyInfo?.Product);
					if (result)
					{
						uidName.Name = assemblyInfo.Product ?? uidName.Name;
						uidName.Uid = assemblyInfo.Guid ?? uidName.Uid;
					}
					return result;
				}
			);
			if (protocolDllPath == null)
				throw new Exception($"Не найдена сборка с Product=\"{uidName.Name ?? ""}\" {{{uidName.Uid?.ToString() ?? ""}}}");

			// Загружаем сборку
			Assembly protocolAssembly = Assembly.LoadFrom(protocolDllPath);

			// Пытаемся достать из сборки стартовый класс DbProtocol
			Type dataProtocolType = protocolAssembly.GetType(className);
			if (dataProtocolType == null)
				throw new Exception($"В сборке дата-провайдера \"{uidName.Name ?? ""}\" {{{uidName.Uid?.ToString() ?? ""}}} - \"{protocolDllPath}\"" +
					$"отстутствует стартовый класс \"{className}\"");
			try
			{
				return Activator.CreateInstance(dataProtocolType);
			}
			catch (Exception ex)
			{
				throw new Exception($"Ошибка создания стартового класса \"{className}\" " +
					$"в сборке дата-провайдера \"{uidName.Name ?? ""}\" {{{uidName.Uid?.ToString() ?? ""}}}) - \"{protocolDllPath}\"\r\n\r\n{ex.GetFullInfo()}");
			}
		}

	}
}
