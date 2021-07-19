using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Diagnostics.Runtime.Interop;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace Peach.Pro.Core.OS.Windows.Debugger
{
	internal class DebugEngine : IDebugEventCallbacks, IDebugOutputCallbacks, IDebugger
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		static readonly Regex ReMajorHash = new Regex(@"^MAJOR_HASH:(0x.*)\r$", RegexOptions.Multiline);
		static readonly Regex ReMinorHash = new Regex(@"^MINOR_HASH:(0x.*)\r$", RegexOptions.Multiline);
		static readonly Regex ReRisk = new Regex(@"^CLASSIFICATION:(.*)\r$", RegexOptions.Multiline);
		static readonly Regex ReTitle = new Regex(@"^SHORT_DESCRIPTION:(.*)\r$", RegexOptions.Multiline);

		delegate uint DebugCreate(ref Guid InterfaceId, [MarshalAs(UnmanagedType.IUnknown)] out object Interface);

		readonly StringBuilder _output = new StringBuilder();

		IntPtr _hDll;
		IntPtr _hProcess;
		object _dbgEng;
		bool _handlingException;
		bool _processCreated;
		bool _stop;
		int _processId;

		public Action OnConnected { private get; set; }

		public IDebugClient DebugClient { get; set; }
		public IDebugControl DebugControl { get; set; }
		public IDebugSymbols DebugSymbols { get; set; }

		public MonitorData Fault { get; private set; }

		public DebugEngine(string winDbgPath)
		{
			try
			{
				Initialize(winDbgPath);
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		private void Initialize(string winDbgPath)
		{
			if (!Path.IsPathRooted(winDbgPath))
				throw new ArgumentException("Must be an absolute path.", "winDbgPath");

			_hDll = Interop.LoadLibraryEx(winDbgPath, IntPtr.Zero, Interop.LOAD_WITH_ALTERED_SEARCH_PATH);
			if (_hDll == IntPtr.Zero)
				throw new Win32Exception();

			var addr = Interop.GetProcAddress(_hDll, "DebugCreate");
			var func = (DebugCreate)Marshal.GetDelegateForFunctionPointer(addr, typeof(DebugCreate));
			var guid = Marshal.GenerateGuidForType(typeof(IDebugClient5));
			var ret = func(ref guid, out _dbgEng);

			if (ret != 0)
				throw new InvalidOperationException();

			DebugClient = (IDebugClient5)_dbgEng;
			DebugControl = (IDebugControl)_dbgEng;
			DebugSymbols = (IDebugSymbols)_dbgEng;

			var evtCb = Marshal.GetComInterfaceForObject(this, typeof(IDebugEventCallbacks));
			DebugClient.SetEventCallbacks(evtCb);

			var outCb = Marshal.GetComInterfaceForObject(this, typeof(IDebugOutputCallbacks));
			DebugClient.SetOutputCallbacks(outCb);

			var filter = new[]
				{
					new DEBUG_EXCEPTION_FILTER_PARAMETERS
					{
						ExceptionCode = 0x80000001,
						ExecutionOption = DEBUG_FILTER_EXEC_OPTION.BREAK,
						ContinueOption = DEBUG_FILTER_CONTINUE_OPTION.GO_NOT_HANDLED,
					},
					new DEBUG_EXCEPTION_FILTER_PARAMETERS
					{
						ExceptionCode = 0xC000001D,
						ExecutionOption = DEBUG_FILTER_EXEC_OPTION.BREAK,
						ContinueOption = DEBUG_FILTER_CONTINUE_OPTION.GO_NOT_HANDLED,
					},
					new DEBUG_EXCEPTION_FILTER_PARAMETERS
					{
						ExceptionCode = 0xC0000005,
						ExecutionOption = DEBUG_FILTER_EXEC_OPTION.BREAK,
						ContinueOption = DEBUG_FILTER_CONTINUE_OPTION.GO_NOT_HANDLED,
					}
				};

			var hr = DebugControl.SetExceptionFilterParameters((uint)filter.Length, filter);
			if (hr != 0)
				Marshal.ThrowExceptionForHR(hr);
		}

		public void Dispose()
		{
			DebugClient = null;
			DebugControl = null;
			DebugSymbols = null;

			if (_hProcess != IntPtr.Zero)
			{
				Interop.CloseHandle(_hProcess);
				_hProcess = IntPtr.Zero;
			}

			if (_dbgEng != null)
			{
				Marshal.FinalReleaseComObject(_dbgEng);
				_dbgEng = null;
			}

			if (_hDll != IntPtr.Zero)
			{
				Interop.FreeLibrary(_hDll);
				_hDll = IntPtr.Zero;
			}
		}

		#region IDebugEventCallbacks

		int IDebugEventCallbacks.Breakpoint(IDebugBreakpoint Bp)
		{
			Logger.Trace("Breakpoint: {0}", Bp);
			return 0;
		}

		int IDebugEventCallbacks.ChangeDebuggeeState(DEBUG_CDS Flags, ulong Argument)
		{
			Logger.Trace("ChangeDebuggeeState: {0} {1}", Flags, Argument);

			if (_processId != 0 &&
				!_processCreated &&
				Flags == DEBUG_CDS.REGISTERS)
			{
				_processCreated = true;

				if (ProcessCreated != null)
					ProcessCreated(_processId);
			}

			return 0;
		}

		int IDebugEventCallbacks.ChangeEngineState(DEBUG_CES Flags, ulong Argument)
		{
			Logger.Trace("ChangeEngineState: {0} {1}", Flags, Argument);

			return 0;
		}

		int IDebugEventCallbacks.ChangeSymbolState(DEBUG_CSS Flags, ulong Argument)
		{
			Logger.Trace("ChangeSymbolState: {0} {1}", Flags, Argument);
			return 0;
		}

		int IDebugEventCallbacks.CreateProcess(ulong ImageFileHandle, ulong Handle, ulong BaseOffset, uint ModuleSize, string ModuleName, string ImageName, uint CheckSum, uint TimeDateStamp, ulong InitialThreadHandle, ulong ThreadDataOffset, ulong StartOffset)
		{
			Logger.Trace("CreateProcess: {0} {1} {2} {3} {4} {5}", ImageFileHandle, Handle, BaseOffset, ModuleSize, ModuleName, ImageName);

			if (_processId == 0)
			{
				_processId = Interop.GetProcessId((uint)Handle);
				_hProcess = Interop.OpenProcess(Interop.ProcessAccessFlags.All, false, _processId);
			}

			return 0;
		}

		int IDebugEventCallbacks.CreateThread(ulong Handle, ulong DataOffset, ulong StartOffset)
		{
			Logger.Trace("CreateThread: 0x{0:x8} 0x{1:x8}0x {2:x8}", Handle, DataOffset, StartOffset);
			return 0;
		}

		unsafe int IDebugEventCallbacks.Exception(ref EXCEPTION_RECORD64 Exception, uint FirstChance)
		{
			Logger.Debug("Exception: 0x{0:x8}, FirstChance: {1}", Exception.ExceptionCode, FirstChance);

			var ev = new ExceptionEvent
			{
				FirstChance = FirstChance,
				Code = Exception.ExceptionCode,
				Info = new long[2]
			};

			fixed (ulong* ptr = Exception.ExceptionInformation)
			{
				ev.Info[0] = (long)ptr[0];
				ev.Info[1] = (long)ptr[1];
			}

			OnException(ev);

			return (int)DEBUG_STATUS.NO_CHANGE;
		}

		int IDebugEventCallbacks.ExitProcess(uint ExitCode)
		{
			Logger.Trace("ExitProcess: {0}", ExitCode);
			return 0;
		}

		int IDebugEventCallbacks.ExitThread(uint ExitCode)
		{
			Logger.Trace("ExitThread: {0}", ExitCode);
			return 0;
		}

		int IDebugEventCallbacks.GetInterestMask(out DEBUG_EVENT Mask)
		{
			Mask = DEBUG_EVENT.EXCEPTION |
				DEBUG_EVENT.SESSION_STATUS |
				DEBUG_EVENT.SYSTEM_ERROR |
				DEBUG_EVENT.CHANGE_DEBUGGEE_STATE |
				DEBUG_EVENT.CHANGE_ENGINE_STATE |
				DEBUG_EVENT.BREAKPOINT |
				DEBUG_EVENT.CREATE_PROCESS |
				DEBUG_EVENT.EXIT_PROCESS
				;
			return 0;
		}

		int IDebugEventCallbacks.LoadModule(ulong ImageFileHandle, ulong BaseOffset, uint ModuleSize, string ModuleName, string ImageName, uint CheckSum, uint TimeDateStamp)
		{
			Logger.Trace("LoadModule: {0} {1} {2} {3} {4}", ImageFileHandle, BaseOffset, ModuleSize, ModuleName, ImageName);
			return 0;
		}

		int IDebugEventCallbacks.SessionStatus(DEBUG_SESSION Status)
		{
			Logger.Trace("SessionStatus: {0}", Status);

			if (Status == DEBUG_SESSION.ACTIVE && OnConnected != null)
				OnConnected();

			return 0;
		}

		int IDebugEventCallbacks.SystemError(uint Error, uint Level)
		{
			Logger.Trace("SystemError: {0} {1}", Error, Level);
			return 0;
		}

		int IDebugEventCallbacks.UnloadModule(string ImageBaseName, ulong BaseOffset)
		{
			Logger.Trace("UnloadModule: {0} {1}", ImageBaseName, BaseOffset);
			return 0;
		}

		#endregion

		#region IDebugOutputCallbacks

		int IDebugOutputCallbacks.Output(DEBUG_OUTPUT Mask, string Text)
		{
			_output.Append(Text);
			return 0;
		}

		#endregion

		private void OnException(ExceptionEvent ev)
		{
			bool keepGoing;

			if (ev.Code == 0x80000003)
			{
				// We stop the debugger by triggering a first change breakpoint
				if (ev.FirstChance == 1 && _stop)
					return;

				// Kernel crashes come in as bugcheck breakpoints
				keepGoing = false;
			}
			else if (HandleAccessViolation != null)
			{
				// If handler is registered, ask whether to keep going
				keepGoing = HandleAccessViolation(ev);
			}
			else
			{
				// Default is to break on all non-first chance exceptions
				keepGoing = ev.FirstChance == 1;
			}

			if (keepGoing)
				return;

			// Don't recurse (does this really happen?)
			if (_handlingException)
				return;

			_handlingException = true;

			Logger.Debug("Fault detected, collecting info from windbg...");

			// 1. Output registers

			DebugControl.Execute(DEBUG_OUTCTL.THIS_CLIENT, "r", DEBUG_EXECUTE.ECHO);
			DebugControl.Execute(DEBUG_OUTCTL.THIS_CLIENT, "rF", DEBUG_EXECUTE.ECHO);
			DebugControl.Execute(DEBUG_OUTCTL.THIS_CLIENT, "rX", DEBUG_EXECUTE.ECHO);
			DebugClient.FlushCallbacks();
			_output.Append("\n\n");

			// 2. Output stacktrace

			// Note: There is a known issue with dbgeng that can cause stack traces to take days due to issues in 
			// resolving symbols.  There is no known work arround.  We need the ability to skip a stacktrace
			// when this occurs.

			DebugControl.Execute(DEBUG_OUTCTL.THIS_CLIENT, "kb", DEBUG_EXECUTE.ECHO);
			DebugClient.FlushCallbacks();
			_output.Append("\n\n");

			// 3. Dump File

			// Note: This can cause hangs on a bad day.  Don't think it's all that important, so skipping.

			// 4. !exploitable

			var path = IntPtr.Size == 4
				? "Debuggers\\DebugEngine\\msec86.dll"
				: "Debuggers\\DebugEngine\\msec64.dll";

			path = Path.Combine(Utilities.ExecutionDirectory, path);
			DebugControl.Execute(DEBUG_OUTCTL.THIS_CLIENT, ".load " + path, DEBUG_EXECUTE.ECHO);
			DebugControl.Execute(DEBUG_OUTCTL.THIS_CLIENT, "!exploitable -m", DEBUG_EXECUTE.ECHO);
			_output.Append("\n\n");

			_output.Replace("\x0a", "\r\n");

			var output = _output.ToString();

			var fault = new MonitorData
			{
				Title = ReTitle.Match(output).Groups[1].Value,
				DetectionSource = "WindowsDebugEngine",
				Fault = new MonitorData.Info
				{
					Description = output,
					MajorHash = ReMajorHash.Match(output).Groups[1].Value,
					MinorHash = ReMinorHash.Match(output).Groups[1].Value,
					Risk = ReRisk.Match(output).Groups[1].Value,
					MustStop = false,
				},
				Data = new Dictionary<string, Stream>()
			};

			Logger.Debug("Completed gathering windbg information");

			Fault = fault;
		}

		public Func<ExceptionEvent, bool> HandleAccessViolation { get; set; }

		public Action<int> ProcessCreated { get; set; }

		public static DebugEngine CreateProcess(string winDbgPath, string symbolsPath, string commandLine)
		{
			if (winDbgPath == null)
				throw new ArgumentNullException("winDbgPath");
			if (symbolsPath == null)
				throw new ArgumentNullException("symbolsPath");
			if (commandLine == null)
				throw new ArgumentNullException("commandLine");

			var dbg = new DebugEngine(Path.Combine(winDbgPath, "dbgeng.dll"));

			dbg.DebugControl.SetInterruptTimeout(1);
			dbg.DebugSymbols.SetSymbolPath(symbolsPath);

			try
			{
				var hr = dbg.DebugClient.CreateProcessAndAttach(
					0,
					commandLine,
					(DEBUG_CREATE_PROCESS)Interop.DEBUG_PROCESS,
					0,
					DEBUG_ATTACH.DEFAULT);

				var ex = Marshal.GetExceptionForHR(hr);
				if (ex != null)
					throw ex;

				return dbg;
			}
			catch
			{
				dbg.Dispose();
				throw;
			}
		}

		public static DebugEngine AttachToProcess(string winDbgPath, string symbolsPath, int pid)
		{
			var dbg = new DebugEngine(Path.Combine(winDbgPath, "dbgeng.dll"));

			dbg.DebugControl.SetInterruptTimeout(1);
			dbg.DebugSymbols.SetSymbolPath(symbolsPath);

			try
			{
				var hr = dbg.DebugClient.AttachProcess(
					0,
					(uint)pid,
					DEBUG_ATTACH.DEFAULT);

				var ex = Marshal.GetExceptionForHR(hr);
				if (ex != null)
					throw ex;

				return dbg;
			}
			catch
			{
				dbg.Dispose();
				throw;
			}
		}

		public ulong TotalProcessorTicks
		{
			get
			{
				FILETIME ftCreation, ftExit, ftKernel, ftUser;
				if (!Interop.GetProcessTimes(_hProcess, out ftCreation, out ftExit, out ftKernel, out ftUser))
				{
					var ex = new Win32Exception(Marshal.GetLastWin32Error());
					Logger.Trace("Failed to get process times for process 0x{0:X}.  {1}", _processId, ex.Message);
					return 0;
				}

				var kernel = (ulong)ftKernel.dwLowDateTime;
				kernel <<= 32;
				kernel += (ulong)ftKernel.dwLowDateTime;

				var user = (ulong)ftUser.dwLowDateTime;
				user <<= 32;
				user += (ulong)ftUser.dwLowDateTime;

				return kernel + user;
			}
		}

		public void Run()
		{
			while (Fault == null && !_stop)
			{
				var hr = DebugControl.WaitForEvent(DEBUG_WAIT.DEFAULT, uint.MaxValue);

				Logger.Trace("WaitForEvent: 0x{0:x8}", hr);

				// E_UNEXPECTED means ran to completion
				if ((uint)hr == 0x8000ffff)
					break;

				var ex = Marshal.GetExceptionForHR(hr);
				if (ex != null)
					throw ex;
			}

			DebugClient.EndSession(DEBUG_END.PASSIVE);

			Logger.Debug("Debugger ended gracefully");
		}

		public void Stop()
		{
			// Cause the debugger to break by generating a synthetic exception

			Logger.Trace("Stoping debugger...");

			_stop = true;
			DebugControl.SetInterrupt(DEBUG_INTERRUPT.ACTIVE);
		}
	}
}
