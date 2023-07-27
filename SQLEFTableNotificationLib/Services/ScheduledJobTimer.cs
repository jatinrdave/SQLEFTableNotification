using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLEFTableNotificationLib.Services
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
                this.DueTime = dueTime - DateTime.Now;
            }
            if (this.DueTime < TimeSpan.Zero)
            {
                this.DueTime = TimeSpan.Zero;
            }
            this.Period = period;

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
                    this.PreviousExecuteTime = DateTime.Now;
                    if (this.Period == Timeout.InfiniteTimeSpan)
                    {
                        this.NextExecuteTime = DateTime.MinValue;  
                        if (AutoDispose)
                        {
                            this.Dispose();
                        }
                    }
                    else
                    {
                        this.NextExecuteTime = this.PreviousExecuteTime + this.Period;
                    }

                    Interlocked.Exchange(ref _isRunning, 0);
                }
            }
        }

        public void Start(object state = null)
        {
            if (_timer == null)
            {
                _timer = new Timer(callback: Execute, state, this.DueTime, this.Period);
            }
            else
            {
                _timer.Change(this.DueTime, this.Period);
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
