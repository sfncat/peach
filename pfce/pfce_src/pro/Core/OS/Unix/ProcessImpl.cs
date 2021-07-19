using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using Mono.Unix;
using Peach.Core;
using Peach.Core.IO;
using Logger = NLog.Logger;
using SysProcess = System.Diagnostics.Process;

namespace Peach.Pro.Core.OS.Unix
{
	public abstract class ProcessImpl : Process
	{
		#region Common Unix IProcess Implementation

		public abstract class BaseUnixProcess : IProcess
		{
			protected abstract bool Attached { get; }

			protected readonly SysProcess _process;

			protected BaseUnixProcess(SysProcess process)
			{
				_process = process;
			}

			public void Dispose()
			{
				// Mirror windows behaivor of ensuring owned processes get killed
				if (!Attached && !_process.HasExited)
					Kill();
				
				_process.Close();
			}

			public int Id
			{
				get { return _process.Id; }
			}

			public int ExitCode
			{
				get { return _process.ExitCode; }
			}

			public bool HasExited
			{
				get { return _process.HasExited; }
			}

			public StreamWriter StandardInput
			{
				get { return _process.StandardInput; }
			}

			public StreamReader StandardOutput
			{
				get { return _process.StandardOutput; }
			}

			public StreamReader StandardError
			{
				get { return _process.StandardError; }
			}

			public abstract ProcessInfo Snapshot();

			public void Terminate()
			{
				if (Attached)
				{
					_process.CloseMainWindow();
				}
				else
				{
					var ret = killpg(_process.Id, SIGTERM);
					UnixMarshal.ThrowExceptionForLastErrorIf(ret);
				}
			}

			public void Kill()
			{
				if (Attached)
				{
					_process.Kill();
				}
				else
				{
					var ret = killpg(_process.Id, SIGKILL);
					UnixMarshal.ThrowExceptionForLastErrorIf(ret);
				}
				_process.WaitForExit(-1);
			}

			public virtual bool WaitForExit(int timeout)
			{
				return _process.WaitForExit(timeout);
			}

			protected bool PollForExit(int timeout)
			{
				if (timeout == 0)
					return _process.HasExited;

				var sw = System.Diagnostics.Stopwatch.StartNew();

				while (!_process.HasExited && timeout > 0 && sw.ElapsedMilliseconds < timeout)
					Thread.Sleep(10);

				return _process.HasExited;
			}
		}

		#endregion

		#region Unix Process Helper

		public abstract class BaseProcessHelper : IProcessHelper
		{
			public ProcessRunResult Run(Logger logger, string executable, string arguments, Dictionary<string, string> environment, string workingDirectory, int timeout)
			{
				using (var p = NewProcess(logger))
				{
					return p.Run(executable, arguments, environment, workingDirectory, timeout);
				}
			}

			public Process Start(Logger logger, string executable, string arguments, Dictionary<string, string> environment, string logDir)
			{
				var ret = NewProcess(logger);

				try
				{
					ret.Start(executable, arguments, environment, logDir);
					return ret;
				}
				catch
				{
					ret.Dispose();
					throw;
				}
			}

			public Process GetCurrentProcess(Logger logger)
			{
				return AttachProcess(logger, SysProcess.GetCurrentProcess());
			}

			public Process GetProcessById(Logger logger, int id)
			{
				return AttachProcess(logger, SysProcess.GetProcessById(id));
			}

			public Process[] GetProcessesByName(Logger logger, string name)
			{
				return GetProcessesByName(name)
					.Select(p => AttachProcess(logger, p))
					.OfType<Process>()
					.ToArray();
			}

			protected ProcessImpl AttachProcess(Logger logger, SysProcess process)
			{
				var ret = NewProcess(logger);
				ret._process = ret.MakeAttachedProcess(process);
				return ret;
			}

			protected abstract ProcessImpl NewProcess(Logger logger);

			protected abstract IEnumerable<SysProcess> GetProcessesByName(string name);
		}

		#endregion

		protected abstract IProcess MakeOwnedProcess(SysProcess process);

		protected abstract IProcess MakeAttachedProcess(SysProcess process);

		protected ProcessImpl(Logger logger)
			: base(logger)
		{
		}

		protected override IProcess AttachProcess(int pid)
		{
			return MakeAttachedProcess(SysProcess.GetProcessById(pid));
		}

