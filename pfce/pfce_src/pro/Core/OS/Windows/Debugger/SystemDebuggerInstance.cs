using System;
using System.Diagnostics;
using System.Management;
using System.ServiceProcess;
using System.Threading;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Monitor = System.Threading.Monitor;
using SysProcess = System.Diagnostics.Process;

namespace Peach.Pro.Core.OS.Windows.Debugger
{
	public class SystemDebuggerInstance : IDebuggerInstance
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public string WinDbgPath { get; set; }
		public string SymbolsPath { get; set; }

		public bool IgnoreFirstChanceGuardPage { get; set; }
		public bool IgnoreSecondChanceGuardPage { get; set; }

		public bool IgnoreFirstChanceReadAv { get; set; }

		private readonly object _mutex = new object();

		private bool _detectedFault;
		private MonitorData _fault;
		private Exception _exception;
		private IDebugger _debugger;

		protected virtual IDebugger OnStartProcess(string commandLine)
		{
			return SystemDebugger.CreateProcess(commandLine);
		}

		protected virtual IDebugger OnAttachProcess(int pid)
		{
			return SystemDebugger.AttachToProcess(pid);
		}

		internal static int GetProcessPid(string processName)
		{
			var procs = SysProcess.GetProcessesByName(processName);
			if (procs.Length > 0)
			{
				var ret = procs[0].Id;

				foreach (var p in procs)
					p.Close();

				return ret;
			}

			int pid;
			if (int.TryParse(processName, out pid))
				return pid;

			throw new PeachException("Unable to locate pid of process named \"" + processName + "\".");
		}

		internal static int GetServicePid(string serviceName, TimeSpan startTimeout)
		{
			using (var sc = new ServiceController(serviceName))
			{
				var wait = true;

				sc.Refresh();

				switch (sc.Status)
				{
					case ServiceControllerStatus.ContinuePending:
						break;
					case ServiceControllerStatus.Paused:
						sc.Continue();
						break;
					case ServiceControllerStatus.PausePending:
						sc.WaitForStatus(ServiceControllerStatus.Paused, startTimeout);
						sc.Continue();
						break;
					case ServiceControllerStatus.Running:
						wait = false;
						break;
					case ServiceControllerStatus.StartPending:
						break;
					case ServiceControllerStatus.Stopped:
						sc.Start();
						break;
					case ServiceControllerStatus.StopPending:
						sc.WaitForStatus(ServiceControllerStatus.Stopped, startTimeout);
						sc.Start();
						break;
				}

				if (wait)
					sc.WaitForStatus(ServiceControllerStatus.Running, startTimeout);

				using (var mo = new ManagementObject(@"Win32_service.Name='" + sc.ServiceName + "'"))
				{
					var pid = mo.GetPropertyValue("ProcessId");
					return (int)(uint)pid;
				}
			}
		}

		public virtual string Name
		{
			get { return "SystemDebugger"; }
		}

		public bool IsRunning
		{
			get
			{
				lock (_mutex)
				{
					return _debugger != null;
				}
			}
		}

		public bool DetectedFault
		{
			get
			{
				lock (_mutex)
				{
					return _detectedFault;
				}
			}
		}

		public MonitorData Fault
		{
			get
			{
				lock (_mutex)
				{
					if (!_detectedFault)
						return null;

					if (_fault == null)
						Monitor.Wait(_mutex);

					Debug.Assert(_fault != null);

					return _fault;
				}
			}
		}

		public void Dispose()
		{
			lock (_mutex)
			{
				if (_debugger == null)
					return;

				_debugger.Stop();
				Monitor.Wait(_mutex);
			}
		}

		public bool WaitForExit(int timeout)
		{
			lock (_mutex)
			{
				if (_debugger == null)
					return true;

				if (timeout == 0)
				{
					_debugger.Stop();
					Monitor.Wait(_mutex);
					return false;
				}

				Logger.Debug("WaitForExit({0})", timeout == -1 ? "INFINITE" : timeout.ToString());

				if (Monitor.Wait(_mutex, timeout))
					return true;

				Logger.Debug("WaitForExit ran out of time, killing debugger!");

				_debugger.Stop();
				Monitor.Wait(_mutex);

				return false;
			}
		}

