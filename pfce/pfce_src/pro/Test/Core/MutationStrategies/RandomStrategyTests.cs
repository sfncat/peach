using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Test;
using Peach.Pro.Core;
using Action = Peach.Core.Dom.Action;
using Random = Peach.Core.Random;

namespace Peach.Pro.Test.Core.MutationStrategies
{
	[TestFixture]
	[Quick]
	[Peach]
	class RandomStrategyTests : DataModelCollector
	{
		public RandomStrategyTests()
		{
			cloneActions = true;
		}

		[Test]
		public void Test1()
		{
			// Test fuzzing does something

			string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<Number name='num1' size='32' value='100' signed='false'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Strategy class='Random'/>
	</Test>
</Peach>";

			RunEngine(xml, 1, 999);

			// verify values
			Assert.AreEqual(999, mutations.Count);
			Assert.AreEqual(999, allStrategies.Count);
		}

		[Test]
		public void Test2()
		{
			// Random strategy picks a list of elements across all models
			// so make sure this is working

			string xml = @"
<Peach>
	<DataModel name='TheDataModel1'>
		<Number name='num1' size='32' value='100' signed='false'/>
	</DataModel>

	<DataModel name='TheDataModel2'>
		<Number name='num1' size='32' value='100' signed='false'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action name='Action1' type='output'>
				<DataModel ref='TheDataModel1'/>
			</Action>
			<Action name='Action2' type='output'>
				<DataModel ref='TheDataModel2'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default' controlIteration='200'>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Strategy class='Random'/>
	</Test>
</Peach>";

			RunEngine(xml, 1, 999);

			// verify values
			var dm1 = 0;
			var dm2 = 0;

			foreach (var item in allStrategies)
			{
				if (item.Contains("TheDataModel1"))
					dm1 += 1;
				else if (item.Contains("TheDataModel2"))
					dm2 += 1;
			}

			// 999 mutations, control at iteration 1, 201, 401, 601, 801, two data models = (999+5)*2
			Assert.AreEqual((999+5)*2, actions.Count);
			Assert.AreEqual(allStrategies.Count, dm1 + dm2);

			// Make sure each data model was fuzzed about the same number of times
			var diff = (1.0 * dm1) / (dm1 + dm2);
			Assert.Greater(diff, 0.45);
			Assert.Less(diff, 0.55);
		}

		[Test]
		public void Test3()
		{
			// Test strategy only mutates a random number
			// between 1 and MaxFieldsToMutate every iteration

			string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<Number name='num1' size='32' value='100' signed='false'/>
		<Number name='num2' size='32' value='100' signed='false'/>
		<Number name='num3' size='32' value='100' signed='false'/>
		<Number name='num4' size='32' value='100' signed='false'/>
		<Number name='num5' size='32' value='100' signed='false'/>
		<Number name='num6' size='32' value='100' signed='false'/>
		<Number name='num7' size='32' value='100' signed='false'/>
		<Number name='num8' size='32' value='100' signed='false'/>
		<Number name='num9' size='32' value='100' signed='false'/>
		<Number name='num10' size='32' value='100' signed='false'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default' controlIteration='200'>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Strategy class='Random'>
			<Param name='MaxFieldsToMutate' value='5'/>
		</Strategy>
	</Test>
</Peach>";

			RunEngine(xml, 1, 1000);

			// verify values
			// 1000 mutations, control on iteration 1, 201, 401, 601, 801 = 1005 actions
			// Random gaussian number between 1 and 5 is on average 1.8, for 1000 iterations is 1800 mutations
			Assert.AreEqual(1005, actions.Count);
			Assert.Greater(allStrategies.Count, 1800);
			Assert.Less(allStrategies.Count, 2000);
		}

