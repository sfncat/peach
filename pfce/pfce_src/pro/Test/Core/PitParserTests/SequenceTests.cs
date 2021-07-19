using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.PitParserTests
{
	[TestFixture]
	[Category("Peach")]
	[Quick]
	class SequenceTests
	{
		[Test]
		public void FieldSettingName()
		{
			const string xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='DM'>
		<Sequence name='seq'>
			<Block name='blk'>
				<String name='str'/>
			</Block>
			<String name='str'/>
		</Sequence>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
				<Data>
					<Field name='seq.blk.str' value='Hello' />
					<Field name='seq.str' value='World' />
				</Data>
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

			e.startFuzzing(dom, cfg);

			var val = dom.tests[0].stateModel.states[0].actions[0].dataModel.InternalValue.BitsToString();

			Assert.AreEqual("HelloWorld", val);
		}

		[Test]
		public void FieldSettingIndex()
		{
			const string xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='DM'>
		<Sequence name='seq'>
			<Block name='blk'>
				<String name='str'/>
			</Block>
			<String name='str'/>
		</Sequence>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
				<Data>
					<Field name='seq[0].str' value='Hello' />
					<Field name='seq[1]' value='World' />
				</Data>
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

			e.startFuzzing(dom, cfg);

			var val = dom.tests[0].stateModel.states[0].actions[0].dataModel.InternalValue.BitsToString();

			Assert.AreEqual("HelloWorld", val);
		}
	}
}
