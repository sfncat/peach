using System.Collections.Generic;
using System.Text;
using NLog;

namespace Peach.Core
{
	public struct ProcessRunResult
	{
		public int Pid;
		public bool Timeout;
		public int ExitCode;
		public StringBuilder StdErr;
		public StringBuilder StdOut;
	}

	public interface IProcessHelper
	{
		ProcessRunResult Run(NLog.Logger logger,
			string executable,
			string arguments,
			Dictionary<string, string> environment,
			string workingDirectory,
			int timeout);

		Process Start(NLog.Logger logger,
			string executable,
			string arguments,
			Dictionary<string, string> environment,
			string workingDirectory);

		Process GetCurrentProcess(NLog.Logger logger);

		Process GetProcessById(NLog.Logger logger, int id);

		Process[] GetProcessesByName(NLog.Logger logger, string name);
	}

	public class ProcessHelper : StaticPlatformFactory<IProcessHelper>
	{
		static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public static ProcessRunResult Run(string executable,
			string arguments,
			Dictionary<string, string> environment,
			string workingDirectory,
			int timeout)
		{
			return Instance.Run(Logger, executable, arguments, environment, workingDirectory, timeout);
		}

		public static Process Start(string executable,
			string arguments,
			Dictionary<string, string> environment,
			string workingDirectory)
		{
			return Instance.Start(Logger, executable, arguments, environment, workingDirectory);
		}

		public static Process GetCurrentProcess()
		{
			return Instance.GetCurrentProcess(Logger);
		}

		public static Process GetProcessById(int id)
		{
			return Instance.GetProcessById(Logger, id);
		}

		public static Process[] GetProcessesByName(string name)
		{
			return Instance.GetProcessesByName(Logger, name);
		}
	}

	public class ProcessInfo
	{
		public int Id;
		public string ProcessName;
		public bool Responding;

		public ulong TotalProcessorTicks;
		public ulong UserProcessorTicks;
		public ulong PrivilegedProcessorTicks;

		public long PeakVirtualMemorySize64;
		public long PeakWorkingSet64;
		public long PrivateMemorySize64;
		public long VirtualMemorySize64;
		public long WorkingSet64;
	}
}