		[Test]
		public void Test4()
		{
			// Test that subsequent runs of the same seed produce identical results

			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel1\">" +
				"       <Number name=\"num1\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"       <Number name=\"num2\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"       <Number name=\"num3\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"       <Number name=\"num4\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"   </DataModel>" +

				"   <DataModel name=\"TheDataModel2\">" +
				"       <Number name=\"num1\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"       <Number name=\"num2\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"       <Number name=\"num3\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"       <Number name=\"num4\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"   </DataModel>" +

				"   <DataModel name=\"TheDataModel3\">" +
				"       <Number name=\"num1\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"       <Number name=\"num2\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"       <Number name=\"num3\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"       <Number name=\"num4\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"   </DataModel>" +

				"   <StateModel name=\"TheState\" initialState=\"Initial\">" +
				"       <State name=\"Initial\">" +
				"           <Action name=\"Action1\" type=\"output\">" +
				"               <DataModel ref=\"TheDataModel1\"/>" +
				"           </Action>" +
				"           <Action name=\"Action2\" type=\"output\">" +
				"               <DataModel ref=\"TheDataModel2\"/>" +
				"           </Action>" +
				"           <Action name=\"Action3\" type=\"output\">" +
				"               <DataModel ref=\"TheDataModel3\"/>" +
				"           </Action>" +
				"       </State>" +
				"   </StateModel>" +

				"   <Test name=\"Default\" controlIteration=\"200\">" +
				"       <StateModel ref=\"TheState\"/>" +
				"       <Publisher class=\"Null\"/>" +
				"       <Strategy class=\"RandomStrategy\"/>" +
				"   </Test>" +
				"</Peach>";

			RunEngine(xml, 1, 1000);

			// Sanity check
			// 1000 mutations, control on 1, 201, 401, 601, 801, 3 data models
			Assert.AreEqual((1000+5)*3, actions.Count);

			var oldStrategies = allStrategies;
			var oldActions = actions;

			// Reset the DataModelCollector
			ResetContainers();

			RunEngine(xml, 1, 1000);

			// Verify
			VerifySameResults(oldStrategies, oldActions);
		}

		[Test]
		public void Test5()
		{
			// Test that subsequent runs of the same seed produce identical results
			// when the second run includes a subset of iterations of the first

			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel1\">" +
				"       <Number name=\"num1\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"       <Number name=\"num2\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"       <Number name=\"num3\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"       <Number name=\"num4\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"   </DataModel>" +

				"   <DataModel name=\"TheDataModel2\">" +
				"       <Number name=\"num1\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"       <Number name=\"num2\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"       <Number name=\"num3\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"       <Number name=\"num4\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"   </DataModel>" +

				"   <DataModel name=\"TheDataModel3\">" +
				"       <Number name=\"num1\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"       <Number name=\"num2\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"       <Number name=\"num3\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"       <Number name=\"num4\" size=\"32\" value=\"100\" signed=\"false\"/>" +
				"   </DataModel>" +

				"   <StateModel name=\"TheState\" initialState=\"Initial\">" +
				"       <State name=\"Initial\">" +
				"           <Action name=\"Action1\" type=\"output\">" +
				"               <DataModel ref=\"TheDataModel1\"/>" +
				"           </Action>" +
				"           <Action name=\"Action2\" type=\"output\">" +
				"               <DataModel ref=\"TheDataModel2\"/>" +
				"           </Action>" +
				"           <Action name=\"Action3\" type=\"output\">" +
				"               <DataModel ref=\"TheDataModel3\"/>" +
				"           </Action>" +
				"       </State>" +
				"   </StateModel>" +

				"   <Test name=\"Default\" controlIteration=\"200\">" +
				"       <StateModel ref=\"TheState\"/>" +
				"       <Publisher class=\"Null\"/>" +
				"       <Strategy class=\"RandomStrategy\"/>" +
				"   </Test>" +
				"</Peach>";

			RunEngine(xml, 1, 1000);

			// Sanity check
			// 1000 mutations, control on 1, 201, 401, 601, 801, 3 data models
			Assert.AreEqual((1000+5)*3, actions.Count);

			var oldStrategies = allStrategies;
			var oldActions = actions;

			// Reset the DataModelCollector
			ResetContainers();

			RunEngine(xml, 501, 1000);

			// Sanity check
			// 500 mutations, control on 501, 601, 801, 3 data models
			Assert.AreEqual((500+3)*3, actions.Count);

			oldStrategies.RemoveRange(0, oldStrategies.Count - allStrategies.Count);
			oldActions.RemoveRange(0, oldActions.Count - actions.Count);

			// Verify
			VerifySameResults(oldStrategies, oldActions);
		}

