using System.Linq;
using NUnit.Framework;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	[Quick]
	[Peach]
	class StringLengthEdgeCaseTests : DataModelCollector
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("StringLengthEdgeCase");

			var str = new Peach.Core.Dom.String("String");

			Assert.True(runner.IsSupported(str));
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("StringLengthEdgeCase");

			var str = new Peach.Core.Dom.String("String");

			var m = runner.Sequential(str);

			// All edges from 0 to ushort max +/- 50
			// [0,50], 127 +/- 50, 255 +/- 50, 32767 +/- 50, 65535 - 50
			// 51        101          101        101          51
			Assert.AreEqual(405, m.Count());

			foreach (var item in m)
			{
				var asStr = (string)item.InternalValue;
				Assert.NotNull(asStr);

				var val = item.Value.ToArray();
				Assert.NotNull(val);

				// Are all ascii strings
				Assert.AreEqual(asStr.Length, val.Length);
			}
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("StringLengthEdgeCase");

			var str = new Peach.Core.Dom.String("String");

			var m = runner.Random(1000, str);
			Assert.AreEqual(1000, m.Count());

			// Ensure all items are strings
			foreach (var item in m)
			{
				var asStr = (string)item.InternalValue;
				Assert.NotNull(asStr);

				var val = item.Value.ToArray();
				Assert.NotNull(val);

				// Are all ascii strings
				Assert.AreEqual(asStr.Length, val.Length);
			}
		}

		[Test]
		public void TestMaxOutputSize()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
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
			<Mutator class='StringLengthEdgeCase' />
		</Mutators>
	</Test>
</Peach>
";

			RunEngine(xml);

			// Sizes 0 to 50 is 51 mutations
			Assert.AreEqual(51, mutatedDataModels.Count);

			foreach (var m in mutatedDataModels)
				Assert.LessOrEqual(m.Value.Length, 50);
		}
	}
}