		public bool WaitForIdle(int timeout, uint pollInterval)
		{
			lock (_mutex)
			{
				var first = true;
				var lastTime = (ulong)0;
				var sw = Stopwatch.StartNew();

				while (_debugger != null)
				{
					var newTime = _debugger.TotalProcessorTicks;

					Logger.Trace("CpuKill: OldTicks={0} NewTicks={1}", lastTime, newTime);

					if (!first && lastTime <= newTime)
					{
						Logger.Debug("Cpu is idle, stopping process.");
						_debugger.Stop();
						Monitor.Wait(_mutex);
						return true;
					}

					lastTime = newTime;
					first = false;

					var remain = timeout - sw.ElapsedMilliseconds;

					if (remain <= 0)
					{
						Logger.Debug("Timed out waiting for cpu idle, stopping process.");
						_debugger.Stop();
						Monitor.Wait(_mutex);
						return true;
					}

					Monitor.Wait(_mutex, (int)Math.Min(remain, pollInterval));
				}

				return true;
			}
		}

		public void StartProcess(string commandLine)
		{
			Start(() => OnStartProcess(commandLine));
		}

		public void AttachProcess(string processName)
		{
			var pid = GetProcessPid(processName);

			Start(() => OnAttachProcess(pid));
		}

		public void StartService(string serviceName, TimeSpan startTimeout)
		{
			var pid = GetServicePid(serviceName, startTimeout);

			Start(() => OnAttachProcess(pid));
		}

		private bool HandleAccessViolation(ExceptionEvent ev)
		{
			if (IgnoreFirstChanceGuardPage && ev.FirstChance != 0 && ev.Code == 0x80000001)
				return true;

			if (IgnoreSecondChanceGuardPage && ev.FirstChance == 0 && ev.Code == 0x80000001)
				return true;

			// Only some first chance exceptions are interesting
			while (ev.FirstChance != 0)
			{
				// Guard page or illegal op
				if (ev.Code == 0x80000001 || ev.Code == 0xC000001D)
					break;

				// http://msdn.microsoft.com/en-us/library/windows/desktop/aa363082(v=vs.85).aspx

				// Access violation
				if (ev.Code == 0xC0000005)
				{
					// A/V on EIP
					if (ev.Info[0] == 0 && !IgnoreFirstChanceReadAv)
						break;

					// write a/v not near null
					if (ev.Info[0] == 1 && ev.Info[1] != 0)
						break;

					// DEP
					if (ev.Info[0] == 8)
						break;
				}

				// Skip uninteresting first chance and keep going
				return true;
			}

			lock (_mutex)
			{
				_detectedFault = true;
			}

			return false;
		}

		private void Start(Func<IDebugger> createFn)
		{
			lock (_mutex)
			{
				if (_debugger != null)
					throw new InvalidOperationException("Can not start system debugger, it is alread running.");

				_fault = null;
				_exception = null;

				var th = new Thread(() => Run(createFn));

				th.Start();

				Monitor.Wait(_mutex);

				if (_debugger != null)
					return;

				Debug.Assert(_exception != null);

				throw new PeachException(_exception.Message, _exception);
			}
		}

		private void Run(Func<IDebugger> createFn)
		{
			try
			{
				var dbg = createFn();

				dbg.HandleAccessViolation = HandleAccessViolation;

				dbg.ProcessCreated = pid =>
				{
					lock (_mutex)
					{
						_debugger = dbg;
						Monitor.Pulse(_mutex);
					}
				};

				dbg.Run();
			}
			catch (Exception ex)
			{
				_exception = ex;
			}
			finally
			{
				lock (_mutex)
				{
					if (_debugger != null)
					{
						_fault = _debugger.Fault;
						_debugger.Dispose();
						_debugger = null;
					}

					Monitor.Pulse(_mutex);
				}
			}
		}
	}
}