		[Test]
		public void Test6()
		{
			// Test that the random strategy properly cycles through data models on the specified switch count
			string temp1 = Path.GetTempFileName();
			string temp2 = Path.GetTempFileName();

			File.WriteAllBytes(temp1, Encoding.ASCII.GetBytes("Hello\x00World\x00"));
			File.WriteAllBytes(temp2, Encoding.ASCII.GetBytes("Foo\x00"));

			// Test loading a dataset from a file
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel\">" +
				"       <String name=\"str1\" value=\"Initial\" maxOccurs=\"100\" nullTerminated=\"true\"/>" +
				"   </DataModel>" +

				"   <StateModel name=\"TheState\" initialState=\"Initial\">" +
				"       <State name=\"Initial\">" +
				"           <Action type=\"output\">" +
				"               <DataModel ref=\"TheDataModel\"/>" +
				"               <Data fileName=\"" + temp1 + "\"/>" +
				"               <Data fileName=\"" + temp2 + "\"/>" +
				"           </Action>" +
				"       </State>" +
				"   </StateModel>" +

				"   <Test name=\"Default\">" +
				"       <StateModel ref=\"TheState\"/>" +
				"       <Publisher class=\"Null\"/>" +
				"       <Strategy class=\"RandomStrategy\">" +
				"           <Param name=\"SwitchCount\" value=\"10\"/>" +
				"       </Strategy>" +
				"   </Test>" +
				"</Peach>";

			PitParser parser = new PitParser();

			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			dom.tests[0].includedMutators = new List<string>();
			dom.tests[0].includedMutators.Add("StringCaseMutator");

			RunConfiguration config = new RunConfiguration();
			config.rangeStart = 1;
			config.rangeStop = 50;
			config.range = true;
			config.randomSeed = 12345;

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);

			Assert.AreEqual(50, mutations.Count);

			// 50 mutations, control on 1, 11, 21, 31, 41
			Assert.AreEqual(50 + 5, dataModels.Count);

			int lastSize = 0;

			// Skip data model 0, its the magical 1st pass w/o mutations
			for (int i = 0; i < 55; ++i)
			{
				Assert.AreEqual(1, dataModels[i].Count);
				Peach.Core.Dom.Array item = dataModels[i][0] as Peach.Core.Dom.Array;

				// Its either an array of 1 or an array of 2
				Assert.GreaterOrEqual(item.Count, 1);
				Assert.LessOrEqual(item.Count, 2);

				if (lastSize != item.Count)
				{
					// Change of data model should only occur at iteration 1, 11, 21, 31, 41
					// which is the 0, 11, 22, 33, 44 indices
					Assert.AreEqual(i / 10, i % 10);
					lastSize = item.Count;
				}

			}
		}

