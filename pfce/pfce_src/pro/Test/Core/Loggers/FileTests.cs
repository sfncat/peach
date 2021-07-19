using System;
using System.Collections.Generic;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.IO;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Loggers
{
	[TestFixture]
	[Quick]
	[Peach]
	class ControlIterationFaultTests
	{
		class ExceptionalPublisher : Publisher
		{
			static readonly NLog.Logger ClassLogger = NLog.LogManager.GetCurrentClassLogger();

			readonly RunContext _context;

			public ExceptionalPublisher(RunContext context, Dictionary<string, Variant> args)
				: base(args)
			{
				Name = "Pub";
				_context = context;
			}

			protected override void OnOutput(BitwiseStream data)
			{
				if (data == null) throw new ArgumentNullException("data");
			}

			protected override Variant OnCall(string method, List<BitwiseStream> args)
			{
				if (_context.controlIteration && !_context.controlRecordingIteration)
				{
					if (method == "throw")
						throw new SoftException("Erroring on a control iteration");

					return new Variant(new BitStream(Encoding.ASCII.GetBytes("false")));
				}

				return new Variant(new BitStream(Encoding.ASCII.GetBytes("true")));
			}

			protected override NLog.Logger Logger
			{
				get { return ClassLogger; }
			}
		}

		[Test]
		public void TestSoftException()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
			<Action type='call' method='throw'>
				<Param>
					<DataModel ref='DM' />
				</Param>
			</Action>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default' controlIteration='2'>
		<StateModel ref='SM' />
		<Publisher class='Null' name='Pub' />
		<Strategy class='Random' />
	</Test>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);
			var e = new Engine(null);

			e.TestStarting += ctx =>
				dom.tests[0].publishers[0] = new ExceptionalPublisher(ctx, new Dictionary<string, Variant>());

			var cfg = new RunConfiguration
			{
				range = true,
				rangeStart = 1,
				rangeStop = 3,
			};

			Fault[] faults = null;

			e.ReproFault += (ctx, it, sm, f) =>
			{
				Assert.Null(faults, "Should only detect a single fault.");
				Assert.AreEqual(3, it, "Should have detected fault on iteration 2");
				Assert.True(ctx.controlIteration, "Should have detected fault on control iteration");
				Assert.False(ctx.controlRecordingIteration, "Should have detected fault on non record control iteration");

				faults = f;

				ctx.continueFuzzing = false;
			};

			e.ReproFailed += (ctx, it) => Assert.Fail("Shouuld never get repro failed");
			e.Fault += (ctx, it, sm, f) => Assert.Fail("Shouuld never get repro success");

			e.startFuzzing(dom, cfg);

			Assert.NotNull(faults, "Should have detected a fault");
			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual(3, faults[0].iteration);

			Assert.AreEqual("Peach Control Iteration Failed", faults[0].title);
			Assert.AreEqual("ControlIteration", faults[0].detectionSource);
			Assert.AreEqual("ControlIteration", faults[0].folderName);

			var asStr = faults[0].description;

			StringAssert.Contains("Erroring on a control iteration", asStr);

			// No stacktraces!
			StringAssert.DoesNotContain("Peach.Core.SoftException", asStr);
		}

		[Test]
		public void TestDifferentActions()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
			<Action type='call' method='foo'>
				<Param>
					<DataModel ref='DM' />
				</Param>
				<Result>
					<DataModel ref='DM' />
				</Result>
			</Action>
			<Action name='Action_X1' type='output' when='str(state.actions[1].result.dataModel[0].DefaultValue) == ""true""'>
				<DataModel ref='DM' />
			</Action>
			<Action name='Action_X2' type='output' when='str(state.actions[1].result.dataModel[0].DefaultValue) == ""true""'>
				<DataModel ref='DM' />
			</Action>
			<Action name='Action_X3' type='output' when='str(state.actions[1].result.dataModel[0].DefaultValue) == ""true""'>
				<DataModel ref='DM' />
			</Action>
			<Action name='Action_Y1' type='output' when='str(state.actions[1].result.dataModel[0].DefaultValue) != ""true""'>
				<DataModel ref='DM' />
			</Action>
			<Action name='Action_Y2' type='output' when='str(state.actions[1].result.dataModel[0].DefaultValue) != ""true""'>
				<DataModel ref='DM' />
			</Action>
			<Action name='Action_Y3' type='output' when='str(state.actions[1].result.dataModel[0].DefaultValue) != ""true""'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default' controlIteration='2'>
		<StateModel ref='SM' />
		<Publisher class='Null' name='Pub' />
		<Strategy class='Random' />
	</Test>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);
			var e = new Engine(null);

			e.TestStarting += ctx => 
				dom.tests[0].publishers[0] = new ExceptionalPublisher(ctx, new Dictionary<string, Variant>());

			var cfg = new RunConfiguration
			{
				range = true,
				rangeStart = 1,
				rangeStop = 3,
			};

			Fault[] faults = null;

			e.ReproFault += (ctx, it, sm, f) =>
			{
				Assert.Null(faults, "Should only detect a single fault.");
				Assert.AreEqual(3, it, "Should have detected fault on iteration 2");
				Assert.True(ctx.controlIteration, "Should have detected fault on control iteration");
				Assert.False(ctx.controlRecordingIteration, "Should have detected fault on non record control iteration");

				faults = f;

				ctx.continueFuzzing = false;
			};

			e.ReproFailed += (ctx, it) => Assert.Fail("Shouuld never get repro failed");
			e.Fault += (ctx, it, sm, f) => Assert.Fail("Shouuld never get repro success");

			e.startFuzzing(dom, cfg);

			Assert.NotNull(faults, "Should have detected a fault");
			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual(3, faults[0].iteration);

			Assert.AreEqual("Peach Control Iteration Failed", faults[0].title);
			Assert.AreEqual("ControlIteration", faults[0].detectionSource);
			Assert.AreEqual("ControlIteration", faults[0].folderName);

			const string majorHash = "Initial:Initial";
			const string minorHash = "Initial:Initial,Action_X1:Action_Y1";

			Assert.AreEqual(Monitor2.Hash(majorHash), faults[0].majorHash);
			Assert.AreEqual(Monitor2.Hash(minorHash), faults[0].minorHash);

			var asStr = faults[0].description.Replace("\r\n", "\n");

			var exp = @"The Peach control iteration performed failed to execute same as initial control.

