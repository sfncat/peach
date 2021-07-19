using System.IO;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.StateModel
{
	[TestFixture]
	[Quick]
	[Peach]
	class InputTests
	{
		[Test]
		public void Test1()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel1\">" +
				"       <String value=\"Hello\"/>" +
				"   </DataModel>" +

				"   <StateModel name=\"TheStateModel\" initialState=\"InitialState\">" +
				"       <State name=\"InitialState\">" +
				"           <Action name=\"Action1\" type=\"input\">" +
				"               <DataModel ref=\"TheDataModel1\"/>" +
				"           </Action>" +
				"       </State>" +
				"   </StateModel>" +

				"   <Test name=\"Default\">" +
				"       <StateModel ref=\"TheStateModel\"/>" +
				"       <Publisher class=\"Null\"/>" +
				"       <Strategy class=\"RandomDeterministic\"/>" +
				"   </Test>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			MemoryStream stream = new MemoryStream(ASCIIEncoding.ASCII.GetBytes("Hello World!"));
			dom.tests[0].publishers[0] = new MemoryStreamPublisher(stream);

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(null);
			e.startFuzzing(dom, config);

			var stateModel = dom.tests[0].stateModel;
			var state = stateModel.initialState;

			Assert.AreEqual("Input", state.actions.First().type);
			Assert.AreEqual(ASCIIEncoding.ASCII.GetBytes("Hello World!"), state.actions[0].dataModel.Value.ToArray());
		}

		[Test]
		public void CrackFailSaveData()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String length='3' value='0'>
			<Relation type='size' of='value' />
		</String>
		<String value='two' token='true' />
		<String name='value' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action name='Act1' type='input'>
				<DataModel ref='DM' />
			</Action>
			<Action name='Act2' type='input'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null' />
	</Test>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);

			const string part1 = "005twothree";
			const string part2 = "007TWOlongstuff";
			var exp = Encoding.ASCII.GetBytes(part1 + part2);

			dom.tests[0].publishers[0] = new MemoryStreamPublisher(new MemoryStream(exp));

			var e = new Engine(null);

			var cfg = new RunConfiguration
			{
				singleIteration = true,
			};

			var ex = Assert.Throws<PeachException>(() => e.startFuzzing(dom, cfg));

			StringAssert.IsMatch("String 'DM.DataElement_1' failed to crack. Token did not match 'TWO' vs. 'two'.", ex.Message);

			var data = dom.tests[0].stateModel.dataActions.ToList();

			Assert.AreEqual(2, data.Count);

			Assert.AreEqual("1.Initial.Act1.bin", data[0].Key);
			Assert.AreEqual(Encoding.ASCII.GetBytes(part1), data[0].Value.ToArray());

			Assert.AreEqual("2.Initial.Act2.bin", data[1].Key);
			Assert.AreEqual(Encoding.ASCII.GetBytes(part2), data[1].Value.ToArray());
		}
	}
}
