using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Pro.Core.Agent.Monitors.Utilities;
using Monitor = Peach.Core.Agent.Monitor2;

namespace Peach.Pro.Core.Agent.Monitors
{
	/// <summary>
	/// Start a process
	/// </summary>
	[Monitor("Process")]
	[Alias("process.Process")]
	[Description("Controls a process during a fuzzing run")]
	[Parameter("Executable", typeof(string), "Executable to launch")]
	[Parameter("Arguments", typeof(string), "Optional command line arguments", "")]
	[Parameter("RestartOnEachTest", typeof(bool), "Restart process for each interation", "false")]
	[Parameter("RestartAfterFault", typeof(bool), "Restart the process if a different monitor detects a fault", "true")]
	[Parameter("FaultOnEarlyExit", typeof(bool), "Trigger fault if process exits", "false")]
	[Parameter("NoCpuKill", typeof(bool), "Disable process killing when CPU usage nears zero", "false")]
	[Parameter("StartOnCall", typeof(string), "Start command on state model call", "")]
	[Parameter("WaitForExitOnCall", typeof(string), "Wait for process to exit on state model call and fault if timeout is reached", "")]
	[Parameter("WaitForExitTimeout", typeof(int), "Wait for exit timeout value in milliseconds (-1 is infinite)", "10000")]
	public class ProcessMonitor : Monitor
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		StringBuilder _asanResult = new StringBuilder();
		readonly Process _process;
		MonitorData _data;
		bool _messageExit;

		public string Executable { get; set; }
		public string Arguments { get; set; }
		public bool RestartOnEachTest { get; set; }
		public bool RestartAfterFault { get; set; }
		public bool FaultOnEarlyExit { get; set; }
		public bool NoCpuKill { get; set; }
		public string StartOnCall { get; set; }
		public string WaitForExitOnCall { get; set; }
		public int WaitForExitTimeout { get; set; }

		public ProcessMonitor(string name)
			: base(name)
		{
			_process = PlatformFactory<Process>.CreateInstance(Logger);
		}

		void _Start()
		{
			if (_process.IsRunning)
				return;
			
			try
			{
				var sb = new StringBuilder();

				_process.StandardError = line =>
				{
					lock (sb)
					{
						if (sb.Length > 0 || Asan.CheckForAsanFault(line))
						{
							sb.AppendLine(line);
						}
					}
				};

				_asanResult = sb;

				_process.Start(Executable, Arguments, null, null);
				OnInternalEvent(EventArgs.Empty);
			}
			catch (Exception ex)
			{
				throw new PeachException("Could not start process '{0}'. {1}.".Fmt(Executable, ex.Message), ex);
			}
		}

		bool _CheckAsan()
		{
			lock (_asanResult)
			{
				if (_asanResult.Length == 0)
					return false;
			}

			// ASAN kills the process, so just wait for it to exit.
			// Don't SIGKILL when asan is in the middle of writing
			// its output to stderr or we will miss lost of information.
			_process.WaitForExit(WaitForExitTimeout);

			lock (_asanResult)
			{
				_data = Asan.AsanToMonitorData(null, _asanResult.ToString());
			}

			return true;
		}

		MonitorData MakeFault(string reason, string title)
		{
			return new MonitorData
			{
				Title = title,
				Data = new Dictionary<string, Stream>(),
				Fault = new MonitorData.Info
				{
					MajorHash = Hash(Class + Executable),
					MinorHash = Hash(reason),
				}
			};
		}

		public override void SessionStarting()
		{
			if (StartOnCall == null && !RestartOnEachTest)
				_Start();
		}

		public override void SessionFinished()
		{
			_process.Stop(WaitForExitTimeout);
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			_data = null;
			_messageExit = false;

			if ((RestartAfterFault && args.LastWasFault) || RestartOnEachTest)
				_process.Stop(WaitForExitTimeout);

			if (StartOnCall == null)
				_Start();
		}

		public override bool DetectedFault()
		{
			// NOTE: Always check ASAN first.
			// ASAN faults trump all other early-exit faults
			if (_CheckAsan())
				return true;

			if (!_messageExit && FaultOnEarlyExit && !_process.IsRunning)
			{
				_data = MakeFault("ExitedEarly", "Process '{0}' exited early.".Fmt(Executable));
				_process.Stop(WaitForExitTimeout);
			}
			else  if (StartOnCall != null)
			{
				if (!NoCpuKill)
					_process.WaitForIdle(WaitForExitTimeout);
				else
					_process.WaitForExit(WaitForExitTimeout);
			}
			else if (RestartOnEachTest)
			{
				_process.Stop(WaitForExitTimeout);
			}

			return _data != null;
		}

		public override MonitorData GetMonitorData()
		{
			return _data;
		}

		public override void IterationFinished()
		{
		}

		public override void Message(string msg)
		{
			if (msg == StartOnCall)
			{
				_process.Stop(WaitForExitTimeout);
				_Start();
			}
			else if (msg == WaitForExitOnCall)
			{
				_messageExit = true; 
				if (!_process.WaitForExit(WaitForExitTimeout))
					_data = MakeFault("FailedToExit", "Process '{0}' did not exit in {1}ms.".Fmt(Executable, WaitForExitTimeout));
			}
		}
	}
}
