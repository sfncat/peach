using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Test;

namespace Peach.Pro.Test.OS.Windows.Agent.Monitors
{
	[TestFixture]
	[Quick]
	[Peach]
	[Platform("Win")]
	public class WindowsDebuggerHybridTest
	{
		private static readonly string CrashingFileConsumer = Utilities.GetAppResourcePath("CrashingFileConsumer.exe");
		private static readonly string CrashableServer = Utilities.GetAppResourcePath("CrashableServer.exe");

		[SetUp]
		public void SetUp()
		{
			if (!Environment.Is64BitProcess && Environment.Is64BitOperatingSystem)
				Assert.Ignore("Cannot run the 32bit version of this test on a 64bit operating system.");

			if (Environment.Is64BitProcess && !Environment.Is64BitOperatingSystem)
				Assert.Ignore("Cannot run the 64bit version of this test on a 32bit operating system.");
		}

		private static Fault[] RunEngine(string mutator, RunConfiguration cfg)
		{
			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String value='Hello'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Agent name='LocalAgent'>
		<Monitor class='WindowsDebugger'>
			<Param name='Executable' value='{0}'/>
			<Param name='Arguments' value='127.0.0.1 44444'/>
		</Monitor>
	</Agent>

	<Test name='Default' targetLifetime='iteration'>
		<Agent ref='LocalAgent'/>
		<StateModel ref='TheState'/>
		<Publisher class='Tcp'>
			<Param name='Host' value='127.0.0.1'/>
			<Param name='Port' value='44444'/>
		</Publisher>
		<Strategy class='Sequential'/>
	</Test>
</Peach>".Fmt(CrashableServer);

			var dom = DataModelCollector.ParsePit(xml);

			dom.tests[0].includedMutators = new List<string>(new[] { mutator });

			var e = new Engine(null);
			var ret = new List<Fault>();

			e.Fault += (context, currentIteration, stateModel, faults) =>
			{
				Assert.AreEqual(0, ret.Count);
				Assert.True(context.reproducingFault);
				Assert.AreEqual(330, context.reproducingInitialIteration);
				ret.AddRange(faults);
			};

			e.startFuzzing(dom, cfg);

			return ret.ToArray();
		}

