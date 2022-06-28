using System;
using System.Linq;

namespace iFlow.DataProtocols
{
    /// <summary>
    /// Класс используется в Ftp-провайдере для замеров времени скачивания списка файлов или содержимого
    /// отдельных файлов.
    /// </summary>
	internal class Speedometer
	{
		private int[] DurationArray = new int[5];
		private int Index;
		private DateTime StartTime;

		public void Start()
		{
			StartTime = DateTime.Now;
		}

		public void Stop()
		{
            DurationArray[Index++] = (int)(DateTime.Now - StartTime).TotalMilliseconds;
			if (Index >= DurationArray.Length)
				Index = 0;
		}

		public int Duration { get { return DurationArray.Sum() / DurationArray.Length; } }
	}

}
