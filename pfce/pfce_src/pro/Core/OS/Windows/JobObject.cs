using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using SysProcess = System.Diagnostics.Process;

namespace Peach.Pro.Core.OS.Windows
{
	public class JobObject : IDisposable
	{
		#region P/Invokes

		// ReSharper disable MemberCanBePrivate.Local
		// ReSharper disable FieldCanBeMadeReadOnly.Local
		// ReSharper disable UnusedMember.Local

		const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

		enum JOBOBJECTINFOCLASS
		{
			AssociateCompletionPortInformation = 7,
			BasicLimitInformation = 2,
			BasicUIRestrictions = 4,
			EndOfJobTimeInformation = 6,
			ExtendedLimitInformation = 9,
			SecurityLimitInformation = 5,
			GroupInformation = 11
		}

		[StructLayout(LayoutKind.Sequential)]
		struct JOBOBJECT_BASIC_LIMIT_INFORMATION
		{
			public long PerProcessUserTimeLimit;
			public long PerJobUserTimeLimit;
			public uint LimitFlags;
			public IntPtr MinimumWorkingSetSize;
			public IntPtr MaximumWorkingSetSize;
			public uint ActiveProcessLimit;
			public IntPtr Affinity;
			public uint PriorityClass;
			public uint SchedulingClass;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct IO_COUNTERS
		{
			public ulong ReadOperationCount;
			public ulong WriteOperationCount;
			public ulong OtherOperationCount;
			public ulong ReadTransferCount;
			public ulong WriteTransferCount;
			public ulong OtherTransferCount;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
		{
			public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
			public IO_COUNTERS IoInfo;
			public IntPtr ProcessMemoryLimit;
			public IntPtr JobMemoryLimit;
			public IntPtr PeakProcessMemoryUsed;
			public IntPtr PeakJobMemoryUsed;
		}

		// ReSharper restore UnusedMember.Local
		// ReSharper restore FieldCanBeMadeReadOnly.Local
		// ReSharper restore MemberCanBePrivate.Local

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool SetInformationJobObject(IntPtr hJob, JOBOBJECTINFOCLASS infoType, ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpJobObjectInfo, int cbJobObjectInfoLength);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool CloseHandle(IntPtr hObject);

		#endregion

		private IntPtr _hJob;

		public JobObject()
		{
			_hJob = CreateJobObject(IntPtr.Zero, null);
			if (_hJob == IntPtr.Zero)
				throw new Win32Exception(Marshal.GetLastWin32Error());

			var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
			{
				BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
				{
					LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
				}
			};

			var ret = SetInformationJobObject(_hJob, JOBOBJECTINFOCLASS.ExtendedLimitInformation, ref info, Marshal.SizeOf(info));
			if (!ret)
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		public void Dispose()
		{
			if (_hJob != IntPtr.Zero)
			{
				CloseHandle(_hJob);
				_hJob = IntPtr.Zero;
			}
		}

		public void AssignProcess(SysProcess p)
		{
			// Will return ACCESS_DENIED on Vista/Win7 if PCA gets in the way:
			// http://stackoverflow.com/questions/3342941/kill-child-process-when-parent-process-is-killed
			var ret = AssignProcessToJobObject(_hJob, p.Handle);
			if (!ret)
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}
	}
}
