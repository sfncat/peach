

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Mono.Unix;
using Mono.Unix.Native;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Pro.Core.OS;
using Monitor = Peach.Core.Agent.Monitor2;

namespace Peach.Pro.OS.Linux.Agent.Monitors
{
	[Monitor("LinuxCoreFile")]
	[Alias("LinuxCrashMonitor")]
	[Description("Detect when a process crashes and collect its resulting core file")]
	[Parameter("Executable", typeof(string), "Target executable used to filter crashes.", "")]
	[Parameter("LogFolder", typeof(string), "Folder with log files. Defaults to /var/peachcrash", "/var/peachcrash")]
	public class LinuxCoreFile : Monitor
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		private const string Handler = "PeachCrashHandler.sh";
		private const string CorePattern = "|{0}/{1} -p=%p -u=%u -g=%g -s=%s -t=%t -h=%h -e=%e -E=%E";

		private readonly Stack<IDisposable> _cleanup = new Stack<IDisposable>();
		private MonitorData _fault;

		public string LogFolder { get; set; }
		public string Executable { get; set; }

		public LinuxCoreFile(string name)
			: base(name)
		{
		}

		public override void SessionStarting()
		{
			// NOTE: Do all this in SessionStarting() so StopMonitor() will get called

			// The core_pattern has a max length of 128 bytes, so LogFolder must be <60 bytes
			if (LogFolder.Length >= 60)
				throw new PeachException(string.Format("The specified log folder is too long, it must be less than 60 characters: '{0}'", LogFolder));

			// 1) Ensure only one monitor exists...
			var si = Pal.SingleInstance("LinuxCoreFile");
			if (!si.TryLock())
				throw new PeachException("Only a single running instance of the core file monitor is allowed on a host at any time.");
			_cleanup.Push(si);

			// 2) Create log folder
			if (!Directory.Exists(LogFolder))
			{
				try
				{
					Directory.CreateDirectory(LogFolder);
					Logger.Trace("Created directory '{0}'", LogFolder);
				}
				catch (Exception ex)
				{
					throw new PeachException("Failed to create log folder '{0}', {1}.".Fmt(LogFolder, ex.Message), ex);
				}
			}

			// 3) Extract PeachCrashHandler.sh
			var script = Path.Combine(LogFolder, Handler);
			try
			{
				Utilities.ExtractEmbeddedResource(
					Assembly.GetExecutingAssembly(),
					"Peach.Pro.Core.Resources.PeachCrashHandler.sh",
					script);
				Logger.Trace("Extracted core handler '{0}'", script);
			}
			catch (Exception ex)
			{
				throw new PeachException("Failed to save the core handler '{0}', {1}.".Fmt(script, ex.Message), ex);
			}

			// 4) Ensure PeachCrashHandler.sh is chmod 755
			try
			{
				var ret = Syscall.chmod(script, (FilePermissions)Convert.ToInt32("755", 8));
				UnixMarshal.ThrowExceptionForLastErrorIf(ret);
			}
			catch (Exception ex)
			{
				throw new PeachException("Failed to set the core handler '{0}' as executable, {1}.".Fmt(script, ex.Message), ex);
			}

			// 5) Set the core pattern
			_cleanup.Push(new ProcSetter("/proc/sys/kernel/core_pattern", CorePattern.Fmt(LogFolder, Handler)));

			// 6) Enable core files for suid programs
			_cleanup.Push(new ProcSetter("/proc/sys/fs/suid_dumpable", "1"));

			// 7) Set max core file size
			_cleanup.Push(new UlimitUnlimited());

			// 8) Ensure all .info files are gone from the LogsFolder
			foreach (var file in GetInfoFiles())
				TryDelete(file);

			Logger.Debug("Registered core handler '{0}'", script);
		}

		public override void  StopMonitor()
		{
			while (_cleanup.Count > 0)
				_cleanup.Pop().Dispose();
		}

		public override bool DetectedFault()
		{
			_fault = null;

			foreach (var file in GetInfoFiles())
			{
				if (Executable != null && !file.Contains(Executable))
					continue;

				Logger.Debug("Detected Fault '{0}'", file);

				// Format is /path/to/file.PID.info
				var exe = Path.GetFileName(file) ?? ".info";
				exe = exe.Substring(0, exe.Length - 5);
				var idx = exe.LastIndexOf('.');
				if (idx != -1)
					exe = exe.Substring(0, idx);

				_fault = new MonitorData
				{
					Title = "{0} core dumped".Fmt(exe),
					Data = new Dictionary<string, Stream>
					{
						{ Path.GetFileName(file), new MemoryStream(File.ReadAllBytes(file)) }
					},
					Fault = new MonitorData.Info
					{
						Description = File.ReadAllText(file),
						MajorHash = Hash(Class + "." + exe),
						MinorHash = Hash("CORE"),
						Risk = "UNKNOWN"
					}
				};

				var core = file.Substring(0, file.Length - 5) + ".core";
				if (File.Exists(core))
				{
					Logger.Debug("Saving core '{0}'", core);
					_fault.Data.Add(Path.GetFileName(core), new MemoryStream(File.ReadAllBytes(core)));
					TryDelete(core);
				}

				TryDelete(file);

				return true;
			}

			return false;
		}

