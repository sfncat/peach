using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Pro.Core.OS.Windows.Debugger;
using Peach.Pro.OS.Windows.Agent.Monitors;
using System.ComponentModel;

namespace Peach.Pro.Core.OS.Windows.Agent.Monitors
{
	[Monitor("WindowsDebugger")]
	[Alias("WindowsDebuggerHybrid")]
	[Alias("WindowsDebugEngine")]
	[Alias("debugger.WindowsDebugEngine")]
	[Description("Controls a Windows debugger instance")]
	[Parameter("Executable", typeof(string), "Executable to launch", "")]
	[Parameter("Arguments", typeof(string), "Optional command line arguments", "")]
	[Parameter("ProcessName", typeof(string), "Name of process to attach too.", "")]
	[Parameter("Service", typeof(string), "Name of Windows Service to attach to.  Service will be started if stopped or crashes.", "")]
	[Parameter("SymbolsPath", typeof(string), "Optional Symbol path.  Default is Microsoft public symbols server.", "SRV*http://msdl.microsoft.com/download/symbols")]
	[Parameter("WinDbgPath", typeof(string), "Path to WinDbg install.  If not provided we will try and locate it.", "")]
	[Parameter("StartOnCall", typeof(string), "Indicate the debugger should wait to start or attach to process until notified by state machine.", "")]
	[Parameter("IgnoreFirstChanceReadAv", typeof(bool), "Ignore first chance read access violation exceptions.  These are sometimes false posistives or anti-debugging faults.", "false")]
	[Parameter("IgnoreFirstChanceGuardPage", typeof(bool), "Ignore first chance guard page faults.  These are sometimes false posistives or anti-debugging faults.", "false")]
	[Parameter("IgnoreSecondChanceGuardPage", typeof(bool), "Ignore second chance guard page faults.  These are sometimes false posistives or anti-debugging faults.", "false")]
	[Parameter("NoCpuKill", typeof(bool), "Don't use process CPU usage to terminate early.", "false")]
	[Parameter("CpuPollInterval", typeof(uint), "How often to poll for idle CPU in milliseconds.", "200")]
	[Parameter("FaultOnEarlyExit", typeof(bool), "Trigger fault if process exists (defaults to false)", "false")]
	[Parameter("WaitForExitOnCall", typeof(string), "Wait for process to exit on state model call and fault if timeout is reached", "")]
	[Parameter("WaitForExitTimeout", typeof(int), "Wait for exit timeout value in milliseconds (-1 is infinite)", "10000")]
	[Parameter("RestartOnEachTest", typeof(bool), "Restart process for each interation", "false")]
	[Parameter("RestartAfterFault", typeof(bool), "Restart the process if a different monitor detects a fault", "true")]
	[Parameter("ServiceStartTimeout", typeof(int), "How many seconds to wait for target windows service to start", "60")]
	[Parameter("DebuggerMode", typeof(HybridMode), "What mode the debugger should run in.  ForceWinDbg will cause fuzzing speeds to drop significantly.", "Fast")]
	public class WindowsDebuggerHybrid : Monitor2
	{
		public enum HybridMode
		{
			Fast,
			ForceWinDbg
		}

		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public string CommandLine { get; set; }
		public string Executable { get; set; }
		public string Arguments { get; set; }
		public string ProcessName { get; set; }
		public string Service { get; set; }
		public string SymbolsPath { get; set; }
		public string WinDbgPath { get; set; }
		public string StartOnCall { get; set; }
		public bool IgnoreFirstChanceReadAv { get; set; }
		public bool IgnoreFirstChanceGuardPage { get; set; }
		public bool IgnoreSecondChanceGuardPage { get; set; }
		public bool NoCpuKill { get; set; }
		public uint CpuPollInterval { get; set; }
		public bool FaultOnEarlyExit { get; set; }
		public string WaitForExitOnCall { get; set; }
		public int WaitForExitTimeout { get; set; }
		public bool RestartOnEachTest { get; set; }
		public bool RestartAfterFault { get; set; }
		public int ServiceStartTimeout { get; set; }
		public HybridMode DebuggerMode { get; set; }

		private IDebuggerInstance _debugger;
		private bool _replay;
		private bool _stopMessage;
		private bool _exitEarly;
		private bool _exitTimeout;
		private MonitorData _fault;

		public WindowsDebuggerHybrid(string name)
			: base(name)
		{
		}

		public override void StartMonitor(Dictionary<string, string> args)
		{
			if (!Environment.Is64BitProcess && Environment.Is64BitOperatingSystem)
				throw new PeachException("Error: Cannot use the 32bit version of Peach on a 64bit operating system.");

			if (Environment.Is64BitProcess && !Environment.Is64BitOperatingSystem)
				throw new PeachException("Error: Cannot use the 64bit version of Peach on a 32bit operating system.");

			base.StartMonitor(args);

			ParameterParser.EnsureOne(this, "Executable", "ProcessName", "Service");

			if (!string.IsNullOrEmpty(Executable) && !string.IsNullOrEmpty(Arguments))
				CommandLine = Executable + " " + Arguments;
			else
				CommandLine = Executable;

			WinDbgPath = WindowsKernelDebugger.FindWinDbg(WinDbgPath);

		}

