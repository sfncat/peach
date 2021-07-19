using System;
using Peach.Core.Agent;

namespace Peach.Pro.Core.OS.Windows.Debugger
{
	public class ExceptionEvent
	{
		public uint FirstChance { get; set; }
		public uint Code { get; set; }
		public long[] Info { get; set; }
	}

	public interface IDebugger : IDisposable
	{
		Func<ExceptionEvent, bool> HandleAccessViolation { get; set; }
		Action<int> ProcessCreated { get; set; }

		void Run();
		void Stop();

		ulong TotalProcessorTicks { get; }
		MonitorData Fault { get; }
	}
}