The 3rd executed action is different from what was expexted.

Expected
----------------------------------------
Output      | Initial.Action
Call        | Initial.Action_1
Output      | Initial.Action_X1
Output      | Initial.Action_X2
Output      | Initial.Action_X3

Actual
----------------------------------------
Output      | Initial.Action
Call        | Initial.Action_1
Output      | Initial.Action_Y1
Output      | Initial.Action_Y2
Output      | Initial.Action_Y3
".Replace("\r\n", "\n");

			Assert.AreEqual(exp, asStr);
		}

		[Test]
		public void TestTooManyActions()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
			<Action type='call' method='foo'>
				<Param>
					<DataModel ref='DM' />
				</Param>
				<Result>
					<DataModel ref='DM' />
				</Result>
			</Action>
			<Action name='Action_Y1' type='output' when='str(state.actions[1].result.dataModel[0].DefaultValue) != ""true""'>
				<DataModel ref='DM' />
			</Action>
			<Action name='Action_Y2' type='output' when='str(state.actions[1].result.dataModel[0].DefaultValue) != ""true""'>
				<DataModel ref='DM' />
			</Action>
			<Action name='Action_Y3' type='output' when='str(state.actions[1].result.dataModel[0].DefaultValue) != ""true""'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default' controlIteration='2'>
		<StateModel ref='SM' />
		<Publisher class='Null' name='Pub' />
		<Strategy class='Random' />
	</Test>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);
			var e = new Engine(null);

			e.TestStarting += ctx =>
				dom.tests[0].publishers[0] = new ExceptionalPublisher(ctx, new Dictionary<string, Variant>());

			var cfg = new RunConfiguration
			{
				range = true,
				rangeStart = 1,
				rangeStop = 3,
			};

			Fault[] faults = null;

			e.ReproFault += (ctx, it, sm, f) =>
			{
				Assert.Null(faults, "Should only detect a single fault.");
				Assert.AreEqual(3, it, "Should have detected fault on iteration 2");
				Assert.True(ctx.controlIteration, "Should have detected fault on control iteration");
				Assert.False(ctx.controlRecordingIteration, "Should have detected fault on non record control iteration");

				faults = f;

				ctx.continueFuzzing = false;
			};

			e.ReproFailed += (ctx, it) => Assert.Fail("Shouuld never get repro failed");
			e.Fault += (ctx, it, sm, f) => Assert.Fail("Shouuld never get repro success");

			e.startFuzzing(dom, cfg);

			Assert.NotNull(faults, "Should have detected a fault");
			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual(3, faults[0].iteration);

			Assert.AreEqual("Peach Control Iteration Failed", faults[0].title);
			Assert.AreEqual("ControlIteration", faults[0].detectionSource);
			Assert.AreEqual("ControlIteration", faults[0].folderName);

			const string majorHash = ":Initial";
			const string minorHash = ":Initial,:Action_Y1";

			Assert.AreEqual(Monitor2.Hash(majorHash), faults[0].majorHash);
			Assert.AreEqual(Monitor2.Hash(minorHash), faults[0].minorHash);

			var asStr = faults[0].description.Replace("\r\n", "\n");

			var exp = @"The Peach control iteration performed failed to execute same as initial control.