		public override void SessionStarting()
		{
			if (StartOnCall == null && !RestartOnEachTest)
				_StartDebugger();
		}

		public override void SessionFinished()
		{
			if (_debugger != null)
				_StopDebugger();
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			_stopMessage = false;
			_exitEarly = false;
			_exitTimeout = false;
			_replay = args.IsReproduction;

			if (_debugger != null)
			{
				if ((RestartAfterFault && args.LastWasFault) || !_debugger.IsRunning)
					_StopDebugger();
			}

			if (StartOnCall == null && _debugger == null)
				_StartDebugger();
		}

		public override void IterationFinished()
		{
		}

		public override void Message(string msg)
		{
			if (msg == StartOnCall)
			{
				if (_debugger != null)
					_StopDebugger();

				_StartDebugger();
			}
			else if (msg == WaitForExitOnCall)
			{
				_stopMessage = true;
				_exitTimeout = !_debugger.WaitForExit(WaitForExitTimeout);
			}
		}

		public override bool DetectedFault()
		{
			Logger.Trace("DetectedFault()");

			// Moved from Iteration finished to push exit
			// check into detected fault.
			if (_debugger != null)
			{
				if (!_stopMessage && FaultOnEarlyExit && !_debugger.IsRunning)
				{
					_exitEarly = true;
				}
				else if (StartOnCall != null)
				{
					if (NoCpuKill)
						_exitTimeout = !_debugger.WaitForExit(WaitForExitTimeout);
					else
						_debugger.WaitForIdle(WaitForExitTimeout, CpuPollInterval);
				}
				else if (RestartOnEachTest)
				{
					_debugger.WaitForExit(0);
				}
			}

			_fault = null;

			if (_debugger != null)
			{
				Logger.Debug("Using {0}, checking for fault", _debugger.Name);

				if (_debugger.DetectedFault)
				{
					Logger.Info("Caught fault with {0}", _debugger.Name);
					return true;
				}
			}

			if (_exitEarly)
			{
				Logger.Info("Fault detected, process exited early");
				_fault = GetGeneralFault("ExitedEarly", "Process exited early.");
			}
			else if (_exitTimeout)
			{
				Logger.Info("Fault detected, timed out waiting for process to exit");
				_fault = GetGeneralFault("FailedToExit", "Process did not exit in " + WaitForExitTimeout + "ms.");
			}

			if (_fault != null)
				return true;

			Logger.Trace("No fault detected");
			return false;
		}

		public override MonitorData GetMonitorData()
		{
			if (_debugger != null && _debugger.DetectedFault)
				_fault = _debugger.Fault;

			return _fault;
		}

		private void _StartDebugger()
		{
			Debug.Assert(_debugger == null);

			_debugger = _replay || DebuggerMode == HybridMode.ForceWinDbg
				? GetDebuggerInstance<DebuggerProxy<DebugEngineInstance>>()
				//? GetDebuggerInstance<DebugEngineInstance>()
				//: GetDebuggerInstance<DebuggerProxy<SystemDebuggerInstance>>();
				: GetDebuggerInstance<SystemDebuggerInstance>();

			if (!string.IsNullOrEmpty(CommandLine))
				_debugger.StartProcess(CommandLine);
			else if (!string.IsNullOrEmpty(ProcessName))
				_debugger.AttachProcess(ProcessName);
			else if (!string.IsNullOrEmpty(Service))
				_debugger.StartService(Service, TimeSpan.FromSeconds(ServiceStartTimeout));
			else
				throw new NotSupportedException();

			OnInternalEvent(EventArgs.Empty);
		}

		private void _StopDebugger()
		{
			Debug.Assert(_debugger != null);

			_debugger.Dispose();
			_debugger = null;
		}

		private IDebuggerInstance GetDebuggerInstance<T>() where T : class, IDebuggerInstance, new()
		{
			return new T
			{
				IgnoreFirstChanceReadAv = IgnoreFirstChanceReadAv,
				IgnoreFirstChanceGuardPage = IgnoreFirstChanceGuardPage,
				IgnoreSecondChanceGuardPage = IgnoreSecondChanceGuardPage,
				WinDbgPath = WinDbgPath,
				SymbolsPath = SymbolsPath
			};
		}

		private MonitorData GetGeneralFault(string type, string reason)
		{
			var title = string.Empty;

			if (ProcessName != null)
				title = ProcessName;
			else if (Executable != null)
				title = Executable;
			else if (Service != null)
				title = Service;

			var desc = reason + ": " + title;

			var fault = new MonitorData
			{
				DetectionSource = _debugger.Name,
				Title = reason,
				Data = new Dictionary<string, Stream>(),
				Fault = new MonitorData.Info
				{
					Description = desc,
					MajorHash = Hash(Class + title),
					MinorHash = Hash(type),
				}
			};

			return fault;
		}

	}
}

