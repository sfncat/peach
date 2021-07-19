using System.IO;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Core.Test;
using System.Collections.Generic;
using Peach.Core.IO;
using Peach.Pro.Core.Publishers;
using NLog;

namespace Peach.Pro.Test.Core.StateModel
{
	[TestFixture]
	[Quick]
	[Peach]
	class SlurpTests
	{
		[Test]
		public void Test1()
		{
			string xml = @"
<Peach>
	<DataModel name='TheDataModel1'>
		<String name='String1' value='1234567890'/>
	</DataModel>
	<DataModel name='TheDataModel2'>
		<String name='String2' value='Hello World!'/>
	</DataModel>

	<StateModel name='TheStateModel' initialState='InitialState'>
		<State name='InitialState'>
			<Action name='Action1' type='output'>
				<DataModel ref='TheDataModel1'/>
			</Action>

			<Action name='Action2' type='slurp' valueXpath='//String1' setXpath='//String2' />

			<Action name='Action3' type='output'>
				<DataModel ref='TheDataModel2'/>
			</Action>

			<Action name='Action4' type='output'>
				<DataModel ref='TheDataModel2'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheStateModel'/>
		<Publisher class='Null'/>
		<Strategy class='RandomDeterministic'/>
	</Test>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			var config = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);
			e.startFuzzing(dom, config);

			var stateModel = dom.tests[0].stateModel;
			var state = stateModel.initialState;

			Assert.AreEqual(state.actions[0].dataModel.Value.ToArray(), state.actions[2].dataModel.Value.ToArray());
			Assert.AreEqual(state.actions[0].dataModel.Value.ToArray(), state.actions[3].dataModel.Value.ToArray());
		}

		[Test]
		public void Test2()
		{
			string xml = @"
<Peach>
	<DataModel name='TheDataModel1'>
		<String name='String1' value='1234567890'/>
	</DataModel>
	<DataModel name='TheDataModel2'>
		<String name='String2' value='Hello World!'/>
	</DataModel>

	<StateModel name='TheStateModel' initialState='InitialState'>
		<State name='InitialState'>
			<Action name='Action1' type='output'>
				<DataModel ref='TheDataModel1'/>
			</Action>

			<Action name='Action2' type='slurp' valueXpath='//Action1/TheDataModel1/String1' setXpath='//String2' />

			<Action name='Action3' type='output'>
				<DataModel ref='TheDataModel1'/>
			</Action>

			<Action name='Action4' type='output'>
				<DataModel ref='TheDataModel2'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheStateModel'/>
		<Publisher class='Null'/>
		<Strategy class='RandomDeterministic'/>
	</Test>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			var config = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);
			e.startFuzzing(dom, config);

			var stateModel = dom.tests[0].stateModel;
			var state = stateModel.initialState;