The state model should have finished but executed action 'Initial.Action_Y1' instead.

Expected
----------------------------------------
Output      | Initial.Action
Call        | Initial.Action_1

Actual
----------------------------------------
Output      | Initial.Action
Call        | Initial.Action_1
Output      | Initial.Action_Y1
Output      | Initial.Action_Y2
Output      | Initial.Action_Y3
".Replace("\r\n", "\n");

			Assert.AreEqual(exp, asStr);
		}

		[Test]
		public void TestTooFewActions()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
			<Action type='call' method='foo'>
				<Param>
					<DataModel ref='DM' />
				</Param>
				<Result>
					<DataModel ref='DM' />
				</Result>
			</Action>
			<Action name='Action_X1' type='output' when='str(state.actions[1].result.dataModel[0].DefaultValue) == ""true""'>
				<DataModel ref='DM' />
			</Action>
			<Action name='Action_X2' type='output' when='str(state.actions[1].result.dataModel[0].DefaultValue) == ""true""'>
				<DataModel ref='DM' />
			</Action>
			<Action name='Action_X3' type='output' when='str(state.actions[1].result.dataModel[0].DefaultValue) == ""true""'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default' controlIteration='2'>
		<StateModel ref='SM' />
		<Publisher class='Null' name='Pub' />
		<Strategy class='Random' />
	</Test>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);
			var e = new Engine(null);

			e.TestStarting += ctx =>
				dom.tests[0].publishers[0] = new ExceptionalPublisher(ctx, new Dictionary<string, Variant>());

			var cfg = new RunConfiguration
			{
				range = true,
				rangeStart = 1,
				rangeStop = 3,
			};

			Fault[] faults = null;

			e.ReproFault += (ctx, it, sm, f) =>
			{
				Assert.Null(faults, "Should only detect a single fault.");
				Assert.AreEqual(3, it, "Should have detected fault on iteration 2");
				Assert.True(ctx.controlIteration, "Should have detected fault on control iteration");
				Assert.False(ctx.controlRecordingIteration, "Should have detected fault on non record control iteration");

				faults = f;

				ctx.continueFuzzing = false;
			};

			e.ReproFailed += (ctx, it) => Assert.Fail("Shouuld never get repro failed");
			e.Fault += (ctx, it, sm, f) => Assert.Fail("Shouuld never get repro success");

			e.startFuzzing(dom, cfg);

			Assert.NotNull(faults, "Should have detected a fault");
			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual(3, faults[0].iteration);

			Assert.AreEqual("Peach Control Iteration Failed", faults[0].title);
			Assert.AreEqual("ControlIteration", faults[0].detectionSource);
			Assert.AreEqual("ControlIteration", faults[0].folderName);

			const string majorHash = "Initial:";
			const string minorHash = "Initial:,Action_X1:";

			Assert.AreEqual(Monitor2.Hash(majorHash), faults[0].majorHash);
			Assert.AreEqual(Monitor2.Hash(minorHash), faults[0].minorHash);

			var asStr = faults[0].description.Replace("\r\n", "\n");

			var exp = @"The Peach control iteration performed failed to execute same as initial control.

