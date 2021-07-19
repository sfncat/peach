using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Diagnostics.Runtime.Interop;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Monitor = System.Threading.Monitor;

namespace Peach.Pro.Core.OS.Windows.Debugger
{
	public class KernelDebuggerInstance : MarshalByRefObject, IKernelDebugger
	{
		static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		bool _connected;
		bool _stop;

		IDebugger _winDbg;
		Thread _thread;
		Exception _lastError;
		string _kernelConnectionString;

		public MonitorData Fault { get; private set; }

		public string SymbolsPath { get; set; }
		public string WinDbgPath { get; set; }

		public void Dispose()
		{
			if (_thread != null)
			{
				lock (_thread)
				{
					_stop = true;

					if (_winDbg != null)
					{
						if (_connected)
						{
							// Stops active debugger connections
							_winDbg.Stop();
						}
						else
						{
							// Doesn't seem to be possible to stop the debugger
							// when it is waiting for a connection
							throw new InvalidOperationException("Can't dispose debugger when it is not connected.");
						}
					}
				}

				_thread.Join();
				_thread = null;
			}
		}

		public void AcceptKernel(string kernelConnectionString)
		{
			if (_thread != null)
				throw new NotSupportedException("Kernel debugger is already running.");

			_lastError = null;

			_kernelConnectionString = kernelConnectionString;

			_thread = new Thread(DebugLoop);

			lock (_thread)
			{
				_thread.Start();

				Monitor.Wait(_thread);

				if (_lastError != null)
					throw new PeachException(_lastError.Message, _lastError);
			}
		}

		public void WaitForConnection(uint timeout)
		{
			if (_thread == null)
				throw new NotSupportedException("Kernel debugger is not running.");

			lock (_thread)
			{
				if (!_connected && !Monitor.Wait(_thread, TimeSpan.FromMilliseconds(timeout)))
					throw new TimeoutException("Kenel connection timed out.");

				if (_connected)
					return;

				Debug.Assert(_lastError != null);
				throw new PeachException(_lastError.Message, _lastError);
			}
		}

		private void DebugLoop()
		{
			try
			{
				using (var dbg = new DebugEngine(Path.Combine(WinDbgPath, "dbgeng.dll")))
				{
					dbg.OnConnected = () =>
					{
						lock (_thread)
						{
							Logger.Debug("Kernel connection established");

							_connected = true;
							Monitor.Pulse(_thread);
						}
					};

					dbg.DebugControl.SetInterruptTimeout(1);
					dbg.DebugSymbols.SetSymbolPath(SymbolsPath);

					Logger.Debug("Starting kernel debugger");

					var hr = dbg.DebugClient.AttachKernel(DEBUG_ATTACH.KERNEL_CONNECTION, _kernelConnectionString);
					if (hr != 0)
						Marshal.ThrowExceptionForHR(hr);

					// Signal that we are ready to accept kernel connections

					lock (_thread)
					{
						Logger.Debug("Waiting for kernel connection");

						_winDbg = dbg;
						Monitor.Pulse(_thread);
					}

					while (!_stop && dbg.Fault == null)
					{
						hr = dbg.DebugControl.WaitForEvent(DEBUG_WAIT.DEFAULT, uint.MaxValue);
						Logger.Trace("WaitForEvent: {0}", hr);
					}

					lock (_thread)
					{
						_winDbg = null;
						Fault = dbg.Fault;
					}

					dbg.DebugClient.EndSession(DEBUG_END.PASSIVE);

					Logger.Debug("Kernel connection ended gracefully");
				}
			}
			catch (Exception ex)
			{
				lock (_thread)
				{
					_winDbg = null;
					_lastError = ex;
					Monitor.Pulse(_thread);
				}
			}
		}
	}
}
