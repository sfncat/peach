using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Peach]
	[Quick]
	class ReproSessionTests
	{
		class Args
		{
			public Args()
			{
				RangeStop = 10;

				ControlIterations = 0;
				SwitchCount = uint.MaxValue;

				WaitTime = 0.0;
				FaultWaitTime = 0.0;

				InjectFault = true;
			}

			public uint RangeStop { get; set; }

			public uint SwitchCount { get; set; }
			public uint ControlIterations { get; set; }

			public double WaitTime { get; set; }
			public double FaultWaitTime { get; set; }

			public string Initial { get; set; }
			public string InitialRepro { get; set; }
			public string Fault { get; set; }
			public string Repro { get; set; }

			public bool FaultOnControl { get; set; }
			public bool SoftException { get; set; }
			public bool InjectFault { get; set; }

			public uint? MaxBackSearch { get; set; }
		}

		private readonly List<TimeSpan> _waitTimes = new List<TimeSpan>();
		private readonly List<string> _faults = new List<string>();
		private readonly List<string> history = new List<string>();


		[SetUp]
		public void SetUp()
		{
			_waitTimes.Clear();
			_faults.Clear();
			history.Clear();
		}


		private string Run(Args args)
		{
			var max = args.MaxBackSearch.HasValue ? "maxBackSearch='{0}'".Fmt(args.MaxBackSearch) : "";

			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String name='str'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
				<Data>
					<Field name='str' value='Hello'/>
				</Data>
				<Data>
					<Field name='str' value='World'/>
				</Data>
			</Action>
		</State>
	</StateModel>

	<!-- Need an agent to measure time between iteration finished and detected fault -->
	<Agent name='LocalAgent' />

	<Test name='Default' waitTime='{0}' faultWaitTime='{1}' controlIteration='{2}' {3}>
		<Agent ref='LocalAgent'/>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Strategy class='Random'>
			<Param name='SwitchCount' value='{4}'/>
		</Strategy>
	</Test>
</Peach>
".Fmt(args.WaitTime, args.FaultWaitTime, args.ControlIterations, max, args.SwitchCount);

			var dom = DataModelCollector.ParsePit(xml);

			var config = new RunConfiguration
			{
				range = true,
				rangeStart = 0,
				rangeStop = args.RangeStop,
			};


			var e = new Engine(null);

			var sw = new Stopwatch();

			e.TestStarting += ctx =>
			{
				ctx.DetectedFault += (c, agent) => _waitTimes.Add(sw.Elapsed);

				var trigger = new Action<RunContext, bool, Action>((c, add, fn) =>
				{
					var it = ctx.currentIteration;

					string i;

					if (ctx.controlRecordingIteration)
						i = "R" + it;
					else if (ctx.controlIteration)
						i = "C" + it;
					else
						i = it.ToString(CultureInfo.InvariantCulture);

					if (add)
						history.Add(i);

					var j = "{0},{1}".Fmt(i, history.Count(h => h == i));

					var fault = false;

					if (!ctx.reproducingFault && (i == args.Fault || j == args.Fault || i == args.Initial || j == args.Initial))
						fault = true;
					else if (ctx.reproducingFault && (i == args.Repro || j == args.Repro || i == args.InitialRepro || j == args.InitialRepro))
						fault = true;

					if (fault)
						fn();
				});

				ctx.StateModelStarting += (c, sm) =>
				{
					trigger(c, true, () =>
					{
						if (args.SoftException)
							throw new SoftException("Simulated SoftException");
					});
				};
				

				if (args.InjectFault)
				{
					ctx.DetectedFault += (c, agent) =>
					{
						trigger(c, false, c.InjectFault);
					};
				}

				ctx.IterationFinished += (c, it) =>
				{
					sw.Restart();
				};

				ctx.IterationStarting += (c, agent) =>
				{
					if (c.FaultOnPreviousIteration)
						history.Add("LastWasFault");
				};
			};

			e.Fault += (ctx, it, sm, fault) =>
			{
				history.Add("Fault");
				_faults.Add(fault.Where(f => f.type == FaultType.Fault).Select(f => f.title).First());

				if (!args.SoftException)
					Assert.AreEqual(1, fault.Length);
				Assert.LessOrEqual(it, ctx.reproducingInitialIteration);
				Assert.GreaterOrEqual(ctx.reproducingIterationJumpCount, 0);

			};
			e.ReproFault += (ctx, it, sm, fault) =>
			{
				history.Add("ReproFault");

				if (!args.SoftException)
					Assert.AreEqual(1, fault.Length);
				Assert.AreEqual(it, ctx.reproducingInitialIteration);
				Assert.AreEqual(0, ctx.reproducingIterationJumpCount);
			};

			e.ReproFailed += (ctx, it) =>
			{
				history.Add("ReproFailed");

				Assert.AreEqual(it, ctx.reproducingInitialIteration);
				Assert.Greater(ctx.reproducingIterationJumpCount, 0);
			};

			if (args.FaultOnControl)
			{
				var ex = Assert.Throws<PeachException>(() =>  e.startFuzzing(dom, config));
				Assert.AreEqual("Fault detected on control iteration.", ex.Message);
			}
			else
			{
				e.startFuzzing(dom, config);
			}

			return string.Join(" ", history);
		}

		[Test]
		public void TestSimpleRepro()
		{
			// Fuzz from 1 to 10
			// Fault on iteration 8
			// Repro on iteration 8

			var act = Run(new Args { Fault = "8", Repro = "8" });

			const string exp = "R1 1 2 3 4 5 6 7 8 ReproFault LastWasFault " +
			                   "C8 8 Fault LastWasFault C9 9 10";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestReproBack10()
		{
			// Fuzz from 1 to 15
			// Fault on iteration 12
			// Repro on iteration 11

			// On fault replays iteration 12
			// Jumps back 10 and goes forward until reproduces on 11
			// Resumes on iteration 13

			var act = Run(new Args { RangeStop = 15, Fault = "12", Repro = "11" });

			const string exp = "R1 1 2 3 4 5 6 7 8 9 10 11 12 ReproFault " +
			                   "LastWasFault C12 12 " +
			                   "LastWasFault C2 2 3 4 5 6 7 8 9 10 11 Fault "+
			                   "LastWasFault C13 13 14 15";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestReproBack5()
		{
			// Fuzz from 1 to 10
			// Fault on iteration 6
			// Repro on iteration 5

			// On fault replays iteration 6
			// Jumps back 5 and goes forward until reproduces on 5
			// Resumes on iteration 7

			var act = Run(new Args { Fault = "6", Repro = "5" });

			const string exp = "R1 1 2 3 4 5 6 ReproFault " +
			                   "LastWasFault C6 6 "+
			                   "LastWasFault C1 1 2 3 4 5 Fault "+
			                   "LastWasFault C7 7 8 9 10";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestReproLastFault()
		{
			// Fuzz from 1 to 20
			// Fault and repro on on iteration 4
			// Fault on iteration 18

			// On fault replays iteration 18
			// Jumps back 10 and fails to repro
			// Jumps back to iteration 5 (last fault was 4) and reproduces on iteration 7
			// Resumes on iteration 19

			var act = Run(new Args { RangeStop = 20, Fault = "18", Repro = "7", Initial = "4", InitialRepro = "4" });

			const string exp = "R1 1 2 3 4 ReproFault " +
			                   "LastWasFault C4 4 Fault " +
			                   "LastWasFault C5 5 6 7 8 9 10 11 12 13 14 15 16 17 18 ReproFault "+
			                   "LastWasFault C18 18 "+
			                   "LastWasFault C8 8 9 10 11 12 13 14 15 16 17 18 " +
			                   "LastWasFault C5 5 6 7 Fault " +
			                   "LastWasFault C19 19 20";

			Assert.AreEqual(exp, act);
		}


		[Test]
		public void TestNoReproBack5()
		{
			// Fuzz from 1 to 10
			// Fault on iteration 6
			// Never reproduce

			// On fault replays iteration 6
			// Jumps back 5 and goes forward until runs iteration 6 and says repro failed
			// Resumes on iteration 7

			var act = Run(new Args { Fault = "6" });

			const string exp = "R1 1 2 3 4 5 6 ReproFault " +
			                   "LastWasFault C6 6 " +
			                   "LastWasFault C1 1 2 3 4 5 6 ReproFailed " +
			                   "7 8 9 10";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestReproBackDouble()
		{
			// Fuzz from 1 to 25
			// Fault on iteration 23
			// Reproduce on iteration 10

			// On fault replays iteration 23
			// Jumps back 10, then jumps back 2 * 10
			// Resumes on iteration 24

			var act = Run(new Args { RangeStop = 25, Fault = "23", Repro = "10" });

			var exp = "R1";

			for (var i = 1; i <= 23; ++i)
				exp += " " + i;

			exp += " ReproFault LastWasFault C23 23";

			exp += " LastWasFault C13";
			for (var i = 13; i <= 23; ++i)
				exp += " " + i;

			exp += " LastWasFault C3";
			for (var i = 3; i <= 10; ++i)
				exp += " " + i;

			exp += " Fault LastWasFault C24 24 25";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestMaxBackSearch500()
		{
			// Set MaxBackSearch to 500
			// Fuzz from iteration 1 to 550
			// Fault on iteration 540 without repro
			// Should run 540, 530-540, keep doubling backlog until it maxes at 500

			var act = Run(new Args { RangeStop = 550, Fault = "540", MaxBackSearch = 500 });

			var sb = new StringBuilder();
			sb.Append("R1");

			for (var i = 1; i <= 540; ++i)
				sb.AppendFormat(" {0}", i);

			sb.Append(" ReproFault LastWasFault C540 540");

			for (var i = 10; i <= 640; i *= 2)
			{
				var start = 540 - Math.Min(i, 500);
				sb.AppendFormat(" LastWasFault C{0}", start);

				for (var j = start; j <= 540; ++j)
					sb.AppendFormat(" {0}", j);
			}

			sb.Append(" ReproFailed");

			for (var i = 541; i <= 550; ++i)
				sb.AppendFormat(" {0}", i);

			var exp = sb.ToString();
			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestMaxBackSearchDefault()
		{
			// Set MaxBackSearch to 500
			// Fuzz from iteration 1 to 550
			// Fault on iteration 540 without repro
			// Should run 540, 530-540, keep doubling backlog until it maxes at default of 80

			var act = Run(new Args { RangeStop = 550, Fault = "540" });

			var sb = new StringBuilder();
			sb.Append("R1");

			for (var i = 1; i <= 540; ++i)
				sb.AppendFormat(" {0}", i);

			sb.Append(" ReproFault LastWasFault C540 540");

			for (var i = 10; i <= 80; i *= 2)
			{
				var start = 540 - i;
				sb.AppendFormat(" LastWasFault C{0}", start);

				for (var j = start; j <= 540; ++j)
					sb.AppendFormat(" {0}", j);
			}

			sb.Append(" ReproFailed");

			for (var i = 541; i <= 550; ++i)
				sb.AppendFormat(" {0}", i);

			var exp = sb.ToString();
			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestWaitTime()
		{
			// Fuzz from 1 to 25
			// Fault on iteration 23
			// Reproduce on iteration 10

			// On fault replays iteration 23
			// Jumps back 10, then jumps back 2 * 10
			// Resumes on iteration 24

			var act = Run(new Args { RangeStop = 25, Fault = "23", WaitTime = 0.1 });

			var exp = "R1";

			for (var i = 1; i <= 23; ++i)
				exp += " " + i;

			exp += " ReproFault LastWasFault C23 23";

			exp += " LastWasFault C13";
			for (var i = 13; i <= 23; ++i)
				exp += " " + i;

			exp += " LastWasFault C3";
			for (var i = 3; i <= 23; ++i)
				exp += " " + i;

			exp += " LastWasFault C1";
			for (var i = 1; i <= 23; ++i)
				exp += " " + i;

			exp += " ReproFailed 24 25";

			Assert.AreEqual(exp, act);

			// R1, 1..23, C23, 23, C23, 13..23, C3, 3..23, C1, 1..25
			const int iterations = 1 + 23 + 2 + 1 + 11 + 1 + 21 + 1 + 25;
			Assert.AreEqual(iterations, _waitTimes.Count);

			foreach (var ts in _waitTimes)
			{
				Assert.GreaterOrEqual(ts, TimeSpan.FromMilliseconds(80));
				Assert.LessOrEqual(ts, TimeSpan.FromMilliseconds(180));
			}
		}

		[Test]
		public void TestFaultWaitTime()
		{
			// Fuzz from 1 to 25
			// Fault on iteration 23
			// Reproduce on iteration 10

			// On fault replays iteration 23
			// Jumps back 10, then jumps back 2 * 10
			// Resumes on iteration 24

			var act = Run(new Args { RangeStop = 25, Fault = "23", FaultWaitTime = 0.1 });
			var times = new List<int>();

			var exp = "R1";
			times.Add(0);

			for (var i = 1; i <= 23; ++i)
			{
				exp += " " + i;
				times.Add(0);
			}

			exp += " ReproFault LastWasFault C23 23";
			times.Add(100); // Waits FaultWaitTime after control
			times.Add(100); // Waits FaultWaitTime after 1st repro

			exp += " LastWasFault C13";
			times.Add(100);

			for (var i = 13; i <= 23; ++i)
			{
				exp += " " + i;
				times.Add(100); // Waits FaultWaitTime after each of 1st 10 repros
			}

			exp += " LastWasFault C3";
			times.Add(0);

			for (var i = 3; i <= 23; ++i)
			{
				exp += " " + i;
				times.Add(0); // Does not wait when doing scan past 10
			}

			times[times.Count - 1] = 100; // Waits FaultWaitTime after full sequence

			exp += " LastWasFault C1";
			times.Add(0);

			for (var i = 1; i <= 23; ++i)
			{
				exp += " " + i;
				times.Add(0); // Does not wait when doing scan past 10
			}

			times[times.Count - 1] = 100; // Waits FaultWaitTime after full sequence

			exp += " ReproFailed 24 25";

			times.Add(0);
			times.Add(0);

			Assert.AreEqual(exp, act);

			Assert.AreEqual(times.Count, _waitTimes.Count);

			for (var i = 0; i < times.Count; ++i)
			{
				Assert.GreaterOrEqual(_waitTimes[i], TimeSpan.FromMilliseconds(times[i] - 20));
				Assert.LessOrEqual(_waitTimes[i], TimeSpan.FromMilliseconds(times[i] + 50));
			}
		}

		[Test]
		public void TestDataSetSwitch()
		{
			// /If jumping backwards results in a data set switch
			// make sure it happens.

			// Fuzz from 1 to 25
			// Fault on iteration 23
			// Reproduce on iteration 10
			// SwitchCount is 15

			// On fault replays iteration 23
			// Jumps back 10, then jumps back 2 * 10
			// Resumes on iteration 24

			var act = Run(new Args { RangeStop = 25, Fault = "23", Repro = "7", SwitchCount = 15 });

			const string exp = "R1 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 " +
			                   // R16 -> switches to data set 2
			                   "R16 16 17 18 19 20 21 22 23 ReproFault LastWasFault C23 23 " +
			                   // R13 -> switches back to data set initially used
			                   "LastWasFault R13 13 14 15 " +
			                   // R16 -> switches to data set 2
			                   "R16 16 17 18 19 20 21 22 23 " +
			                   // R3 -> switches back to data set initially used
			                   "LastWasFault R3 3 4 5 6 7 Fault " +
			                   // R24 -> switches back to data set 2
			                   "LastWasFault R24 24 25";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestControlFault()
		{
			// Fault on control and immediatley reproduce

			// Fuzz from 1 to 10
			// Fault on iteration C6
			// Repro on iteration C6
			// Stop fuzzing after repro

			var act = Run(new Args { Fault = "C6", Repro = "C6", ControlIterations = 5, FaultOnControl = true });

			const string exp = "R1 1 2 3 4 5 C6 ReproFault LastWasFault C6 Fault";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestControlFaultReproSearchEnd()
		{
			// Fault on control and reproduce after searching

			// Fuzz from 1 to 10
			// Fault on iteration C6
			// Fail to repro initially
			// Run 1 C2 2 C3 3 C4 4 C6 5 C6
			// Repro on iteration C6

			var act = Run(new Args { Fault = "C6,1", Repro = "C6,3", ControlIterations = 5 });

			const string exp = "R1 1 2 3 4 5 C6 ReproFault " +
			                   "LastWasFault C6 1 C2 2 C3 3 C4 4 C5 5 C6 Fault " +
			                   "LastWasFault C6 6 7 8 9 10";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestControlFaultSearchMiddle()
		{
			// Fault on control and reproduce on non-control while searching prev 10

			// Fuzz from 1 to 10
			// Fault on iteration C6
			// Fail to repro initially
			// Run 1 C2 2 C3 3
			// Repro on iteration 3
			// Resume at 6 7 8 9 10

			var act = Run(new Args { Fault = "C6,1", Repro = "3", ControlIterations = 5 });

			const string exp = "R1 1 2 3 4 5 C6 ReproFault " +
			                   "LastWasFault C6 1 C2 2 C3 3 Fault " +
			                   "LastWasFault C6 6 7 8 9 10";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestControlFaultSearchControl()
		{
			// Fault on control and reproduce on control while searching prev 10

			// Fuzz from 1 to 10
			// Fault on iteration C6
			// Fail to repro initially
			// Run 1 C2 2 C3 3 C4
			// Repro on iteration C4
			// Resume at 6 7 8 9 10

			var act = Run(new Args { Fault = "C6,1", Repro = "C4", ControlIterations = 5 });

			const string exp = "R1 1 2 3 4 5 C6 ReproFault " +
			                   "LastWasFault C6 1 C2 2 C3 3 C4 Fault " +
			                   "LastWasFault C6 6 7 8 9 10";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestControlFaultNoRepro()
		{
			// Fault on control and never repro, verify control runs at correct times

			// Fuzz from 1 to 10
			// Fault on iteration C6
			// Fail to repro 
			// Run 1 C2 2 C3 3 C4 4 C5 5 C6
			// Repro Fail
			// Resume at 6 7 8 9 10

			var act = Run(new Args { Fault = "C6,1", ControlIterations = 5 });

			const string exp = "R1 1 2 3 4 5 C6 ReproFault " +
			                   "LastWasFault C6 1 C2 2 C3 3 C4 4 C5 5 C6 ReproFailed " +
			                   "6 7 8 9 10";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestNonControlFaultNoRepro()
		{
			// Fault on control and never repro, verify control runs at correct times

			// Fuzz from 1 to 10
			// Fault on iteration 6
			// Fail to repro 
			// Run 1 C2 2 C3 3 C4 4 C5 5 C6 6 C7
			// Repro Fail
			// Resume at 7 8 9 10

			var act = Run(new Args { Fault = "6", ControlIterations = 5 });

			const string exp = "R1 1 2 3 4 5 C6 6 ReproFault " +
			                   "LastWasFault C6 6 C6 " +
			                   "LastWasFault C1 1 C2 2 C3 3 C4 4 C5 5 C6 6 C6 ReproFailed 7 8 9 10";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestControlFaultBigSearchMiddle()
		{
			// Fault on control and reproduce while searching prev 20

			// Fuzz from 1 to 25
			// Fault on iteration C21
			// Fail to repro 
			// Run 11 C12 12 C13 13 C14 14 C15 15 C16 16 C17 17 C18 18 C19 19 C20 20 C21
			// Run 1..13
			// Repro on 13
			// Resume at 21 22 23 24 25

			var act = Run(new Args { RangeStop = 25, Fault = "C21,1", Repro = "13,3", ControlIterations = 5 });

			var exp =
				"R1 1 2 3 4 5 C6 6 7 8 9 10 C11 11 12 13 14 15 C16 16 17 18 19 20 C21 ReproFault " +
				"C21 11 C12 12 C13 13 C14 14 C15 15 C16 16 C17 17 C18 18 C19 19 C20 20 C21 ";

			for (var i = 1; i <= 13; ++i)
				exp += "{0} ".Fmt(i);

			exp += "Fault C21 21 22 23 24 25";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestNonControlFaultBigSearchMiddle()
		{
			// Fault on non-control and reproduce while searching prev 20

			// Fuzz from 1 to 25
			// Fault on iteration 21
			// Fail to repro 
			// Run 11 C12 12 C13 13 C14 14 C15 15 C16 16 C17 17 C18 18 C19 19 C20 20 C20
			// Run 1..13
			// Repro on 13
			// Resume at 21 22 23 24 25

			var act = Run(new Args { RangeStop = 25, Fault = "21", Repro = "13,3", ControlIterations = 5 });

			var exp =
				"R1 1 2 3 4 5 C6 6 7 8 9 10 C11 11 12 13 14 15 C16 16 17 18 19 20 C21 21 ReproFault " +
				"LastWasFault C21 21 C21 " +
				"LastWasFault C11 11 C12 12 C13 13 C14 14 C15 15 C16 16 C17 17 C18 18 C19 19 C20 20 C21 21 C21 ";

			exp += "LastWasFault C1 ";

			for (var i = 1; i <= 13; ++i)
				exp += "{0} ".Fmt(i);

			exp += "Fault LastWasFault C22 22 23 24 25";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestControlFaultBigNoRepro()
		{
			// Fault on control and never repro, verify control runs at correct times

			// Fuzz from 1 to 30
			// Fault on iteration C26
			// Never reproduces
			// Runs -10 with control after every iteration
			// Runs 2*10 back with control after each search

			// Resume at 26 27 28 29 30

			var act = Run(new Args { RangeStop = 30, Fault = "C26", ControlIterations = 5 });

			const string exp =
				"R1 1 2 3 4 5 C6 6 7 8 9 10 C11 11 12 13 14 15 C16 16 17 18 19 20 " +
				"C21 21 22 23 24 25 C26 ReproFault LastWasFault C26 " +
				"16 C17 17 C18 18 C19 19 C20 20 C21 21 C22 22 C23 23 C24 24 C25 25 C26 " +
				"LastWasFault C6 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 C26 " +
				"LastWasFault C1 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 " +
				"C26 ReproFailed 26 27 28 29 30";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestNonControlFaultBigNoRepro()
		{
			// Fault on non-control and never repro, verify control runs at correct times

			// Fuzz from 1 to 30
			// Fault on iteration 26
			// Never reproduces
			// Runs -10 with control after every iteration
			// Runs 2*10 back with control after each search
			// Control is always C26 incase iteration 27 requires a data set switch!

			// Resume at 27 28 29 30

			var act = Run(new Args { RangeStop = 30, Fault = "26", ControlIterations = 5 });

			const string exp =
				"R1 1 2 3 4 5 C6 6 7 8 9 10 C11 11 12 13 14 15 C16 16 17 18 19 20 " +
				"C21 21 22 23 24 25 C26 26 ReproFault LastWasFault C26 26 C26 " +
				"LastWasFault C16 16 C17 17 C18 18 C19 19 C20 20 C21 21 C22 22 C23 23 C24 24 C25 25 C26 26 C26 " +
				"LastWasFault C6 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 C26 " +
				"LastWasFault C1 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 C26 " +
				"ReproFailed 27 28 29 30";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestNonControlNotPastControlFault()
		{
			// Fault on non-control and never repro, don't search past last control fault

			// Fuzz from 1 to 30
			// Fault and repro after searching on C11
			// Fault on iteration C26
			// Never reproduces
			// Runs -10 with control after every iteration
			// Runs 2*10 back with control after each search but never runs C11

			// Resume at 27 28 29 30

			var act = Run(new Args { RangeStop = 30, Initial = "C11,1", InitialRepro = "C11,3", Fault = "C26", ControlIterations = 5 });

			const string exp =
				"R1 1 2 3 4 5 C6 6 7 8 9 10 C11 ReproFault LastWasFault C11 " +
				"1 C2 2 C3 3 C4 4 C5 5 C6 6 C7 7 C8 8 C9 9 C10 10 C11 Fault LastWasFault C11 " +
				"11 12 13 14 15 C16 16 17 18 19 20 C21 21 22 23 24 25 C26 ReproFault LastWasFault C26 " +
				"16 C17 17 C18 18 C19 19 C20 20 C21 21 C22 22 C23 23 C24 24 C25 25 C26 " +
				"LastWasFault C11 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 C26 " +
				"ReproFailed 26 27 28 29 30";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestNonControlNotPastFault()
		{
			// Fault on non-control and never repro, don't search past last fault

			// Fuzz from 1 to 30
			// Fault and repro on 11
			// Fault on iteration C26
			// Never reproduces
			// Runs -10 with control after every iteration
			// Runs 2*10 back with control after each search but never runs 11

			// Resume at 27 28 29 30

			var act = Run(new Args { RangeStop = 30, Initial = "11", InitialRepro = "11", Fault = "C26", ControlIterations = 5 });

			const string exp =
				"R1 1 2 3 4 5 C6 6 7 8 9 10 C11 11 ReproFault LastWasFault C11 11 Fault LastWasFault C12 " +
				"12 13 14 15 C16 16 17 18 19 20 C21 21 22 23 24 25 C26 ReproFault " +
				"LastWasFault C26 16 C17 17 C18 18 C19 19 C20 20 C21 21 C22 22 C23 23 C24 24 C25 25 C26 " +
				"LastWasFault C12 12 13 14 15 16 17 18 19 20 21 22 23 24 25 C26 " +
				"ReproFailed 26 27 28 29 30";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestNoSearchPastNonRepro()
		{
			// If a non-reproducible fault is detected, future backlog searches
			// should not go prior to that iteration

			// Fuzz 1 to 10
			// Fault on iteration 2 and don't reproduce
			// Fault on iteration 6, repro on 5
			// should not run iteration 2 a 2nd time.

			var act = Run(new Args { Initial = "2", Fault = "6", Repro = "5" });

			const string exp = 
				"R1 1 2 ReproFault " +
				"LastWasFault C2 2 " +
				"LastWasFault C1 1 2 ReproFailed 3 4 5 6 ReproFault " +
				"LastWasFault C6 6 " +
				"LastWasFault C3 3 4 5 Fault " +
				"LastWasFault C7 7 8 9 10";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestNoSearchPastInitialRepro()
		{
			// If a reproducible fault is detected, future backlog searches
			// should not go prior to the initial iteration that caused the fault

			// Fuzz 1 to 10
			// Fault on iteration 3 and reproduce on iteration 2
			// Fault on iteration 6, repro on 5
			// should not run iteration 3 a 2nd time.

			var act = Run(new Args { Initial = "3", InitialRepro = "2", Fault = "6", Repro = "5" });

			const string exp =
				"R1 1 2 3 ReproFault LastWasFault C3 3 " +
				"LastWasFault C1 1 2 Fault " +
				"LastWasFault C4 4 5 6 ReproFault " +
				"LastWasFault C6 6 " +
				"LastWasFault C4 4 5 Fault " +
				"LastWasFault C7 7 8 9 10";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestIterationNoReproControlRecord()
		{
			// Fault detected on record control iteration
			// Replay and don't reproduce, search for fault and don't find
			// Continue fuzzing

			var act = Run(new Args { Initial = "R6", SwitchCount = 5 });

			const string exp = "R1 1 2 3 4 5 R6 ReproFault LastWasFault R6 " +
				"R1 1 C2 2 C3 3 C4 4 C5 5 R6 ReproFailed 6 7 8 9 10";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestIterationReproControlRecord()
		{
			// Fault detected on record control iteration
			// Replay and reproduce
			// Stop fuzzing

			var ex = Assert.Throws<PeachException>(() =>
				Run(new Args { Initial = "R6", Repro = "R6", SwitchCount = 5 }));

			const string exp = "Fault detected on control recording iteration.";

			Assert.AreEqual(exp, ex.Message);

			var evts = string.Join(" ", history);

			Assert.AreEqual("R1 1 2 3 4 5 R6 ReproFault LastWasFault R6 Fault", evts);
		}

		[Test]
		public void TestIterationReproControl()
		{
			// Fault detected on control iteration
			// that immediately follows the fault means automation
			// failed and we need to stop fuzzing

			var ex = Assert.Throws<PeachException>(() =>
				Run(new Args { Fault = "6", Repro = "C6" }));

			const string exp = "Fault detected on control iteration.";

			Assert.AreEqual(exp, ex.Message);

			var evts = string.Join(" ", history);

			Assert.AreEqual("R1 1 2 3 4 5 6 ReproFault LastWasFault C6 Fault", evts);
		}

		[Test]
		public void TestIterationReproControlBeforeRecord()
		{
			Assert.Ignore("Implement me when we get the engine to control data set switching");

			// R(d1) 1 2 3 4 5 c(d1) r(d2) fault r(d2) (no-repro)
			// R(d1) 1 c(d1) 2 c(d1) 3 c(d1) 4 c(d1) 5 c(d1) r(d2) (give up search)
			// 6 7 8 9...
		}

		[Test]
		public void TestIterationReproControlRecordSearch()
		{
			// Fault detected on record control iteration
			// Replay and don't reproduce, search and eventually reproduce
			// Continue fuzzing

			var act = Run(new Args { Fault = "R6,1", Repro = "R6,3", SwitchCount = 5 });

			const string exp = "R1 1 2 3 4 5 R6 ReproFault " +
			                   "LastWasFault R6 R1 1 C2 2 C3 3 C4 4 C5 5 R6 Fault " +
			                   "LastWasFault R6 6 7 8 9 10";

			Assert.AreEqual(exp, act);
		}

		[Test]
		public void TestIterationReproControlRecordSearchDeep()
		{
			// Fault detected on record control iteration
			// Replay and don't reproduce, search twice and eventually reproduce
			// Continue fuzzing
		}

		[Test]
		public void TestSoftExceptionFirst()
		{
			// SoftException on 1st Record Control 
			// Stop fuzzing
		}

		[Test]
		public void TestSoftExceptionReproNoFault()
		{
			// SoftException on data set switch
			// No fault detected, SoftException reproduces
			// Stop fuzzing
		}

		[Test]
		public void TestSoftExceptionNoReproNoFault()
		{
			// SoftException on data set switch
			// No fault detected and SoftException does not reproduce
			// Treat SoftException as fault
			// Continue fuzzing

			var act = Run(new Args { Fault = "R6,1", Repro = "R6,3", SwitchCount = 5, InjectFault = false, SoftException = true });

			const string exp = "R1 1 2 3 4 5 R6 ReproFault " +
			                   "LastWasFault R6 " +
			                   "R1 1 C2 2 C3 3 C4 4 C5 5 R6 Fault " +
			                   "LastWasFault R6 6 7 8 9 10";

			Assert.AreEqual(exp, act);

			CollectionAssert.AreEqual(new[] { "Peach Control Iteration Failed" }, _faults);
		}

		[Test]
		public void TestSoftExceptionNoReproFault()
		{
			// SoftException on data set switch
			// Fault detected, start searching for repro and don't find one
			// Continue fuzzing
		}

		[Test]
		public void TestSoftExceptionReproFault()
		{
			// SoftException on data set switch
			// Fault detected, start searching for repro and  find one
			// Continue fuzzing


			var act = Run(new Args { Fault = "R6,1", Repro = "R6,3", SwitchCount = 5, SoftException = true });

			const string exp = "R1 1 2 3 4 5 R6 ReproFault " +
			                   "LastWasFault R6 " +
			                   "R1 1 C2 2 C3 3 C4 4 C5 5 R6 Fault " +
			                   "LastWasFault R6 6 7 8 9 10";

			Assert.AreEqual(exp, act);

			CollectionAssert.AreEqual(new[] { "UnitTest" }, _faults);
		}
	}
}