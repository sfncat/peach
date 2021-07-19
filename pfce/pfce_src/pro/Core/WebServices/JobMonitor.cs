using System;
using System.IO;
using Peach.Core;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.Core.Storage;

namespace Peach.Pro.Core.WebServices
{
	public interface IJobMonitor : IDisposable
	{
		int Pid { get; }
		bool IsTracking(Job job);
		bool IsControllable { get; }

		Job GetJob();

		Job Start(string pitLibraryPath, string pitFile, JobRequest jobRequest);

		bool Pause();
		bool Continue();
		bool Stop();
		bool Kill();

		EventHandler InternalEvent { set; }
	}

	public abstract class BaseJobMonitor
	{
		static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		readonly int _pid = Utilities.GetCurrentProcessId();

		private Guid _guid = Guid.Empty;

		protected string PitFile { get; private set; }
		protected string PitLibraryPath { get; private set; }

		public EventHandler InternalEvent { get; set; }

		public int Pid { get { return _pid; } }

		public bool IsTracking(Job job)
		{
			lock (this)
			{
				return _guid == job.Guid;
			}
		}

		public bool IsControllable { get { return true; } }

		public Job GetJob()
		{
			lock (this)
			{
				if (_guid == Guid.Empty)
				{
					Logger.Trace("Job not started yet");
					return null;
				}
			}

			using (var db = new NodeDatabase())
			{
				return db.GetJob(_guid);
			}
		}

		public Job Start(string pitLibraryPath, string pitFile, JobRequest jobRequest)
		{
			lock (this)
			{
				if (IsRunning)
					return null;

				PitFile = Path.GetFullPath(pitFile);
				PitLibraryPath = pitLibraryPath;

				var job = new Job(jobRequest, PitFile);
				_guid = job.Guid;

				try
				{
					OnStart(job);
					return job;
				}
				catch
				{
					using (var db = new NodeDatabase())
					{
						db.DeleteJob(_guid);
					}
					throw;
				}
			}
		}

		protected abstract void OnStart(Job job);
		protected abstract bool IsRunning { get; }
	}
}