#if DISABLED
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Pro.OS.Windows.Agent.Monitors.WindowsDebug;
using Peach.Pro.OS.Windows.Debuggers;
using Monitor = Peach.Core.Agent.Monitor2;
using Random = Peach.Core.Random;
using System.ComponentModel;

namespace Peach.Pro.OS.Windows.Agent.Monitors
{
	[Monitor("WindowsDebugger")]
	[Alias("WindowsDebuggerHybrid")]
	[Alias("WindowsDebugEngine")]
	[Alias("debugger.WindowsDebugEngine")]
	[Description("Controls a Windows debugger instance")]
	[Parameter("Executable", typeof(string), "Executable to launch", "")]
	[Parameter("Arguments", typeof(string), "Optional command line arguments", "")]
	[Parameter("ProcessName", typeof(string), "Name of process to attach too.", "")]
	[Parameter("Service", typeof(string), "Name of Windows Service to attach to.  Service will be started if stopped or crashes.", "")]
	[Parameter("SymbolsPath", typeof(string), "Optional Symbol path.  Default is Microsoft public symbols server.", "SRV*http://msdl.microsoft.com/download/symbols")]
	[Parameter("WinDbgPath", typeof(string), "Path to WinDbg install.  If not provided we will try and locate it.", "")]
	[Parameter("StartOnCall", typeof(string), "Indicate the debugger should wait to start or attach to process until notified by state machine.", "")]
	[Parameter("IgnoreFirstChanceGuardPage", typeof(bool), "Ignore first chance guard page faults.  These are sometimes false posistives or anti-debugging faults.", "false")]
	[Parameter("IgnoreSecondChanceGuardPage", typeof(bool), "Ignore second chance guard page faults.  These are sometimes false posistives or anti-debugging faults.", "false")]
	[Parameter("NoCpuKill", typeof(bool), "Don't use process CPU usage to terminate early.", "false")]
	[Parameter("CpuPollInterval", typeof(uint), "How often to poll for idle CPU in milliseconds.", "200")]
	[Parameter("FaultOnEarlyExit", typeof(bool), "Trigger fault if process exists (defaults to false)", "false")]
	[Parameter("WaitForExitOnCall", typeof(string), "Wait for process to exit on state model call and fault if timeout is reached", "")]
	[Parameter("WaitForExitTimeout", typeof(int), "Wait for exit timeout value in milliseconds (-1 is infinite)", "10000")]
	[Parameter("RestartOnEachTest", typeof(bool), "Restart process for each interation", "false")]
	[Parameter("RestartAfterFault", typeof(bool), "Restart process after any fault occurs", "false")]
	[Parameter("ServiceStartTimeout", typeof(int), "How many seconds to wait for target windows service to start", "60")]
	public class WindowsDebuggerHybrid : Monitor
	{
		protected static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		string _commandLine = null;
		string _processName = null;
		string _kernelConnectionString = null;
		string _service = null;

		string _winDbgPath = null;
		string _symbolsPath = "SRV*http://msdl.microsoft.com/download/symbols";
		string _waitForExitOnCall = null;
		string _startOnCall = null;

		int _waitForExitTimeout = 10000;
		int _cpuPollInterval = 200;
		int _serviceStartTimeout = 60;

		bool _ignoreFirstChanceGuardPage = false;
		bool _ignoreSecondChanceGuardPage = false;
		bool _noCpuKill = false;
		bool _faultOnEarlyExit = false;
		bool _restartOnEachTest = false;
		bool _restartAfterFault = false;

		bool _waitForExitFailed = false;
		bool _earlyExitFault = false;
		bool _stopMessage = false;
		bool _hybrid = true;
		bool _replay = false;
		MonitorData _fault = null;

		DebuggerInstance _debugger = null;
		IDebuggerInstance _systemDebugger = null;
		Thread _ipcHeartBeatThread = null;
		System.Threading.Mutex _ipcHeartBeatMutex = null;

		public WindowsDebuggerHybrid(string name)
			: base(name)
		{
		}

