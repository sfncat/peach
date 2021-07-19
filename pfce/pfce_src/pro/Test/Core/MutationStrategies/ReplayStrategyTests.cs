using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.MutationStrategies
{
	[TestFixture]
	[Quick]
	[Peach]
	class ReplayStrategyTests
	{
		TempDirectory _tmpDir;

		[SetUp]
		public void SetUp()
		{
			_tmpDir = new TempDirectory();
		}

		[TearDown]
		public void TearDown()
		{
			_tmpDir.Dispose();
		}


		[Test]
		public void TestNoFiles()
		{
			const string xml = @"
<Peach>
	<DataModel name='TheDataModel' />

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState' />
		<Publisher class='Null' />
		<Strategy class='Replay' />
	</Test>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration();
			var e = new Engine(null);
			var count = 0;

			e.IterationStarting += (ctx,it,tot) => ++count;

			e.startFuzzing(dom, cfg);

			Assert.AreEqual(1, count);
		}

		[Test]
		public void TestControlIteration()
		{
			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<!-- Ensure we can replay non-crackable sample files -->
		<String value='Hello' token='true' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel' />
				<Data fileName='{0}/sample.bin' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState' />
		<Publisher class='Null' />
		<Strategy class='Replay' />
	</Test>
</Peach>".Fmt(_tmpDir.Path);

			File.WriteAllText(Path.Combine(_tmpDir.Path, "sample.bin"), "World");

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration { singleIteration = true, countOnly = true };
			var e = new Engine(null);
			uint? total = null;

			e.HaveCount += (ctx, tot) => total = tot;

			e.startFuzzing(dom, cfg);

			Assert.True(total.HasValue, "Should have gotten total");
			Assert.AreEqual(1, total);
		}

		[Test]
		public void TestFuzz()
		{
			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<!-- Ensure we can replay non-crackable sample files -->
		<String value='Hello' token='true' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel' />
				<Data fileName='{0}/*.txt' />
				<Data fileName='{0}/a.bin' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState' />
		<Publisher class='Null' />
		<Strategy class='Replay' />
	</Test>
</Peach>".Fmt(_tmpDir.Path);

			File.WriteAllText(Path.Combine(_tmpDir.Path, "s1.txt"), "Fuzz1");
			File.WriteAllText(Path.Combine(_tmpDir.Path, "s2.txt"), "Fuzz2");
			File.WriteAllText(Path.Combine(_tmpDir.Path, "a.bin"), "Fuzz3");

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration();
			var e = new Engine(null);

			var values = new List<string>();
			var mutations = new List<string>();

			e.TestStarting += (ctx) =>
			{
				ctx.ActionFinished += (context, action) =>
				{
					Assert.False(action.error);
					values.Add(Encoding.ASCII.GetString(action.dataModel.Value.ToArray()));
				};

				ctx.DataMutating += (context, actionData, elem, mutator) =>
				{
					mutations.Add("{0}/{1}".Fmt(elem.Name, mutator.Name));
				};
			};

			e.startFuzzing(dom, cfg);

			CollectionAssert.AreEqual(new[]
			{
				"Hello",
				"Fuzz1",
				"Fuzz2",
				"Fuzz3"
			}, values);

			// Order should be same as defined in the <Action> element
			CollectionAssert.AreEqual(new[]
			{
				"TheDataModel/s1.txt",
				"TheDataModel/s2.txt",
				"TheDataModel/a.bin"
			}, mutations);
		}

		[Test]
		public void TestBadFile()
		{
			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<!-- Ensure we can replay non-crackable sample files -->
		<String value='Hello' token='true' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel' />
				<Data fileName='{0}/*.txt' />
				<Data fileName='{0}/a.bin' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState' />
		<Publisher class='Null' />
		<Strategy class='Replay' />
	</Test>
</Peach>".Fmt(_tmpDir.Path);

			File.WriteAllText(Path.Combine(_tmpDir.Path, "s1.txt"), "Fuzz1");
			File.WriteAllText(Path.Combine(_tmpDir.Path, "s2.txt"), "Fuzz2");
			File.WriteAllText(Path.Combine(_tmpDir.Path, "a.bin"), "Fuzz3");

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration();
			var e = new Engine(null);

			// Delete s1.txt after running the pit parser
			File.Delete(Path.Combine(_tmpDir.Path, "s1.txt"));

			var values = new List<string>();
			var mutations = new List<string>();
			var errors = 0;

			e.TestStarting += (ctx) =>
			{
				ctx.ActionFinished += (context, action) =>
				{
					if (action.error)
						++errors;
					else
						values.Add(Encoding.ASCII.GetString(action.dataModel.Value.ToArray()));
				};

				ctx.DataMutating += (context, actionData, elem, mutator) =>
				{
					mutations.Add("{0}/{1}".Fmt(elem.Name, mutator.Name));
				};
			};

			e.startFuzzing(dom, cfg);

			CollectionAssert.AreEqual(new[]
			{
				"Hello",
				"Fuzz2",
				"Fuzz3"
			}, values);

			// Order should be same as defined in the <Action> element
			CollectionAssert.AreEqual(new[]
			{
				"TheDataModel/s2.txt",
				"TheDataModel/a.bin"
			}, mutations);

			// When we try to mutate to s1.txt, it should raise a soft exception
			Assert.AreEqual(1, errors);
		}

		[Test]
		public void TestOneStateMultipleDataSets()
		{
			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<!-- Ensure we can replay non-crackable sample files -->
		<String value='Hello' token='true' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel' />
				<Data fileName='{0}/*.txt' />
				<Data fileName='{0}/a.bin' />
			</Action>
		</State>

		<!-- Its allowable to have more than 1 state with <Data> -->
		<!-- As long as only one action is ever run -->
		<State name='Secondary'>
			<Action type='output'>
				<DataModel ref='TheDataModel' />
				<Data fileName='{0}/*.txt' />
				<Data fileName='{0}/a.bin' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState' />
		<Publisher class='Null' />
		<Strategy class='Replay' />
	</Test>
</Peach>".Fmt(_tmpDir.Path);

			File.WriteAllText(Path.Combine(_tmpDir.Path, "s1.txt"), "Fuzz1");
			File.WriteAllText(Path.Combine(_tmpDir.Path, "s2.txt"), "Fuzz2");
			File.WriteAllText(Path.Combine(_tmpDir.Path, "a.bin"), "Fuzz3");

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration();
			var e = new Engine(null);

			var values = new List<string>();
			var mutations = new List<string>();

			e.TestStarting += (ctx) =>
			{
				ctx.ActionFinished += (context, action) =>
				{
					Assert.False(action.error);
					values.Add(Encoding.ASCII.GetString(action.dataModel.Value.ToArray()));
				};

				ctx.DataMutating += (context, actionData, elem, mutator) =>
				{
					mutations.Add("{0}/{1}".Fmt(elem.Name, mutator.Name));
				};
			};

			e.startFuzzing(dom, cfg);

			CollectionAssert.AreEqual(new[]
			{
				"Hello",
				"Fuzz1",
				"Fuzz2",
				"Fuzz3"
			}, values);

			// Order should be same as defined in the <Action> element
			CollectionAssert.AreEqual(new[]
			{
				"TheDataModel/s1.txt",
				"TheDataModel/s2.txt",
				"TheDataModel/a.bin"
			}, mutations);
		}

		[Test]
		public void TestMultipleStatesOneDataSet()
		{
			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<!-- Ensure we can replay non-crackable sample files -->
		<String value='Hello' token='true' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel name='TheDataModel'>
					<String value='Foo' />
				</DataModel>
			</Action>
			<Action type='changeState' ref='Secondary' />
		</State>

		<!-- Its allowable to have more than output action -->
		<!-- As long as only one action has data sets -->
		<State name='Secondary'>
			<Action type='output'>
				<DataModel ref='TheDataModel' />
				<Data fileName='{0}/*.txt' />
				<Data fileName='{0}/a.bin' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState' />
		<Publisher class='Null' />
		<Strategy class='Replay' />
	</Test>
</Peach>".Fmt(_tmpDir.Path);

			File.WriteAllText(Path.Combine(_tmpDir.Path, "s1.txt"), "Fuzz1");
			File.WriteAllText(Path.Combine(_tmpDir.Path, "s2.txt"), "Fuzz2");
			File.WriteAllText(Path.Combine(_tmpDir.Path, "a.bin"), "Fuzz3");

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration();
			var e = new Engine(null);

			var values = new List<string>();
			var mutations = new List<string>();

			e.TestStarting += (ctx) =>
			{
				ctx.ActionFinished += (context, action) =>
				{
					Assert.False(action.error);
					if (action.outputData.Any())
						values.Add(Encoding.ASCII.GetString(action.dataModel.Value.ToArray()));
				};

				ctx.DataMutating += (context, actionData, elem, mutator) =>
				{
					mutations.Add("{0}/{1}".Fmt(elem.Name, mutator.Name));
				};
			};

			e.startFuzzing(dom, cfg);

			CollectionAssert.AreEqual(new[]
			{
				"Foo",
				"Hello",
				"Foo",
				"Fuzz1",
				"Foo",
				"Fuzz2",
				"Foo",
				"Fuzz3"
			}, values);

			// Order should be same as defined in the <Action> element
			CollectionAssert.AreEqual(new[]
			{
				"TheDataModel/s1.txt",
				"TheDataModel/s2.txt",
				"TheDataModel/a.bin"
			}, mutations);
		}

		[Test]
		public void TestMultipleDataSets()
		{
			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<!-- Ensure we can replay non-crackable sample files -->
		<String value='Hello' token='true' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel' />
				<Data fileName='{0}/sample.bin' />
			</Action>
			<Action type='output'>
				<DataModel ref='TheDataModel' />
				<Data fileName='{0}/sample.bin' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState' />
		<Publisher class='Null' />
		<Strategy class='Replay' />
	</Test>
</Peach>".Fmt(_tmpDir.Path);

			File.WriteAllText(Path.Combine(_tmpDir.Path, "sample.bin"), "World");

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration();
			var e = new Engine(null);

			var ex = Assert.Throws<PeachException>(() => e.startFuzzing(dom, cfg));

			Assert.AreEqual("Error, the Replay strategy only supports state models with data sets on a single action.", ex.Message);
		}
	}
}
