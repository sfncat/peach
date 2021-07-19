

using System.IO;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.PitParserTests
{
	[TestFixture]
	[Quick]
	[Peach]
	class ChoiceTests
	{
		[Test]
		public void NumberDefaults()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Choice> "+
				"			<Number name=\"N1\" size=\"8\" endian=\"big\" signed=\"true\"/>" +
				"			<Number name=\"N2\" size=\"8\" endian=\"big\" signed=\"true\"/>" +
				"			<Number name=\"N3\" size=\"8\" endian=\"big\" signed=\"true\"/>" +
				"		</Choice> " +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels[0].Count == 1);
			Assert.IsTrue(dom.dataModels[0][0] is Choice);
			Assert.AreEqual(3, ((Choice)dom.dataModels[0][0]).choiceElements.Count);
		}

		[Test]
		public void VerifyParents()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Choice> " +
				"			<Number name=\"N1\" size=\"8\" endian=\"big\" signed=\"true\"/>" +
				"			<Number name=\"N2\" size=\"8\" endian=\"big\" signed=\"true\"/>" +
				"			<Number name=\"N3\" size=\"8\" endian=\"big\" signed=\"true\"/>" +
				"		</Choice> " +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels[0].Count == 1);
			var choice = dom.dataModels[0][0] as Choice;
			Assert.NotNull(choice);
			Assert.AreEqual(3, choice.choiceElements.Count);
			Assert.AreEqual(0, choice.Count);
			foreach (var element in choice.choiceElements)
			{
				Assert.NotNull(element.parent);
				Assert.AreEqual(choice, element.parent);
			}
		}

		[Test]
		public void TestOverWrite()
		{
			string xml = @"
<Peach>
	<DataModel name='Base'>
		<Choice name='c'>
			<Block name='b1'>
				<String name='s' value='Hello'/>
			</Block>
			<Block name='b2'>
				<String name='s' value='World'/>
			</Block>
			<Block name='b3'>
				<String name='s' value='!'/>
			</Block>
		</Choice>
	</DataModel>

	<DataModel name='Derived' ref='Base'>
		<String name='c.b1.s' value='World'/>
		<String name='c.b3' value='.'/>
	</DataModel>

</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(2, dom.dataModels.Count);

			Assert.AreEqual(1, dom.dataModels[0].Count);
			var c1 = dom.dataModels[0][0] as Choice;
			Assert.NotNull(c1);
			Assert.AreEqual(3, c1.choiceElements.Count);
			var c1_b1 = c1.choiceElements[0] as Block;
			Assert.NotNull(c1_b1);
			Assert.AreEqual(1, c1_b1.Count);
			Assert.AreEqual("Hello", (string)c1_b1[0].DefaultValue);
			var c1_b2 = c1.choiceElements[1] as Block;
			Assert.NotNull(c1_b2);
			Assert.AreEqual(1, c1_b2.Count);
			Assert.AreEqual("World", (string)c1_b2[0].DefaultValue);
			var c1_b3 = c1.choiceElements[2] as Block;
			Assert.NotNull(c1_b3);
			Assert.AreEqual(1, c1_b3.Count);
			Assert.AreEqual("!", (string)c1_b3[0].DefaultValue);

			Assert.AreEqual(1, dom.dataModels[1].Count);
			var c2 = dom.dataModels[1][0] as Choice;
			Assert.NotNull(c2);
			Assert.AreEqual(3, c2.choiceElements.Count);
			var c2_b1 = c2.choiceElements[0] as Block;
			Assert.NotNull(c2_b1);
			Assert.AreEqual(1, c2_b1.Count);
			Assert.AreEqual("World", (string)c2_b1[0].DefaultValue);
			var c2_b2 = c2.choiceElements[1] as Block;
			Assert.NotNull(c2_b2);
			Assert.AreEqual(1, c2_b2.Count);
			Assert.AreEqual("World", (string)c2_b2[0].DefaultValue);
			var c3_b3 = c2.choiceElements[2] as String;
			Assert.NotNull(c3_b3);
			Assert.AreEqual(".", (string)c3_b3.DefaultValue);

		}

		[Test]
		public void TestOverWriteMiddle()
		{
			string xml = @"
<Peach>
	<DataModel name='Base'>
		<Choice name='c'>
			<Block name='b1'>
				<String name='s' value='Hello'/>
			</Block>
			<Block name='b2'>
				<String name='s' value='World'/>
			</Block>
			<Block name='b3'>
				<String name='s' value='!'/>
			</Block>
		</Choice>
	</DataModel>

	<DataModel name='Derived' ref='Base'>
		<String name='c.b1.s' value='World'/>
		<String name='c.b2' value='Hello'/>
	</DataModel>

</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(2, dom.dataModels.Count);

			Assert.AreEqual(1, dom.dataModels[0].Count);
			var c1 = dom.dataModels[0][0] as Choice;
			Assert.NotNull(c1);
			Assert.AreEqual(3, c1.choiceElements.Count);
			var c1_b1 = c1.choiceElements[0] as Block;
			Assert.NotNull(c1_b1);
			Assert.AreEqual(1, c1_b1.Count);
			Assert.AreEqual("Hello", (string)c1_b1[0].DefaultValue);
			var c1_b2 = c1.choiceElements[1] as Block;
			Assert.NotNull(c1_b2);
			Assert.AreEqual(1, c1_b2.Count);
			Assert.AreEqual("World", (string)c1_b2[0].DefaultValue);
			var c1_b3 = c1.choiceElements[2] as Block;
			Assert.NotNull(c1_b3);
			Assert.AreEqual(1, c1_b3.Count);
			Assert.AreEqual("!", (string)c1_b3[0].DefaultValue);

			Assert.AreEqual(1, dom.dataModels[1].Count);
			var c2 = dom.dataModels[1][0] as Choice;
			Assert.NotNull(c2);
			Assert.AreEqual(3, c2.choiceElements.Count);
			var c2_b1 = c2.choiceElements[0] as Block;
			Assert.NotNull(c2_b1);
			Assert.AreEqual(1, c2_b1.Count);
			Assert.AreEqual("World", (string)c2_b1[0].DefaultValue);
			var c2_b2 = c2.choiceElements[1] as String;
			Assert.NotNull(c2_b2);
			Assert.AreEqual("Hello", (string)c2_b2.DefaultValue);
			var c3_b3 = c2.choiceElements[2] as Block;
			Assert.NotNull(c3_b3);
			Assert.AreEqual(1, c3_b3.Count);
			Assert.AreEqual("!", (string)c3_b3[0].DefaultValue);

		}

		[Test]
		public void TestArrayOverwrite()
		{
			string xml = @"
<Peach>
	<DataModel name='Base'>
		<Block name='B' minOccurs='1'>
			<Choice name='c'>
				<Block name='b1'>
					<String name='s' value='Hello'/>
				</Block>
				<Block name='b2'>
					<String name='s' value='World'/>
				</Block>
				<Block name='b3'>
					<String name='s' value='!'/>
				</Block>
			</Choice>
		</Block>
	</DataModel>

	<DataModel name='Derived' ref='Base'>
		<String name='B.c.b1.s' value='World'/>
		<String name='B.c.b3' value='.'/>
	</DataModel>

</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(2, dom.dataModels.Count);

			Assert.NotNull(dom.dataModels[0].Value);
			Assert.NotNull(dom.dataModels[1].Value);

			Assert.AreEqual(1, dom.dataModels[0].Count);
			var a1 = dom.dataModels[0][0] as Array;
			Assert.NotNull(a1);
			Assert.AreEqual(1, a1.Count);
			var b1 = a1[0] as Block;
			Assert.NotNull(b1);
			Assert.AreEqual(1, b1.Count);
			var c1 = b1[0] as Choice;
			Assert.NotNull(c1);
			Assert.AreEqual(3, c1.choiceElements.Count);
			var c1_b1 = c1.choiceElements[0] as Block;
			Assert.NotNull(c1_b1);
			Assert.AreEqual(1, c1_b1.Count);
			Assert.AreEqual("Hello", (string)c1_b1[0].DefaultValue);
			var c1_b2 = c1.choiceElements[1] as Block;
			Assert.NotNull(c1_b2);
			Assert.AreEqual(1, c1_b2.Count);
			Assert.AreEqual("World", (string)c1_b2[0].DefaultValue);
			var c1_b3 = c1.choiceElements[2] as Block;
			Assert.NotNull(c1_b3);
			Assert.AreEqual(1, c1_b3.Count);
			Assert.AreEqual("!", (string)c1_b3[0].DefaultValue);

			Assert.AreEqual(1, dom.dataModels[1].Count);
			var a2 = dom.dataModels[1][0] as Array;
			Assert.NotNull(a2);
			Assert.AreEqual(1, a2.Count);
			var b2 = a2[0] as Block;
			Assert.NotNull(b1);
			Assert.AreEqual(1, b2.Count);
			var c2 = b2[0] as Choice;
			Assert.NotNull(c2);
			Assert.AreEqual(3, c2.choiceElements.Count);
			var c2_b1 = c2.choiceElements[0] as Block;
			Assert.NotNull(c2_b1);
			Assert.AreEqual(1, c2_b1.Count);
			Assert.AreEqual("World", (string)c2_b1[0].DefaultValue);
			var c2_b2 = c2.choiceElements[1] as Block;
			Assert.NotNull(c2_b2);
			Assert.AreEqual(1, c2_b2.Count);
			Assert.AreEqual("World", (string)c2_b2[0].DefaultValue);
			var c3_b3 = c2.choiceElements[2] as String;
			Assert.NotNull(c3_b3);
			Assert.AreEqual(".", (string)c3_b3.DefaultValue);

		}


		[Test]
		public void TestAddNewChoice()
		{
			string xml = @"
<Peach>
	<DataModel name='Base'>
		<Choice name='c'>
			<String name='s1' value='Hello'/>
			<String name='s2' value='World'/>
		</Choice>
	</DataModel>

	<DataModel name='Derived' ref='Base'>
		<String name='c.s3' value='Hello'/>
	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));


			Assert.AreEqual(2, dom.dataModels.Count);

			Assert.AreEqual(1, dom.dataModels[0].Count);
			var c1 = dom.dataModels[0][0] as Choice;
			Assert.AreEqual(2, c1.choiceElements.Count);

			Assert.AreEqual(1, dom.dataModels[1].Count);
			var c2 = dom.dataModels[1][0] as Choice;
			Assert.AreEqual(3, c2.choiceElements.Count);
		}

		[Test]
		public void RemoveChoiceRelation()
		{
			string xml = @"
<Peach>
	<DataModel name='Base'>
		<Choice name='c'>
			<Number name='n1' size='8'>
				<Relation type='size' of='b'/>
			</Number>
			<Number name='n2' size='8'>
				<Relation type='size' of='b'/>
			</Number>
			<Number name='n3' size='8'>
				<Relation type='size' of='b'/>
			</Number>
		</Choice>
		<Blob name='b'/>
	</DataModel>

	<DataModel name='Derived' ref='Base'>
		<String name='c.s3' value='Hello'/>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var choice = dom.dataModels[0][0] as Choice;
			Assert.AreEqual(null, choice.SelectedElement);
			choice.SelectElement(choice.choiceElements[0]);
			Assert.AreEqual(1, choice.Count);

			var blob = dom.dataModels[0][1] as Blob;
			Assert.NotNull(blob);
			Assert.AreEqual(4, blob.relations.Of<SizeRelation>().Count());
			// 4 relations, 1 for each choice and 1 for chosen item

			// Remove the chosen element
			choice.Remove(choice[0]);

			// Removing chosen element removes choice
			Assert.AreEqual(1, dom.dataModels[0].Count);

			// Removing choice cleans up relations for choiceElements
			Assert.AreEqual(0, blob.relations.Of<SizeRelation>().Count());
		}


	}
}
