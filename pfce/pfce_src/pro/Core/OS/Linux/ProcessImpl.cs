using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using Peach.Core;
using Logger = NLog.Logger;
using SysProcess = System.Diagnostics.Process;

namespace Peach.Pro.Core.OS.Linux
{
	[PlatformImpl(Platform.OS.Linux)]
	public class ProcessImpl : Unix.ProcessImpl
	{
		class OwnedProcess : BaseUnixProcess
		{
			protected override bool Attached { get { return false; } }

			public OwnedProcess(SysProcess process)
				: base(process)
			{
			}

			public override ProcessInfo Snapshot()
			{
				return TakeSnapshot(_process);
			}
		}

		class AttachedProcess : OwnedProcess
		{
			protected override bool Attached { get { return true; } }

			public AttachedProcess(SysProcess process)
				: base(process)
			{
			}

			public override bool WaitForExit(int timeout)
			{
				return PollForExit(timeout);
			}
		}

		[PlatformImpl(Platform.OS.Linux)]
		public class ProcessHelper : BaseProcessHelper
		{
			protected override Unix.ProcessImpl NewProcess(Logger logger)
			{
				return new ProcessImpl(logger);
			}

			protected override IEnumerable<SysProcess> GetProcessesByName(string name)
			{
				foreach (var p in SysProcess.GetProcesses())
				{
					string procName;

					try
					{
						procName = Path.GetFileName(p.ProcessName);
					}
					catch (InvalidOperationException)
					{
						procName = ReadProc(p.Id, false).Item1;
					}

					if (name == procName)
						yield return p;
					else
						p.Dispose();
				}
			}
		}

		public ProcessImpl(Logger logger)
			: base(logger)
		{
		}

		protected override IProcess MakeOwnedProcess(SysProcess process)
		{
			return new OwnedProcess(process);
		}

		protected override IProcess MakeAttachedProcess(SysProcess process)
		{
			return new AttachedProcess(process);
		}

		static ProcessInfo TakeSnapshot(SysProcess p)
		{
			var tuple = ReadProc(p.Id, true);
			if (tuple.Item1 == null)
				throw new ArgumentException();

			var parts = tuple.Item2;

			var pi = new ProcessInfo
			{
				Id = p.Id
			};

			try
			{
				pi.ProcessName = p.ProcessName;
			}
			catch (InvalidOperationException)
			{
				pi.ProcessName = tuple.Item1;
			}

			pi.Responding = parts[(int)Fields.State] != "Z";

			pi.UserProcessorTicks = ulong.Parse(parts[(int)Fields.UserTime]);
			pi.PrivilegedProcessorTicks = ulong.Parse(parts[(int)Fields.KernelTime]);
			pi.TotalProcessorTicks = pi.UserProcessorTicks + pi.PrivilegedProcessorTicks;

			pi.PrivateMemorySize64 = p.PrivateMemorySize64;         // /proc/[pid]/status VmData
			pi.VirtualMemorySize64 = p.VirtualMemorySize64;         // /proc/[pid]/status VmSize
			pi.PeakVirtualMemorySize64 = p.PeakVirtualMemorySize64; // /proc/[pid]/status VmPeak
			pi.WorkingSet64 = p.WorkingSet64;                       // /proc/[pid]/status VmRSS
			pi.PeakWorkingSet64 = p.PeakWorkingSet64;               // /proc/[pid]/status VmHWM

			return pi;
		}

		#region Proc Helpers

		const string StatPath = "/proc/{0}/stat";

		// ReSharper disable UnusedMember.Local
		enum Fields
		{
			State = 0,
			UserTime = 11,
			KernelTime = 12,
			Max = 13,
		}
		// ReSharper restore UnusedMember.Local

		static Tuple<string, string[]> ReadProc(int pid, bool stats)
		{
			var path = string.Format(StatPath, pid);
			string stat;

			try
			{
				stat = File.ReadAllText(path);
			}
			catch
			{
				return new Tuple<string, string[]>(null, null);
			}

			var start = stat.IndexOf('(');
			var end = stat.LastIndexOf(')');

			if (stat.Length < 2 || start < 0 || end < start)
				return new Tuple<string, string[]>(null, null);

			var before = stat.Substring(0, start);
			var middle = stat.Substring(start + 1, end - start - 1);
			var after = stat.Substring(end + 1);

			var strPid = before.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (strPid.Length != 1 || strPid[0] != pid.ToString(CultureInfo.InvariantCulture))
				return new Tuple<string, string[]>(null, null);

			if (string.IsNullOrEmpty(middle))
				return new Tuple<string, string[]>(null, null);

			if (!stats)
				return new Tuple<string, string[]>(middle, null);

			var parts = after.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < (int)Fields.Max)
				return null;

			return new Tuple<string, string[]>(middle, parts);
		}

		#endregion
	}
}
