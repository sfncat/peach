using NUnit.Framework;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	[Quick]
	[Peach]
	class SizedEdgeCaseTests : DataModelCollector
	{
		[Test]
		public void TestMaxOutputSize()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Number name='num' size='32'>
			<Relation type='size' of='str' />
		</Number>
		<String name='str' value='Hello World' />
	</DataModel>

	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default' maxOutputSize='50'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='SizedEdgeCase' />
		</Mutators>
	</Test>
</Peach>
";

			RunEngine(xml);

			// min is 0, max is 46 = 47 mutations
			Assert.AreEqual(47, mutatedDataModels.Count);

			foreach (var m in mutatedDataModels)
				Assert.LessOrEqual(m.Value.Length, 50);
		}
	}
}
