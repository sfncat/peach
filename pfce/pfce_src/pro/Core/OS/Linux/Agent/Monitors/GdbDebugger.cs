using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Encoding = Peach.Core.Encoding;
using Monitor = Peach.Core.Agent.Monitor2;
using Nustache.Core;

namespace Peach.Pro.OS.Linux.Agent.Monitors
{
	// Notes regarding gdb-server usage
	// http://stackoverflow.com/questions/75255/how-do-you-start-running-the-program-over-again-in-gdb-with-target-remote
	// We might need to associate a command/script to restart the remote gdb-server?

	[Monitor("Gdb")]
	[Alias("LinuxDebugger")]
	[Description("Uses GDB to launch an executable, monitoring it for exceptions")]
	[Parameter("Executable", typeof(string), "Executable to launch")]
	[Parameter("Arguments", typeof(string), "Optional command line arguments", "")]
	[Parameter("FaultOnEarlyExit", typeof(bool), "Trigger fault if process exists", "false")]
	[Parameter("GdbPath", typeof(string), "Path to gdb", "/usr/bin/gdb")]
	[Parameter("NoCpuKill", typeof(bool), "Disable process killing when CPU usage nears zero", "false")]
	[Parameter("RestartAfterFault", typeof(bool), "Restart the process if a different monitor detects a fault", "true")]
	[Parameter("RestartOnEachTest", typeof(bool), "Restart process for each interation", "false")]
	[Parameter("StartOnCall", typeof(string), "Start command on state model call", "")]
	[Parameter("WaitForExitOnCall", typeof(string), "Wait for process to exit on state model call and fault if timeout is reached", "")]
	[Parameter("WaitForExitTimeout", typeof(int), "Wait for exit timeout value in milliseconds (-1 is infinite)", "10000")]
	[Parameter("HandleSignals", typeof(string), "Signals to consider faults. Space separated list of signals/exceptions to handle as faults.", "SIGSEGV SIGFPE SIGABRT SIGILL SIGPIPE SIGBUS SIGSYS SIGXCPU SIGXFSZ EXC_BAD_ACCESS EXC_BAD_INSTRUCTION EXC_ARITHMETIC SIGSTOP")]
	[Parameter("Script", typeof(string), "Script file used to drive GDB and perform crash analysis.", "")]
	public class GdbDebugger : Monitor
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		static readonly protected string template_log_if_crash = @"
define log_if_crash
 if ($_thread != 0x00)
  printf ""Crash detected, running exploitable.\n""
  set logging overwrite on
  set logging redirect on
  set logging on {{gdbLog}}
  exploitable -v
  printf ""\n--- Info Frame ---\n\n""
  info frame
  printf ""\n--- Info Registers ---\n\n""
  info registers
  printf ""\n--- Backtrace ---\n\n""
  thread apply all bt full
  set logging off
 end
end
";

		static readonly string template = @"

handle all nostop noprint
handle {{handleSignals}} stop print

file {{executable}}
set args {{arguments}}
source {{exploitableScript}}

python
def on_start(evt):
    import tempfile, os
    h,tmp = tempfile.mkstemp()
    os.close(h)
    with open(tmp, 'w') as f:
        f.write(str(gdb.inferiors()[0].pid))
    os.renames(tmp, '{{gdbPid}}')
    gdb.events.cont.disconnect(on_start)
gdb.events.cont.connect(on_start)
end

printf ""starting inferior: '{{executable}} {{arguments}}'\n""

run
log_if_crash
quit
";

		protected Process _gdb;
		protected Process _inferior;
		protected MonitorData _fault;
		protected bool _messageExit = false;
		protected bool _secondStart = false;
		protected string _exploitable = null;
		protected TempDirectory _tmpDir = null;
		protected string _gdbCmd = null;
		protected string _gdbPid = null;
		protected string _gdbLog = null;
		protected string _template = null;

		protected Regex reHash = new Regex(@"^Hash: (\w+)\.(\w+)$", RegexOptions.Multiline);
		protected Regex reClassification = new Regex(@"^Exploitability Classification: (.*)$", RegexOptions.Multiline);
		protected Regex reDescription = new Regex(@"^Short description: (.*)$", RegexOptions.Multiline);
		protected Regex reOther = new Regex(@"^Other tags: (.*)$", RegexOptions.Multiline);

