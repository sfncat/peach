using System;
using Peach.Core.Agent;

namespace Peach.Pro.Core.OS.Windows.Debugger
{
	public interface IKernelDebugger : IDisposable
	{
		void AcceptKernel(string kernelConnectionString);
		void WaitForConnection(uint timeout);

		MonitorData Fault { get; }

		string SymbolsPath { get; set; }
		string WinDbgPath { get; set; }
	}
}
