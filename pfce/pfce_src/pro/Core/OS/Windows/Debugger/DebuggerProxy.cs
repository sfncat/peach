using System;
using System.Diagnostics;
using System.Runtime.Remoting;
using NLog;
using Peach.Core;
using Peach.Core.Agent;

namespace Peach.Pro.Core.OS.Windows.Debugger
{
	public abstract class BaseDebuggerProxy : IDebuggerInstance
	{
		protected static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		protected IDebuggerInstance _dbg;

		public string WinDbgPath
		{
			get { return _dbg.WinDbgPath; }
			set { _dbg.WinDbgPath = value; }
		}

		public string SymbolsPath
		{
			get { return _dbg.SymbolsPath; }
			set { _dbg.SymbolsPath = value; }
		}

		public bool IgnoreFirstChanceReadAv
		{
			get { return _dbg.IgnoreFirstChanceReadAv; }
			set { _dbg.IgnoreFirstChanceReadAv = value; }
		}

		public bool IgnoreFirstChanceGuardPage
		{
			get { return _dbg.IgnoreFirstChanceGuardPage; }
			set { _dbg.IgnoreFirstChanceGuardPage = value; }
		}

		public bool IgnoreSecondChanceGuardPage
		{
			get { return _dbg.IgnoreSecondChanceGuardPage; }
			set { _dbg.IgnoreSecondChanceGuardPage = value; }
		}

		public abstract void Dispose();

		public string Name
		{
			get { return _dbg.Name; }
		}

		public bool IsRunning
		{
			get
			{
				try
				{
					return _dbg.IsRunning;
				}
				catch (RemotingException)
				{
					return false;
				}
			}
		}

		public void AttachProcess(string processName)
		{
			_dbg.AttachProcess(processName);
		}

		public void StartProcess(string commandLine)
		{
			_dbg.StartProcess(commandLine);
		}

		public void StartService(string serviceName, TimeSpan startTimeout)
		{
			_dbg.StartService(serviceName, startTimeout);
		}

		public bool WaitForExit(int timeout)
		{
			return _dbg.WaitForExit(timeout);
		}

		public bool WaitForIdle(int timeout, uint pollInterval)
		{
			return _dbg.WaitForIdle(timeout, pollInterval);
		}

		public bool DetectedFault
		{
			get { return _dbg.DetectedFault; }
		}

		public MonitorData Fault
		{
			get { return _dbg.Fault; }
		}
	}

	public class DebuggerProxy<T> : BaseDebuggerProxy where T : class, IDebuggerInstance, new()
	{
		#region Remote Proxy Object

		public class Proxy : MarshalByRefObject, IDebuggerInstance
		{
			private readonly T instance = new T();

			public string WinDbgPath
			{
				get { return instance.WinDbgPath; }
				set { instance.WinDbgPath = value; }
			}

			public string SymbolsPath
			{
				get { return instance.SymbolsPath; }
				set { instance.SymbolsPath = value; }
			}

			public bool IgnoreFirstChanceReadAv
			{
				get { return instance.IgnoreFirstChanceReadAv; }
				set { instance.IgnoreFirstChanceReadAv = value; }
			}

			public bool IgnoreFirstChanceGuardPage
			{
				get { return instance.IgnoreFirstChanceGuardPage; }
				set { instance.IgnoreFirstChanceGuardPage = value; }
			}

			public bool IgnoreSecondChanceGuardPage
			{
				get { return instance.IgnoreSecondChanceGuardPage; }
				set { instance.IgnoreSecondChanceGuardPage = value; }
			}

			public void Dispose()
			{
				instance.Dispose();
			}

			public string Name
			{
				get { return instance.Name; }
			}

			public bool IsRunning
			{
				get { return instance.IsRunning; }
			}

			public void AttachProcess(string processName)
			{
				instance.AttachProcess(processName);
			}

			public void StartProcess(string commandLine)
			{
				instance.StartProcess(commandLine);
			}

			public void StartService(string serviceName, TimeSpan startTimeout)
			{
				instance.StartService(serviceName, startTimeout);
			}

			public bool WaitForExit(int timeout)
			{
				return instance.WaitForExit(timeout);
			}

			public bool WaitForIdle(int timeout, uint pollInterval)
			{
				return instance.WaitForIdle(timeout, pollInterval);
			}

			public bool DetectedFault
			{
				get { return instance.DetectedFault; }
			}

			public MonitorData Fault
			{
				get { return instance.Fault; }
			}
		}

		#endregion

		private Remotable<DebuggerServer> _process;

		public DebuggerProxy()
		{
			var sw = Stopwatch.StartNew();

			_process = new Remotable<DebuggerServer>();

			try
			{
				Logger.Debug("Creating {0} from {1}", typeof(T).Name, _process.Url);

				var remote = _process.GetObject();

				var logLevel = Logger.IsTraceEnabled ? 2 : (Logger.IsDebugEnabled ? 1 : 0);

				_dbg = remote.GetProcessDebugger<Proxy>(logLevel);

				Logger.Trace("{0} created in {1}ms", typeof(T).Name, sw.ElapsedMilliseconds);
			}
			catch (RemotingException ex)
			{
				throw new SoftException("Failed to initialize kernel process.", ex);
			}
		}

		public override void Dispose()
		{
			// Killing the process will cause everything to get cleaned up
			_dbg = null;

			if (_process != null)
			{
				_process.Dispose();
				_process = null;
			}
		}
	}
}