		public string GdbPath { get; set; }
		public string Executable { get; set; }
		public string Arguments { get; set; }
		public bool RestartOnEachTest { get; set; }
		public bool RestartAfterFault { get; set; }
		public bool FaultOnEarlyExit { get; set; }
		public bool NoCpuKill { get; set; }
		public string StartOnCall { get;  set; }
		public string WaitForExitOnCall { get; set; }
		public int WaitForExitTimeout { get; set; }
		public string HandleSignals { get; set; }
		public string Script { get; set; }

		public GdbDebugger(string name)
			: base(name)
		{
			_gdb = PlatformFactory<Process>.CreateInstance(logger);
			_inferior = PlatformFactory<Process>.CreateInstance(logger);

			_template = template_log_if_crash + template;
		}

		public override void StartMonitor(Dictionary<string, string> args)
		{
			base.StartMonitor(args);

			if (!string.IsNullOrEmpty(Script))
			{
				if (File.Exists(Script))
					_template = File.ReadAllText(Script);
				else
				{
					throw new SoftException(string.Format("Error, Script file not found for Gdb monitor: {0}", Script));
				}
			}

			_exploitable = FindExploitable();
		}

		protected string FindExploitable()
		{
			var target = Path.Combine("gdb", "exploitable", "exploitable.py");

			var dirs = new List<string> {
				Utilities.ExecutionDirectory,
				Environment.CurrentDirectory,
			};

			var path = Environment.GetEnvironmentVariable("PATH");
			if (!string.IsNullOrEmpty(path))
				dirs.AddRange(path.Split(Path.PathSeparator));

			foreach (var dir in dirs)
			{
				var full = Path.Combine(dir, target);
				if (File.Exists(full))
					return full;
			};

			throw new PeachException("Error, Gdb could not find '" + target + "' in search path.");
		}

		protected virtual void _Start()
		{
			if (File.Exists(_gdbPid))
				File.Delete(_gdbPid);

			if (File.Exists(_gdbLog))
				File.Delete(_gdbLog);

			try
			{
				_gdb.Start(GdbPath, "-batch -n -x {0}".Fmt(_gdbCmd), null, _tmpDir.Path);
			}
			catch (Exception ex)
			{
				throw new PeachException("Could not start debugger '{0}'. {1}.".Fmt(GdbPath, ex.Message), ex);
			}

			// Wait for pid file to exist, open it up and read it
			while (!File.Exists(_gdbPid) && _gdb.IsRunning)
				Thread.Sleep(10);

			if (!File.Exists(_gdbPid) && !_gdb.IsRunning)
				throw new PeachException("GDB was unable to start '{0}'.".Fmt(Executable));

			try
			{
				var pid = Convert.ToInt32(File.ReadAllText(_gdbPid));
				_inferior.Attach(pid);
			}
			catch (ArgumentException)
			{
				// inferior ran to completion
			}

			// Notify event handler the process started
			OnInternalEvent(EventArgs.Empty);
		}

		protected virtual void _Stop()
		{
			_inferior.Shutdown();
			_gdb.WaitForIdle(WaitForExitTimeout);
			_inferior.Dispose();
		}

		protected virtual MonitorData MakeFault(string type, string reason)
		{
			var ret = new MonitorData
			{
				Title = reason,
				Data = new Dictionary<string, Stream>
				{
					{ "stdout.log", new MemoryStream(File.ReadAllBytes(Path.Combine(_tmpDir.Path, "stdout.log"))) },
					{ "stderr.log", new MemoryStream(File.ReadAllBytes(Path.Combine(_tmpDir.Path, "stderr.log"))) }
				},
				Fault = new MonitorData.Info
				{
					Description = "{0} {1} {2}".Fmt(reason, Executable, Arguments),
					MajorHash = Hash(Class + Executable),
					MinorHash = Hash(type),
				}
			};

			return ret;
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			var firstStart = !_secondStart;

			_fault = null;
			_messageExit = false;
			_secondStart = true;

			if ((RestartAfterFault && args.LastWasFault) || RestartOnEachTest || !_gdb.IsRunning)
				_Stop();
			else if (firstStart)
				return;

			if (!_gdb.IsRunning && StartOnCall == null)
				_Start();
		}

