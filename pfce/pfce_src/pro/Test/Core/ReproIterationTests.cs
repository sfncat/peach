using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Peach]
	[Quick]
	class ReproIterationTests
	{
		readonly List<uint> _iterationHistory = new List<uint>();
		readonly List<string> _faultHistory = new List<string>();
		readonly List<TimeSpan> _waitTimes = new List<TimeSpan>();

		/// <summary>
		/// Fuzz from iteration 1 to 10
		/// Fault on 'faultIter'
		/// Reproduce fault on 'faultIter'
		/// How often to run control iterations
		/// </summary>
		/// <param name="faultIter"></param>
		/// <param name="repro"></param>
		/// <param name="controlIter"></param>
		/// <param name="waitTime"></param>
		/// <param name="faultWaitTime"></param>
		/// <param name="softException"></param>
		void RunIter(string faultIter, bool repro, uint controlIter, string waitTime = "0.0", string faultWaitTime = "0.0", bool softException = false)
		{
			const string template = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String name='s' value='Hello World'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<StateModel name='TheStateSwitch' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
				<Data>
					<Field name='s' value='Hello' />
				</Data>
				<Data>
					<Field name='s' value='World' />
				</Data>
			</Action>
		</State>
	</StateModel>

	<!-- Need an agent to measure time between iteration finished and detected fault -->
	<Agent name='LocalAgent' />

	<Test name='Default' targetLifetime='iteration' controlIteration='{0}' waitTime='{1}' faultWaitTime='{2}'>
		<Agent ref='LocalAgent'/>
		<StateModel ref='{3}'/>
		<Publisher class='Null'/>
		<Strategy class='Random'>
			<Param name='SwitchCount' value='5' />
		</Strategy>
	</Test>
</Peach>";

			_iterationHistory.Clear();
			_faultHistory.Clear();
			_waitTimes.Clear();

			var xml = string.Format(template, controlIter, waitTime, faultWaitTime, faultIter.StartsWith("R") ? "TheStateSwitch" : "TheState");

			var dom = DataModelCollector.ParsePit(xml);

			var config = new RunConfiguration
			{
				range = true,
				rangeStart = 1,
				rangeStop = 10,
			};

			var sw = new Stopwatch();

			var e = new Engine(null);

			e.TestStarting += ctx =>
			{
				ctx.DetectedFault += (c, agent) => _waitTimes.Add(sw.Elapsed);
				ctx.StateModelStarting += (c, sm) =>
				{
					var it = c.currentIteration;

					_iterationHistory.Add(it);

					if (faultIter.StartsWith("RC"))
					{
						if (ctx.controlRecordingIteration && ctx.controlIteration && it == int.Parse(faultIter.Substring(2)) && (!ctx.reproducingFault || repro))
						{
							if (softException)
								throw new SoftException("Simulated SoftException");

							ctx.InjectFault();
						}
					}
					else if (faultIter.StartsWith("C"))
					{
						if (ctx.controlIteration && it == int.Parse(faultIter.Substring(1)) && (!ctx.reproducingFault || repro))
							ctx.InjectFault();
					}
					else if (faultIter == it.ToString(CultureInfo.InvariantCulture))
					{
						if (repro || !ctx.reproducingFault)
							ctx.InjectFault();
					}

				};
			};

			e.IterationStarting += (ctx, it, ti) =>
			{
			};

			e.IterationFinished += (ctx, it) => sw.Restart();
			e.Fault += (ctx, it, sm, fault) => SaveFault("Fault", ctx);
			e.ReproFault += (ctx, it, sm, fault) => SaveFault("ReproFault", ctx);
			e.ReproFailed += (ctx, it) => SaveFault("ReproFailed", ctx);

			e.startFuzzing(dom, config);
		}

		private void SaveFault(string type, RunContext context)
		{
			var item = "{0}_{1}{2}{3}".Fmt(
				type,
				context.controlRecordingIteration ? "R" : "",
				context.controlIteration ? "C" : "",
				context.currentIteration);
			_faultHistory.Add(item);
		}

		[Test]
		public void TestIterationNoRepro()
		{
			// Target lifetime is per iteration and non-reproducible fault
			// found on a fuzzing iteration.
			// expect it to only replay the same interation

			RunIter("3", false, 0);

			var expected = new uint[]
			{
				1,  // Control
				1, 2,
				3, // Trigger repro
				3, // Non-repro
				4, 5, 6, 7, 8, 9, 10
			};

			var actual = _iterationHistory.ToArray();
			Assert.AreEqual(expected, actual);

			var faults = new[]
			{
				"ReproFault_3",
				"ReproFailed_3"
			};

			Assert.AreEqual(faults, _faultHistory.ToArray());
		}

		[Test]
		public void TestIterationRepro()
		{
			// Target lifetime is per iteration and reproducible fault
			// found on a fuzzing iteration.
			// expect it to only replay the same interation

			RunIter("3", true, 0);

			var expected = new uint[] {
				1,  // Control
				1, 2,
				3, // Trigger repro
				3, // repro
				4, 5, 6, 7, 8, 9, 10
			};

			var actual = _iterationHistory.ToArray();
			Assert.AreEqual(expected, actual);

			var faults = new[]
			{
				"ReproFault_3",
				"Fault_3"
			};

			Assert.AreEqual(faults, _faultHistory.ToArray());
		}

		[Test]
		public void TestIterationNoReproControl()
		{
			// Target lifetime is per iteration and non-reproducible fault
			// found on a control iteration.
			// expect it to only replay the same interation

			RunIter("C5", false, 2);

			var expected = new uint[] {
				1,  // Control
				1, 2,
				3, // Control
				3, 4,
				5, // Control & fault
				5, // Control & non-repro
				5, 6,
				7, // Control
				7, 8,
				9, // Control
				9, 10
			};

			var actual = _iterationHistory.ToArray();
			Assert.AreEqual(expected, actual);

			var faults = new[]
			{
				"ReproFault_C5",
				"ReproFailed_C5"
			};

			Assert.AreEqual(faults, _faultHistory.ToArray());
		}

		[Test]
		public void TestIterationReproControl()
		{
			// Target lifetime is per iteration and reproducible fault
			// found on a control iteration.
			// expect it to only replay the same interation and
			// stop fuzzing.

			var ex = Assert.Throws<PeachException>(() => RunIter("C5", true, 2));

			Assert.AreEqual("Fault detected on control iteration.", ex.Message);

			var expected = new uint[] {
				1,  // Control
				1, 2,
				3, // Control
				3, 4,
				5, // Control & fault
				5 // Control & repro
			};

			var actual = _iterationHistory.ToArray();
			Assert.AreEqual(expected, actual);

			var faults = new[]
			{
				"ReproFault_C5",
				"Fault_C5"
			};

			Assert.AreEqual(faults, _faultHistory.ToArray());
		}

		[Test]
		public void TestIterationNoReproControlRecord()
		{
			// Target lifetime is per iteration and non-reproducible fault
			// found on a control record iteration.
			// expect it to only replay the same interation and
			// continue fuzzing.

			RunIter("RC6", false, 0);

			var expected = new uint[] {
				1,  // Control
				1, 2, 3, 4, 5,
				6, // Record Control & fault
				6, // Record Control & non-repro
				6, 7, 8, 9, 10
			};

			var actual = _iterationHistory.ToArray();
			Assert.AreEqual(expected, actual);

			var faults = new[]
			{
				"ReproFault_RC6",
				"ReproFailed_RC6"
		};

			Assert.AreEqual(faults, _faultHistory.ToArray());
		}

		[Test]
		public void TestIterationReproControlRecord()
		{
			// Target lifetime is per iteration and reproducible fault
			// found on a control record iteration.
			// expect it to only replay the same interation and
			// stop fuzzing.

			var ex = Assert.Throws<PeachException>(() => RunIter("RC6", true, 0));

			Assert.AreEqual("Fault detected on control record iteration.", ex.Message);

			var expected = new uint[] {
				1,  // Control
				1, 2, 3, 4, 5,
				6, // Record Control & fault
				6 // Record Control & repro
			};

			var actual = _iterationHistory.ToArray();
			Assert.AreEqual(expected, actual);

			var faults = new[]
			{
				"ReproFault_RC6",
				"Fault_RC6"
			};

			Assert.AreEqual(faults, _faultHistory.ToArray());
		}

		[Test]
		public void TestSoftExceptionFirst()
		{
			// Target lifetime is per iteration and soft exception happens
			// on a record iteration.  Expect fuzzing to stop.

			var ex = Assert.Throws<PeachException>(() => RunIter("RC1", true, 0, softException: true));

			Assert.AreEqual("Simulated SoftException", ex.Message);

			var expected = new uint[] {
				1 // Record Control & Soft Exception
			};

			var actual = _iterationHistory.ToArray();
			Assert.AreEqual(expected, actual);

			CollectionAssert.IsEmpty(_faultHistory);
		}

		[Test]
		public void TestSoftExceptionLater()
		{
			// Target lifetime is per iteration and soft exception happens
			// on a record iteration.  Expect fuzzing to stop.

			var ex = Assert.Throws<PeachException>(() => RunIter("RC6", true, 0, softException: true));

			Assert.AreEqual("Fault detected on control record iteration.", ex.Message);

			var expected = new uint[] {
				1,  // Control
				1, 2, 3, 4, 5,
				6, // Record Control & soft exceptiopn
				6 // Record Control & soft exceptiopn repro
			};

			var actual = _iterationHistory.ToArray();
			Assert.AreEqual(expected, actual);

			var faults = new[]
			{
				"ReproFault_RC6",
				"Fault_RC6"
			};

			Assert.AreEqual(faults, _faultHistory.ToArray());
		}

		[Test]
		public void TestIterationWaitTime()
		{
			// waitTime = 0.5s
			// faultWaitTime = 1.0s
			// waits 0.5s after IterationFinished but before DetectedFault on every iteration:
			// record, control, fuzz, reproduction

			RunIter("3", true, 0, "0.5", "1.0");

			var expected = new[]
			{
				0.5, // 1 Control
				0.5, 0.5, // 1 & 2
				0.5, // 3 Trigger repro
				1.5, // 3 Repro (faultWaitTime + waitTime)
				0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 // 4, 5, 6, 7, 8, 9, 10
			};

			var actual = _waitTimes.ToArray();
			Assert.AreEqual(12, actual.Length);

			for (var i = 0; i < expected.Length; ++i)
			{
				Assert.Greater(actual[i].TotalSeconds, expected[i] - 0.10);
				Assert.Less(actual[i].TotalSeconds, expected[i] + 0.10);
			}
		}

		[Test]
		public void TestIterationFaultWaitTime()
		{
			// waitTime = 0.0s
			// faultWaitTime = 2.0s
			// waits 0.5s after IterationFinished but before DetectedFault on every iteration:
			// record, control, fuzz, reproduction

			RunIter("3", true, 0, "0.0", "2.0");

			var expected = new[]
			{
				0.0, // 1 Control
				0.0, 0.0, // 1 & 2
				0.0, // 3 Trigger repro
				2.0, // 3 Repro (faultWaitTime + waitTime)
				0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 // 4, 5, 6, 7, 8, 9, 10
			};

			var actual = _waitTimes.ToArray();
			Assert.AreEqual(12, actual.Length);

			for (var i = 0; i < expected.Length; ++i)
			{
				Assert.Greater(actual[i].TotalSeconds, expected[i] - 0.10);
				Assert.Less(actual[i].TotalSeconds, expected[i] + 0.10);
			}
		}
	}
}