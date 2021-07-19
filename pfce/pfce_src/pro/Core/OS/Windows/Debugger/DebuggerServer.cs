using System;
using Peach.Core;

namespace Peach.Pro.Core.OS.Windows.Debugger
{
	public class DebuggerServer : MarshalByRefObject
	{
		public IKernelDebugger GetKernelDebugger(int logLevel)
		{
			Utilities.ConfigureLogging(logLevel);

			return new KernelDebuggerInstance();
		}

		public IDebuggerInstance GetProcessDebugger<T>(int logLevel)
			where T: MarshalByRefObject, IDebuggerInstance, new()
		{
			Utilities.ConfigureLogging(logLevel);

			return new T();
		}
	}
}
