using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using NLog;
using Peach.Core;
using SysProcess = System.Diagnostics.Process;

namespace Peach.Pro.Core.OS.Windows
{
	public abstract class Remotable
	{
		protected static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
	}

	public class Remotable<T> : Remotable, IDisposable where T: MarshalByRefObject
	{
		private readonly JobObject _job;
		private readonly SysProcess _process;

		public Remotable()
		{
			_job = new JobObject();

			var guid = Guid.NewGuid().ToString();

			using (var readyEvt = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\" + guid))
			{
				_process = new SysProcess()
				{
					StartInfo = new ProcessStartInfo
					{
						CreateNoWindow = true,
						UseShellExecute = false,
						Arguments = "--ipc {0} \"{1}\"".Fmt(guid, typeof(T).AssemblyQualifiedName),
						FileName = Utilities.GetAppResourcePath("PeachTrampoline.exe")
					}
				};

				if (Logger.IsTraceEnabled)
				{
					_process.EnableRaisingEvents = true;
					_process.OutputDataReceived += LogProcessData;
					_process.ErrorDataReceived += LogProcessData;
					_process.StartInfo.RedirectStandardError = true;
					_process.StartInfo.RedirectStandardOutput = true;
				}

				_process.Start();

				// Add process to JobObject so it will get killed if peach crashes
				_job.AssignProcess(_process);

				if (Logger.IsTraceEnabled)
				{
					_process.BeginErrorReadLine();
					_process.BeginOutputReadLine();
				}

				var procEvt = new ManualResetEvent(false)
				{
					SafeWaitHandle = new SafeWaitHandle(_process.Handle, false)
				};

				// Wait for either ready event or process exit
				var idx = WaitHandle.WaitAny(new WaitHandle[] { readyEvt, procEvt });

				if (idx == 2)
					throw new SoftException("Failed to create remote {0}, the process exited prematurley.".Fmt(typeof(T).Name));
			}

			Url = "ipc://{0}/{1}".Fmt(guid, typeof(T).Name);
		}

		public void Dispose()
		{
			if (_job == null)
				return;

			// Disposing the job object will kill the process
			_job.Dispose();

			if (_process != null)
			{
				_process.WaitForExit();
				_process.Close();
			}
		}

		public string Url
		{
			get;
			private set;
		}

		public T GetObject()
		{
			return (T)Activator.GetObject(typeof(T), Url);
		}

		private static void LogProcessData(object sender, DataReceivedEventArgs e)
		{
			if (!string.IsNullOrEmpty(e.Data))
				Logger.Debug(e.Data);
		}
	}
}
