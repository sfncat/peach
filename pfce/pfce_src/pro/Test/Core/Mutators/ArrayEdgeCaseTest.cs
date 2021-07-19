using NUnit.Framework;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	[Quick]
	[Peach]
	class ArrayEdgeCaseTests : DataModelCollector
	{
		// TODO: Test we hit +/- N around each edge case
		// TODO: Test the hint works as well
		// TODO: Ensure if data element remove fires that it doesn't screw up expansion
		// TODO: Test the point of insertion/removal moves around with each mutation
		// TODO: Test count relation overflow

		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("ArrayEdgeCase");

			var array = new Peach.Core.Dom.Array("Array")
			{
				OriginalElement = new Peach.Core.Dom.String("Str")
			};
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
            var runner = new MutatorRunner("ArrayEdgeCase");

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
			const string xml = @"
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
			<Mutator class='ArrayEdgeCase' />
		</Mutators>
	</Test>
</Peach>
";

			RunEngine(xml);

			// Size is 11 bytes, max is 1024, default is 11 bytes
			// (1024 - 11)/11 = 92 (expansions)
			// plus 1 reduce for 93 mutations total
			Assert.AreEqual(93, mutatedDataModels.Count);
		}

        [Test]
        public void SequenceTestMaxOutputSize()
        {
            const string xml = @"
<Peach>
	<DataModel name='DM'>
        <Sequence name='seq'>
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
			<Mutator class='ArrayEdgeCase' />
		</Mutators>
	</Test>
</Peach>
";

            RunEngine(xml);

            // Size is 11 bytes, max is 1024, default is 11 bytes
            // (1024 - 11)/11 = 92 (expansions)
            // plus 1 reduce for 93 mutations total
            Assert.AreEqual(93, mutatedDataModels.Count);
        }
	}
}
