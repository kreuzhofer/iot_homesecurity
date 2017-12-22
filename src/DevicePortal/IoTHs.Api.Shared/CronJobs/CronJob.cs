using System;
using System.Threading;

namespace IoTHs.Api.Shared.CronJobs
{
    public interface ICronJob
    {
        void Execute(DateTime date_time);
        void Abort();
    }

    public class CronJob : ICronJob
    {
        private readonly ICronSchedule _cronSchedule;
        private readonly ThreadStart _threadStart;
        private Thread _thread;

        public CronJob(string schedule, ThreadStart threadStart)
        {
            _cronSchedule = new CronSchedule(schedule);
            _threadStart = threadStart;
            _thread = new Thread(threadStart);
        }

        private readonly object _lock = new object();
        public void Execute(DateTime dateTime)
        {
            lock (_lock)
            {
                if (!_cronSchedule.IsTime(dateTime))
                    return;

                if (_thread.ThreadState == ThreadState.Running)
                    return;

                _thread = new Thread(_threadStart);
                _thread.Start();
            }
        }

        public void Abort()
        {
          _thread.Abort();  
        }

    }
}
