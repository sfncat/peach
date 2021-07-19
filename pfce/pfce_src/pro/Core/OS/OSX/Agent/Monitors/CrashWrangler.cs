

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using Debug = System.Diagnostics.Debug;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using SysProcess = System.Diagnostics.Process;
using Peach.Core;
using Peach.Core.Agent;
using Monitor = Peach.Core.Agent.Monitor2;
using NLog;

namespace Peach.Pro.OS.OSX.Agent.Monitors
{
	/// <summary>
	/// Monitor will use OS X's built in CrashReporter (similar to watson)
	/// to detect and report crashes.
	/// </summary>
	[Monitor("CrashWrangler")]
	[Alias("osx.CrashWrangler")]
	[Description("Launch a process and monitor it for crashes")]
	[Parameter("Executable", typeof(string), "Executable to launch")]
	[Parameter("Arguments", typeof(string), "Optional command line arguments", "")]
	[Parameter("StartOnCall", typeof(string), "Start command on state model call", "")]
	[Parameter("UseDebugMalloc", typeof(bool), "Use OS X Debug Malloc (slower) (defaults to false)", "false")]
	[Parameter("ExecHandler", typeof(string), "Crash Wrangler Execution Handler program.", "exc_handler")]
	[Parameter("ExploitableReads", typeof(bool), "Are read a/v's considered exploitable? (defaults to false)", "false")]
	[Parameter("NoCpuKill", typeof(bool), "Disable process killing by CPU usage? (defaults to false)", "false")]
	[Parameter("CwLogFile", typeof(string), "CrashWrangler Log file (defaults to cw.log)", "cw.log")]
	[Parameter("CwLockFile", typeof(string), "CrashWRangler Lock file (defaults to cw.lock)", "cw.lock")]
	[Parameter("CwPidFile", typeof(string), "CrashWrangler PID file (defaults to cw.pid)", "cw.pid")]
	[Parameter("FaultOnEarlyExit", typeof(bool), "Trigger fault if process exists (defaults to false)", "false")]
	[Parameter("WaitForExitOnCall", typeof(string), "Wait for process to exit on state model call and fault if timeout is reached", "")]
	[Parameter("WaitForExitTimeout", typeof(int), "Wait for exit timeout value in milliseconds (-1 is infinite)", "10000")]
	[Parameter("RestartOnEachTest", typeof(bool), "Restart process for each interation", "false")]
	[Parameter("RestartAfterFault", typeof(bool), "Restart the process if a different monitor detects a fault", "true")]
	public class CrashWrangler : Monitor
	{
		static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public string Executable { get; set; }
		public string Arguments { get; set; }
		public string StartOnCall { get; set; }
		public bool UseDebugMalloc { get; set; }
		public string ExecHandler { get; set; }
		public bool ExploitableReads { get; set; }
		public bool NoCpuKill { get; set; }
		public string CwLogFile { get; set; }
		public string CwLockFile { get; set; }
		public string CwPidFile { get; set; }
		public bool FaultOnEarlyExit { get; set; }
		public string WaitForExitOnCall { get; set; }
		public int WaitForExitTimeout { get; set; }
		public bool RestartOnEachTest { get; set; }
		public bool RestartAfterFault { get; set; }

		private Process _wrangler;
		private Process _inferior;
		private bool? _detectedFault;  // Was a fault detected
		private bool _faultExitFail;   // Failed to exit within WaitForExitTimeout
		private bool _faultExitEarly;  // Process exited early
		private bool _messageExit;     // Process exited due to WaitForExitOnCall

		public CrashWrangler(string name) : base(name)
		{
			_wrangler = PlatformFactory<Process>.CreateInstance(Logger);
			_inferior = PlatformFactory<Process>.CreateInstance(Logger);
		}

		public override void StartMonitor(Dictionary<string, string> args)
		{
			string val;
			if (args.TryGetValue("Command", out val) && !args.ContainsKey("Executable"))
			{
				Logger.Info("The parameter 'Command' on the monitor 'CrashWrangler' is deprecated.  Use the parameter 'Executable' instead.");
				args["Executable"] = val;
				args.Remove("Command");
			}

			base.StartMonitor(args);
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			_detectedFault = null;
			_faultExitFail = false;
			_faultExitEarly = false;
			_messageExit = false;

			if (RestartAfterFault && args.LastWasFault)
				_Stop();

			if (!_wrangler.IsRunning && StartOnCall == null)
				_Start();
		}

		public override bool DetectedFault()
		{
			if (_detectedFault == null)
			{
				// Give CrashWrangler a chance to write the log
				Thread.Sleep(500);
				_detectedFault = File.Exists(CwLogFile);

				if (!_detectedFault.Value)
				{
					if (FaultOnEarlyExit && _faultExitEarly)
						_detectedFault = true;
					else if (_faultExitFail)
						_detectedFault = true;
				}
			}

			return _detectedFault.Value;
		}