		[Test]
		public void Test7()
		{
			// Test that the random strategy is reproducible when starting at an
			// arbitrary iteration when configured to cycles through data models
			// with multiple actions
			string temp1 = Path.GetTempFileName();
			string temp2 = Path.GetTempFileName();
			string temp3 = Path.GetTempFileName();
			string temp4 = Path.GetTempFileName();

			File.WriteAllBytes(temp1, Encoding.ASCII.GetBytes("Foo1"));
			File.WriteAllBytes(temp2, Encoding.ASCII.GetBytes("Foo2Bar2"));
			File.WriteAllBytes(temp3, Encoding.ASCII.GetBytes("Foo3Bar3Baz3"));
			File.WriteAllBytes(temp4, Encoding.ASCII.GetBytes("Foo4Bar4Baz4Qux4"));

			// Test loading a dataset from a file
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel1\">" +
				"       <String name=\"str1\" value=\"Init\" maxOccurs=\"100\" lengthType=\"chars\" length=\"4\"/>" +
				"   </DataModel>" +

				"   <DataModel name=\"TheDataModel2\">" +
				"       <String name=\"str1\" value=\"Init\" maxOccurs=\"100\" lengthType=\"chars\" length=\"4\"/>" +
				"   </DataModel>" +

				"   <StateModel name=\"TheState\" initialState=\"Initial\">" +
				"       <State name=\"Initial\">" +
				"           <Action type=\"output\">" +
				"               <DataModel ref=\"TheDataModel1\"/>" +
				"               <Data fileName=\"" + temp1 + "\"/>" +
				"               <Data fileName=\"" + temp2 + "\"/>" +
				"           </Action>" +
				"           <Action type=\"output\">" +
				"               <DataModel ref=\"TheDataModel2\"/>" +
				"               <Data fileName=\"" + temp3 + "\"/>" +
				"               <Data fileName=\"" + temp4 + "\"/>" +
				"           </Action>" +
				"       </State>" +
				"   </StateModel>" +

				"   <Test name=\"Default\" maxOutputSize='65535'>" +
				"       <StateModel ref=\"TheState\"/>" +
				"       <Publisher class=\"Null\"/>" +
				"       <Strategy class=\"RandomStrategy\">" +
				"           <Param name=\"SwitchCount\" value=\"10\"/>" +
				"       </Strategy>" +
				"   </Test>" +
				"</Peach>";

			RunSwitchTest(xml, 1, 100);
			// 2 actions, 100 mutations, switch every 10
			Assert.AreEqual((100 + 10) * 2, dataModels.Count);
			Assert.AreEqual(100 * 2, mutatedDataModels.Count);

			var oldDataModels = dataModels;
			var oldMutations = mutatedDataModels;

			ResetContainers();
			Assert.AreEqual(0, dataModels.Count);
			Assert.AreEqual(0, mutatedDataModels.Count);

			RunSwitchTest(xml, 48, 100);
			// 2 actions, 53 mutations, control iterations at 48, 51, 61, 71, 81, 91
			Assert.AreEqual((53 + 6) * 2, dataModels.Count);
			Assert.AreEqual(53 * 2, mutatedDataModels.Count);

			oldDataModels.RemoveRange(0, oldDataModels.Count - dataModels.Count);
			Assert.AreEqual(dataModels.Count, oldDataModels.Count);

			oldMutations.RemoveRange(0, oldMutations.Count - mutatedDataModels.Count);
			Assert.AreEqual(oldMutations.Count, mutatedDataModels.Count);

			// Because there are two actions, the first two entries in dataModels are the 0th iteration
			var oldDm = oldMutations;
			var newDm = mutatedDataModels;

			for (int i = 2; i < oldDm.Count; ++i)
			{
				var oldVal = oldDm[i].Value.ToArray();
				var newVal = newDm[i].Value.ToArray();

				Assert.AreEqual(oldVal.Length, newVal.Length);

				for (int j = 0; j < oldVal.Length; ++j)
					if (oldVal[j] != newVal[j])
						Assert.Fail("Values differ at index: " + j.ToString());
			}
		}

