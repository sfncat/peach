using NUnit.Framework;
using Peach.Core.Dom;

namespace Peach.Core.Test
{
	[TestFixture]
	[Quick]
	internal class DataElementContainerTests : DataModelCollector
	{
		[Test]
		[Category("Peach")]
		public void RemoteCleanup()
		{
			var pit = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='Example1'>
		<Block name='Test'>
			<Number name='Of' size='16'>
				<Relation type='size' of='Data'/>
			</Number>
			<Blob name='Data' />
		</Block>
	</DataModel>
</Peach>
";
			var dom = ParsePit(pit);
			var testElement = dom.dataModels[0][0];
			var ofElement = dom.dataModels[0].find("Of");

			Assert.AreEqual(1, ofElement.relations.Count);

			dom.dataModels[0].Remove(testElement, false);

			Assert.AreEqual(1, ofElement.relations.Count);

			dom = ParsePit(pit);
			testElement = dom.dataModels[0][0];
			ofElement = dom.dataModels[0].find("Of");

			Assert.AreEqual(1, ofElement.relations.Count);

			dom.dataModels[0].Remove(testElement, true);

			Assert.AreEqual(0, ofElement.relations.Count);
		}


		[Test]
		[Category("Peach")]
		public void SetItemByName()
		{
			var pit = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='Example1'>
		<Block name='Item1'/>
		<String name='Item2'/>
		<Block name='Item3'/>
		<Block name='Item4'/>
	</DataModel>
</Peach>
";
			var dom = ParsePit(pit);
			var item2 = dom.dataModels[0]["Item2"];
			var newItem2 = new Block(item2.Name);

			Assert.AreEqual("Item1", dom.dataModels[0][0].Name);
			Assert.AreEqual("Item2", dom.dataModels[0][1].Name);
			Assert.AreEqual("Item3", dom.dataModels[0][2].Name);
			Assert.AreEqual("Item4", dom.dataModels[0][3].Name);

			item2.parent[item2.Name] = newItem2;

			Assert.AreEqual("Item1", dom.dataModels[0][0].Name);
			Assert.AreEqual("Item2", dom.dataModels[0][1].Name);
			Assert.AreEqual("Item3", dom.dataModels[0][2].Name);
			Assert.AreEqual("Item4", dom.dataModels[0][3].Name);
		}
	}
}