		public override MonitorData GetMonitorData()
		{
			return _fault;
		}

		private IEnumerable<string> GetInfoFiles()
		{
			return Directory.GetFiles(LogFolder, "*.info", SearchOption.TopDirectoryOnly);
		}

		private static void TryDelete(string fileName)
		{
			try
			{
				File.Delete(fileName);
			}
			catch (Exception ex)
			{
				Logger.Trace("Failed to delete {0}, {1}.", fileName, ex.Message);
			}
		}

		#region Ulimit

		private class UlimitUnlimited : IDisposable
		{
			[StructLayout(LayoutKind.Sequential)]
			private struct rlimit
			{
				public IntPtr rlim_curr;
				public IntPtr rlim_max;
			}

			private const int RLIMIT_CORE = 4;

			[DllImport("libc", SetLastError = true)]
			private static extern int getrlimit(int resource, ref rlimit rlim);

			[DllImport("libc", EntryPoint = "getrlimit", SetLastError = true)]
			private static extern int setrlimit(int resource, ref rlimit rlim);

			private readonly rlimit _initial;
			private readonly bool _reset;

			public UlimitUnlimited()
			{
				_initial = new rlimit();

				if (0 != getrlimit(RLIMIT_CORE, ref _initial))
				{
					var err = Marshal.GetLastWin32Error();
					var ex = new Win32Exception(err);
					throw new PeachException("Error, could not query the core size resource limit.  " + ex.Message, ex);
				}

				var rlim = new rlimit { rlim_curr = _initial.rlim_max, rlim_max = _initial.rlim_max };

				if (0 != setrlimit(RLIMIT_CORE, ref rlim))
				{
					var err = Marshal.GetLastWin32Error();
					var ex = new Win32Exception(err);
					throw new PeachException("Error, could not set the core size resource limit.  " + ex.Message, ex);
				}

				_reset = true;
			}

			public void Dispose()
			{
				if (!_reset)
					return;

				var rlim = new rlimit { rlim_curr = _initial.rlim_curr, rlim_max = _initial.rlim_max };
				if (0 != setrlimit(RLIMIT_CORE, ref rlim))
					Logger.Trace("Failed to restore the rlimit to {0}", _initial.rlim_curr);
			}
		}

		#endregion

		#region Proc Setter

		private class ProcSetter : IDisposable
		{
			private readonly string _path;
			private readonly string _initial;

			public ProcSetter(string path, string value)
			{
				string tmp1;

				try
				{
					// Check the current value
					tmp1 = File.ReadAllText(path);
				}
				catch (UnauthorizedAccessException ex)
				{
					throw new PeachException("Read access denied. {0}".Fmt(ex.Message), ex);
				}
				catch (Exception ex)
				{
					throw new PeachException("Read fail. {0}".Fmt(ex.Message), ex);
				}

				if (tmp1 == value)
				{
					Logger.Trace("{0} is already '{1}', not changing", path, value);
					return;
				}

				try
				{
					// Write the new value
					File.WriteAllText(path, value);
				}
				catch (UnauthorizedAccessException ex)
				{
					throw new PeachException("Write access denied. {0}".Fmt(ex.Message), ex);
				}
				catch (Exception ex)
				{
					throw new PeachException("Write fail. {0}".Fmt(ex.Message), ex);
				}

				string tmp2;

				try
				{
					// Ensure the new value took effect
					tmp2 = File.ReadAllText(path).Trim();
				}
				catch (UnauthorizedAccessException ex)
				{
					throw new PeachException("Verify Access denied. {0}".Fmt(ex.Message), ex);
				}
				catch (Exception ex)
				{
					throw new PeachException("Verify fail. {0}".Fmt(ex.Message), ex);
				}

				if (tmp2 != value)
					throw new PeachException("Set fail. Expected '{0}' but is actually '{1}'.".Fmt(value, tmp2));

				_initial = tmp1;
				_path = path;

				Logger.Trace("Changed {0} to '{1}'", path, value);
			}

			public void Dispose()
			{
				if (_path != null)
				{
					try
					{
						File.WriteAllText(_path, _initial);
						Logger.Trace("Restored {0} to '{1}'", _path, _initial);
					}
					catch (Exception ex)
					{
						Logger.Trace("Failed to restored {0} to '{1}'. {2}.", _path, _initial, ex.Message);
					}
				}
			}
		}

		#endregion
	}
}
