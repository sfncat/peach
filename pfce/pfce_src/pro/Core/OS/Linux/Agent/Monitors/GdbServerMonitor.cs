using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Monitor = Peach.Core.Agent.Monitor2;

namespace Peach.Pro.OS.Linux.Agent.Monitors
{
	// Notes regarding gdb-server usage
	// http://stackoverflow.com/questions/75255/how-do-you-start-running-the-program-over-again-in-gdb-with-target-remote
	// We might need to associate a command/script to restart the remote gdb-server?

	[Monitor("GdbServer")]
	[Description("Connects to a remote GDB Server instance, monitoring it for exceptions")]
	
	[Parameter("Target", typeof(string), "Target parameters, e.g. 'remote 192.168.1.2:6000'. When possible enable extended-remote 'extended-remote IP:PORT'.")]
	[Parameter("LocalExecutable", typeof(string), "Executable that remote target is running")]
	[Parameter("RemoteExecutable", typeof(string), "Optional, if remote GDB server supports extended remote mode and multi-process capabilities, this is the remote exec file-name", "")]

	[Parameter("GdbPath", typeof(string), "Path to gdb", "/usr/bin/gdb")]
	[Parameter("RestartOnEachTest", typeof(bool), "Restart process for each interation", "false")]
	[Parameter("RestartAfterFault", typeof(bool), "Restart the debugger if a different monitor detects a fault", "true")]
	[Parameter("FaultOnEarlyExit", typeof(bool), "Trigger fault if process exists", "false")]
	[Parameter("NoCpuKill", typeof(bool), "Disable process killing when CPU usage nears zero", "false")]
	[Parameter("StartOnCall", typeof(string), "Start command on state model call", "")]
	[Parameter("WaitForExitOnCall", typeof(string), "Wait for process to exit on state model call and fault if timeout is reached", "")]
	[Parameter("WaitForExitTimeout", typeof(int), "Wait for exit timeout value in milliseconds (-1 is infinite)", "10000")]

	[Parameter("HandleSignals", typeof(string), "Signals to consider faults. Space separated list of signals/exceptions to handle as faults.", "SIGSEGV SIGFPE SIGABRT SIGILL SIGPIPE SIGBUS SIGSYS SIGXCPU SIGXFSZ EXC_BAD_ACCESS EXC_BAD_INSTRUCTION EXC_ARITHMETIC")]
	[Parameter("Script", typeof(string), "Script file used to drive GDB and perform crash analysis.", "")]
	public class GdbServerMonitor : GdbDebugger
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		private static readonly string template = @"

handle all nostop noprint
handle {{handleSignals}} stop print

file {{executable}}
source {{exploitableScript}}

python

import sys

def on_start(evt):
    import tempfile, os
    h,tmp = tempfile.mkstemp()
    os.close(h)
    with open(tmp, 'w') as f:
        f.write(str(gdb.inferiors()[0].pid))
    os.renames(tmp, '{{gdbPid}}')
    gdb.events.cont.disconnect(on_start)

gdb.events.cont.connect(on_start)

print(""starting inferior: '{{target}} {{remoteExecutable}}'"")

try:
  if len('{{remoteExecutable}}') > 1:
    print('starting in extended-remote, multi-process mode')
    gdb.execute('set remote exec-file {{remoteExecutable}}')
    gdb.execute('target {{target}}')
    gdb.execute('run')
  else:
    gdb.execute('target {{target}}')
    gdb.execute('continue')
	
except:
  e = sys.exc_info()[1]
  print('Exception starting target: ' + str(e))
  import tempfile, os
  h,tmp = tempfile.mkstemp()
  os.close(h)
  with open(tmp, 'w') as f:
    f.write(str(e))
  os.renames(tmp, '{{gdbConnectError}}')
  gdb.execute('quit')

end

log_if_crash
quit
";
		protected string _gdbConnectError = null;

		public string Target { get; private set; }
		public string LocalExecutable { get; private set; }
		public string RemoteExecutable { get; private set; }

		public GdbServerMonitor(string name)
			: base(name)
		{
			_gdb = PlatformFactory<Process>.CreateInstance(logger);
			_inferior = PlatformFactory<Process>.CreateInstance(logger);

			_template = template_log_if_crash + template;
		}

		/// <summary>
		/// Populate values that can be used in the mutashe gdb script template.
		/// </summary>
		/// <param name="locals"></param>
		protected override void PopulateTemplateParameters(Dictionary<string, object> locals)
		{
			_gdbConnectError = Path.Combine(_tmpDir.Path, "gdb.connect.error");

			// GdbServer specific
			locals["gdbConnectError"] = _gdbConnectError;
			// Monitor Parameters
			locals["target"] = Target;
			locals["localExecutable"] = LocalExecutable;
			locals["remoteExecutable"] = RemoteExecutable;
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

		protected override void _Start()
		{
			if (File.Exists(_gdbConnectError))
				File.Delete(_gdbConnectError);

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
			while (!File.Exists(_gdbPid) && _gdb.IsRunning && !File.Exists(_gdbConnectError))
				Thread.Sleep(10);

			if (File.Exists(_gdbConnectError))
			{
				var error = File.ReadAllText(_gdbConnectError);
				throw new PeachException("GDB reported an error connecting to target: {0}".Fmt(error));
			}

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
	}
}