		public override MonitorData GetMonitorData()
		{
			if (!DetectedFault())
				return null;

			var fault = new MonitorData {
				Data = new Dictionary<string, Stream>(),
				Fault = new MonitorData.Info(),
			};

			if (File.Exists(CwLogFile))
			{
				fault.Fault.Description = File.ReadAllText(CwLogFile);

				var s = new Summary(fault.Fault.Description);

				fault.Title = s.Title;
				fault.Fault.MajorHash = s.MajorHash;
				fault.Fault.MinorHash = s.MinorHash;
				fault.Fault.Risk = s.Exploitable;
			}
			else if (!_faultExitFail)
			{
				fault.Title = "Process exited early.";
				fault.Fault.Description = "{0} {1} {2}".Fmt(fault.Title, Executable, Arguments);
				fault.Fault.MajorHash = Hash(Class + Executable);
				fault.Fault.MinorHash = Hash("ExitedEarly");
			}
			else
			{
				fault.Title = "Process did not exit in " + WaitForExitTimeout + "ms.";
				fault.Fault.Description = "{0} {1} {2}".Fmt(fault.Title, Executable, Arguments);
				fault.Fault.MajorHash = Hash(Class + Executable);
				fault.Fault.MinorHash = Hash("FailedToExit");
			}
			return fault;
		}

		public override void SessionStarting()
		{
			ExecHandler = Utilities.FindProgram(
				Path.GetDirectoryName(ExecHandler),
				Path.GetFileName(ExecHandler),
				"ExecHandler"
			);

			if (StartOnCall == null)
				_Start();
		}

		public override void SessionFinished()
		{
			_Stop();
		}

		public override void IterationFinished()
		{
			if (!_messageExit && FaultOnEarlyExit && !_wrangler.IsRunning)
			{
				_faultExitEarly = true;
				_Stop();
			}
			else if (StartOnCall != null)
			{
				if (!NoCpuKill)
					_inferior.WaitForIdle(WaitForExitTimeout);
				else
					_inferior.WaitForExit(WaitForExitTimeout);
				_wrangler.Stop(WaitForExitTimeout);
			}
			else if (RestartOnEachTest)
			{
				_Stop();
			}
		}

		public override void Message(string msg)
		{
			if (msg == StartOnCall)
			{
				_Stop();
				_Start();
			}
			else if (msg == WaitForExitOnCall)
			{
				_messageExit = true;
				if (!_wrangler.WaitForExit(WaitForExitTimeout))
				{
					_detectedFault = true;
					_faultExitFail = true;
				}
			}
		}

		private bool _CommandExists()
		{
			using (var p = new SysProcess())
			{
				p.StartInfo = new ProcessStartInfo("which", "-s \"" + Executable + "\"");
				p.Start();
				p.WaitForExit();
				return p.ExitCode == 0;
			}
		}

		private void _Start()
		{
			if (!_CommandExists())
				throw new PeachException("CrashWrangler: Could not find command \"" + Executable + "\"");

			if (File.Exists(CwLogFile))
				File.Delete(CwLogFile);

			if (File.Exists(CwLockFile))
				File.Delete(CwLockFile);

			if (File.Exists(CwPidFile))
				File.Delete(CwPidFile);

			var env = new Dictionary<string, string>();

			foreach (DictionaryEntry item in Environment.GetEnvironmentVariables())
				env[item.Key.ToString()] = item.Value.ToString();

			env["CW_NO_CRASH_REPORTER"] = "1";
			env["CW_QUIET"] = "1";
			env["CW_LOG_PATH"] = CwLogFile;
			env["CW_PID_FILE"] = CwPidFile;
			env["CW_LOCK_FILE"] = CwLockFile;

			if (UseDebugMalloc)
				env["CW_USE_GMAL"] = "1";

			if (ExploitableReads)
				env["CW_EXPLOITABLE_READS"] = "1";

			var args = "\"" + Executable + "\"" + (string.IsNullOrEmpty(Arguments) ? "" : " ") + Arguments;
			_wrangler.Start(ExecHandler, args, env, null);

			// Wait for pid file to exist, open it up and read it
			while (!File.Exists(CwPidFile) && _wrangler.IsRunning)
				Thread.Sleep(10);

			if (!File.Exists(CwPidFile) && !_wrangler.IsRunning)
				throw new PeachException("CrashWrangler was unable to start '{0}'.".Fmt(Executable));

			try
			{
				var pid = Convert.ToInt32(File.ReadAllText(CwPidFile));
				_inferior.Attach(pid);
			}
			catch (ArgumentException)
			{
				// inferior ran to completion
			}

			OnInternalEvent(EventArgs.Empty);
		}

