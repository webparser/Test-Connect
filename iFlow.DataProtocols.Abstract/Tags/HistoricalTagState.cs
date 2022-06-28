using System;
using System.Collections.Generic;
using iFlow.Interfaces;

namespace iFlow.DataProtocols
{
	public class HistoricalTagState
	{
		public HistoricalTagState()
			: base()
		{
		}

		public DateTime LastDeviceTime;
		public int LastDeviceIndex;

		public bool OnlyFreshData = false;
		public DateTime LastRequestTime = DateTime.MinValue;
		public object LastValue;

		public static IComparer<HistoricalTagState> Comparer
		{
			get
			{
				return Comparer<HistoricalTagState>.Create((x, y) =>
				{
					if (x.LastDeviceTime != y.LastDeviceTime)
						return x.LastDeviceTime.CompareTo(y.LastDeviceTime);
					return x.LastDeviceIndex.CompareTo(y.LastDeviceIndex);
				});
			}
		}

	}

}