		protected override IProcess CreateProcess(
			string executable,
			string arguments,
			string workingDirectory,
			Dictionary<string, string> environment)
		{
			var listener = new TcpListener(IPAddress.Loopback, 0);
			listener.Start();

			try
			{
				var local = (IPEndPoint)listener.LocalEndpoint;

				_logger.Trace("CreateProcess(): TcpListener bound to: {0}", local);

				var args = string.Join(" ",
					"--debugger-agent=transport=dt_socket,address=127.0.0.1:{0},setpgid=y".Fmt(local.Port),
					Utilities.GetAppResourcePath("PeachTrampoline.exe"),
					executable,
					arguments
				);

				var si = new System.Diagnostics.ProcessStartInfo
				{
					FileName = "mono",
					Arguments = args,
					UseShellExecute = false,
					RedirectStandardInput = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					WorkingDirectory = workingDirectory ?? "",
				};

				if (environment != null)
					environment.ForEach(x => si.EnvironmentVariables[x.Key] = x.Value);

				_logger.Debug("CreateProcess(): \"{0} {1}\"", executable, arguments);
				var process = SysProcess.Start(si);

				var task = listener.AcceptTcpClientAsync();
				while (!task.Wait(TimeSpan.FromMilliseconds(100)))
				{
					if (process.HasExited)
						throw new PeachException("Failed to start with exit code: {0}".Fmt(process.ExitCode));
				}

				DebuggerServer(task.Result);

				return MakeOwnedProcess(process);
			}
			finally
			{
				// Don't call listener.Stop() since after it closes the open socket a new one is opened
				listener.Server.Close();
			}
		}

		private void DebuggerServer(TcpClient tcp)
		{
			using (tcp)
			using (var stream = tcp.GetStream())
			{
				var handshake = new byte[13]; // "DWP handshake"
				_logger.Trace("DebuggerServer(): Reading handshake...");
				stream.Read(handshake, 0, handshake.Length);
				_logger.Trace("DebuggerServer(): Echo: {0}", Encoding.UTF8.GetString(handshake));
				stream.Write(handshake, 0, handshake.Length);

				EnableExceptions(stream);

				var buf = new byte[HEADER_LENGTH];

				// wait until EOF
				while (true)
				{
					var ret = stream.Read(buf, 0, buf.Length);
					if (ret == 0)
						break;

					using (var reader = new BitReader(new BitStream(buf)))
					{
						reader.BigEndian();

						var header = new CommandHeader
						{
							Length = reader.ReadInt32(),
							Id = reader.ReadInt32(),
							Flags = reader.ReadByte(),
							CommandSet = (CommandSet)reader.ReadByte(),
							Command = (Command)reader.ReadByte(),
						};

						var data = new byte[header.Length - buf.Length];

						ret = stream.Read(data, 0, data.Length);
						if (ret != data.Length)
							throw new IOException();

						_logger.Trace("DebuggerServer(): len: {0}, id: {1}, flags: {2}, set: {3}, cmd: {4}, data: {5} bytes",
							header.Length,
							header.Id,
							header.Flags,
							header.CommandSet,
							header.Command,
							data.Length
						);

						if (header.Flags != REPLY_FLAG &&
							header.CommandSet == CommandSet.EVENT &&
							header.Command == Command.EVENT_COMPOSITE)
						{
							ParseEvent(stream, data);
						}
					}
				}
			}
		}

		private void ParseEvent(Stream stream, byte[] buf)
		{
			using (var reader = new BitReader(new BitStream(buf)))
			{
				reader.BigEndian();

				var policy = (SuspendPolicy)reader.ReadByte();
				var nevents = reader.ReadInt32();

				_logger.Trace("ParseEvent(): suspend: {0}, events: {1}", policy, nevents);

				for (var i = 0; i < nevents; i++)
				{
					var kind = (EventKind)reader.ReadByte();
					var id = reader.ReadInt32();
					var threadId = reader.ReadInt32();

					_logger.Trace("ParseEvent(): kind: {0}, id: {1}, tid: {2}", kind, id, threadId);

					switch (kind)
					{
						case EventKind.VM_START:
							Resume(stream);
							break;
						case EventKind.VM_DEATH:
							reader.ReadInt32();
							break;
						case EventKind.THREAD_START:
							break;
						case EventKind.THREAD_DEATH:
							break;
						case EventKind.ASSEMBLY_LOAD:
							reader.ReadInt32();
							break;
						case EventKind.ASSEMBLY_UNLOAD:
							reader.ReadInt32();
							break;
						case EventKind.TYPE_LOAD:
							reader.ReadInt32();
							break;
						case EventKind.METHOD_ENTRY:
							reader.ReadInt32();
							break;
						case EventKind.METHOD_EXIT:
							reader.ReadInt32();
							break;
						case EventKind.BREAKPOINT:
							reader.ReadInt32();
							reader.ReadInt64();
							break;
						case EventKind.STEP:
							reader.ReadInt32();
							reader.ReadInt64();
							break;
						case EventKind.EXCEPTION:
							reader.ReadInt32();
							throw new Exception("Child process failed to start");
						case EventKind.APPDOMAIN_CREATE:
							reader.ReadInt32();
							break;
						case EventKind.APPDOMAIN_UNLOAD:
							reader.ReadInt32();
							break;
						case EventKind.USER_LOG:
							reader.ReadInt32();
							reader.ReadString();
							reader.ReadString();
							break;
						case EventKind.USER_BREAK:
							break;
						case EventKind.KEEPALIVE:
							break;
					}
				}
			}
		}

