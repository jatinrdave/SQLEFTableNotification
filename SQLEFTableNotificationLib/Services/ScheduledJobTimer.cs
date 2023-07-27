using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLEFTableNotification.Services
{
    public sealed class ScheduledJobTimer : IDisposable
    {
        #region Fields
        private Timer _timer;
        private int _isRunning;
        private Action<object> _job;
        #endregion

        #region Properties
        public bool AutoDispose { get; set; }
        public TimeSpan DueTime { get; private set; }
        public TimeSpan Period { get; private set; }
        public DateTime PreviousExecuteTime { get; private set; }
        public DateTime NextExecuteTime { get; private set; }
        #endregion

        #region Methods
        private ScheduledJobTimer(Action<object> job, DateTime dueTime, TimeSpan period)
        {
            Contract.Requires(job != null);

            _job = job;
            if (dueTime != DateTime.MinValue)
            {
                DueTime = dueTime - DateTime.Now;
            }
            if (DueTime < TimeSpan.Zero)
            {
                DueTime = TimeSpan.Zero;
            }
            Period = period;

            AutoDispose = true;
            //App.DisposeService.Register(this.GetType(), this);
        }
        public ScheduledJobTimer(Action<object> job, DateTime dueTime)
            : this(job, dueTime, Timeout.InfiniteTimeSpan)
        {

        }
        public ScheduledJobTimer(Action<object> job, TimeSpan period)
            : this(job, DateTime.Now.AddSeconds(2d), period)
        {

        }

        public void Execute(object state)
        {
            if (Interlocked.Exchange(ref _isRunning, 1) == 0)
            {
                try
                {
                    _job(state);
                }
                catch (Exception ex)
                {
                    //App.LogError(ex, "JobTimer");
                }
                finally
                {
                    PreviousExecuteTime = DateTime.Now;
                    if (Period == Timeout.InfiniteTimeSpan)
                    {
                        NextExecuteTime = DateTime.MinValue;
                        if (AutoDispose)
                        {
                            Dispose();
                        }
                    }
                    else
                    {
                        NextExecuteTime = PreviousExecuteTime + Period;
                    }

                    Interlocked.Exchange(ref _isRunning, 0);
                }
            }
        }

        public void Start(object state = null)
        {
            if (_timer == null)
            {
                _timer = new Timer(callback: Execute, state, DueTime, Period);
            }
            else
            {
                _timer.Change(DueTime, Period);
            }
        }

        public void Stop()
        {
            if (_timer == null)
            {
                return;
            }
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void Dispose()
        {
            if (_job != null)
            {
                _job = null;
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }

                //App.DisposeService.Release(this.GetType(), this);
            }
        }
        #endregion
    }
}
