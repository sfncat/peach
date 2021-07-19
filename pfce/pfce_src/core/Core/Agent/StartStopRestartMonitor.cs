using System.Collections.Generic;

namespace Peach.Core.Agent
{
	public abstract class StartStopRestartMonitor : Monitor2
	{
		public MonitorWhen When { get; set; }
		public abstract bool StopOnEnd { get; }
		public abstract string RestartOnCall { get; }

		private IStartStopRestart _control;

		protected StartStopRestartMonitor(string name)
			: base(name)
		{
		}

		protected abstract IStartStopRestart CreateStartStopControl();

		public override void StartMonitor(Dictionary<string, string> args)
		{
			base.StartMonitor(args);
			_control = CreateStartStopControl();
		}

		public override void SessionStarting()
		{
			_control.Start();

			if (When.HasFlag(MonitorWhen.OnStart))
				_control.Restart();
		}

		public override void SessionFinished()
		{
			if (When.HasFlag(MonitorWhen.OnEnd))
				_control.Restart();

			if (StopOnEnd)
				_control.Stop();
		}
		public override void IterationStarting(IterationStartingArgs args)
		{
			if (When.HasFlag(MonitorWhen.OnIterationStart) ||
			    (args.LastWasFault && When.HasFlag(MonitorWhen.OnIterationStartAfterFault)))
				_control.Restart();
		}

		public override void IterationFinished()
		{
			if (When.HasFlag(MonitorWhen.OnIterationEnd))
				_control.Restart();
		}

		public override bool DetectedFault()
		{
			if (When.HasFlag(MonitorWhen.DetectFault))
				_control.Restart();

			return false;
		}

		public override MonitorData GetMonitorData()
		{
			if (When.HasFlag(MonitorWhen.OnFault))
				_control.Restart();

			return null;
		}

		public override void Message(string msg)
		{
			if (When.HasFlag(MonitorWhen.OnCall) && RestartOnCall == msg)
				_control.Restart();
		}
	}
}

