using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Peach.Core.Test;
using Peach.Core;

namespace Peach.Pro.Test.Core.Fixups
{
	[TestFixture]
	[Quick]
	[Peach]
	public class UnixTimeFixupTests
	{
		[Test]
		public void TestNoFormat()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String>
			<Fixup class='UnixTime' />
		</String>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null' />
	</Test>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);

			var before = DateTime.UtcNow;

			e.startFuzzing(dom, cfg);

			var after = DateTime.UtcNow;

			var dm = dom.tests[0].stateModel.states[0].actions[0].dataModel;

			var asNum = long.Parse(dm.InternalValue.BitsToString());

			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

			Assert.LessOrEqual((long)(before - epoch).TotalSeconds, asNum);
			Assert.GreaterOrEqual((long)(after - epoch).TotalSeconds, asNum);
		}

		[Test]
		public void TestNoFormatGmt()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String>
			<Fixup class='UnixTime' />
		</String>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null' />
	</Test>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);

			var before = DateTime.UtcNow;

			e.startFuzzing(dom, cfg);

			var after = DateTime.UtcNow;

			var dm = dom.tests[0].stateModel.states[0].actions[0].dataModel;

			var asNum = long.Parse(dm.InternalValue.BitsToString());

			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local);

			Assert.LessOrEqual((long)(before - epoch).TotalSeconds, asNum);
			Assert.GreaterOrEqual((long)(after - epoch).TotalSeconds, asNum);
		}

		[Test]
		public void TestValidFormat()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String>
			<Fixup class='UnixTime'>
				<Param name='Format' value='r' />
			</Fixup>
		</String>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null' />
	</Test>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);

			// Strip off millisecond and microsecond components
			var before = DateTime.ParseExact(DateTime.UtcNow.ToString("r"), "r", CultureInfo.InvariantCulture);

			e.startFuzzing(dom, cfg);

			var after = DateTime.ParseExact(DateTime.UtcNow.ToString("r"), "r", CultureInfo.InvariantCulture);

			var dm = dom.tests[0].stateModel.states[0].actions[0].dataModel;

			var asStr = dm.InternalValue.BitsToString();

			var dt = DateTime.ParseExact(asStr, "r", CultureInfo.InvariantCulture);

			Console.WriteLine(before.Ticks);
			Console.WriteLine(dt.Ticks);
			Console.WriteLine(after.Ticks);

			Assert.LessOrEqual(before, dt);
			Assert.GreaterOrEqual(after, dt);
		}

		[Test]
		public void VerifyVolatile()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String mutable='false'>
			<Fixup class='UnixTime'>
				<Param name='Format' value='r' />
			</Fixup>
		</String>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null' />
	</Test>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration { range = true, rangeStart = 1, rangeStop = 3 };
			var e = new Engine(null);

			var values = new List<string>();

			e.IterationFinished += (ctx, it) =>
			{
				var dm = dom.tests[0].stateModel.states[0].actions[0].dataModel;
				var asStr = dm.InternalValue.BitsToString();
				values.Add(asStr);

				Thread.Sleep(2000);
			};

			e.startFuzzing(dom, cfg);

			Assert.AreEqual(4, values.Count);

			var uniq = values.Distinct();

			CollectionAssert.AreEqual(values, uniq);
		}
	}
}
