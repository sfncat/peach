using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SysProcess = System.Diagnostics.Process;
using Peach.Core;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Core.WebServices
{
	public class ExternalJobMonitor : BaseJobMonitor, IJobMonitor
	{
		SysProcess _process;
		Task _taskMonitor;
		Task _taskStderr;
		volatile bool _pendingKill;

		static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		public ExternalJobMonitor()
		{
			using (var db = new NodeDatabase())
			{
				db.Migrate();
			}
		}

		protected override bool IsRunning { get { return _process != null; } }

		enum Command
		{
			Stop,
			Pause,
			Continue,
		}

		public bool Pause()
		{
			return SendCommand(Command.Pause);
		}

		public bool Continue()
		{
			return SendCommand(Command.Continue);
		}

		public bool Stop()
		{
			return SendCommand(Command.Stop);
		}

		public bool Kill()
		{
			Logger.Trace("Kill");

			lock (this)
			{
				if (!IsRunning)
					return true;

				try
				{
					_pendingKill = true;
					_process.Kill();
					return true;
				}
				catch (InvalidOperationException)
				{
					// The process has already been killed
					return true;
				}
				catch (Exception ex)
				{
					Logger.Debug(ex, "Exception in Kill");
					return false;
				}
			}
		}

		// used by unit tests
		// kill the worker without setting _pendingKill
		// this should test Restarts
		internal void Terminate()
		{
			_process.Kill();
		}

		protected override void OnStart(Job job)
		{
			Logger.Trace("StartProcess");

			var fileName = Utilities.GetAppResourcePath("PeachWorker.exe");

			var args = new List<string>
			{
				"--pits", PitLibraryPath,
				"--guid", job.Id,
				PitFile,
			};

			if (!string.IsNullOrEmpty(Configuration.LogRoot))
				args.AddRange(new[] { "--logRoot", Configuration.LogRoot });

			if (!Configuration.UseAsyncLogging)
				args.Add("--syncLogging");

			if (Configuration.LogLevel == NLog.LogLevel.Debug)
				args.Add("-v");

			if (Configuration.LogLevel == NLog.LogLevel.Trace)
				args.Add("-vv");

			_process = new SysProcess
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = fileName,
					Arguments = string.Join(" ", args),
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardError = true,
					RedirectStandardInput = true,
					RedirectStandardOutput = true,
					WorkingDirectory = Utilities.ExecutionDirectory,
				}
			};
			_process.Start();

			var root = Path.Combine(Configuration.LogRoot, job.Id);
			Directory.CreateDirectory(root);
			var logFile = Path.Combine(root, "error.log");
			_taskStderr = Task.Factory.StartNew(LoggingTask, new LoggingTaskArgs
			{
				Source = _process.StandardError,
				Sink = new StreamWriter(logFile),
			}, TaskCreationOptions.LongRunning);

			_taskMonitor = Task.Factory.StartNew(MonitorTask, job, TaskCreationOptions.LongRunning);

			// wait for prompt
			_process.StandardOutput.ReadLine();
		}

		bool SendCommand(Command cmd)
		{
			Logger.Trace("SendCommand: {0}", cmd);

			lock (this)
			{
				if (!IsRunning)
					return false;

				try
				{
					_process.StandardInput.WriteLine(cmd.ToString().ToLower());
					_process.StandardOutput.ReadLine();
					return true;
				}
				catch (Exception ex)
				{
					Logger.Debug(ex, "Exception in SendCommand");
					return false;
				}
			}
		}

		struct LoggingTaskArgs
		{
			public StreamReader Source;
			public StreamWriter Sink;
		}

		void LoggingTask(object obj)
		{
			var args = (LoggingTaskArgs)obj;
			try
			{
				using (args.Source)
				using (args.Sink)
				{
					while (!_process.HasExited)
					{
						var line = args.Source.ReadLine();
						if (line == null)
							return;

						Logger.Error(line);
						args.Sink.WriteLine(line);
						args.Sink.Flush();
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Debug(ex, "Exception in LoggingTask");
			}
		}

		void MonitorTask(object obj)
		{
			var job = (Job)obj;

			Logger.Trace("WaitForExit> pid: {0}", _process.Id);

			_process.WaitForExit();
			var exitCode = _process.ExitCode;

			Logger.Trace("WaitForExit> ExitCode: {0}", exitCode);

			// this shouldn't throw, LoggingTask should catch it
			_taskStderr.Wait();

			Logger.Trace("LoggingTask done");

			if (exitCode == 0 || exitCode == 2 || _pendingKill)
			{
				if (exitCode != 0 && _pendingKill)
					JobHelper.Fail(job.Guid, db => db.GetTestEventsByJob(job.Guid), "Job has been aborted.");
				FinishJob();
			}
			else
			{
				Thread.Sleep(TimeSpan.FromSeconds(1));

				lock (this)
				{
					_process.Dispose();
					_process = null;

					OnStart(job);
				}
			}
		}

		void FinishJob()
		{
			Logger.Trace(">>> FinishJob");

			lock (this)
			{
				try
				{
					_process.Dispose();
				}
				catch (Exception ex)
				{
					Logger.Trace("Exception during _process.Dispose: {0}", ex.Message);
				}

				_process = null;
				_taskStderr = null;
				_pendingKill = false;
			}

			if (InternalEvent != null)
				InternalEvent(this, EventArgs.Empty);

			Logger.Trace("<<< FinishJob");
		}

		// used by unit tests
		public void Dispose()
		{
			Logger.Trace(">>> Dispose");

			if (Kill())
			{
				Logger.Trace("Waiting for process to die");
				_taskMonitor.Wait(TimeSpan.FromSeconds(5));
			}

			Logger.Trace("<<< Dispose");
		}
	}
}