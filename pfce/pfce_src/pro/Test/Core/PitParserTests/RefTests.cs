

using System.IO;
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
	class RefTests
	{
		[Test]
		public void BasicTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel1\">" +
				"		<String name=\"Str1\" />" +
				"		<String name=\"Str2\" />" +
				"		<String name=\"Str3\" />" +
				"	</DataModel>" +
				"	<DataModel name=\"TheDataModel2\" ref=\"TheDataModel1\">" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(3, dom.dataModels[0].Count);
			Assert.AreEqual(3, dom.dataModels[1].Count);
			Assert.AreEqual("Str1", dom.dataModels[1][0].Name);
			Assert.AreEqual("Str2", dom.dataModels[1][1].Name);
			Assert.AreEqual("Str3", dom.dataModels[1][2].Name);
		}

		[Test]
		public void BasicTest2()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel1\">" +
				"		<String name=\"Str1\" />" +
				"		<String name=\"Str2\" />" +
				"		<String name=\"Str3\" />" +
				"	</DataModel>" +
				"	<DataModel name=\"TheDataModel2\" ref=\"TheDataModel1\">" +
				"		<String name=\"Str4\" />" +
				"		<String name=\"Str5\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(3, dom.dataModels[0].Count);
			Assert.AreEqual(5, dom.dataModels[1].Count);
			Assert.AreEqual("Str1", dom.dataModels[1][0].Name);
			Assert.AreEqual("Str2", dom.dataModels[1][1].Name);
			Assert.AreEqual("Str3", dom.dataModels[1][2].Name);
			Assert.AreEqual("Str4", dom.dataModels[1][3].Name);
			Assert.AreEqual("Str5", dom.dataModels[1][4].Name);
		}

		[Test]
		public void BasicTest3()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel2\">" +
				"		<Block name=\"Block1\">" +
				"			<String name=\"Str1\" />" +
				"			<String name=\"Str2\" />" +
				"			<String name=\"Str3\" />" +
				"		</Block>" +
				"		<Block ref=\"Block1\">" +
				"		</Block>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(3, ((Block)dom.dataModels[0][0]).Count);
			Assert.AreEqual(3, ((Block)dom.dataModels[0][1]).Count);
			Assert.AreEqual("Str1", ((Block)dom.dataModels[0][1])[0].Name);
			Assert.AreEqual("Str2", ((Block)dom.dataModels[0][1])[1].Name);
			Assert.AreEqual("Str3", ((Block)dom.dataModels[0][1])[2].Name);
		}

		[Test]
		public void BasicTest4()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Block name=\"Block1\">" +
				"			<String name=\"Str1\" />" +
				"			<String name=\"Str2\" />" +
				"			<String name=\"Str3\" />" +
				"		</Block>" +
				"		<Block name=\"Block2\" ref=\"Block1\">" +
				"			<String name=\"Str4\" />" +
				"			<String name=\"Str5\" />" +
				"		</Block>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(3, ((Block)dom.dataModels[0][0]).Count);
			Assert.AreEqual(5, ((Block)dom.dataModels[0][1]).Count);
			Assert.AreEqual("Str1", ((Block)dom.dataModels[0][1])[0].Name);
			Assert.AreEqual("Str2", ((Block)dom.dataModels[0][1])[1].Name);
			Assert.AreEqual("Str3", ((Block)dom.dataModels[0][1])[2].Name);
			Assert.AreEqual("Str4", ((Block)dom.dataModels[0][1])[3].Name);
			Assert.AreEqual("Str5", ((Block)dom.dataModels[0][1])[4].Name);
		}

		[Test]
		public void BasicTestReplace1()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Block name=\"Block1\">" +
				"			<String name=\"Str1\" />" +
				"			<String name=\"Str2\" />" +
				"			<String name=\"Str3\" />" +
				"		</Block>" +
				"		<Block name=\"Block2\" ref=\"Block1\">" +
				"			<String name=\"Str1\" />" +
				"			<String name=\"Str2\" />" +
				"			<String name=\"Str3\" />" +
				"		</Block>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(3, ((Block)dom.dataModels[0][0]).Count);
			Assert.AreEqual(3, ((Block)dom.dataModels[0][1]).Count);
			Assert.AreEqual("Str1", ((Block)dom.dataModels[0][1])[0].Name);
			Assert.AreEqual("Str2", ((Block)dom.dataModels[0][1])[1].Name);
			Assert.AreEqual("Str3", ((Block)dom.dataModels[0][1])[2].Name);
		}

		[Test]
		public void BasicTestReplace2()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Block name=\"Block1\">" +
				"			<Block name=\"Block1a\">" +
				"				<String name=\"Str1\" />" +
				"				<String name=\"Str2\" />" +
				"			</Block>" +
				"			<String name=\"Str3\" />" +
				"		</Block>" +
				"		<Block name=\"Block2\" ref=\"Block1\">" +
			"				<String name=\"Block1a.Str1\" />" +
			"				<String name=\"Block1a.Str2\" />" +
				"			<String name=\"Str3\" />" +
				"		</Block>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(2, ((Block)dom.dataModels[0][0]).Count);
			Assert.AreEqual(2, ((Block)dom.dataModels[0][1]).Count);
			Assert.AreEqual(2, ((Block)((Block)dom.dataModels[0][1])[0]).Count);
			Assert.AreEqual("Str1", ((Block)((Block)dom.dataModels[0][1])[0])[0].Name);
			Assert.AreEqual("Str2", ((Block)((Block)dom.dataModels[0][1])[0])[1].Name);
			Assert.AreEqual("Str3", ((Block)dom.dataModels[0][1])[1].Name);
		}

		[Test]
		public void BlockTest1()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel1\">" +
				"		<String name=\"Str1\" />" +
				"		<String name=\"Str2\" />" +
				"		<String name=\"Str3\" />" +
				"	</DataModel>" +
				"	<DataModel name=\"TheDataModel2\">" +
				"		<Block name=\"TheBlock1\" ref=\"TheDataModel1\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(3, dom.dataModels[0].Count);
			Assert.AreEqual(1, dom.dataModels[1].Count);
			Assert.AreEqual(3, ((Block)dom.dataModels[1][0]).Count);
			Assert.AreEqual("Str1", ((Block)dom.dataModels[1][0])[0].Name);
			Assert.AreEqual("Str2", ((Block)dom.dataModels[1][0])[1].Name);
			Assert.AreEqual("Str3", ((Block)dom.dataModels[1][0])[2].Name);
		}

		[Test]
		public void BlockMinMaxTest1()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel1\">" +
				"		<String name=\"Str1\" />" +
				"		<String name=\"Str2\" />" +
				"		<String name=\"Str3\" />" +
				"	</DataModel>" +
				"	<DataModel name=\"TheDataModel2\">" +
				"		<Block name=\"TheBlock1\" minOccurs=\"0\" maxOccurs=\"1\" ref=\"TheDataModel1\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(3, dom.dataModels[0].Count);
			Assert.AreEqual(1, dom.dataModels[1].Count);
			Peach.Core.Dom.Array BlockArray = dom.dataModels[1][0] as Peach.Core.Dom.Array;
			Assert.NotNull(BlockArray);

			Block ReferencedBlock = ((Block)BlockArray.OriginalElement);
			Assert.AreEqual(3, ReferencedBlock.Count);
			Assert.AreEqual("Str1", ReferencedBlock[0].Name);
			Assert.AreEqual("Str2", ReferencedBlock[1].Name);
			Assert.AreEqual("Str3", ReferencedBlock[2].Name);
		}

	}
}

// end