		public override void StartMonitor(Dictionary<string, string> args)
		{
			//var color = Console.ForegroundColor;
			if (!Environment.Is64BitProcess && Environment.Is64BitOperatingSystem)
			{
				//Console.ForegroundColor = ConsoleColor.Yellow;
				//Console.WriteLine("\nError: Cannot use the 32bit version of Peach 3 on a 64bit operating system.");
				//Console.ForegroundColor = color;
				//return;
				throw new PeachException("Error: Cannot use the 32bit version of Peach 3 on a 64bit operating system.");
			}
			else if (Environment.Is64BitProcess && !Environment.Is64BitOperatingSystem)
			{
				//Console.ForegroundColor = ConsoleColor.Yellow;
				//Console.WriteLine("\nError: Cannot use the 64bit version of Peach 3 on a 32bit operating system.");
				//Console.ForegroundColor = color;

				throw new PeachException("Error: Cannot use the 64bit version of Peach 3 on a 32bit operating system.");
			}

			if (args.ContainsKey("Executable"))
			{
				_commandLine = (string)args["Executable"];

				if (args.ContainsKey("Arguments"))
				{
					_commandLine += " ";
					_commandLine += (string)args["Arguments"];

				}
			}
			else if (args.ContainsKey("CommandLine"))
			{
				logger.Info("The parameter 'CommandLine' on the monitor 'WindowsDebugger' is deprecated.  Use the parameters 'Executable' and 'Arguments' instead.");
				_commandLine = (string)args["CommandLine"];
			}
			else if (args.ContainsKey("ProcessName"))
			{
				_processName = (string)args["ProcessName"];
			}
			else if (args.ContainsKey("Service"))
				_service = (string)args["Service"];
			else
				throw new PeachException("Error, WindowsDebugEngine started with out a Executable, ProcessName, KernelConnectionString or Service parameter.");

			if (args.ContainsKey("SymbolsPath"))
				_symbolsPath = (string)args["SymbolsPath"];
			if (args.ContainsKey("StartOnCall"))
				_startOnCall = (string)args["StartOnCall"];
			if (args.ContainsKey("WaitForExitOnCall"))
				_waitForExitOnCall = (string)args["WaitForExitOnCall"];
			if (args.ContainsKey("WaitForExitTimeout") && !int.TryParse((string)args["WaitForExitTimeout"], out _waitForExitTimeout))
				throw new PeachException("Error, 'WaitForExitTimeout' is not a valid number.");
			if (args.ContainsKey("ServiceStartTimeout") && !int.TryParse(args["ServiceStartTimeout"], out _serviceStartTimeout))
				throw new PeachException("Error, 'ServiceStartTimeout' is not a valid number.");

			if (args.ContainsKey("WinDbgPath"))
			{
				_winDbgPath = (string)args["WinDbgPath"];

				var type = FileInfoImpl.GetMachineType(Path.Combine(_winDbgPath, "dbgeng.dll"));
				if (Environment.Is64BitProcess && type != Platform.Architecture.x64)
					throw new PeachException("Error, provided WinDbgPath is not x64.");
				else if (!Environment.Is64BitProcess && type != Platform.Architecture.x86)
					throw new PeachException("Error, provided WinDbgPath is not x86.");
			}
			else
			{
				_winDbgPath = FindWinDbg();
				if (_winDbgPath == null)
					throw new PeachException("Error, unable to locate WinDbg, please specify using 'WinDbgPath' parameter.");
			}

			if (args.ContainsKey("RestartAfterFault") && ((string)args["RestartAfterFault"]).ToLower() == "true")
				_restartAfterFault = true;
			if (args.ContainsKey("RestartOnEachTest") && ((string)args["RestartOnEachTest"]).ToLower() == "true")
				_restartOnEachTest = true;
			if (args.ContainsKey("IgnoreFirstChanceGuardPage") && ((string)args["IgnoreFirstChanceGuardPage"]).ToLower() == "true")
				_ignoreFirstChanceGuardPage = true;
			if (args.ContainsKey("IgnoreSecondChanceGuardPage") && ((string)args["IgnoreSecondChanceGuardPage"]).ToLower() == "true")
				_ignoreSecondChanceGuardPage = true;
			if (args.ContainsKey("NoCpuKill") && ((string)args["NoCpuKill"]).ToLower() == "true")
				_noCpuKill = true;
			if (args.ContainsKey("FaultOnEarlyExit") && ((string)args["FaultOnEarlyExit"]).ToLower() == "true")
				_faultOnEarlyExit = true;
			if (args.ContainsKey("CpuPollInterval"))
				_cpuPollInterval = Convert.ToInt32((string)args["CpuPollInterval"]);
		}

		/// <summary>
		/// Make sure we clean up debuggers
		/// </summary>
		~WindowsDebuggerHybrid()
		{
			_FinishDebugger();
		}

		/// <summary>
		/// Send a hearbeat to the debugger process to keep it alive.
		/// </summary>
		public void IpcHeartBeat()
		{
			try
			{
				while (true)
				{
					if (_debugger == null)
					{
						logger.Trace("Exiting heartbeat thread, debugger is null.");
						return;
					}

					if (_ipcHeartBeatMutex.WaitOne(10 * 1000))
					{
						logger.Trace("Exiting heartbeat thread, mutex acquired.");
						return;
					}

					logger.Trace("Sending debugger heartbeat.");
					_debugger.HeartBeat();
				}
			}
			catch(Exception ex)
			{
				logger.Warn("Exception while sending heartbeat: " + ex.Message);
				logger.Debug(ex);
			}
		}

