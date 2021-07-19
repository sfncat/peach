using System;
using System.Runtime.Remoting;
using NLog;
using Peach.Core;
using Peach.Core.Agent;

namespace Peach.Pro.Core.OS.Windows.Debugger
{
	internal class KernelDebugger : IKernelDebugger
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		Remotable<DebuggerServer> _process;
		IKernelDebugger _dbg;

		public MonitorData Fault
		{
			get { return Guard(() => _dbg.Fault); }
		}

		public string SymbolsPath { get; set; }
		public string WinDbgPath { get; set; }

		public void Dispose()
		{
			_dbg = null;

			// No way to gracefully stop kernel debugger so just kill process
			if (_process != null)
			{
				_process.Dispose();
				_process = null;
			}
		}

		public void AcceptKernel(string kernelConnectionString)
		{
			if (_process != null)
				throw new NotSupportedException("Kernel debugger is already running.");

			_process = new Remotable<DebuggerServer>();

			Logger.Debug("Creating KernelDebuggerInstance from {0}", _process.Url);

			try
			{
				var remote = _process.GetObject();

				var logLevel = Logger.IsTraceEnabled ? 2 : (Logger.IsDebugEnabled ? 1 : 0);

				_dbg = remote.GetKernelDebugger(logLevel);
				_dbg.SymbolsPath = SymbolsPath;
				_dbg.WinDbgPath = WinDbgPath;

				_dbg.AcceptKernel(kernelConnectionString);
			}
			catch (RemotingException ex)
			{
				throw new SoftException("Failed to initialize kernel debugger process.", ex);
			}
		}

		public void WaitForConnection(uint timeout)
		{
			try
			{
				_dbg.WaitForConnection(timeout);
			}
			catch (TimeoutException ex)
			{
				throw new SoftException(ex);
			}
			catch (RemotingException ex)
			{
				throw new SoftException("Error occured when waiting for kernel connection.", ex);
			}
		}

		private T Guard<T>(Func<T> func)
		{
			if (_dbg == null)
				return default(T);

			try
			{
				return func();
			}
			catch (RemotingException ex)
			{
				throw new SoftException(ex);
			}
		}
	}
}