		[Test]
		public void TestSwitchDataField()
		{
			string temp0 = Path.GetTempFileName();
			string temp1 = Path.GetTempFileName();
			string temp2 = Path.GetTempFileName();
			string temp3 = Path.GetTempFileName();

			File.WriteAllBytes(temp0, Encoding.ASCII.GetBytes("111Hello"));
			File.WriteAllBytes(temp1, Encoding.ASCII.GetBytes("222World"));
			File.WriteAllBytes(temp2, Encoding.ASCII.GetBytes("555Foo"));
			File.WriteAllBytes(temp3, Encoding.ASCII.GetBytes("666Bar"));

			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str1' value='000' length='3' mutable='false'/>
		<String name='str2'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data fileName='{0}'/>
				<Data fileName='{1}'/>
				<Data>
					<Field name='str1' value='333'/>
					<Field name='str2' value='Data1'/>
				</Data>
				<Data>
					<Field name='str1' value='444'/>
					<Field name='str2' value='Data2'/>
				</Data>
			</Action>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data fileName='{2}'/>
				<Data fileName='{3}'/>
				<Data>
					<Field name='str1' value='777'/>
					<Field name='str2' value='MoreData1'/>
				</Data>
				<Data>
					<Field name='str1' value='888'/>
					<Field name='str2' value='MoreData2'/>
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Strategy class='RandomStrategy'>
			<Param name='SwitchCount' value='2'/>
		</Strategy>
	</Test>
</Peach>".Fmt(temp0, temp1, temp2, temp3);

			RunSwitchTest(xml, 1, 100);

			Assert.AreEqual(300, dataModels.Count);
			var res = new Dictionary<string, int>();
			for (int i = 0; i < dataModels.Count; ++i)
			{
				var dm = dataModels[i];
				string key = (string)dm[0].InternalValue;
				int val = 0;
				res.TryGetValue(key, out val);
				res[key] = ++val;
			}

			Assert.AreEqual(8, res.Count);
		}

		[Test]
		public void TestSwitchValueTypeHex()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' mutable='false' />
		<Blob name='blb' mutable='false' />
		<Number name='num' size='32' mutable='false' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data>
					<Field name='str' valueType='hex' value='41 42 43 44'/>
					<Field name='blb' valueType='hex' value='45 46 47 48'/>
					<Field name='num' valueType='hex' value='49 4a 4b 4c'/>
				</Data>
				<Data>
					<Field name='str' valueType='hex' value='61 62 63 64'/>
					<Field name='blb' valueType='hex' value='65 66 67 68'/>
					<Field name='num' valueType='hex' value='69 6a 6b 6c'/>
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Strategy class='RandomStrategy'>
			<Param name='SwitchCount' value='2'/>
		</Strategy>
	</Test>
</Peach>";

			RunSwitchTest(xml, 1, 100);

			Assert.AreEqual(150, dataModels.Count);

			var res = new Dictionary<string, int>();
			for (int i = 0; i < dataModels.Count; ++i)
			{
				var dm = dataModels[i];
				var key = dm.InternalValue.BitsToString();
				int val = 0;
				res.TryGetValue(key, out val);
				res[key] = ++val;
			}

			Assert.AreEqual(2, res.Count);
			Assert.GreaterOrEqual(res["ABCDEFGHIJKL"], 70);
			Assert.GreaterOrEqual(res["abcdefghijkl"], 70);
		}

		[Test]
		public void ReEnterState()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Number name='num' size='8' mutable='false'>
			<Fixup class='SequenceIncrementFixup'>
				<Param name='Offset' value='0'/>
			</Fixup>
		</Number>
		<String name='str'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data>
					<Field name='str' value='Hello'/>
				</Data>
			</Action>
			<Action type='changeState' ref='Second'/>
		</State>

		<State name='Second'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data>
					<Field name='str' value='World'/>
				</Data>
			</Action>
			<Action type='changeState' ref='Initial' when='int(state.actions[0].dataModel[&quot;num&quot;].InternalValue) &lt; 4'/>
		</State>

	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
		<Strategy class='Random'>
			<Param name='MaxFieldsToMutate' value='1'/>
		</Strategy>
		<Mutators mode='include'>
			<Mutator class='StringCaseLower' />
			<Mutator class='StringCaseUpper' />
		</Mutators>
	</Test>