			Assert.AreEqual(state.actions[0].dataModel.Value.ToArray(), state.actions[2].dataModel.Value.ToArray());
			Assert.AreEqual(state.actions[0].dataModel.Value.ToArray(), state.actions[3].dataModel.Value.ToArray());
		}

		[Test]
		public void SlurpSelectedFieldOutput()
		{
			string xml = @"
<Peach>
	<DataModel name='DM1'>
		<String name='strSource' value='Hello'/>
	</DataModel>

<DataModel name='DM2'>
		<Choice name='q'>
			<Blob name='blob'/>
			<Block name='blk'>
				<String name='str1'/>
				<String name='str2'/>
			</Block>
		</Choice>
	</DataModel>

	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='DM1'/>
			</Action> 

			<Action type='slurp' valueXpath='//strSource' setXpath='//str1' />

			<Action type='output'>
				<DataModel ref='DM2'/>
				<Data>
					<Field name='q.blk.str2' value='World' />
				</Data>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));
			var config = new RunConfiguration { singleIteration = true, };
			var engine = new Engine(null);
			engine.startFuzzing(dom, config);

			var dm = dom.tests[0].stateModel.states["initial"].actions[2].dataModel;
			var val = dm.Value.ToArray();
			var exp = Encoding.ASCII.GetBytes("HelloWorld");

			Assert.AreEqual(exp, val);
		}

		[Test]
		public void SlurpSelectedFieldOutputSwitch()
		{
			// Ensure that slurp will set values for non-selected choice elements
			// The engine runs, and before the state model starts, all data models
			// get cached in originalDataModel.  When this happens, DM2 uses the
			// first data set called "Field1" that picks the choice "blk1"

			// The state model starts to run, the slurp action runs which should
			// set every element called "str1" to have the value "Hello".

			// The final output action runs, and the RandomStrategy switches the
			// dataSet to "Field2".  This dataSet selects choice "blk2"

			// The user expects the "str1" in "blk2" to be "Hello" because of the slurp.

			string xml = @"
<Peach>
	<DataModel name='DM1'>
		<String name='strSource' value='Hello'/>
	</DataModel>

	<DataModel name='DM2'>
		<Choice name='q'>
			<Blob name='blob'/>
			<Block name='blk1'>
				<String name='str1'/>
				<String name='str2'/>
			</Block>
			<Block name='blk2'>
				<String name='str1'/>
				<String name='str2'/>
			</Block>
		</Choice>
	</DataModel>

	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='DM1'/>
			</Action> 

			<Action type='slurp' valueXpath='//strSource' setXpath='//str1' />

			<Action type='output'>
				<DataModel ref='DM2'/>
				<Data name='Field1'>
					<Field name='q.blk1.str2' value='World 1' />
				</Data>
				<Data name='Field2'>
					<Field name='q.blk2.str2' value='World 2' />
				</Data>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default'>
		<Strategy class='Random'>
			<Param name='SwitchCount' value='2' />
		</Strategy>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration
			{
				singleIteration = true,
				randomSeed = 1,
				skipToIteration = 9,
			};

			var engine = new Engine(null);
			engine.startFuzzing(dom, config);

			var actionData = dom.tests[0].stateModel.states["initial"].actions[2].allData.First();
			Assert.AreEqual("Field2", actionData.selectedData.Name);
			var dm = actionData.dataModel;
			var val = Encoding.ASCII.GetString(dm.Value.ToArray());

			Assert.AreEqual("HelloWorld 2", val);
		}

		[Test]
		public void SlurpInScopeChoice()
		{
			// Ensure that slurp will only get values for selected choice elements
			// even if multiple elements of that name exist in non-selected
			// or out of scope choice elements

			string xml = @"
<Peach>
	<DataModel name='DM1'>
		<String name='strDest' value='Hello'/>
	</DataModel>

	<DataModel name='DM2'>
		<Choice name='q'>
			<Block name='blk1'>
				<String name='str'/>
			</Block>
			<Block name='blk2'>
				<String name='str'/>
			</Block>
		</Choice>
	</DataModel>

	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action name='act1' type='output'>
				<DataModel ref='DM2'/>
				<Data>
					<Field name='q.blk2.str' value='World' />
				</Data>
			</Action> 

			<Action type='slurp' valueXpath='//act1//str' setXpath='//strDest' />

			<Action type='output'>
				<DataModel ref='DM1'/>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default'>
		<Strategy class='Random'/>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration
			{
				singleIteration = true,
				randomSeed = 1,
				skipToIteration = 9,
			};

			var engine = new Engine(null);
			engine.startFuzzing(dom, config);

			var actionData = dom.tests[0].stateModel.states["initial"].actions[2].allData.First();
			var dm = actionData.dataModel;
			var val = Encoding.ASCII.GetString(dm.Value.ToArray());

			Assert.AreEqual("World", val);
		}

		[Test]
		public void SlurpArrayZeroOccurs()
		{
			string xml = @"
<Peach>
	<DataModel name='Token'>
		<String name='token'/>
	</DataModel>

	<DataModel name='Data'>
		<Block minOccurs='0'>
			<String/>
			<String name='key' token='true' />
		</Block>
	</DataModel>

	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='Token'/>
				<Data>
					<Field name='token' value='EOL'/>
				</Data>
			</Action> 

			<Action type='slurp' valueXpath='//token' setXpath='//key' />

			<Action type='output'>
				<DataModel ref='Data'/>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default'>
		<Strategy class='Random'/>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));
			var config = new RunConfiguration { singleIteration = true, };
			var engine = new Engine(null);
			engine.startFuzzing(dom, config);

			var actionData = dom.tests[0].stateModel.states["initial"].actions[2].allData.First();
			var dm = actionData.dataModel;
			var val = dm.Value.ToArray();

			// In this example, the data model is zero sized (array count is 0)
			// but array.OriginalElement should have its value set via slurp
			// so that expansions, or using the model for input w/token=true
			// will honor the slurped value.

			Assert.AreEqual(new byte[0], val);

			var array = dm[0] as Array;
			Assert.NotNull(array);
			Assert.NotNull(array.OriginalElement);

			Assert.AreEqual("EOL", array.OriginalElement.InternalValue.BitsToString());
		}

		[Test]
		public void SlurpElementPositions()
		{
			string xml = @"
<Peach>
	<DataModel name='Token'>
		<Blob name='source' value='hello' />
	</DataModel>

	<DataModel name='Data'>
		<Block name='blk1'>
			<Blob name='target' />
		</Block>
		<Block name='blk2'>
			<Blob name='target' />
		</Block>
	</DataModel>

	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='Token'/>
			</Action> 

			<Action type='slurp' valueXpath='//source' setXpath='//target' />

			<Action type='output'>
				<DataModel ref='Data'/>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default'>
		<Strategy class='Random'/>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));
			var config = new RunConfiguration { singleIteration = true, };
			var engine = new Engine(null);
			engine.startFuzzing(dom, config);

			var actionData = dom.tests[0].stateModel.states["initial"].actions[2].allData.First();
			var dm = actionData.dataModel;
			var val = dm.Value.ToArray();
			var exp = Encoding.ASCII.GetBytes("hellohello");

			Assert.AreEqual(exp, val);

			// Ensure that when slurping a single element to multiple targets
			// that the targets maintain their proper element names in the
			// final bitstream list.

			var pos = dm.Value.ElementNames();

			var names = new[]
			{
				"Data",
				"Data.blk1",
				"Data.blk1.target",
				"Data.blk2",
				"Data.blk2.target",
			};

			Assert.That(pos, Is.EqualTo(names));
		}

		[Test]
		public void SlurpBadValueXpathName()
		{
			const string xml = @"
<Peach>
	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel name='DM'>
					<String name='Str' />
				</DataModel>
			</Action> 

			<Action type='slurp' valueXpath='//01-source' setXpath='//target' />
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			var config = new RunConfiguration { singleIteration = true };
			var engine = new Engine(null);

			var ex = Assert.Throws<PeachException>(() => engine.startFuzzing(dom, config));

			Assert.AreEqual("Error, slurp valueXpath is not a valid xpath selector. [//01-source]", ex.Message);
		}

		[Test]
		public void SlurpBadSetXpathName()
		{
			const string xml = @"
<Peach>
	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel name='DM'>
					<String name='Str' />
				</DataModel>
			</Action> 

			<Action type='slurp' valueXpath='//Str' setXpath='//01-target' />
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			var config = new RunConfiguration { singleIteration = true };
			var engine = new Engine(null);

			var ex = Assert.Throws<PeachException>(() => engine.startFuzzing(dom, config));

			Assert.AreEqual("Error, slurp setXpath is not a valid xpath selector. [//01-target]", ex.Message);
		}

		[Test]
		public void TestCache()
		{
			string xml = @"
<Peach>
	<DataModel name='TheDataModel1'>
		<String name='String1' value='1234567890'/>
	</DataModel>
	<DataModel name='TheDataModel2'>
		<String name='String2' value='Hello World!'/>
	</DataModel>

	<StateModel name='TheStateModel' initialState='InitialState'>
		<State name='InitialState'>
			<Action name='Action1' type='output'>
				<DataModel ref='TheDataModel1'/>
			</Action>

			<Action name='Action2' type='slurp' valueXpath='//String1' setXpath='//String2' />

			<Action name='Action3' type='output'>
				<DataModel ref='TheDataModel2'/>
			</Action>

			<Action name='Action4' type='output'>
				<DataModel ref='TheDataModel2'/>
				<Data><Field name='String2' value='Foo' /></Data>
				<Data><Field name='String2' value='Bar' /></Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheStateModel'/>
		<Publisher class='Null'/>
		<Exclude/>
		<Strategy class='RandomStrategy'>
			<Param name='SwitchCount' value='2'/>
		</Strategy>
	</Test>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			var config = new RunConfiguration
			{
				range = true,
				rangeStart = 0,
				rangeStop = 10,
			};
			var e = new Engine(null);
			var results = new List<string>();
			e.IterationFinished += (context, iteration) =>
			{
				var sm = context.test.stateModel;
				var initial = sm.initialState;

				var dm0 = initial.actions[0].dataModel;
				var dm2 = initial.actions[2].dataModel;
				var dm3 = initial.actions[3].dataModel;

				var result = string.Join("|",
					StringValue(dm0.Value),
					StringValue(dm2.Value),
					StringValue(dm3.Value)
				); 

				results.Add(result);
			};

			var stateModel = dom.tests[0].stateModel;
			var state = stateModel.initialState;
	
			var source = System.Text.Encoding.ASCII.GetString(state.actions[0].dataModel.Value.ToArray());
			var value = "{0}|{0}|{0}".Fmt(source);
			var expected = Enumerable.Repeat(value, 15);

			e.startFuzzing(dom, config);

			CollectionAssert.AreEqual(expected, results);
		}

		string StringValue(BitwiseStream bs)
		{
			return System.Text.Encoding.ASCII.GetString(bs.ToArray());
		}

		class TestPublisher : Peach.Core.Publishers.StreamPublisher
		{
			private static readonly NLog.Logger ClassLogger = NLog.LogManager.GetCurrentClassLogger();

			private int _seqNum = 0;

			public TestPublisher(Dictionary<string, Variant> args) : base(args)
			{
			}

			protected override NLog.Logger Logger { get { return ClassLogger; } }

			protected override void OnOpen()
			{
				stream = new MemoryStream(Encoding.ASCII.GetBytes((++_seqNum).ToString()));
			}

			protected override void OnOutput(BitwiseStream data)
			{
			}
		}

		[Test]
		public void TestChangesCache()
		{
			string xml = @"
<Peach>
	<DataModel name='TheDataModel1'>
		<String name='String1' />
	</DataModel>
	<DataModel name='TheDataModel2'>
		<Block name='b' mutable='false'>
			<Choice mutable='false'>
				<Block mutable='false'>
					<String name='String2' mutable='false' value='foo' />
				</Block>
				<Block mutable='false'>
					<String name='Alt'/>
				</Block>
			</Choice>
		</Block>
		<String name='CRLF' value='0d0a' valueType='hex' mutable='false' />
		<String name='Payload' value='Hello World' />
	</DataModel>

	<StateModel name='TheStateModel' initialState='InitialState'>
		<State name='InitialState'>
			<Action name='Action1' type='input'>
				<DataModel ref='TheDataModel1'/>
			</Action>

			<Action name='Action2' type='slurp' valueXpath='//String1' setXpath='//String2' />

			<Action name='Action3' type='output'>
				<DataModel ref='TheDataModel2'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheStateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			var config = new RunConfiguration
			{
				range = true,
				rangeStart = 0,
				rangeStop = 10,
			};
			var e = new Engine(null);
			var results = new List<string>();
			e.IterationFinished += (context, iteration) =>
			{
				var sm = context.test.stateModel;
				var initial = sm.initialState;

				var dm0 = initial.actions[0].dataModel;
				var dm2 = initial.actions[2].dataModel;

				var result = string.Join("|",
					StringValue(dm0.Value),
					StringValue(dm2.Value)
				);

				results.Add(result);
			};

			dom.tests[0].publishers[0] = new TestPublisher(new Dictionary<string, Variant>()) {Name = "Pub"};

			e.startFuzzing(dom, config);

			Assert.AreEqual(11, results.Count);
			for (var i = 0; i < results.Count; ++i)
			{
				var expected = "{0}|{0}\r\n".Fmt(i + 1);
				StringAssert.StartsWith(expected, results[i]);
			}
		}
	}
}