		public override bool DetectedFault()
		{
			if (!_messageExit && FaultOnEarlyExit && !_gdb.IsRunning)
			{
				_Stop(); // Stop 1st so stdout/stderr logs are closed
				_fault = MakeFault("ExitedEarly", "Process exited early.");
			}
			else if (StartOnCall != null)
			{
				if (!NoCpuKill)
					_inferior.WaitForIdle(WaitForExitTimeout);
				else
					_inferior.WaitForExit(WaitForExitTimeout);
				_gdb.Stop(WaitForExitTimeout);
			}
			else if (RestartOnEachTest)
			{
				_Stop();
			}

			if (!File.Exists(_gdbLog))
				return _fault != null;

			logger.Info("Caught fault with gdb");

			_Stop();

			byte[] bytes = File.ReadAllBytes(_gdbLog);
			string output = Encoding.UTF8.GetString(bytes);

			_fault = new MonitorData
			{
				Data = new Dictionary<string, Stream>(),
				Fault = new MonitorData.Info()
			};

			var hash = reHash.Match(output);
			if (hash.Success)
			{
				_fault.Fault.MajorHash = hash.Groups[1].Value.Substring(0, 8).ToUpper();
				_fault.Fault.MinorHash = hash.Groups[2].Value.Substring(0, 8).ToUpper();
			}

			var exp = reClassification.Match(output);
			if (exp.Success)
				_fault.Fault.Risk = exp.Groups[1].Value;

			var desc = reDescription.Match(output);
			if (desc.Success)
				_fault.Title = desc.Groups[1].Value;

			var other = reOther.Match(output);
			if (other.Success)
				_fault.Title += ", " + other.Groups[1].Value;

			_fault.Data.Add("StackTrace.txt", new MemoryStream(bytes));
			_fault.Data.Add("stdout.log", new MemoryStream(File.ReadAllBytes(Path.Combine(_tmpDir.Path, "stdout.log"))));
			_fault.Data.Add("stderr.log", new MemoryStream(File.ReadAllBytes(Path.Combine(_tmpDir.Path, "stderr.log"))));
			_fault.Fault.Description = output;

			return true;
		}

		public override MonitorData GetMonitorData()
		{
			return _fault;
		}

		/// <summary>
		/// Populate values that can be used in the mutashe gdb script template.
		/// </summary>
		/// <param name="locals"></param>
		protected virtual void PopulateTemplateParameters(Dictionary<string, object> locals)
		{
			// Monitor Parameters
			locals["executable"] = Executable;
			locals["arguments"] = Arguments;
			locals["gdbPath"] = GdbPath;
			locals["restartOnEachTest"] = RestartOnEachTest;
			locals["restartAfterFault"] = RestartAfterFault;
			locals["faultOnEarlyExit"] = FaultOnEarlyExit;
			locals["noCpuKill"] = NoCpuKill;
			locals["startOnCall"] = StartOnCall;
			locals["waitForExitOnCall"] = WaitForExitOnCall;
			locals["waitForExitTimeout"] = WaitForExitTimeout;
			locals["handleSignals"] = HandleSignals;
		}

		public override void SessionStarting()
		{
			_tmpDir = new TempDirectory();
			_gdbCmd = Path.Combine(_tmpDir.Path, "gdb.cmd");
			_gdbPid = Path.Combine(_tmpDir.Path, "gdb.pid");
			_gdbLog = Path.Combine(_tmpDir.Path, "gdb.log");

			var locals = new Dictionary<string, object>();

			locals["gdbTempDir"] = _tmpDir;
			locals["gdbLog"] = _gdbLog;
			locals["gdbPid"] = _gdbPid;
			locals["gdbCmd"] = _gdbCmd;
			locals["exploitableScript"] = _exploitable;

			PopulateTemplateParameters(locals);

			var cmd = Render.StringToString(_template, locals);
			cmd = cmd.Replace("\r", "");
			File.WriteAllText(_gdbCmd, cmd);

			logger.Debug("Wrote gdb commands to '{0}'", _gdbCmd);
			logger.Trace(cmd);

			if (StartOnCall == null && !RestartOnEachTest)
				_Start();
		}

		public override void SessionFinished()
		{
			_Stop();
			_tmpDir.Dispose();
		}

		public override void IterationFinished()
		{
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

				if (!_gdb.WaitForExit(WaitForExitTimeout))
					_fault = MakeFault("FailedToExit", "Process did not exit in " + WaitForExitTimeout + "ms.");
			}
		}
	}
}