</Peach>";

			RunSwitchTest(xml, 1, 10);

			Assert.AreEqual(44, dataModels.Count);

			Assert.AreEqual("Hello", (string)dataModels[0][1].InternalValue);
			Assert.AreEqual("World", (string)dataModels[1][1].InternalValue);
			Assert.AreEqual("Hello", (string)dataModels[2][1].InternalValue);
			Assert.AreEqual("World", (string)dataModels[3][1].InternalValue);

			int total = 0;
			for (int i = 4; i < 44; i += 4)
			{
				// For any given iteration, only 1 field should be mutated
				int changed = 0;

				if ("Hello" != (string)dataModels[i + 0][1].InternalValue)
					++changed;
				if ("World" != (string)dataModels[i + 1][1].InternalValue)
					++changed;
				if ("Hello" != (string)dataModels[i + 2][1].InternalValue)
					++changed;
				if ("World" != (string)dataModels[i + 3][1].InternalValue)
					++changed;

				Assert.AreEqual(1, changed);
				total += changed;
			}
			Assert.AreEqual(10, total);
		}

		[Test]
		public void ControlAndSwitch()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data>
					<Field name='str' value='Data Field 1'/>
				</Data>
				<Data>
					<Field name='str' value='Data Field 2'/>
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default' controlIteration='5'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
		<Strategy class='Random'>
			<Param name='SwitchCount' value='2'/>
		</Strategy>
	</Test>
</Peach>";

			RunSwitchTest(xml, 1, 20);

			// 20 fuzz + 10 control & record
			// control at iterations 5, 10, 15, 20
			// 5 and 15 are already switching iterations
			// so we have 20 fuzz, 10 Ctrl&Rec, 2 Ctrl
			Assert.AreEqual(32, dataModels.Count);
		}

		[Test]
		public void TwoStates()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action>
			<Action type='changeState' ref='Second'/>
		</State>
		<State name='Second'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
		<Strategy class='Random'>
			<Param name='MaxFieldsToMutate' value='2'/>
		</Strategy>
		<Mutators mode='include'>
			<Mutator class='StringCaseLower' />
		</Mutators>
	</Test>
