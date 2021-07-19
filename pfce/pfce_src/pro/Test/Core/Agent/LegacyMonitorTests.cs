using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using NLog;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Agent;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

// Disable obsolete warnings
using Peach.Core.Test;

#pragma warning disable 618

namespace Peach.Pro.Test.Core.Agent
{
	[TestFixture]
	[Quick]
	[Peach]
	class LegacyMonitorTests
	{
		static readonly string Target = Platform.GetOS() == Platform.OS.Windows ? "ipconfig" : "ifconfig";

		[Test]
		public void VerifyNormal()
		{
			// Ensure monitors that derive from Peach.Core.Agent.Monitor work
			// as expected even though the api has been deprecated.

			var xml = @"
<Peach>
	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='call' method='Foo' publisher='Peach.Agent' />
		</State>
	</StateModel>

	<Agent name='LocalAgent'>
		<Monitor class='LegacyRunCommand'>
			<Param name='StartOnCall' value='Foo' />
			<Param name='When' value='OnCall' />
			<Param name='Command' value='{0}' />
		</Monitor>
	</Agent>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Agent ref='LocalAgent' />
		<Publisher class='Null' />
	</Test>
</Peach>
".Fmt(Target);

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);

			e.startFuzzing(dom, cfg);
		}

		[Test]
		public void VerifyBadParams()
		{
			// Ensure monitors that derive from Peach.Core.Agent.Monitor work
			// as expected even though the api has been deprecated.

			const string xml = @"
<Peach>
	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='call' method='Foo' publisher='Peach.Agent' />
		</State>
	</StateModel>

	<Agent name='LocalAgent'>
		<Monitor class='LegacyRunCommand' />
	</Agent>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Agent ref='LocalAgent' />
		<Publisher class='Null' />
	</Test>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);

			var pe = Assert.Throws<PeachException>(() => e.startFuzzing(dom, cfg));

