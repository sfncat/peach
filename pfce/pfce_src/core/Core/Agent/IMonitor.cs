using System;
using System.Collections.Generic;

namespace Peach.Core.Agent
{
	public interface IMonitor : INamed
	{
		string Class { get; }

		void StartMonitor(Dictionary<string, string> args);
		void StopMonitor();
		void SessionStarting();
		void SessionFinished();
		void IterationStarting(IterationStartingArgs args);
		void IterationFinished();
		bool DetectedFault();
		MonitorData GetMonitorData();
		void Message(string msg);
		event EventHandler InternalEvent;
	}
}
