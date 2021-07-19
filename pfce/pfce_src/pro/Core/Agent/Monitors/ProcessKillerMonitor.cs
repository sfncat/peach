using System;
using System.ComponentModel;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Monitor = Peach.Core.Agent.Monitor2;

namespace Peach.Pro.Core.Agent.Monitors
{
	[Monitor("ProcessKiller")]
	[Description("Terminates the specified processes after each iteration")]
	[Parameter("ProcessNames", typeof(string[]), "Comma separated list of process to kill.")]
	public class ProcessKillerMonitor : Monitor
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public string[] ProcessNames { get; set; }

		public ProcessKillerMonitor(string name)
			: base(name)
		{
		}

		public override void IterationFinished()
		{
			foreach (var item in ProcessNames)
				Kill(item);
		}

		private static void Kill(string processName)
		{
			var procs = ProcessHelper.GetProcessesByName(processName);

			foreach (var p in procs)
			{
				try
				{
					p.Stop(-1);
				}
				catch (Exception ex)
				{
					Logger.Debug("Unable to kill process '{0}' (pid: {2}). {1}", processName, p.Id, ex.Message);
				}
				finally
				{
					p.Dispose();
				}
			}
		}
	}
}