</Peach>";

			RunSwitchTest(xml, 1, 20);

			Assert.AreEqual(42, dataModels.Count);

			Assert.AreEqual("Hello", (string)dataModels[0][0].InternalValue);
			Assert.AreEqual("Hello", (string)dataModels[1][0].InternalValue);

			int total = 0;
			for (int i = 2; i < dataModels.Count; i += 2)
			{
				// When two fields are chosen, it should mutate one field in each state
				if ("Hello" != (string)dataModels[i + 0][0].InternalValue && "Hello" != (string)dataModels[i + 1][0].InternalValue)
					++total;
			}

			Assert.GreaterOrEqual(total, 1);
			Assert.LessOrEqual(total, 10);
		}

		[Test]
		public void SwitchWithIncludedDataModel()
		{
			string temp1 = Path.GetTempFileName();
			string temp2 = Path.GetTempFileName();
			string temp3 = Path.GetTempFileName();
			string temp4 = Path.GetTempFileName();

			string include = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello'/>
	</DataModel>
</Peach>
";

			string sm = @"
<Peach>
	<Include ns='other' src='{0}'/>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='other:DM'/>
				<Data fileName='{1}'/>
				<Data fileName='{2}'/>
				<Data>
					<Field name='str' value='Data Field 1'/>
				</Data>
				<Data>
					<Field name='str' value='Data Field 2'/>
				</Data>
			</Action>
		</State>
	</StateModel>
</Peach>".Fmt(temp1, temp2, temp3);

			string xml = @"
<Peach>
	<Include ns='sm' src='{0}'/>

	<Test name='Default'>
		<StateModel ref='sm:SM'/>
		<Publisher class='Null'/>
		<Strategy class='Random'>
			<Param name='SwitchCount' value='2'/>
		</Strategy>
		<Mutators mode='include'>
			<Mutator class='StringCaseLower' />
			<Mutator class='StringCaseUpper' />
		</Mutators>
	</Test>
</Peach>".Fmt(temp4);

			File.WriteAllText(temp1, include);
			File.WriteAllText(temp4, sm);
			File.WriteAllText(temp2, "Data Set 1");
			File.WriteAllText(temp3, "Data Set 2");

			RunSwitchTest(xml, 1, 20);

			Assert.AreEqual(30, dataModels.Count);

			int[] choices = new int[4];

			for (int i = 0; i < dataModels.Count; ++i)
			{
				string val = (string)dataModels[i][0].InternalValue;
				switch (val)
				{
					case "Data Set 1":
						choices[0] += 1;
						break;
					case "Data Set 2":
						choices[1] += 1;
						break;
					case "Data Field 1":
						choices[2] += 1;
						break;
					case "Data Field 2":
						choices[3] += 1;
						break;
					default:
						Assert.AreNotEqual("Hello", val);
						break;
				}
			}

			Assert.Greater(choices[0], 0);
			Assert.Greater(choices[1], 0);
			Assert.Greater(choices[2], 0);
			Assert.Greater(choices[3], 0);
		}

		private void RunSwitchTest(string xml, uint start, uint stop)
		{
			PitParser parser = new PitParser();

			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.rangeStart = start;
			config.rangeStop = stop;
			config.range = true;
			config.randomSeed = 12345;

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);
		}

		private void VerifySameResults(List<string> oldStrategies, List<Action> oldActions)
		{
			Assert.AreEqual(allStrategies.Count, oldStrategies.Count);
			Assert.AreEqual(actions.Count, oldActions.Count);

			for (int i = 0; i < allStrategies.Count; ++i)
			{
				Assert.AreEqual(allStrategies[i], oldStrategies[i]);
			}

			for (int i = 0; i < actions.Count; ++i)
			{
				Assert.AreEqual(actions[i].Name, oldActions[i].Name);
				Assert.AreEqual(actions[i].dataModel.Name, oldActions[i].dataModel.Name);
				var oldDataModel = oldActions[i].dataModel;
				var dataModel = actions[i].dataModel;

				//Assert.AreEqual(4, oldDataModel.Count);
				//Assert.AreEqual(4, dataModel.Count);
				Assert.AreEqual(oldDataModel.Count, dataModel.Count);

				for (int j = 0; j < oldDataModel.Count; ++j)
				{
					var lhs = oldDataModel[j].Value.ToArray();
					var rhs = dataModel[j].Value.ToArray();
					Assert.AreEqual(lhs, rhs);
				}
			}
		}

		private void RunEngine(string xml, uint start, uint stop)
		{
			var dom = ParsePit(xml);

			var config = new RunConfiguration();
			config.rangeStart = start;
			config.rangeStop = stop;
			config.range = true;
			config.randomSeed = 12345;

			var e = new Engine(this);
			e.startFuzzing(dom, config);
		}

		[Test]
		public void TestNoSwitch()
		{
			// Switching data sets causes a control iteration every time
			// the switch takes place.  Even if the user sets the 'SwitchCount'
			// value really low, the strategy should only switch when
			// there are multiple data sets on an action

			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data>
					<Field name='str' value='Data Field 1'/>
				</Data>
			</Action>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data>
					<Field name='str' value='Data Field 2'/>
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
		<Strategy class='Random'>
			<Param name='SwitchCount' value='1'/>
		</Strategy>
	</Test>
</Peach>";

			RunSwitchTest(xml, 1, 20);

			// 20 iterations, 1 control, 2 data models per iteration
			Assert.AreEqual(42, dataModels.Count);
		}

		[Test]
		public void StateMutations()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action>
			<Action type='changeState' ref='Second'/>
		</State>
		<State name='Second'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
		<Strategy class='Random'>
			<Param name='StateMutation' value='true'/>
		</Strategy>
	</Test>
</Peach>";

			RunSwitchTest(xml, 1, 20);

			// Normally there are 40 fuzzed outputs.
			// With state/action mutations there should be more.
			Assert.Greater(dataModels.Count, 40);
		}

		[Test]
		public void TestEqualWeights()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
		<Strategy class='Random'/>
		<Mutators mode='include'>
			<Mutator class='StringCaseLower' />
			<Mutator class='StringCaseUpper' />
		</Mutators>
	</Test>