		public static string FindWinDbg()
		{
			// Lets try a few common places before failing.
			List<string> pgPaths = new List<string>();
			pgPaths.Add(@"c:\");
			pgPaths.Add(Environment.GetEnvironmentVariable("SystemDrive"));
			pgPaths.Add(Environment.GetEnvironmentVariable("ProgramFiles"));

			if (Environment.GetEnvironmentVariable("ProgramW6432") != null)
				pgPaths.Add(Environment.GetEnvironmentVariable("ProgramW6432"));
			if (Environment.GetEnvironmentVariable("ProgramFiles") != null)
				pgPaths.Add(Environment.GetEnvironmentVariable("ProgramFiles"));
			if (Environment.GetEnvironmentVariable("ProgramFiles(x86)") != null)
				pgPaths.Add(Environment.GetEnvironmentVariable("ProgramFiles(x86)"));

			List<string> dbgPaths = new List<string>();
			dbgPaths.Add("Debuggers");
			dbgPaths.Add("Debugger");
			dbgPaths.Add("Debugging Tools for Windows");
			dbgPaths.Add("Debugging Tools for Windows (x64)");
			dbgPaths.Add("Debugging Tools for Windows (x86)");
			dbgPaths.Add("Windows Kits\\8.0\\Debuggers\\x64");
			dbgPaths.Add("Windows Kits\\8.0\\Debuggers\\x86");
			dbgPaths.Add("Windows Kits\\8.1\\Debuggers\\x64");
			dbgPaths.Add("Windows Kits\\8.1\\Debuggers\\x86");
			
			foreach (string path in pgPaths)
			{
				foreach (string dpath in dbgPaths)
				{
					string pathCheck = Path.Combine(path, dpath);
					if (Directory.Exists(pathCheck) && File.Exists(Path.Combine(pathCheck, "dbgeng.dll")))
					{
						//verify x64 vs x86
						var type = FileInfoImpl.GetMachineType(Path.Combine(pathCheck, "dbgeng.dll"));

						if (Environment.Is64BitProcess && type != Platform.Architecture.x64)
							continue;
						else if (!Environment.Is64BitProcess && type != Platform.Architecture.x86)
							continue;

						return pathCheck;
					}
				}
			}

			return null;
		}

		public override void Message(string msg)
		{
			if (msg == _startOnCall)
			{
				_StopDebugger();
				_StartDebugger();
			}
			else if (msg == _waitForExitOnCall)
			{
				_stopMessage = true;
				_WaitForExit(false);
				_StopDebugger();
			}
		}

		public override void StopMonitor()
		{
			_StopDebugger();
			_FinishDebugger();

			_debugger = null;
			_systemDebugger = null;
		}

		public override void SessionStarting()
		{
			logger.Debug("SessionStarting");

			if (_startOnCall != null)
				return;

			_StartDebugger();
		}

		public override void SessionFinished()
		{
			logger.Debug("SessionFinished");

			_StopDebugger();
			_FinishDebugger();
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			_replay = args.IsReproduction;
			_waitForExitFailed = false;
			_earlyExitFault = false;
			_stopMessage = false;

			if (_restartAfterFault && args.LastWasFault)
				_FinishDebugger();

			if (!_IsDebuggerRunning() && _startOnCall == null)
				_StartDebugger();
		}

		public override void IterationFinished()
		{
			if (!_stopMessage && _faultOnEarlyExit && !_IsDebuggerRunning())
			{
				_earlyExitFault = true;
				_StopDebugger();
			}
			else if (_startOnCall != null)
			{
				_WaitForExit(true);
				_StopDebugger();
			}
			else if (_restartOnEachTest)
			{
				_StopDebugger();
			}
		}

		public override bool DetectedFault()
		{
			logger.Debug("DetectedFault()");

			_fault = null;

			if (_systemDebugger != null)
			{
				_fault = _systemDebugger.Fault;

				if (_fault != null)
				{
					logger.Debug("DetectedFault - Using system debugger, caught exception");
					_systemDebugger.Stop();
					_systemDebugger = null;
				}
			}
			else if (_debugger != null && _hybrid)
			{
				logger.Debug("DetectedFault - Using WinDbg, checking for fault");

				// Lets give windbg a chance to detect the crash.
				// 10 seconds should be enough.
				for (int i = 0; i < 10; i++)
				{
					if (_debugger != null && _debugger.caughtException)
					{
						// Kill off our debugger process and re-create
						_debuggerProcessUsage = _debuggerProcessUsageMax;
						_fault = GetDebuggerFault(_debugger.crashInfo);
						break;
					}

					Thread.Sleep(1000);
				}

				if (_fault == null && _earlyExitFault)
					_fault = GetEarlyExitFault();

				if(_fault != null)
					logger.Debug("DetectedFault - Caught fault with windbg");

				if (_debugger != null && _hybrid && _fault == null)
				{
					_StopDebugger();
					_FinishDebugger();
				}
			}
			else if (_debugger != null && _debugger.caughtException)
			{
				// Kill off our debugger process and re-create
				_debuggerProcessUsage = _debuggerProcessUsageMax;
				_fault = GetDebuggerFault(_debugger.crashInfo);
			}
			else if (_earlyExitFault)
			{
				logger.Debug("DetectedFault() - Fault detected, process exited early");
				_fault = GetEarlyExitFault();
			}
			else if (_waitForExitFailed)
			{
				logger.Debug("DetectedFault() - Fault detected, WaitForExitOnCall failed");
				_fault = GetGeneralFault("FailedToExit", "Process did not exit in " + _waitForExitTimeout + "ms.");
			}

			if(_fault == null)
				logger.Debug("DetectedFault() - No fault detected");

			return _fault != null;
		}

		public override MonitorData GetMonitorData()
		{
			if (_fault != null && _hybrid)
			{
				_StopDebugger();
				_FinishDebugger();
			}

			return _fault;
		}

		protected MonitorData GetEarlyExitFault()
		{
			return GetGeneralFault("ExitedEarly", "Process exited early.");
		}

		protected MonitorData GetDebuggerFault(Fault f)
		{
			return new MonitorData
			{
				DetectionSource = _systemDebugger != null ? "SystemDebugger" : "WindowsDebugEngine",
				Title = f.title,
				Data = f.collectedData.ToDictionary(i => i.Key, i => (Stream)new MemoryStream(i.Value)),
				Fault = new MonitorData.Info
				{
					Description = f.description,
					MajorHash = f.majorHash,
					MinorHash = f.minorHash,
					Risk = f.exploitability,
				}
			};
		}

		protected MonitorData GetGeneralFault(string type, string reason)
		{
			var name = string.Empty;

			if (_processName != null)
				name = _processName;
			else if (_commandLine != null)
				name = _commandLine;
			else if (_kernelConnectionString != null)
				name = _kernelConnectionString;
			else if (_service != null)
				name = _service;

			var desc = reason + ": " + name;

			var fault = new MonitorData
			{
				DetectionSource = _systemDebugger != null ? "SystemDebugger" : "WindowsDebugEngine",
				Title = reason,
				Data = new Dictionary<string, Stream>(),
				Fault = new MonitorData.Info
				{
					Description = desc,
					MajorHash = Hash(Class + name),
					MinorHash = Hash(type),
				}
			};

			return fault;
		}

		protected bool _IsDebuggerRunning()
		{
			if (_systemDebugger != null && _systemDebugger.IsRunning)
				return true;

			if (_debugger != null && _debugger.IsRunning)
				return true;

			return false;
		}

		System.Diagnostics.Process _debuggerProcess = null;
		int _debuggerProcessUsage = 0;
		int _debuggerProcessUsageMax = 100;
		string _debuggerChannelName = null;

		protected void _StartDebugger()
		{
			if (_hybrid)
				_StartDebuggerHybrid();
			else
				_StartDebuggerNonHybrid();
		}

		/// <summary>
		/// The hybrid mode uses both WinDbg and System Debugger
		/// </summary>
		/// <remarks>
		/// When _hybrid == true &amp;&amp; _replay == false we will use the
		/// System Debugger.
		/// 
		/// When we hit a fault in _hybrid mode we will replay with windbg.
		/// 
		/// When _hybrid == false we will just use windbg.
		/// </remarks>
		protected void _StartDebuggerHybrid()
		{
			if (_replay)
			{
				_StartDebuggerHybridReplay();
				return;
			}

			// Start system debugger
			if (_systemDebugger == null || !_systemDebugger.IsRunning)
			{
				if (_systemDebugger == null)
					_systemDebugger = new SystemDebuggerInstance();

				if (!string.IsNullOrEmpty(_commandLine))
					_systemDebugger.StartProcess(_commandLine);
				else if (!string.IsNullOrEmpty(_service))
					_systemDebugger.StartService(_service, TimeSpan.FromSeconds(_serviceStartTimeout));
				else
					_systemDebugger.AttachProcess(_processName);

				OnInternalEvent(EventArgs.Empty);
			}
		}

		static int ipcChannelCount = 0;

		/// <summary>
		/// Hybrid replay mode uses windbg
		/// </summary>
		protected void _StartDebuggerHybridReplay()
		{
			if (_debuggerProcess == null || _debuggerProcess.HasExited)
			{
				using (var p = System.Diagnostics.Process.GetCurrentProcess())
				{
					_debuggerChannelName = "PeachCore_" + p.Id + "_" + (ipcChannelCount++);
				}

				// Launch the server process
				_debuggerProcess = new System.Diagnostics.Process();

				_debuggerProcess.StartInfo.CreateNoWindow = true;
				_debuggerProcess.StartInfo.UseShellExecute = false;
				_debuggerProcess.StartInfo.Arguments = _debuggerChannelName;
				_debuggerProcess.StartInfo.FileName = Utilities.GetAppResourcePath("Peach.Pro.WindowsDebugInstance.exe");

				if (logger.IsTraceEnabled)
				{
					_debuggerProcess.EnableRaisingEvents = true;
					_debuggerProcess.StartInfo.Arguments += " --debug";
					_debuggerProcess.OutputDataReceived += _debuggerProcess_OutputDataReceived;
					_debuggerProcess.ErrorDataReceived += _debuggerProcess_ErrorDataReceived;
					_debuggerProcess.StartInfo.RedirectStandardError = true;
					_debuggerProcess.StartInfo.RedirectStandardOutput = true;
				}

				_debuggerProcess.Start();

				if (logger.IsTraceEnabled)
				{
					_debuggerProcess.BeginErrorReadLine();
					_debuggerProcess.BeginOutputReadLine();
				}

				// Let the process get started.
				Thread.Sleep(2000);
			}

			// Try and create instance over IPC.  We will continue trying for 1 minute.

			DateTime startTimer = DateTime.Now;
			while (true)
			{
				try
				{
					logger.Debug("Trying to create DebuggerInstance: ipc://" + _debuggerChannelName + "/DebuggerInstance");

					_debugger = (DebuggerInstance)Activator.GetObject(typeof(DebuggerInstance),
						"ipc://" + _debuggerChannelName + "/DebuggerInstance");

					_debugger.commandLine = _commandLine;
					_debugger.processName = _processName;
					_debugger.kernelConnectionString = _kernelConnectionString;
					_debugger.service = _service;
					_debugger.serviceStartTimeout = _serviceStartTimeout;
					_debugger.symbolsPath = _symbolsPath;
					_debugger.startOnCall = _startOnCall;
					_debugger.ignoreFirstChanceGuardPage = _ignoreFirstChanceGuardPage;
					_debugger.ignoreSecondChanceGuardPage = _ignoreSecondChanceGuardPage;
					_debugger.noCpuKill = _noCpuKill;
					_debugger.winDbgPath = _winDbgPath;

					// Start a thread to send heartbeats to ipc process
					// otherwise ipc process will exit
					_ipcHeartBeatMutex = new Mutex();
					_ipcHeartBeatMutex.WaitOne();
					_ipcHeartBeatThread = new Thread(new ThreadStart(IpcHeartBeat));
					_ipcHeartBeatThread.Start();

					logger.Debug("Created!");
					break;
				}
				catch(Exception ex)
				{
					logger.Debug("IPC Failed: " + ex.Message);
					if ((DateTime.Now - startTimer).Minutes >= 1)
					{
						_debuggerProcess.Kill();
						_debuggerProcess = null;
						throw;
					}
				}
			}

			_debugger.StartDebugger();
		}

		/// <summary>
		/// Origional non-hybrid windbg only mode
		/// </summary>
		protected void _StartDebuggerNonHybrid()
		{
			logger.Debug("_StartDebuggerNonHybrid");

			if (_debuggerProcessUsage >= _debuggerProcessUsageMax && _debuggerProcess != null)
			{
				_FinishDebugger();

				_debuggerProcessUsage = 0;
			}

			if (_debuggerProcess == null || _debuggerProcess.HasExited)
			{
				_debuggerChannelName = "PeachCore_" + (new Random((uint)Environment.TickCount).NextUInt32().ToString());

				// Launch the server process
				_debuggerProcess = new System.Diagnostics.Process();
				_debuggerProcess.StartInfo.CreateNoWindow = true;
				_debuggerProcess.StartInfo.UseShellExecute = false;
				_debuggerProcess.StartInfo.Arguments = _debuggerChannelName;
				_debuggerProcess.StartInfo.FileName = Utilities.GetAppResourcePath("Peach.Pro.WindowsDebugInstance.exe");

				if (logger.IsTraceEnabled)
				{
					_debuggerProcess.EnableRaisingEvents = true;
					_debuggerProcess.StartInfo.Arguments += " --debug";
					_debuggerProcess.OutputDataReceived += _debuggerProcess_OutputDataReceived;
					_debuggerProcess.ErrorDataReceived += _debuggerProcess_ErrorDataReceived;
					_debuggerProcess.StartInfo.RedirectStandardError = true;
					_debuggerProcess.StartInfo.RedirectStandardOutput = true;
				}

				_debuggerProcess.Start();

				if (logger.IsTraceEnabled)
				{
					_debuggerProcess.BeginErrorReadLine();
					_debuggerProcess.BeginOutputReadLine();
				}
			}

			_debuggerProcessUsage++;

			// Try and create instance over IPC.  We will continue trying for 1 minute.

			DateTime startTimer = DateTime.Now;
			while (true)
			{
				try
				{
					logger.Debug("Creating DebuggerInstance: ipc://" + _debuggerChannelName + "/DebuggerInstance");
					_debugger = (DebuggerInstance)Activator.GetObject(typeof(DebuggerInstance),
						"ipc://" + _debuggerChannelName + "/DebuggerInstance");
					//_debugger = new DebuggerInstance();

					_debugger.commandLine = _commandLine;
					_debugger.processName = _processName;
					_debugger.kernelConnectionString = _kernelConnectionString;
					_debugger.service = _service;
					_debugger.symbolsPath = _symbolsPath;
					_debugger.startOnCall = _startOnCall;
					_debugger.ignoreFirstChanceGuardPage = _ignoreFirstChanceGuardPage;
					_debugger.ignoreSecondChanceGuardPage = _ignoreSecondChanceGuardPage;
					_debugger.noCpuKill = _noCpuKill;
					_debugger.winDbgPath = _winDbgPath;

					// Start a thread to send heartbeats to ipc process
					// otherwise ipc process will exit
					_ipcHeartBeatMutex = new Mutex();
					_ipcHeartBeatMutex.WaitOne();
					_ipcHeartBeatThread = new Thread(new ThreadStart(IpcHeartBeat));
					_ipcHeartBeatThread.Start();

					break;
				}
				catch(Exception ex)
				{
					logger.Debug("IPC Exception: " + ex.Message);
					logger.Debug("Retrying IPC connection");

					if ((DateTime.Now - startTimer).Minutes >= 1)
					{
						try
						{
							logger.Debug("IPC Failed");
							_debuggerProcess.Kill();
							_debuggerProcess.Close();
						}
						catch
						{
						}

						throw;
					}
				}
			}

			_debugger.StartDebugger();

			OnInternalEvent(EventArgs.Empty);
		}

		void _debuggerProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (!string.IsNullOrEmpty(e.Data))
				logger.Debug(e.Data);
		}

