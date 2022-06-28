using System.Timers;

namespace iFlow.DataProviders
{
    public interface IFtpTimer
    {
        double Interval { get; set; }
        void Start();
        void Stop();
        event ElapsedEventHandler Elapsed;
    }
}
