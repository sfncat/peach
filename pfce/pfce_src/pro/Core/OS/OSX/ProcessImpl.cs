using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mono.Unix.Native;
using Peach.Core;
using Logger = NLog.Logger;
using SysProcess = System.Diagnostics.Process;

namespace Peach.Pro.Core.OS.OSX
{
	[PlatformImpl(Platform.OS.OSX)]
	public class ProcessImpl : Unix.ProcessImpl
	{
		private static readonly Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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

		[PlatformImpl(Platform.OS.OSX)]
		public class ProcessHelper : BaseProcessHelper
		{
			protected override Unix.ProcessImpl NewProcess(Logger logger)
			{
				return new ProcessImpl(logger);
			}

			protected override IEnumerable<SysProcess> GetProcessesByName(string name)
			{
				Logger.Trace("GetProcessByName: {0}", name);

				foreach (var p in SysProcess.GetProcesses())
				{
					if (GetName(p.Id) == name)
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
			extern_proc kp;
			if (!GetKernProc(p.Id, out kp))
				RaiseError(p);

			proc_taskinfo ti;
			if (!GetTaskInfo(p.Id, out ti))
				RaiseError(p);

			var pi = new ProcessInfo
			{
				Id = p.Id,
				ProcessName = GetName(p.Id),
				Responding = kp.p_stat != (byte)pstat.SZOMB,
				UserProcessorTicks = ti.pti_total_user,
				PrivilegedProcessorTicks = ti.pti_total_system,

				VirtualMemorySize64 = (long)ti.pti_virtual_size,
				WorkingSet64 = (long)ti.pti_resident_size,
				PrivateMemorySize64 = 0,
				PeakVirtualMemorySize64 = 0,
				PeakWorkingSet64 = 0,
			};

			pi.TotalProcessorTicks = pi.UserProcessorTicks + pi.PrivilegedProcessorTicks;

			return pi;
		}

		#region P/Invoke Stuff

		// ReSharper disable UnusedMember.Local
		// ReSharper disable MemberCanBePrivate.Local
		// ReSharper disable FieldCanBeMadeReadOnly.Local

		// <libproc.h>
		[DllImport("libc")]
		private static extern int proc_pidinfo(int pid, int flavor, ulong arg, IntPtr buffer, int buffersize);

		// <sys/proc_info.h>
		[StructLayout(LayoutKind.Sequential)]
		struct proc_taskinfo
		{
			public ulong pti_virtual_size;       /* virtual memory size (bytes) */
			public ulong pti_resident_size;      /* resident memory size (bytes) */
			public ulong pti_total_user;         /* total time */
			public ulong pti_total_system;
			public ulong pti_threads_user;       /* existing threads only */
			public ulong pti_threads_system;
			public int pti_policy;               /* default policy for new threads */
			public int pti_faults;               /* number of page faults */
			public int pti_pageins;              /* number of actual pageins */
			public int pti_cow_faults;           /* number of copy-on-write faults */
			public int pti_messages_sent;        /* number of messages sent */
			public int pti_messages_received;    /* number of messages received */
			public int pti_syscalls_mach;        /* number of mach system calls */
			public int pti_syscalls_unix;        /* number of unix system calls */
			public int pti_csw;                  /* number of context switches */
			public int pti_threadnum;            /* number of threads in the task */
			public int pti_numrunning;           /* number of running threads */
			public int pti_priority;             /* task priority*/
		}

		// <sys/proc_info.h>
		private static int PROC_PIDTASKINFO { get { return 4; } }

		// sizeof(struct kinfo_proc)
		private static int kinfo_proc_size { get { return 648; } }

		// <sys/proc.h>
		// Only contains the interesting parts at the beginning of the struct.
		// However, we allocate kinfo_proc_size when calling the sysctl.
		[StructLayout(LayoutKind.Sequential)]
		struct extern_proc
		{
			public int p_starttime_tv_sec;
			public int p_starttime_tv_usec;
			public IntPtr p_vmspace;
			public IntPtr p_sigacts;
			public int p_flag;
			public byte p_stat;
			public int p_pid;
			public int p_oppid;
			public int p_dupfd;
			public IntPtr user_stack;
			public IntPtr exit_thread;
			public int p_debugger;
			public int sigwait;
			public uint p_estcpu;
			public int p_cpticks;
			public uint p_pctcpu;
			public IntPtr p_wchan;
			public IntPtr p_wmesg;
			public uint p_swtime;
			public uint p_slptime;
			public uint p_realtimer_it_interval_tv_sec;
			public uint p_realtimer_it_interval_tv_usec;
			public uint p_realtimer_it_value_tv_sec;
			public uint p_realtimer_it_value_tv_usec;
			public uint p_rtime_tv_sec;
			public uint p_rtime_tv_usec;
			public ulong p_uticks;
			public ulong p_sticks;
			public ulong p_iticks;
		}

		// <sys/sysctl.h>
		private const int CTL_KERN = 1;
		private const int KERN_PROC = 14;
		private const int KERN_PROC_PID = 1;
		private const int KERN_ARGMAX = 8;
		private const int KERN_PROCARGS2 = 49;

		// <sys/proc.h>
		private enum pstat : byte
		{
			SIDL = 1, // Process being created by fork.
			SRUN = 2, // Currently runnable.
			SSLEEP = 3, // Sleeping on an address.
			SSTOP = 4, // Process debugging or suspension.
			SZOMB = 5, // Awiting collection by parent.
		}

		[DllImport("libc")]
		private static extern int sysctl([MarshalAs(UnmanagedType.LPArray)] int[] name, uint namelen, IntPtr oldp, ref int oldlenp, IntPtr newp, int newlen);

		// ReSharper restore FieldCanBeMadeReadOnly.Local
		// ReSharper restore MemberCanBePrivate.Local
		// ReSharper restore UnusedMember.Local

		#endregion

		#region HGlobal Helper

		class HGlobal : IDisposable
		{
			public IntPtr Pointer { get; private set; }

			public HGlobal(int size)
			{
				Pointer = Marshal.AllocHGlobal(size);
			}

			public void Dispose()
			{
				Marshal.FreeHGlobal(Pointer);
			}

			public T ToStruct<T>()
			{
				return (T)Marshal.PtrToStructure(Pointer, typeof(T));
			}

			public int ReadInt32()
			{
				return Marshal.ReadInt32(Pointer);
			}

			public string ReadString(int offset)
			{
				return Marshal.PtrToStringAnsi(Pointer + offset);
			}
		}

		#endregion

		#region Snapshot Helpers

		static bool GetKernProc(int pid, out extern_proc val)
		{
			var mib = new[] {
				CTL_KERN,
				KERN_PROC,
				KERN_PROC_PID,
				pid
			};

			var len = kinfo_proc_size;
			using (var ptr = new HGlobal(len))
			{
				var ret = sysctl(mib, (uint)mib.Length, ptr.Pointer, ref len, IntPtr.Zero, 0);
				if (ret == -1)
				{
					val = new extern_proc();
					return false;
				}

				val = ptr.ToStruct<extern_proc>();
				return true;
			}
		}

		static bool GetTaskInfo(int pid, out proc_taskinfo val)
		{
			var len = Marshal.SizeOf(typeof(proc_taskinfo));
			using (var ptr = new HGlobal(len))
			{
				var err = proc_pidinfo(pid, PROC_PIDTASKINFO, 0, ptr.Pointer, len);
				if (err != len)
				{
					val = new proc_taskinfo();
					return false;
				}

				val = ptr.ToStruct<proc_taskinfo>();
				return true;
			}
		}

		static int _argmax;

		static int GetArgMax()
		{
			if (_argmax == 0)
			{
				var mib = new[]
				{
					CTL_KERN,
					KERN_ARGMAX
				};

				var len = sizeof(int);
				using (var ptr = new HGlobal(len))
				{
					var ret = sysctl(mib, (uint)mib.Length, ptr.Pointer, ref len, IntPtr.Zero, 0);
					if (ret == -1)
						throw new PeachException("ProcessInfoImpl: Could not get KERN_ARGMAX");
					_argmax = ptr.ReadInt32();
				}
			}
			return _argmax;
		}

		// Reference:
		// http://opensource.apple.com/source/adv_cmds/adv_cmds-153/ps/print.c
		//
		static string GetName(int pid)
		{
			var mib = new[]
			{
				CTL_KERN,
				KERN_PROCARGS2,
				pid
			};

			var argmax = GetArgMax();
			using (var ptr = new HGlobal(argmax))
			{
				var ret = sysctl(mib, (uint)mib.Length, ptr.Pointer, ref argmax, IntPtr.Zero, 0);
				if (ret == -1)
				{
					var errno = Stdlib.GetLastError();
					Logger.Trace("Failed to get process info for pid {0}. {1}", pid, Stdlib.strerror(errno));
					return ""; // ignore errors, usually access denied
				}

				// skip past argc which is an int
				var path = ptr.ReadString(sizeof(int));
				Logger.Trace("Pid: {0} -> {1}", pid, path);

				return Path.GetFileName(path);
			}
		}

		static void RaiseError(SysProcess p)
		{
			bool hasExited;

			try
			{
				hasExited = p.HasExited;
			}
			catch (Exception ex)
			{
				throw new ArgumentException("Failed to check running status of pid '{0}'.".Fmt(p.Id), ex);
			}

			if (hasExited)
				throw new ArgumentException("Can't query info for pid '{0}', it has already exited.".Fmt(p.Id));

			throw new UnauthorizedAccessException("Can't query info for pid '{0}', ensure it is running and the user has appropriate permissions".Fmt(p.Id));
		}

		#endregion
	}
}