		[Test]
		public void TestNoFault()
		{
			var faults = RunEngine("StringCaseMutator", new RunConfiguration());

			Assert.NotNull(faults);
			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void TestFault()
		{
			var cfg = new RunConfiguration
			{
				range = true,
				rangeStart = 330,
				rangeStop = 330
			};

			var faults = RunEngine("StringLengthEdgeCase", cfg);

			Assert.NotNull(faults);
			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual(FaultType.Fault, faults[0].type);
			Assert.AreEqual("WindowsDebugEngine", faults[0].detectionSource);
		}

		[Test]
		[TestCase(true)]
		[TestCase(false)]
		[Timeout(10000)]
		public void TestWaitForExitFault(bool replay)
		{
			var startCount = 0;

			var runner = new MonitorRunner("WindowsDebugger", new Dictionary<string, string>
			{
				{ "Executable", CrashableServer },
				{ "Arguments", "127.0.0.1 0" },
				{ "StartOnCall", "foo" },
				{ "WaitForExitOnCall", "bar" },
				{ "WaitForExitTimeout", "100" },
			})
			{
				StartMonitor = (m, args) =>
				{
					m.InternalEvent += (s, e) => ++startCount;
					m.StartMonitor(args);
				},
				IterationStarting = (m, args) =>
				{
					m.IterationStarting(new IterationStartingArgs { IsReproduction = replay, LastWasFault = args.LastWasFault });
				},
				Message = m =>
				{
					m.Message("foo");
					m.Message("bar");
				}
			};

			var f = runner.Run(5);

			Assert.AreEqual(5, f.Length);
			Assert.AreEqual(5, startCount);

			foreach (var item in f)
			{
				Assert.AreEqual("Process did not exit in 100ms.", item.Title);
			}
		}

		[Test]
		[TestCase(true)]
		[TestCase(false)]
		[Timeout(10000)]
		public void TestExitEarlyFault1(bool replay)
		{
			// FaultOnEarlyExit doesn't fault when stop message is sent

			var startCount = 0;

			var runner = new MonitorRunner("WindowsDebugger", new Dictionary<string, string>
			{
				{ "Executable", CrashingFileConsumer },
				{ "StartOnCall", "foo" },
				{ "WaitForExitOnCall", "bar" },
				{ "FaultOnEarlyExit", "true" },
			})
			{
				StartMonitor = (m, args) =>
				{
					m.InternalEvent += (s, e) => ++startCount;
					m.StartMonitor(args);
				},
				IterationStarting = (m, args) =>
				{
					m.IterationStarting(new IterationStartingArgs { IsReproduction = replay, LastWasFault = args.LastWasFault });
				},
				Message = m =>
				{
					m.Message("foo");
					m.Message("bar");
				}
			};

			var f = runner.Run(5);

			Assert.AreEqual(0, f.Length);
			Assert.AreEqual(5, startCount);
		}

		[Test]
		[TestCase(true)]
		[TestCase(false)]
		[Timeout(20000)]
		public void TestExitEarlyFault2(bool replay)
		{
			// FaultOnEarlyExit faults when StartOnCall is used and stop message is not sent

			var startCount = 0;

			var runner = new MonitorRunner("WindowsDebugger", new Dictionary<string, string>
			{
				{ "Executable", CrashingFileConsumer },
				{ "StartOnCall", "foo" },
				{ "FaultOnEarlyExit", "true" },
			})
			{
				StartMonitor = (m, args) =>
				{
					m.InternalEvent += (s, e) => ++startCount;
					m.StartMonitor(args);
				},
				IterationStarting = (m, args) =>
				{
					m.IterationStarting(new IterationStartingArgs { IsReproduction = replay, LastWasFault = args.LastWasFault });
				},
				Message = m =>
				{
					m.Message("foo");
					Thread.Sleep(1000);
				}
			};

			var f = runner.Run(5);

			Assert.AreEqual(5, f.Length);
			Assert.AreEqual(5, startCount);

			foreach (var item in f)
			{
				Assert.AreEqual("Process exited early.", item.Title);
			}
		}

		[Test]
		[TestCase(true)]
		[TestCase(false)]
		[Timeout(10000)]
		public void TestExitEarlyFault3(bool replay)
		{
			// FaultOnEarlyExit doesn't fault when StartOnCall is used

			var startCount = 0;

			var runner = new MonitorRunner("WindowsDebugger", new Dictionary<string, string>
			{
				{ "Executable", CrashableServer },
				{ "Arguments", "127.0.0.1 0" },
				{ "StartOnCall", "foo" },
				{ "FaultOnEarlyExit", "true" },
			})
			{
				StartMonitor = (m, args) =>
				{
					m.InternalEvent += (s, e) => ++startCount;
					m.StartMonitor(args);
				},
				IterationStarting = (m, args) =>
				{
					m.IterationStarting(new IterationStartingArgs { IsReproduction = replay, LastWasFault = args.LastWasFault });
				},
				Message = m =>
				{
					m.Message("foo");
				}
			};

			var f = runner.Run(5);

			Assert.AreEqual(0, f.Length);
			Assert.AreEqual(5, startCount);
		}

		[Test]
		[TestCase(true)]
		[TestCase(false)]
		[Timeout(10000)]
		public void TestExitEarlyFault4(bool replay)
		{
			// FaultOnEarlyExit doesn't fault when restart every iteration is true

			var startCount = 0;

			var runner = new MonitorRunner("WindowsDebugger", new Dictionary<string, string>
			{
				{ "Executable", CrashableServer },
				{ "Arguments", "127.0.0.1 0" },
				{ "RestartOnEachTest", "true" },
				{ "FaultOnEarlyExit", "true" },
			})
			{
				StartMonitor = (m, args) =>
				{
					m.InternalEvent += (s, e) => ++startCount;
					m.StartMonitor(args);
				},
				IterationStarting = (m, args) =>
				{
					m.IterationStarting(new IterationStartingArgs { IsReproduction = replay, LastWasFault = args.LastWasFault });
				}
			};

			var f = runner.Run(5);

			Assert.AreEqual(0, f.Length);
			Assert.AreEqual(5, startCount);
		}

		[Test]
		[TestCase(true)]
		[TestCase(false)]
		[Timeout(10000)]
		public void TestRestartAfterFault(bool replay)
		{
			var startCount = 0;
			var iteration = 0;

			var runner = new MonitorRunner("WindowsDebugger", new Dictionary<string, string>
			{
				{ "Executable", CrashableServer },
				{ "Arguments", "127.0.0.1 0" },
				{ "RestartAfterFault", "true" },
			})
			{
				StartMonitor = (m, args) =>
				{
					m.InternalEvent += (s, e) => ++startCount;
					m.StartMonitor(args);
				},
				IterationStarting = (m, args) =>
				{
					m.IterationStarting(new IterationStartingArgs { IsReproduction = replay, LastWasFault = args.LastWasFault });
				},
				DetectedFault = m =>
				{
					Assert.False(m.DetectedFault(), "Should not have detected a fault");

					return ++iteration == 2;
				}
			};

			var f = runner.Run(5);

			Assert.AreEqual(0, f.Length);

			Assert.AreEqual(!replay ? 2 : 5, startCount);
		}

		[Test]
		[TestCase(true)]
		[TestCase(false)]
		public void TestCpuKill(bool replay)
		{
			var runner = new MonitorRunner("WindowsDebugger", new Dictionary<string, string>
			{
				{ "Executable", CrashableServer },
				{ "Arguments", "127.0.0.1 0" },
				{ "StartOnCall", "ScoobySnacks" },
			})
			{
				IterationStarting = (m, args) =>
				{
					m.IterationStarting(new IterationStartingArgs { IsReproduction = replay, LastWasFault = args.LastWasFault });
				},
				Message = m =>
				{
					m.Message("ScoobySnacks");
				},
				IterationFinished = m =>
				{
					var sw = Stopwatch.StartNew();

					m.IterationFinished();

					var elapsed = sw.Elapsed;

					Assert.Less(elapsed, TimeSpan.FromSeconds(1));
				}
			};

			var f = runner.Run(1);

			Assert.AreEqual(0, f.Length);
		}
	}
}