		private void EnableExceptions(Stream stream)
		{
			_logger.Trace("EnableExceptions");

			var ms = new MemoryStream();
			const int len = HEADER_LENGTH + 3;
			using (var writer = new BitWriter(new BitStream(ms)))
			{
				writer.BigEndian();
				writer.WriteInt32(len);
				writer.WriteInt32(1);
				writer.WriteByte(0);
				writer.WriteByte((byte)CommandSet.EVENT_REQUEST);
				writer.WriteByte((byte)Command.EVENT_REQUEST_SET);
				writer.WriteByte((byte)EventKind.EXCEPTION);
				writer.WriteByte((byte)SuspendPolicy.NONE);
				writer.WriteByte(0);
			}

			var buf = ms.GetBuffer();
			stream.Write(buf, 0, len);
		}

		private void Resume(Stream stream)
		{
			_logger.Trace("Resume");

			var ms = new MemoryStream();
			using (var writer = new BitWriter(new BitStream(ms)))
			{
				writer.BigEndian();
				writer.WriteInt32(HEADER_LENGTH);
				writer.WriteInt32(2);
				writer.WriteByte(0);
				writer.WriteByte((byte)CommandSet.VM);
				writer.WriteByte((byte)Command.VM_RESUME);
			}

			var buf = ms.GetBuffer();
			stream.Write(buf, 0, HEADER_LENGTH);
		}

		const int SIGKILL = 9;
		const int SIGTERM = 15;

		// ReSharper disable UnusedMember.Local

		enum CommandSet : byte
		{
			VM = 1,
			OBJECT_REF = 9,
			STRING_REF = 10,
			THREAD = 11,
			ARRAY_REF = 13,
			EVENT_REQUEST = 15,
			STACK_FRAME = 16,
			APPDOMAIN = 20,
			ASSEMBLY = 21,
			METHOD = 22,
			TYPE = 23,
			MODULE = 24,
			FIELD = 25,
			EVENT = 64
		}

		enum EventKind : byte
		{
			VM_START = 0,
			VM_DEATH = 1,
			THREAD_START = 2,
			THREAD_DEATH = 3,
			APPDOMAIN_CREATE = 4, // Not in JDI
			APPDOMAIN_UNLOAD = 5, // Not in JDI
			METHOD_ENTRY = 6,
			METHOD_EXIT = 7,
			ASSEMBLY_LOAD = 8,
			ASSEMBLY_UNLOAD = 9,
			BREAKPOINT = 10,
			STEP = 11,
			TYPE_LOAD = 12,
			EXCEPTION = 13,
			KEEPALIVE = 14,
			USER_BREAK = 15,
			USER_LOG = 16
		}

		enum SuspendPolicy : byte
		{
			NONE = 0,
			EVENT_THREAD = 1,
			ALL = 2
		}

		const byte REPLY_FLAG = 0x80;
		const int HEADER_LENGTH = 11;

		enum Command : byte
		{
			// VM
			VM_VERSION = 1,
			VM_ALL_THREADS = 2,
			VM_SUSPEND = 3,
			VM_RESUME = 4,
			VM_EXIT = 5,
			VM_DISPOSE = 6,
			VM_INVOKE_METHOD = 7,
			VM_SET_PROTOCOL_VERSION = 8,
			VM_ABORT_INVOKE = 9,
			VM_SET_KEEPALIVE = 10,
			VM_GET_TYPES_FOR_SOURCE_FILE = 11,
			VM_GET_TYPES = 12,
			VM_INVOKE_METHODS = 13,
			VM_START_BUFFERING = 14,
			VM_STOP_BUFFERING = 15,

			// EVENT_REQUEST
			EVENT_REQUEST_SET = 1,
			EVENT_REQUEST_CLEAR = 2,
			EVENT_REQUEST_CLEAR_ALL = 3,

			// EVENT
			EVENT_COMPOSITE = 100,
		}

		// ReSharper restore UnusedMember.Local

		struct CommandHeader
		{
			public int Length;
			public int Id;
			public byte Flags;
			public CommandSet CommandSet;
			public Command Command;
		}

		[DllImport("libc", SetLastError = true)]
		private static extern int killpg(int pgrp, int sig);
	}
}
