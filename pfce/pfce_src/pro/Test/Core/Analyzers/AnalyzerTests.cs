using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Analyzers
{
	public class AnalyzerTests
	{
		[Test]
		[Peach]
		[Quick]
		public void TestFieldData()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str'>
			<Analyzer class='StringToken'/>
		</String>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data>
					<Field name='str' value='Some,String,To,Split'/>
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
	</Test>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			var config = new RunConfiguration {singleIteration = true};
			var e = new Engine(null);
			e.startFuzzing(dom, config);

			var dataModel = dom.tests[0].stateModel.states[0].actions[0].dataModel;
			var numElements = dataModel.EnumerateAllElements().Count();
			Assert.AreEqual(11, numElements);
		}
	}

}