		void _debuggerProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (!string.IsNullOrEmpty(e.Data))
				logger.Debug(e.Data);
		}

		protected void _FinishDebugger()
		{
			logger.Debug("_FinishDebugger");

			_StopDebugger();

			if (_systemDebugger != null)
			{
				try
				{
					_systemDebugger.Stop();
				}
				catch
				{
				}
			}

			if (_debugger != null)
			{
				try
				{
					_debugger.FinishDebugging();
				}
				catch
				{
				}

				try
				{
					_ipcHeartBeatMutex.ReleaseMutex();
				}
				catch
				{
				}

				_ipcHeartBeatThread.Join();
				_ipcHeartBeatThread = null;
				_ipcHeartBeatMutex = null;
			}

			_debugger = null;
			_systemDebugger = null;

			if (_debuggerProcess != null)
			{
				try
				{
					if(!_debuggerProcess.WaitForExit(2000))
						_debuggerProcess.Kill();
				}
				catch
				{
				}

				_debuggerProcess.Close();
				_debuggerProcess = null;
			}
		}

		protected void _StopDebugger()
		{
			logger.Debug("_StopDebugger");

			if (_systemDebugger != null)
			{
				try
				{
					_systemDebugger.Stop();
				}
				catch
				{
				}
			}

			if (_debugger != null)
			{
				try
				{
					_debugger.StopDebugger();
				}
				catch
				{
				}
			}
		}

