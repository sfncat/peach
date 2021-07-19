using NUnit.Framework;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	[Quick]
	[Peach]
	class ArrayVarianceTests : DataModelCollector
	{
		// TODO: Test we hit +/- N around default value
		// TODO: Test the hint works as well (default is 50)
		// TODO: Ensure we never get default count as a mutation
		// TODO: Ensure if data element remove fires that it doesn't screw up expansion
		// TODO: Test the point of insertion/removal moves around with each mutation
		// TODO: Test count relation overflow

		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("ArrayVariance");

			var array = new Peach.Core.Dom.Array("Array");
			array.OriginalElement = new Peach.Core.Dom.String("Str");
			array.ExpandTo(0);

			// Empty array can be expanded
			Assert.True(runner.IsSupported(array));

			// Single element array can be expanded
			array.ExpandTo(1);
			Assert.True(runner.IsSupported(array));

			// Anything > 1 element is expandable
			array.ExpandTo(2);
			Assert.True(runner.IsSupported(array));

			array.ExpandTo(10);
			Assert.True(runner.IsSupported(array));

			array.isMutable = false;
			Assert.False(runner.IsSupported(array));
		}

		[Test]
		public void SequenceSupportedTest()
		{
			var runner = new MutatorRunner("ArrayVariance");

			var array = new Peach.Core.Dom.Sequence("Sequence");

			// Empty array can be expanded
			Assert.False(runner.IsSupported(array));

			// Single element array can be expanded
			array.Add(new Peach.Core.Dom.String("Str"));
			Assert.True(runner.IsSupported(array));

			// Anything > 1 element is expandable
			array.Add(new Peach.Core.Dom.String("Str2"));
			Assert.True(runner.IsSupported(array));

			array.isMutable = false;
			Assert.False(runner.IsSupported(array));
		}

		[Test]
		public void TestMaxOutputSize()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello World' minOccurs='1' />
	</DataModel>

	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default' maxOutputSize='1024'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='ArrayVariance' />
		</Mutators>
	</Test>
</Peach>
";

			RunEngine(xml);

			// Size is 11 bytes, max is 1024, default is 11 bytes
			// (1024 - 11)/11 - 1 (skip 0) = 91
			// plus 1 reduce for 92 mutations total
			Assert.AreEqual(92, mutatedDataModels.Count);
		}

		[Test]
		public void SequenceMaxOutputSizeTest()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
        <Sequence name='seq'>
            <Number name='num' size='8' value='1'/>
		    <String name='str' value='Hello World'/>
	    </Sequence>
    </DataModel>

	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default' maxOutputSize='1024'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='ArrayVariance' />
		</Mutators>
	</Test>
</Peach>
";

			RunEngine(xml);

			// Size is 11 bytes, max is 1024, default is 11 bytes
			// (1024 - 11)/11 - 1 (skip 0) = 91
			// plus 1 reduce for 92 mutations total
			Assert.AreEqual(92, mutatedDataModels.Count);
		}
	}
}
