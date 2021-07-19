using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Pro.Core.Agent.Monitors.Utilities;
using Monitor = Peach.Core.Agent.Monitor2;

namespace Peach.Pro.Core.Agent.Monitors
{
	[Monitor("RunCommand")]
	[Description("Launches the specified command to perform a utility function")]
	[Parameter("Command", typeof(string), "Command line command to run")]
	[Parameter("Arguments", typeof(string), "Optional command line arguments", "")]
	[Parameter("When", typeof(MonitorWhen), "Period _When the command should be ran (OnCall, OnStart, OnEnd, OnIterationStart, OnIterationEnd, OnFault, OnIterationStartAfterFault)", "OnCall")]
	[Parameter("StartOnCall", typeof(string), "Run when signaled by the state machine", "")]
	[Parameter("FaultOnExitCode", typeof(bool), "Fault when FaultExitCode matches exit code", "false")]
	[Parameter("FaultExitCode", typeof(int), "Exit code to fault on", "1")]
	[Parameter("FaultOnNonZeroExit", typeof(bool), "Fault if exit code is non-zero", "false")]
	[Parameter("FaultOnRegex", typeof(string), "Fault if regex matches", "")]
	[Parameter("Timeout", typeof(int), "Fault if process takes more than 'Timeout' milliseconds to exit, where -1 means infinite timeout", "-1")]
	[Parameter("WorkingDirectory", typeof(string), "Working directory to set when running command", "")]
	public class RunCommand  : Monitor
	{
		public string Command { get; set; }
		public string Arguments { get; set; }
		public string StartOnCall { get; set; }
		public MonitorWhen When { get; set; }
		public int Timeout { get; set; }
		public bool FaultOnNonZeroExit { get; set; }
		public int FaultExitCode { get; set; }
		public bool FaultOnExitCode { get; set; }
		public string FaultOnRegex { get; set; }
		public string WorkingDirectory { get; set; }

		private Regex _faultOnRegex;
		private MonitorData _data;

		public RunCommand(string name)
			: base(name)
		{
		}

		public override void StartMonitor(Dictionary<string, string> args)
		{
			base.StartMonitor(args);

			if (!string.IsNullOrWhiteSpace(FaultOnRegex))
				_faultOnRegex = new Regex(FaultOnRegex);
		}

		void _Start()
		{
			_data = null;

			try
			{
				if (!string.IsNullOrEmpty(WorkingDirectory) && !Directory.Exists(WorkingDirectory))
				{
					throw new PeachException(
						"Specified WorkingDirectory does not exist: '{0}'".Fmt(WorkingDirectory)
					);
				}
				
				var result = ProcessHelper.Run(Command, Arguments, null, WorkingDirectory, Timeout);

				var stdout = result.StdOut.ToString();
				var stderr = result.StdErr.ToString();

				if (Asan.CheckForAsanFault(stderr))
				{
					_data = Asan.AsanToMonitorData(stdout, stderr);
					return;
				}

				_data = new MonitorData
				{
					Data = new Dictionary<string, Stream>()
				};

				_data.Data.Add("stdout", new MemoryStream(Encoding.UTF8.GetBytes(stdout)));
				_data.Data.Add("stderr", new MemoryStream(Encoding.UTF8.GetBytes(stderr)));

				if (_faultOnRegex != null)
				{
					var m = _faultOnRegex.Match(stdout);

					if (m.Success)
					{
						_data.Title = "Process stdout matched FaultOnRegex \"{0}\".".Fmt(FaultOnRegex);
						_data.Fault = new MonitorData.Info
						{
							Description = stdout,
							MajorHash = Hash(Class + Command),
							MinorHash = Hash(m.Value)
						};
					}
					else
					{
						m = _faultOnRegex.Match(stderr);

						if (m.Success)
						{
							_data.Title = "Process stderr matched FaultOnRegex \"{0}\".".Fmt(FaultOnRegex);
							_data.Fault = new MonitorData.Info
							{
								Description = stderr,
								MajorHash = Hash(Class + Command),
								MinorHash = Hash(m.Value)
							};
						}
					}
				}
				else if (result.Timeout)
				{
					_data.Title = "Process failed to exit in allotted time.";
					_data.Fault = new MonitorData.Info
					{
						MajorHash = Hash(Class + Command),
						MinorHash = Hash("FailedToExit")
					};
				}
				else if (FaultOnExitCode && result.ExitCode == FaultExitCode)
				{
					_data.Title = "Process exited with code {0}.".Fmt(result.ExitCode);
					_data.Fault = new MonitorData.Info
					{
						MajorHash = Hash(Class + Command),
						MinorHash = Hash(result.ExitCode.ToString(CultureInfo.InvariantCulture))
					};
				}
				else if (FaultOnNonZeroExit && result.ExitCode != 0)
				{
					_data.Title = "Process exited with code {0}.".Fmt(result.ExitCode);
					_data.Fault = new MonitorData.Info
					{
						MajorHash = Hash(Class + Command),
						MinorHash = Hash(result.ExitCode.ToString(CultureInfo.InvariantCulture))
					};
				}
			}
			catch (Exception ex)
			{
				throw new PeachException("RunCommand ({0}) failed to run command: '{1} {2}'. {3}.".Fmt(
					Name,
					Command, 
					Arguments,
					ex.Message
				), ex);
			}
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			if (When == MonitorWhen.OnIterationStart ||
				(args.LastWasFault && When == MonitorWhen.OnIterationStartAfterFault))
				_Start();
		}

		public override bool DetectedFault()
		{
			return _data != null && _data.Fault != null;
		}

		public override MonitorData GetMonitorData()
		{
			if (When == MonitorWhen.OnFault)
				_Start();

			return _data;
		}

		public override void SessionStarting()
		{
			if (When == MonitorWhen.OnStart)
				_Start();
		}

		public override void SessionFinished()
		{
			if (When == MonitorWhen.OnEnd)
				_Start();
		}

		public override void IterationFinished()
		{
			if (When == MonitorWhen.OnIterationEnd)
				_Start();
		}

		public override void Message(string msg)
		{
			if (msg == StartOnCall && When == MonitorWhen.OnCall)
				_Start();
		}
	}
}