		private void _Stop()
		{
			if (!_wrangler.IsRunning)
				return;
			
			_inferior.Shutdown();

			// Ensure a crash report is not being generated
			while (File.Exists(CwLockFile))
				Thread.Sleep(250);

			_wrangler.WaitForIdle(WaitForExitTimeout);
			_inferior.Dispose();
		}

		internal class Summary
		{
			public string MajorHash { get; private set; }

			public string MinorHash { get; private set; }

			public string Title { get; private set; }

			public string Exploitable { get; private set; }

			private static readonly string[] SystemModules = {
				"libSystem.B.dylib",
				"libsystem_kernel.dylib",
				"libsystem_c.dylib",
				"com.apple.CoreFoundation",
				"libstdc++.6.dylib",
				"libobjc.A.dylib",
				"libgcc_s.1.dylib",
				"libgmalloc.dylib",
				"libc++abi.dylib",
				"modified_gmalloc.dylib", // Apple internal dylib
				"???"                     // For when it doesn't exist in a known module
			};

			private static readonly string[] OffsetFunctions = {
				"__memcpy",
				"__longcopy",
				"__memmove",
				"__bcopy",
				"__memset_pattern",
				"__bzero",
				"memcpy",
				"longcopy",
				"memmove",
				"bcopy",
				"bzero",
				"memset_pattern" 
			};

			private const int MajorDepth = 5;

			public Summary(string log)
			{
				var isExploitable = string.Empty;
				var accessType = string.Empty;
				var exception = string.Empty;

				Exploitable = "UNKNOWN";

				var reProp = new Regex(@"^(((?<key>\w+)=(?<value>[^:]+):)+)", RegexOptions.Multiline);
				var mProp = reProp.Match(log);
				if (mProp.Success)
				{
					var ti = Thread.CurrentThread.CurrentCulture.TextInfo;
					var keys = mProp.Groups["key"].Captures;
					var vals = mProp.Groups["value"].Captures;

					Debug.Assert(keys.Count == vals.Count);

					for (var i = 0; i < keys.Count; ++i)
					{
						var key = keys[i].Value;
						var val = vals[i].Value;

						switch (key)
						{
							case "is_exploitable":
								isExploitable = val.ToLower();
								break;
							case "exception":
								exception = string.Join("", val.ToLower().Split('_').Where(a => a != "exc").Select(ti.ToTitleCase).ToArray());
								break;
							case "access_type":
								accessType = ti.ToTitleCase(val.ToLower());
								break;
						}
					}
				}

				Title = string.Format("{0}{1}", accessType, exception);

				if (string.IsNullOrEmpty(isExploitable))
					Exploitable = "UNKNOWN";
				else if (isExploitable == "yes")
					Exploitable = "EXPLOITABLE";
				else
					Exploitable = "NOT_EXPLOITABLE";

				var reTid = new Regex(@"^Crashed Thread:\s+(\d+)", RegexOptions.Multiline);
				var mTid = reTid.Match(log);
				if (!mTid.Success)
					return;

				var tid = mTid.Groups[1].Value;
				var strReAddr = @"^Thread " + tid + @" Crashed:.*\n((\d+\s+(?<file>\S*)\s+(?<addr>0x[0-9,a-f,A-F]+)\s(?<func>.+)\n)+)";
				var reAddr = new Regex(strReAddr, RegexOptions.Multiline);
				var mAddr = reAddr.Match(log);
				if (!mAddr.Success)
					return;

				var files = mAddr.Groups["file"].Captures;
				var addrs = mAddr.Groups["addr"].Captures;
				var names = mAddr.Groups["func"].Captures;

				var maj = "";
				var min = "";
				var cnt = 0;

				for (var i = 0; i < files.Count; ++i)
				{
					var file = files[i].Value;
					var addr = addrs[i].Value;
					var name = names[i].Value;

					// Ignore certian system modules
					if (SystemModules.Contains(file))
						continue;

					// When generating a signature, remove offsets for common functions
					var other = OffsetFunctions.FirstOrDefault(name.StartsWith);
					if (other != null)
						addr = other;

					var sig = (cnt == 0 ? "" : ",") + addr;
					min += sig;

					if (++cnt <= MajorDepth)
						maj += sig;
				}

				// If we have no usable backtrace info, hash on the reProp line
				if (cnt == 0)
				{
					maj = mProp.Value;
					min = mProp.Value;
				}

				MajorHash = Hash(maj);
				MinorHash = Hash(min);
			}
		}
	}
}