</Peach>";

			RunSwitchTest(xml, 1, 500);

			// 1 control, 500 mutations
			Assert.AreEqual(501, iterStrategies.Count);

			int numUpper = 0;
			int numLower = 0;
			int numOther = 0;

			foreach (var item in iterStrategies)
			{
				if (item.StartsWith("StringCaseLower"))
					++numLower;
				else if (item.StartsWith("StringCaseUpper"))
					++numUpper;
				else
					++numOther;
			}

			Assert.AreEqual(1, numOther); // control iteration

			var pct = (1.0 * numUpper) / (numUpper + numLower);

			Assert.Greater(pct, 0.46);
			Assert.Less(pct, 0.54);
		}

		[Test]
		public void TestUnequalWeights()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
		<Strategy class='Random'>
			<Param name='MaxFieldsToMutate' value='1' />
		</Strategy>
		<Mutators mode='include'>
			<Mutator class='StringCaseLower' />
			<Mutator class='StringStatic' />
		</Mutators>
	</Test>
</Peach>";

			RunSwitchTest(xml, 1, 4000);

			// 1 control, 4000 mutations
			Assert.AreEqual(4001, iterStrategies.Count);

			int numStatic = 0;
			int numLower = 0;
			int numOther = 0;

			foreach (var item in iterStrategies)
			{
				if (item.StartsWith("StringCaseLower"))
					++numLower;
				else if (item.StartsWith("StringStatic"))
					++numStatic;
				else
					++numOther;
			}

			Assert.AreEqual(1, numOther); // control iteration

			// StringStatic has 1659, StringCaseLower has 1
			// Default is log10(x) scaling so with the adjusted weight
			// in 4000 iterations, it should run ~20% of the time

			Assert.LessOrEqual(numLower, 4000 / 4.5);
			Assert.AreEqual(4000 - numLower, numStatic);
		}

		[Test]
		public void TestCombinationExpansion()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Blob name='str' value='Hello' minOccurs='1'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default' maxOutputSize='65000'>
		<StateModel ref='SM'/>
		<Publisher class='Null'>
			<Param name='MaxOutputSize' value='100000' />
		</Publisher>
		<Strategy class='Random'/>
	</Test>
</Peach>";

			RunSwitchTest(xml, 1, 100);
		}


		[Test]
		public void TestWeights()
		{
			const string xml = @"
<Peach>
	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel name='DM'>
					<String name='off' />
					<String name='lowest' />
					<String name='low' />
					<String name='normal' />
					<String name='high' />
					<String name='highest' />
				</DataModel>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
		<Strategy class='Random'>
			<Param name='MaxFieldsToMutate' value='1' />
		</Strategy>

		<Weight xpath='//off' weight='Off' />
		<Weight xpath='//lowest' weight='Lowest' />
		<Weight xpath='//low' weight='BelowNormal' />
		<Weight xpath='//high' weight='AboveNormal' />
		<Weight xpath='//highest' weight='Highest' />
	</Test>
</Peach>
";

			var dom = ParsePit(xml);
			var cfg = new RunConfiguration
			{
				range = true,
				rangeStart = 0,
				rangeStop = 10000
			};

			var e = new Engine(null);

			var count = new Dictionary<string, int>();

			e.TestStarting += ctx =>
			{
				ctx.DataMutating += (c, actionData, element, mutator) =>
				{
					int cnt;
					if (!count.TryGetValue(element.Name, out cnt))
						cnt = 0;
					else
						++cnt;

					count[element.Name] = cnt;
				};
			};

			e.startFuzzing(dom, cfg);

			foreach (var x in count)
				Console.WriteLine("Elem: {0}, Count: {1}", x.Key, x.Value);

			Assert.Less(count["lowest"], count["low"]);
			Assert.Less(count["low"], count["normal"]);
			Assert.Less(count["normal"], count["high"]);
			Assert.Less(count["high"], count["highest"]);

			Assert.False(count.ContainsKey("off"), "off shouldn't be mutated");
		}
	}
}

// end
