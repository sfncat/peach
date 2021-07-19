using System;
using System.Threading;
using Peach.Core;
using Peach.Pro.Core.License;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Core.WebServices
{
	public class InternalJobMonitor : BaseJobMonitor, IJobMonitor
	{
		static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
		volatile JobRunner _runner;
		Thread _thread;
		ILicense _license;

		// For unit tests
		internal Action<Engine> TestHook { get; set; }

		public InternalJobMonitor(ILicense license)
		{
			_license = license;

			using (var db = new NodeDatabase())
			{
				db.Migrate();
			}
		}

		protected override bool IsRunning { get { return _runner != null; } }

		public bool Pause()
		{
			lock (this)
			{
				if (!IsRunning)
					return false;
				_runner.Pause();
				return true;
			}
		}

		public bool Continue()
		{
			lock (this)
			{
				if (!IsRunning)
					return false;
				_runner.Continue();
				return true;
			}
		}

		public bool Stop()
		{
			lock (this)
			{
				if (IsRunning)
					_runner.Stop();
				return true;
			}
		}

		public bool Kill()
		{
			Logger.Trace(">>> Kill");

			lock (this)
			{
				if (!IsRunning)
				{
					Logger.Trace("<<< Kill (!IsRunning)");
					return true;
				}

				Logger.Trace("Abort");
				_runner.Abort();

				Logger.Trace("<<< Kill");
				return true;
			}
		}

		public void Dispose()
		{
			Logger.Trace(">>> Dispose");

			Kill();

			if (_thread != null)
			{
				Logger.Trace("Join");
				_thread.Join(TimeSpan.FromSeconds(5));
			}

			Logger.Trace("<<< Dispose");
		}

		protected override void OnStart(Job job)
		{
			var evtReady = new AutoResetEvent(false);
			_runner = new JobRunner(_license, job, PitLibraryPath, PitFile);
			_thread = new Thread(() =>
			{
				try
				{
					_runner.Run(evtReady, TestHook);
				}
				catch (Exception ex)
				{
					// Eat all exceptions on the worker thread of the internal job monitor
					Logger.Debug(ex, "InternalJobMonitor exception");
				}
				finally
				{
					Logger.Trace("runner.Run() done");
					_runner = null;
				}

				if (InternalEvent != null)
					InternalEvent(this, EventArgs.Empty);
			});

			_thread.Start();
			if (!evtReady.WaitOne(1000))
				throw new PeachException("Timeout waiting for job to start");
		}
	}
}