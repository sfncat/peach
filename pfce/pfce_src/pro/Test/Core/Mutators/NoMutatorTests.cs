using NUnit.Framework;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	[Quick]
	[Peach]
	class NoutatorTests : DataModelCollector
	{
		[Test]
		public void Test()
		{
			// Verify only control iteration runs when no mutations are available

			string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String name='str1' value='Hello, World!' mutable='false' />
	</DataModel>

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
		<Strategy class='Sequential' />
	</Test>
</Peach>";

			RunEngine(xml);

			// Only ran 1 iteration
			Assert.AreEqual(1, values.Count);

			// Performed zero mutations
			Assert.AreEqual(0, mutations.Count);
		}
	}
}