The state model should have executed action 'Initial.Action_X1' but finished instead.

Expected
----------------------------------------
Output      | Initial.Action
Call        | Initial.Action_1
Output      | Initial.Action_X1
Output      | Initial.Action_X2
Output      | Initial.Action_X3

Actual
----------------------------------------
Output      | Initial.Action
Call        | Initial.Action_1
".Replace("\r\n", "\n");

			Assert.AreEqual(exp, asStr);
		}

		[Test]
		public void TestMissedStates()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
			<Action type='call' method='foo'>
				<Param>
					<DataModel ref='DM' />
				</Param>
				<Result>
					<DataModel ref='DM' />
				</Result>
			</Action>
			<Action type='changeState' ref='State_True_1' when='str(state.actions[1].result.dataModel[0].DefaultValue) == ""true""' />
			<Action type='changeState' ref='State_False_1' when='str(state.actions[1].result.dataModel[0].DefaultValue) != ""true""' />
		</State>

		<State name='State_True_1'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
			<Action type='changeState' ref='State_True_2' />
		</State>

		<State name='State_True_2'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
		</State>

		<State name='State_False_1'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
			<Action type='changeState' ref='State_False_2' />
		</State>

		<State name='State_False_2'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default' controlIteration='2'>
		<StateModel ref='SM' />
		<Publisher class='Null' name='Pub' />
		<Strategy class='Random' />
	</Test>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);
			var e = new Engine(null);

			e.TestStarting += ctx =>
				dom.tests[0].publishers[0] = new ExceptionalPublisher(ctx, new Dictionary<string, Variant>());

			var cfg = new RunConfiguration
			{
				range = true,
				rangeStart = 1,
				rangeStop = 3,
			};

			Fault[] faults = null;

			e.ReproFault += (ctx, it, sm, f) =>
			{
				Assert.Null(faults, "Should only detect a single fault.");
				Assert.AreEqual(3, it, "Should have detected fault on iteration 2");
				Assert.True(ctx.controlIteration, "Should have detected fault on control iteration");
				Assert.False(ctx.controlRecordingIteration, "Should have detected fault on non record control iteration");

				faults = f;

				ctx.continueFuzzing = false;
			};

			e.ReproFailed += (ctx, it) => Assert.Fail("Shouuld never get repro failed");
			e.Fault += (ctx, it, sm, f) => Assert.Fail("Shouuld never get repro success");

			e.startFuzzing(dom, cfg);

			Assert.NotNull(faults, "Should have detected a fault");
			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual(3, faults[0].iteration);

			Assert.AreEqual("Peach Control Iteration Failed", faults[0].title);
			Assert.AreEqual("ControlIteration", faults[0].detectionSource);
			Assert.AreEqual("ControlIteration", faults[0].folderName);

			const string majorHash = "Initial:Initial";
			const string minorHash = "Initial:Initial,Action_2:Action_3";

			Assert.AreEqual(Monitor2.Hash(majorHash), faults[0].majorHash);
			Assert.AreEqual(Monitor2.Hash(minorHash), faults[0].minorHash);

			var asStr = faults[0].description.Replace("\r\n", "\n");

			var exp = @"The Peach control iteration performed failed to execute same as initial control.

The 3rd executed action is different from what was expexted.

Expected
----------------------------------------
Output      | Initial.Action
Call        | Initial.Action_1
ChangeState | Initial.Action_2
Output      | State_True_1.Action
ChangeState | State_True_1.Action_1
Output      | State_True_2.Action

Actual
----------------------------------------
Output      | Initial.Action
Call        | Initial.Action_1
ChangeState | Initial.Action_3
Output      | State_False_1.Action
ChangeState | State_False_1.Action_1
Output      | State_False_2.Action
".Replace("\r\n", "\n");

			Assert.AreEqual(exp, asStr);
		}
	}
}
