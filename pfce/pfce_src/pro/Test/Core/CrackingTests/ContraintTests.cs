

using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.CrackingTests
{
	[TestFixture]
	[Quick]
	[Peach]
	public class ConstraintTests
	{
		[Test]
		public void ConstraintChoice1()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Choice>" +
				"			<Blob name=\"Blob10\" length=\"5\" constraint=\"len(value) &lt; 3\" />" +
				"			<Blob name=\"Blob5\" length=\"5\" />" +
				"		</Choice>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 1, 2, 3, 4, 5 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.IsTrue(dom.dataModels[0][0] is Choice);
			Assert.AreEqual("Blob5", ((Choice)dom.dataModels[0][0])[0].Name);
			Assert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, ((DataElementContainer)dom.dataModels[0][0])[0].DefaultValue.BitsToArray());
		}

		[Test]
		public void ConstraintChoice2()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Choice>" +
				"			<Blob name=\"Blob10\" length=\"5\" constraint=\"len(value) &gt; 3\" />" +
				"			<Blob name=\"Blob5\" length=\"5\" />" +
				"		</Choice>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 1, 2, 3, 4, 5 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.IsTrue(dom.dataModels[0][0] is Choice);
			Assert.AreEqual("Blob10", ((Choice)dom.dataModels[0][0])[0].Name);
			Assert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, ((DataElementContainer)dom.dataModels[0][0])[0].DefaultValue.BitsToArray());
		}

		[Test]
		public void ConstraintRegex()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<Import import=\"re\"/>" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String constraint=\"re.search('^\\w+$', value) != None\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "Hello");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello", (string)dom.dataModels[0][0].DefaultValue);
		}

		[Test]
		public void ConstraintNumberRelation()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Block minOccurs='0'>
			<Number size='8' constraint='int(value) > 0'>
				<Relation type='size' of='value'/>
			</Number>
			<Blob name='value'/>
		</Block>
		<Number size='8'/>
		<Blob/>
	</DataModel>
</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 1, 1, 2, 2, 2, 3, 3, 3, 3, 0, 4, 4, 4, 4 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(3, dom.dataModels[0].Count);

			var array = dom.dataModels[0][0] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(3, array.Count);

			var b1 = array[0] as Peach.Core.Dom.Block;
			Assert.NotNull(b1);
			Assert.AreEqual(1, (int)b1[0].DefaultValue);
			Assert.AreEqual(new byte[] { 1 }, b1[0].Value.ToArray());

			var b2 = array[1] as Peach.Core.Dom.Block;
			Assert.NotNull(b2);
			Assert.AreEqual(2, (int)b2[0].DefaultValue);
			Assert.AreEqual(new byte[] { 2, 2 }, b2[1].Value.ToArray());

			var b3 = array[2] as Peach.Core.Dom.Block;
			Assert.NotNull(b3);
			Assert.AreEqual(3, (int)b3[0].DefaultValue);
			Assert.AreEqual(new byte[] { 3, 3, 3 }, b3[1].Value.ToArray());

			var num = dom.dataModels[0][1] as Peach.Core.Dom.Number;
			Assert.NotNull(num);
			Assert.AreEqual(0, (int)num.DefaultValue);

			var blob = dom.dataModels[0][2] as Peach.Core.Dom.Blob;
			Assert.NotNull(blob);
			Assert.AreEqual(new byte[] { 4, 4, 4, 4 }, blob.Value.ToArray());
		}

		[Test]
		public void ConstraintChoice()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Choice>
			<Choice name='choice' constraint='element.SelectedElement.name == ""str10""'>
				<String name='str10' length='10'/>
				<String name='strX' />
			</Choice>
			<String name='unsized' />
		</Choice>
	</DataModel>
</Peach>
";

			var cracker = new DataCracker();
			var parser = new PitParser();

			var dom1 = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			var data1 = Bits.Fmt("{0}", "HelloWorld");
			cracker.CrackData(dom1.dataModels[0], data1);

			Assert.AreEqual(1, dom1.dataModels[0].Count);
			var choice1 = dom1.dataModels[0][0] as Peach.Core.Dom.Choice;
			Assert.NotNull(choice1);
			Assert.AreEqual("choice", choice1.SelectedElement.Name);

			var dom2 = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			var data2 = Bits.Fmt("{0}", "Hello");
			cracker.CrackData(dom2.dataModels[0], data2);

			Assert.AreEqual(1, dom2.dataModels[0].Count);
			var choice2 = dom2.dataModels[0][0] as Peach.Core.Dom.Choice;
			Assert.NotNull(choice2);
			Assert.AreEqual("unsized", choice2.SelectedElement.Name);
		}
	}
}

// end