			Assert.AreEqual("Could not start monitor \"LegacyRunCommand\".  Monitor 'LegacyRunCommand' is missing required parameter 'Command'.", pe.Message);
		}

		[Test]
		public void VerifyFault()
		{
			// Ensure monitors that derive from Peach.Core.Agent.Monitor work
			// as expected even though the api has been deprecated.

			var xml = @"
<Peach>
	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='call' method='Foo' publisher='Peach.Agent' />
		</State>
	</StateModel>

	<Agent name='LocalAgent'>
		<Monitor class='LegacyRunCommand'>
			<Param name='StartOnCall' value='Foo' />
			<Param name='When' value='OnCall' />
			<Param name='Command' value='{0}' />
			<Param name='FaultOnRegex' value='.*' />
		</Monitor>
	</Agent>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Agent ref='LocalAgent' />
		<Publisher class='Null' />
	</Test>
</Peach>
".Fmt(Target);

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);
			var faults = new List<Fault>();

			e.Fault += (ctx, it, sm, f) => faults.AddRange(f);

			var pe = Assert.Throws<PeachException>(() =>  e.startFuzzing(dom, cfg));

			Assert.AreEqual("Fault detected on control recording iteration.", pe.Message);
			Assert.AreEqual(1, faults.Count);
			Assert.AreEqual(FaultType.Fault, faults[0].type);
			Assert.That(faults[0].title, Is.StringStarting("Process output matched FaulOnRegex"));
			Assert.AreEqual(2, faults[0].collectedData.Count);

			var d0 = faults[0].collectedData[0];
			Assert.AreEqual("stdout", d0.Key);
			Assert.Greater(d0.Value.Length, 0);

			var d1 = faults[0].collectedData[1];
			Assert.AreEqual("stderr", d1.Key);
			Assert.AreEqual(d1.Value.Length, 0);
		}
	}

	[Monitor("LegacyRunCommand", true, IsTest = true)]
	[Description("Launches the specified command to perform a utility function")]
	[Parameter("Command", typeof(string), "Command line command to run")]
	[Parameter("Arguments", typeof(string), "Optional command line arguments", "")]
	[Parameter("When", typeof(When), "Period _When the command should be ran (OnCall, OnStart, OnEnd, OnIterationStart, OnIterationEnd, OnFault, OnIterationStartAfterFault)", "OnCall")]
	[Parameter("StartOnCall", typeof(string), "Run when signaled by the state machine", "")]
	[Parameter("FaultOnExitCode", typeof(bool), "Fault when FaultExitCode matches exit code", "false")]
	[Parameter("FaultExitCode", typeof(int), "Exit code to fault on", "1")]
	[Parameter("FaultOnNonZeroExit", typeof(bool), "Fault if exit code is non-zero", "false")]
	[Parameter("FaultOnRegex", typeof(string), "Fault if regex matches", "")]
	[Parameter("AddressSanitizer", typeof(bool), "Enable Google AddressSanitizer support", "false")]
	[Parameter("Timeout", typeof(int), "Fault if process takes more than Timeout seconds where -1 is infinite timeout ", "-1")]
	public class LegacyRunCommand  : Monitor
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public string Command { get; private set; }
		public string Arguments { get; private set; }
		public string StartOnCall { get; private set; }
		public When _When { get; private set; }
		public string CheckValue { get; protected set; }
		public int Timeout { get; private set; }
		public bool FaultOnNonZeroExit { get; protected set; }
		public int FaultExitCode { get; protected set; }
		public bool FaultOnExitCode { get; protected set; }
		public string FaultOnRegex { get; protected set; }
		public bool AddressSanitizer { get; protected set; }

		readonly Regex asanMatch = new Regex(@"==\d+==ERROR: AddressSanitizer:");
		readonly Regex asanBucket = new Regex(@"==\d+==ERROR: AddressSanitizer: ([^\s]+) on address ([0-9a-z]+) at pc ([0-9a-z]+)");
		readonly Regex asanMessage = new Regex(@"(==\d+==ERROR: AddressSanitizer:.*==\d+==ABORTING)");
		readonly Regex asanTitle = new Regex(@"==\d+==ERROR: AddressSanitizer: ([^\r\n]+)");

		Regex _faulOnRegex = null;

		private Fault _fault = null;
		private bool _lastWasFault = false;

		public LegacyRunCommand(IAgent agent, string name, Dictionary<string, Variant> args)
			: base(agent, name, args)
		{
			ParameterParser.Parse(this, args);

			if (!string.IsNullOrWhiteSpace(FaultOnRegex))
				_faulOnRegex = new Regex(FaultOnRegex);
		}

		void _Start()
		{
			_fault = null;

			var startInfo = new ProcessStartInfo();
			startInfo.FileName = Command;
			startInfo.UseShellExecute = false;
			startInfo.Arguments = Arguments;
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;

			logger.Debug("_Start(): Running command " + Command + " with arguments " + Arguments);

			try
			{
				var p = ProcessHelper.Run(Command, Arguments, null, null, Timeout);

				var stdout = p.StdOut.ToString();
				var stderr = p.StdErr.ToString();

				_fault = new Fault();
				_fault.detectionSource = "RunCommand";
				_fault.folderName = "RunCommand";
				_fault.collectedData.Add(new Fault.Data("stdout", System.Text.Encoding.ASCII.GetBytes(stdout)));
				_fault.collectedData.Add(new Fault.Data("stderr", System.Text.Encoding.ASCII.GetBytes(stderr)));

				if (p.Timeout)
				{
					_fault.title = _fault.description = "Process failed to exit in allotted time.";
					_fault.type = FaultType.Fault;
				}
				else if (FaultOnExitCode && p.ExitCode == FaultExitCode)
				{
					_fault.title = _fault.description = "Process exited with code {0}.".Fmt(p.ExitCode);
					_fault.type = FaultType.Fault;
				}
				else if (FaultOnNonZeroExit && p.ExitCode != 0)
				{
					_fault.title = _fault.description = "Process exited with code {0}.".Fmt(p.ExitCode);
					_fault.type = FaultType.Fault;
				}
				else if (_faulOnRegex != null)
				{
					if (_faulOnRegex.Match(stdout).Success)
					{
						_fault.title = _fault.description = "Process output matched FaulOnRegex \"{0}\".".Fmt(FaultOnRegex);
						_fault.type = FaultType.Fault;
					}
					else if (_faulOnRegex.Match(stderr).Success)
					{
						_fault.title = _fault.description = "Process error output matched FaulOnRegex \"{0}\".".Fmt(FaultOnRegex);
						_fault.type = FaultType.Fault;
					}
				}
				else if (AddressSanitizer && asanMatch.IsMatch(stderr))
				{
					_fault.type = FaultType.Fault;
					_fault.folderName = null;

					var match = asanBucket.Match(stderr);
					_fault.exploitability = match.Groups[1].Value;
					_fault.majorHash = match.Groups[3].Value;
					_fault.minorHash = match.Groups[2].Value;

					match = asanTitle.Match(stderr);
					_fault.title = match.Groups[1].Value;

					match = asanMessage.Match(stderr);
					_fault.description = stderr.Substring(match.Groups[1].Index, match.Groups[1].Length);
				}
				else
				{
					_fault.description = stdout;
					_fault.type = FaultType.Data;
				}
			}
			catch (Exception ex)
			{
				throw new PeachException("Could not run command '" + Command + "'.  " + ex.Message + ".", ex);
			}
		}

		public override void IterationStarting(uint iterationCount, bool isReproduction)
		{
			// Sync _lastWasFault incase _Start() throws
			bool lastWasFault = _lastWasFault;
			_lastWasFault = false;

			if (_When == When.OnIterationStart || (lastWasFault && _When == When.OnIterationStartAfterFault))
				_Start();
		}

		public override bool DetectedFault()
		{
			return _fault != null && _fault.type == FaultType.Fault;
		}

		public override Fault GetMonitorData()
		{
			// Some monitor triggered a fault
			_lastWasFault = true;

			if (_When == When.OnFault)
				_Start();

			return _fault;
		}

		public override bool MustStop()
		{
			return false;
		}

		public override void StopMonitor()
		{
			return;
		}

		public override void SessionStarting()
		{
			if (_When == When.OnStart)
				_Start();
		}

		public override void SessionFinished()
		{
			if (_When == When.OnEnd)
				_Start();
		}

		public override bool IterationFinished()
		{
			if (_When == When.OnIterationEnd)
				_Start();

			return true;
		}

		public override Variant Message(string name, Variant data)
		{
			if (name == "Action.Call" && ((string)data) == StartOnCall && _When == When.OnCall)
				_Start();

			return null;
		}
	}
}
