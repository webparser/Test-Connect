using System;
using System.Collections.Generic;
using System.Linq;

namespace iFlow.DataProviders
{
    //internal class Subscription<T> where T : FtpData
    //{
    //    public int? LastId;
    //    public DateTime LastTime;
    //    public DataHandler<T> DataHandler;
    //}

    //internal static class SubsriptionExt
    //{
    //    public static DateTime GetMinDate<T>(this IEnumerable<Subscription<T>> subscriptions) where T : FtpData
    //    {
    //        if (subscriptions == null)
    //            throw new Exception("");
    //        DateTime? d1 = null, d2 = null;
    //        Subscription<T>[] byId = subscriptions.Where(x => x.LastId != null).ToArray();
    //        if (byId.Any())
    //            d1 = byId.Min(x => Utils.DecomposeIndexToDate(x.LastId.Value));
    //        Subscription<T>[] byTime = subscriptions.Where(x => x.LastId == null).ToArray();
    //        if (byTime.Any())
    //            d2 = byTime.Min(x => x.LastTime);

    //        if (d1 != null)
    //        {
    //            if (d2 != null)
    //                return d1.Value < d2.Value ? d1.Value : d2.Value;
    //            return d1.Value;
    //        }
    //        else
    //        {
    //            if (d2 != null)
    //                return d2.Value;
    //            throw new Exception("");
    //        }
    //    }
    //}
}
