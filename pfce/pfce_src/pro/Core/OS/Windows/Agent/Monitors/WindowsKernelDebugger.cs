using System;
using System.Collections.Generic;
using System.IO;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Pro.Core.OS.Windows.Debugger;
using System.ComponentModel;
using SysProcess = System.Diagnostics.Process;

namespace Peach.Pro.OS.Windows.Agent.Monitors
{
	[Monitor("WindowsKernelDebugger")]
	[Description("Debugger monitor for the windows kernel")]
	[Parameter("KernelConnectionString", typeof(string), "Connection string for kernel debugging.")]
	[Parameter("SymbolsPath", typeof(string), "Optional Symbol path.  Default is Microsoft public symbols server.", "SRV*http://msdl.microsoft.com/download/symbols")]
	[Parameter("WinDbgPath", typeof(string), "Path to WinDbg install.  If not provided we will try and locate it.", "")]
	[Parameter("ConnectTimeout", typeof(uint), "How long to wait for kernel connection.", "3000")]
	public class WindowsKernelDebugger : Monitor2
	{
		public string KernelConnectionString { get; set; }
		public string SymbolsPath { get; set; }
		public string WinDbgPath { get; set; }
		public uint ConnectTimeout { get; set; }

		IKernelDebugger _debugger;

		public WindowsKernelDebugger(string name)
			: base(name)
		{
		}

		public override void StartMonitor(Dictionary<string, string> args)
		{
			base.StartMonitor(args);

			WinDbgPath = FindWinDbg(WinDbgPath);

			LaunchDebugger();
		}

		public override void SessionStarting()
		{
			_debugger.WaitForConnection(ConnectTimeout);
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			if (args.LastWasFault)
				_debugger.WaitForConnection(ConnectTimeout);
		}

		public override void StopMonitor()
		{
			if (_debugger != null)
			{
				_debugger.Dispose();
				_debugger = null;
			}
		}

		public override bool DetectedFault()
		{
			return _debugger.Fault != null;
		}

		public override MonitorData GetMonitorData()
		{
			var ret = _debugger.Fault;

			// Restart the debugger
			LaunchDebugger();

			return ret;
		}

		private void LaunchDebugger()
		{
			if (_debugger != null)
				_debugger.Dispose();

			_debugger = new KernelDebugger
			{
				SymbolsPath = SymbolsPath,
				WinDbgPath = WinDbgPath,
			};

			_debugger.AcceptKernel(KernelConnectionString);
		}

		public static string FindWinDbg(string winDbgPath)
		{
			if (!string.IsNullOrEmpty(winDbgPath))
			{
				var file = Path.Combine(winDbgPath, "dbgeng.dll");

				if (!File.Exists(file))
					throw new PeachException("Error, provided WinDbgPath '{0}' does not exist.".Fmt(winDbgPath));

				var type = FileArch.GetWindows(file);

				if (Environment.Is64BitProcess && type != Platform.Architecture.x64)
					throw new PeachException("Error, provided WinDbgPath '{0}' is not x64.".Fmt(winDbgPath));

				if (!Environment.Is64BitProcess && type != Platform.Architecture.x86)
					throw new PeachException("Error, provided WinDbgPath '{0}' is not x86.".Fmt(winDbgPath));

				return winDbgPath;
			}

			// Lets try a few common places before failing.
			var pgPaths = new List<string>
			{
				Environment.GetEnvironmentVariable("SystemDrive"),
				Environment.GetEnvironmentVariable("ProgramFiles"),
				Environment.GetEnvironmentVariable("ProgramW6432"),
				Environment.GetEnvironmentVariable("ProgramFiles"),
				Environment.GetEnvironmentVariable("ProgramFiles(x86)")
			};


			var dbgPaths = new List<string>
			{
				"Debuggers",
				"Debugger",
				"Debugging Tools for Windows",
				"Debugging Tools for Windows (x64)",
				"Debugging Tools for Windows (x86)",
				"Windows Kits\\8.0\\Debuggers\\x64",
				"Windows Kits\\8.0\\Debuggers\\x86",
				"Windows Kits\\8.1\\Debuggers\\x64",
				"Windows Kits\\8.1\\Debuggers\\x86",
				"Windows Kits\\10\\Debuggers\\x64",
				"Windows Kits\\10\\Debuggers\\x86"
			};

			foreach (var pg in pgPaths)
			{
				foreach (var dbg in dbgPaths)
				{
					var path = Path.Combine(pg, dbg);

					if (!Directory.Exists(path))
						continue;

					var file = Path.Combine(path, "dbgeng.dll");

					if (!File.Exists(file))
						continue;

					//verify x64 vs x86
					var type = FileArch.GetWindows(file);

					if (Environment.Is64BitProcess && type != Platform.Architecture.x64)
						continue;

					if (!Environment.Is64BitProcess && type != Platform.Architecture.x86)
						continue;

					return path;
				}
			}

			throw new PeachException("Error, unable to locate WinDbg, please specify using 'WinDbgPath' parameter.");
		}
	}
}