		protected void _WaitForExit(bool useCpuKill)
		{
			if (!_IsDebuggerRunning())
				return;

			try
			{
				int pid = _debugger != null ? _debugger.ProcessId : _systemDebugger.ProcessId;
				using (var proc = ProcessHelper.GetProcessById(pid))
				{
					// TODO: Eventually just call proc.WaitForIdle();
					if (proc == null || !proc.IsRunning)
						return;

					if (useCpuKill && !_noCpuKill)
					{
						ulong lastTime = 0;
						int i = 0;

						for (i = 0; i < _waitForExitTimeout; i += _cpuPollInterval)
						{
							// Note: Performance counters were used and removed due to speed issues.
							//       monitoring the tick count is more reliable and less likely to cause
							//       fuzzing slow-downs.
							var pi = proc.Snapshot();
							// TODO: Handle failure to take snapshot!

							logger.Trace("CpuKill: OldTicks={0} NewTicks={1}", lastTime, pi.TotalProcessorTicks);

							if (i != 0 && lastTime == pi.TotalProcessorTicks)
							{
								logger.Debug("Cpu is idle, stopping process.");
								break;
							}

							lastTime = pi.TotalProcessorTicks;
							Thread.Sleep(_cpuPollInterval);
						}

						if (i >= _waitForExitTimeout)
							logger.Debug("Timed out waiting for cpu idle, stopping process.");
					}
					else
					{
						logger.Debug("WaitForExit({0})", _waitForExitTimeout == -1 ? "INFINITE" : _waitForExitTimeout.ToString());
						if (!proc.WaitForExit(_waitForExitTimeout))
						{
							if (!useCpuKill)
							{
								logger.Debug("FAULT, WaitForExit ran out of time!");
								_waitForExitFailed = true;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				logger.Debug("_WaitForExit() failed: {0}", ex.Message);
			}
		}
	}
}
#endif

// end
