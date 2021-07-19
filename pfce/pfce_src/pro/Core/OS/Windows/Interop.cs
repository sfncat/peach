using System;
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace Peach.Pro.Core.OS.Windows
{
	/// <summary>
	/// Contains definitions for marshaled method calls and related
	/// types.
	/// </summary>
	internal static class Interop
	{
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct STARTUPINFO
		{
			public int cb;
			public string lpReserved;
			public string lpDesktop;
			public string lpTitle;
			public int dwX;
			public int dwY;
			public int dwXSize;
			public int dwYSize;
			public int dwXCountChars;
			public int dwYCountChars;
			public int dwFillAttribute;
			public int dwFlags;
			public short wShowWindow;
			public short cbReserved2;
			public IntPtr lpReserved2;
			public IntPtr hStdInput;
			public IntPtr hStdOutput;
			public IntPtr hStdError;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct PROCESS_INFORMATION
		{
			public IntPtr hProcess;
			public IntPtr hThread;
			public int dwProcessId;
			public int dwThreadId;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SECURITY_ATTRIBUTES
		{
			public int nLength;
			public IntPtr lpSecurityDescriptor;
			public int bInheritHandle;
		}

		public enum SECURITY_IMPERSONATION_LEVEL
		{
			SecurityAnonymous = 0,
			SecurityIdentification = 1,
			SecurityImpersonation = 2,
			SecurityDelegation = 3
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct LUID
		{
			public uint LowPart;
			public int HighPart;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct TOKEN_PRIVILEGES
		{
			public uint PrivilegeCount;
			public LUID Luid;
			public uint Attributes;
		}

		// OpenThreadToken DesiredAccess
		public const uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
		public const uint STANDARD_RIGHTS_READ = 0x00020000;
		public const uint TOKEN_ASSIGN_PRIMARY = 0x0001;
		public const uint TOKEN_DUPLICATE = 0x0002;
		public const uint TOKEN_IMPERSONATE = 0x0004;
		public const uint TOKEN_QUERY = 0x0008;
		public const uint TOKEN_QUERY_SOURCE = 0x0010;
		public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
		public const uint TOKEN_ADJUST_GROUPS = 0x0040;
		public const uint TOKEN_ADJUST_DEFAULT = 0x0080;
		public const uint TOKEN_ADJUST_SESSIONID = 0x0100;
		public const uint TOKEN_READ = STANDARD_RIGHTS_READ | TOKEN_QUERY;
		public const uint TOKEN_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY |
			TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE |
			TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT |
			TOKEN_ADJUST_SESSIONID;

		// TOKEN_PRIVILEGES Attributes
		public const uint SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001;
		public const uint SE_PRIVILEGE_ENABLED = 0x00000002;
		public const uint SE_PRIVILEGE_REMOVED = 0x00000004;
		public const uint SE_PRIVILEGE_USED_FOR_ACCESS = 0x80000000;

		public const int ERROR_NO_TOKEN = 1008; //From VC\PlatformSDK\Include\WinError.h
		public const int ERROR_NOT_ALL_ASSIGNED = 1300;

		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern bool OpenThreadToken(
			IntPtr ThreadHandle,
			uint DesiredAccess,
			bool OpenAsSelf,
			out IntPtr TokenHandle);

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle,
			[MarshalAs(UnmanagedType.Bool)]bool DisableAllPrivileges,
			ref TOKEN_PRIVILEGES NewState,
			uint Zero,
			IntPtr Null1,
			IntPtr Null2);

		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern bool ImpersonateSelf(SECURITY_IMPERSONATION_LEVEL ImpersonationLevel);

		public const uint DEBUG_PROCESS = 0x00000001;

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool CreateProcess(
			string lpApplicationName,
			string lpCommandLine,
			int lpProcessAttributes,
			int lpThreadAttributes,
			bool bInheritHandles,
			uint dwCreationFlags,
			IntPtr lpEnvironment,
			string lpCurrentDirectory,
			[In] ref STARTUPINFO lpStartupInfo,
			out PROCESS_INFORMATION lpProcessInformation);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] IntPtr lpBuffer, UInt32 size, out uint lpNumberOfBytesRead);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool ContinueDebugEvent(uint dwProcessId, uint dwThreadId, uint dwContinueStatus);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr GetCurrentThread();

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

		[DllImport("kernel32.dll")]
		public static extern bool DebugSetProcessKillOnExit(bool KillOnExit);

		public const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReserved, uint dwFlags);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool FreeLibrary(IntPtr hModule);

		[DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
		public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern int GetProcessId(uint hProcess);

		[Flags]
		public enum ProcessAccessFlags : uint
		{
			All = 0x001F0FFF,
			Terminate = 0x00000001,
			CreateThread = 0x00000002,
			VirtualMemoryOperation = 0x00000008,
			VirtualMemoryRead = 0x00000010,
			VirtualMemoryWrite = 0x00000020,
			DuplicateHandle = 0x00000040,
			CreateProcess = 0x000000080,
			SetQuota = 0x00000100,
			SetInformation = 0x00000200,
			QueryInformation = 0x00000400,
			QueryLimitedInformation = 0x00001000,
			Synchronize = 0x00100000
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetProcessTimes(IntPtr hProcess, out FILETIME lpCreationTime, out FILETIME lpExitTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

		public enum DebugEventType : uint
		{
			EXCEPTION_DEBUG_EVENT = 1,
			CREATE_THREAD_DEBUG_EVENT = 2,
			CREATE_PROCESS_DEBUG_EVENT = 3,
			EXIT_THREAD_DEBUG_EVENT = 4,
			EXIT_PROCESS_DEBUG_EVENT = 5,
			LOAD_DLL_DEBUG_EVENT = 6,
			UNLOAD_DLL_DEBUG_EVENT = 7,
			OUTPUT_DEBUG_STRING_EVENT = 8,
			RIP_EVENT = 9,
		};

		public struct DEBUG_EVENT
		{
			public DebugEventType dwDebugEventCode;
			public uint dwProcessId;
			public uint dwThreadId;

			public Union u;
		}

		public struct Union
		{
			public EXCEPTION_DEBUG_INFO Exception;
			public CREATE_THREAD_DEBUG_INFO CreateThread;
			public CREATE_PROCESS_DEBUG_INFO CreateProcessInfo;
			public EXIT_THREAD_DEBUG_INFO ExitThread;
			public EXIT_PROCESS_DEBUG_INFO ExitProcess;
			public LOAD_DLL_DEBUG_INFO LoadDll;
			public UNLOAD_DLL_DEBUG_INFO UnloadDll;
			public OUTPUT_DEBUG_STRING_INFO DebugString;
			public RIP_INFO RipInfo;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct EXCEPTION_DEBUG_INFO
		{
			public EXCEPTION_RECORD ExceptionRecord;
			public uint dwFirstChance;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct EXCEPTION_RECORD
		{
			public uint ExceptionCode;
			public uint ExceptionFlags;
			public IntPtr ExceptionRecord;
			public IntPtr ExceptionAddress;
			public uint NumberParameters;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
			public IntPtr[] ExceptionInformation;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct CREATE_THREAD_DEBUG_INFO
		{
			public IntPtr hThread;
			public IntPtr lpThreadLocalBase;
			public IntPtr lpStartAddress;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct CREATE_PROCESS_DEBUG_INFO
		{
			public IntPtr hFile;
			public IntPtr hProcess;
			public IntPtr hThread;
			public IntPtr lpBaseOfImage;
			public uint dwDebugInfoFileOffset;
			public uint nDebugInfoSize;
			public IntPtr lpThreadLocalBase;
			public IntPtr lpStartAddress;
			public IntPtr lpImageName;
			public ushort fUnicode;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct EXIT_THREAD_DEBUG_INFO
		{
			public uint dwExitCode;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct EXIT_PROCESS_DEBUG_INFO
		{
			public uint dwExitCode;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct LOAD_DLL_DEBUG_INFO
		{
			public IntPtr hFile;
			public IntPtr lpBaseOfDll;
			public uint dwDebugInfoFileOffset;
			public uint nDebugInfoSize;
			public IntPtr lpImageName;
			public ushort fUnicode;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct UNLOAD_DLL_DEBUG_INFO
		{
			public IntPtr lpBaseOfDll;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct OUTPUT_DEBUG_STRING_INFO
		{
			public IntPtr lpDebugStringData;
			public ushort fUnicode;
			public ushort nDebugStringLength;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RIP_INFO
		{
			public uint dwError;
			public uint dwType;
		}

		// Inner union of structs must me aligned on IntPtr boundary
		private static readonly int DEBUG_EVENT_OFFSET = 12 + (12 % IntPtr.Size);

		private static readonly int DEBUG_EVENT_SIZE = Marshal.SizeOf(typeof(EXCEPTION_DEBUG_INFO)) + DEBUG_EVENT_OFFSET;

		public static bool WaitForDebugEvent(out DEBUG_EVENT debug_event, uint dwMilliseconds)
		{
			debug_event = new DEBUG_EVENT();
			var len = DEBUG_EVENT_SIZE;
			var buf = Marshal.AllocHGlobal(len);
			RtlZeroMemory(buf, IntPtr.Zero + len);
			var ret = WaitForDebugEvent(buf, dwMilliseconds);

			if (ret)
			{
				debug_event.dwDebugEventCode = (DebugEventType)Marshal.ReadInt32(buf, 0);
				debug_event.dwProcessId = (uint)Marshal.ReadInt32(buf, 4);
				debug_event.dwThreadId = (uint)Marshal.ReadInt32(buf, 8);

				var offset = buf + DEBUG_EVENT_OFFSET;

				switch (debug_event.dwDebugEventCode)
				{
					case DebugEventType.EXCEPTION_DEBUG_EVENT:
						debug_event.u.Exception = (EXCEPTION_DEBUG_INFO)Marshal.PtrToStructure(offset, typeof(EXCEPTION_DEBUG_INFO));
						break;
					case DebugEventType.CREATE_THREAD_DEBUG_EVENT:
						debug_event.u.CreateThread = (CREATE_THREAD_DEBUG_INFO)Marshal.PtrToStructure(offset, typeof(CREATE_THREAD_DEBUG_INFO));
						break;
					case DebugEventType.CREATE_PROCESS_DEBUG_EVENT:
						debug_event.u.CreateProcessInfo = (CREATE_PROCESS_DEBUG_INFO)Marshal.PtrToStructure(offset, typeof(CREATE_PROCESS_DEBUG_INFO));
						break;
					case DebugEventType.EXIT_THREAD_DEBUG_EVENT:
						debug_event.u.ExitThread = (EXIT_THREAD_DEBUG_INFO)Marshal.PtrToStructure(offset, typeof(EXIT_THREAD_DEBUG_INFO));
						break;
					case DebugEventType.EXIT_PROCESS_DEBUG_EVENT:
						debug_event.u.ExitProcess = (EXIT_PROCESS_DEBUG_INFO)Marshal.PtrToStructure(offset, typeof(EXIT_PROCESS_DEBUG_INFO));
						break;
					case DebugEventType.LOAD_DLL_DEBUG_EVENT:
						debug_event.u.LoadDll = (LOAD_DLL_DEBUG_INFO)Marshal.PtrToStructure(offset, typeof(LOAD_DLL_DEBUG_INFO));
						break;
					case DebugEventType.UNLOAD_DLL_DEBUG_EVENT:
						debug_event.u.UnloadDll = (UNLOAD_DLL_DEBUG_INFO)Marshal.PtrToStructure(offset, typeof(UNLOAD_DLL_DEBUG_INFO));
						break;
					case DebugEventType.OUTPUT_DEBUG_STRING_EVENT:
						debug_event.u.DebugString = (OUTPUT_DEBUG_STRING_INFO)Marshal.PtrToStructure(offset, typeof(OUTPUT_DEBUG_STRING_INFO));
						break;
					case DebugEventType.RIP_EVENT:
						debug_event.u.RipInfo = (RIP_INFO)Marshal.PtrToStructure(offset, typeof(RIP_INFO));
						break;
				}
			}

			Marshal.FreeHGlobal(buf);
			return ret;
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool WaitForDebugEvent(IntPtr lpDebugEvent, uint dwMilliseconds);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DebugBreakProcess(IntPtr hProcess);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DebugActiveProcess(uint dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DebugActiveProcessStop(uint dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseHandle(IntPtr hObject);

		[DllImport("Kernel32.dll", SetLastError = false)]
		private static extern void RtlZeroMemory(IntPtr dest, IntPtr size);
	}
}
