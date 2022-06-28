using System;
using System.IO;

namespace iFlow.DataProviders
{
    internal static class Utils
	{
		private const int MaxLineCount = 110000;

		/// <summary>
		/// 2147483647
		/// 2112000000
		/// </summary>
		/// <param name="date"></param>
		/// <param name="lineNumber"></param>
		/// <returns></returns>
		public static int ComposeIndex(DateTime date, int lineNumber)
		{
			if (date.Year < 2000)
				throw new Exception("Не обрабатываются файлы созданные ранее 2000 года");
			if (date.Year > 2050)
				throw new Exception("Не обрабатываются файлы созданные позднее 2050 года");
			if (lineNumber > MaxLineCount)
				throw new Exception("Превышено максимальное количество строк");
			return (((date.Year - 2000) * 12 + date.Month - 1) * 31 + date.Day - 1) * MaxLineCount + lineNumber;
		}

		public static void DecomposeIndex(int id, out DateTime date, out int lineNumber)
		{
			lineNumber = id % MaxLineCount;
			int dateCode = id / MaxLineCount;
			int day = dateCode % 31 + 1;
			dateCode = dateCode / 31;
			int month = dateCode % 12 + 1;
			dateCode = dateCode / 12;
			int year = dateCode + 2000;
			date = new DateTime(year, month, day);
		}

		public static DateTime DecomposeIndexToDate(int id)
		{
			DateTime date;
			int lineNumber;
			DecomposeIndex(id, out date, out lineNumber);
			return date;
		}

    }

}
