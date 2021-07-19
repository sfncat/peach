using System;
using Peach.Core.Agent;

namespace Peach.Pro.Core.OS.Windows.Debugger
{
	public interface IDebuggerInstance : IDisposable
	{
		string WinDbgPath { get; set; }
		string SymbolsPath { get; set; }

		bool IgnoreFirstChanceReadAv { get; set; }
		bool IgnoreFirstChanceGuardPage { get; set; }
		bool IgnoreSecondChanceGuardPage { get; set; }

		string Name { get; }
		bool IsRunning { get; }

		void AttachProcess(string processName);
		void StartProcess(string commandLine);
		void StartService(string serviceName, TimeSpan startTimeout);

		/// <summary>
		/// Waits for the specified amount of time for the process to exit.
		/// If the process doesn't exit, it will be killed.
		/// </summary>
		/// <param name="timeout">How long to wait for the process to exit.</param>
		/// <returns>True if the process exited gracefully, false if it was killed.</returns>
		bool WaitForExit(int timeout);

		/// <summary>
		/// Waits for the specified amount of time for the process to exit or the cpu to go idle.
		/// If the cpu goes idle, the process will be killed.
		/// If the process doesn't exit or go idle, it will be killed.
		/// </summary>
		/// <param name="timeout">How long to wait for the process to exit.</param>
		/// <param name="pollInterval">How often to poll the cpu usage.</param>
		/// <returns>True if the process exited gracefully or was killed while idle, false if it was killed when not idle.</returns>
		bool WaitForIdle(int timeout, uint pollInterval);

		bool DetectedFault { get; }

		MonitorData Fault { get; }
	}
}
