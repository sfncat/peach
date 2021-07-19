

using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.CrackingTests
{
	[TestFixture]
	[Quick]
	[Peach]
	public class SizeRelationTests
	{
		[Test]
		public void CrackSizeOf1()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"8\">" +
				"			<Relation type=\"size\" of=\"Data\" />" +
				"		</Number>" +
				"		<Blob name=\"Data\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:L8}{1}", 11, "Hello WorldAAAAAAAAAAA");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World".Length, (int)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("Hello World", dom.dataModels[0][1].DefaultValue.BitsToString());
		}

		[Test]
		public void CrackSizeOf2()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"8\">" +
				"			<Relation type=\"size\" of=\"TheDataModel\" />" +
				"		</Number>" +
				"		<Blob name=\"Data\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:L8}{1}", 12, "Hello WorldAAAAAAAAAAA");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World".Length + 1, (int)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("Hello World", dom.dataModels[0][1].DefaultValue.BitsToString());
		}

		[Test]
		public void CrackSizeOf3()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"8\">" +
				"			<Relation type=\"size\" of=\"Data\" />" +
				"		</Number>" +
				"		<Block name=\"Data\">" +
				"			<Blob />" +
				"		</Block>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:L8}{1}", 11, "Hello WorldAAAAAAAAAAA");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World".Length, (int)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("Hello World", ((DataElementContainer)dom.dataModels[0][1])[0].DefaultValue.BitsToString());
		}

		[Test]
		public void CrackSizeOf4()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"8\">" +
				"			<Relation type=\"size\" of=\"Data\" expressionGet=\"size/2\" />" +
				"		</Number>" +
				"		<Blob name=\"Data\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:L8}{1}", 22, "Hello WorldAAAAAAAAAAA");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World".Length*2, (int)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("Hello World", dom.dataModels[0][1].DefaultValue.BitsToString());
		}

		[Test]
		public void CrackSizeOf5()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"8\">" +
				"			<Relation type=\"size\" of=\"Data\" />" +
				"		</Number>" +
				"		<Block name=\"Data\">" +
				"			<Blob name=\"inner\"/>" +
				"		</Block>" +
				"		<Blob name=\"outer\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:L8}{1}", 11, "Hello WorldABCDEFG");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World".Length, (int)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("Hello World", ((DataElementContainer)dom.dataModels[0][1])[0].DefaultValue.BitsToString());
			Assert.AreEqual("ABCDEFG", dom.dataModels[0][2].DefaultValue.BitsToString());
		}

		[Test]
		public void CrackSizeOf6()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"8\">" +
				"			<Relation type=\"size\" of=\"Second\" />" +
				"		</Number>" +
				"		<String name=\"First\"/>" +
				"		<String name=\"Second\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:L8}{1}", 11, "ABCDEFGHello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World".Length, (int)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("ABCDEFG", (string)dom.dataModels[0][1].DefaultValue);
			Assert.AreEqual("Hello World", (string)dom.dataModels[0][2].DefaultValue);
		}

		[Test]
		public void CrackSizeOf7()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"8\">" +
				"			<Relation type=\"size\" of=\"FirstBlock\" />" +
				"		</Number>" +
				"		<Block name=\"FirstBlock\">" +
				"			<Blob name=\"First\"/>" +
				"		</Block>" +
				"		<String name=\"Second\" value=\"ABCDEFG\" token=\"true\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:L8}{1}", 11, "Hello WorldABCDEFG");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World".Length, (int)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("Hello World", ((Block)dom.dataModels[0][1])[0].DefaultValue.BitsToString());
			Assert.AreEqual("ABCDEFG", (string)dom.dataModels[0][2].DefaultValue);
		}

		[Test]
		public void CrackSizeOf8()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"8\">" +
				"			<Relation type=\"size\" of=\"Second\" />" +
				"		</Number>" +
				"		<String name=\"First\"/>" +
				"		<Block>" +
				"			<String name=\"Second\"/>" +
				"		</Block>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:L8}{1}", 11, "ABCDEFGHello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World".Length, (int)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("ABCDEFG", (string)dom.dataModels[0][1].DefaultValue);
			Assert.AreEqual("Hello World", (string)((Peach.Core.Dom.Block)dom.dataModels[0][2])[0].DefaultValue);
		}

		[Test]
		public void CrackSizeOf9()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String length=\"8\">" +
				"			<Relation type=\"size\" of=\"Data\" />" +
				"			<Hint name='NumericalString' value='true' />" +
				"		</String>" +
				"		<String name=\"Data\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "000000088ByteLen");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(8, (int)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("8ByteLen", (string)dom.dataModels[0][1].DefaultValue);
		}

		[Test]
		public void CrackSizeOfBlockReference()
		{
			string xml = @"
<Peach>
	<DataModel name=""Base"">
		<Number size=""8"" name=""blocksize"">
			<Relation type=""size"" of=""smallData"" />
		</Number>
		<Blob name=""smallData""/>
	</DataModel>

	<DataModel name=""DM"">
		<Blob name=""Header"" length=""1""/>
		<Block name=""Base1"" ref=""Base"" />
		<Blob name=""Footer"" valueType=""hex"" value=""00"" length=""1"" token=""true"" />
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 0x01, 0x02, 0x33, 0x44, 0x00 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[1], data);

			Assert.AreEqual(3, dom.dataModels[1].Count);
			Assert.AreEqual(new byte[] { 0x01 }, dom.dataModels[1][0].Value.ToArray());
			Assert.AreEqual(new byte[] { 0x02 }, ((Block)dom.dataModels[1][1])[0].Value.ToArray());
			Assert.AreEqual(new byte[] { 0x33, 0x44 }, ((Block)dom.dataModels[1][1])[1].Value.ToArray());
			Assert.AreEqual(new byte[] { 0x00 }, dom.dataModels[1][2].Value.ToArray());
		}

		[Test]
		public void CrackSizeParent()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
			"	<DataModel name=\"TheDataModel\">" +
			"      <Block name=\"Elem10\">" +
			"         <Number name=\"Elem14\" signed=\"false\" size=\"32\"/>" +
			"         <Number name=\"Elem15\" signed=\"false\" size=\"32\">" +
			"           <Relation type=\"size\" of=\"Elem10\"/>" +
			"         </Number>" +
			"         <Blob name='blob'/>" +
			"      </Block>" +
			"	</DataModel>" +
			"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 0x03, 0xf3, 0x0d, 0x0a, 0x0a, 0x00, 0x00, 0x00, 0xff, 0xff });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			var elem = dom.dataModels[0].find("TheDataModel.Elem10.blob");
			Assert.NotNull(elem);
			Assert.AreEqual(16, elem.Value.LengthBits);
		}

		[Test]
		public void CrackBadSizeParent()
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='TheDataModel'>
		<Block name='block'>
			<Number name='num' signed='false' endian='big' size='32'>
				<Relation type='size' of='block'/>
			</Number>
		</Block>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 0, 0, 0, 2 });

			DataCracker cracker = new DataCracker();
			var ex = Assert.Throws<CrackingFailure>(() => cracker.CrackData(dom.dataModels[0], data));
			Assert.AreEqual("Block 'TheDataModel.block' failed to crack. Length is 16 bits but already read 32 bits.", ex.Message);
		}

		[Test]
		public void CrackBadSizeBlockParent()
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='TheDataModel'>
		<Block name='block'>
			<Block name='inner'>
				<Number name='num' signed='false' endian='big' size='32'>
					<Relation type='size' of='block'/>
				</Number>
			</Block>
		</Block>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 0, 0, 0, 2 });

			DataCracker cracker = new DataCracker();
			var ex = Assert.Throws<CrackingFailure>(() => cracker.CrackData(dom.dataModels[0], data));
			Assert.AreEqual("Block 'TheDataModel.block' failed to crack. Length is 16 bits but already read 32 bits.", ex.Message);
		}

		[Test]
		public void CrackSizeParentArray()
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='TheDataModel'>
		<Block name='block'>
			<Number occurs='1' name='num' signed='false' endian='big' size='32'>
				<Relation type='size' of='block'/>
			</Number>
			<Blob name='blob'/>
		</Block>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 0, 0, 0, 6, 0, 0, 1 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			var elem = dom.dataModels[0].find("TheDataModel.block.blob");
			Assert.NotNull(elem);
			Assert.AreEqual(16, elem.Value.LengthBits);
		}

		[Test]
		public void RecursiveSizeRelation1()
		{
			string xml = @"
<Peach>
	<DataModel name=""DM"">
		<Block name=""TheBlock"">
			<Number name=""Length"" size=""8"">
				<Relation type=""size"" of=""data""/>
			</Number>
			<Blob name=""data""/>
		</Block>
	</DataModel>

    <DataModel name=""DM2"" ref=""DM"">
		<Block name=""TheBlock.data"">
			<Block name=""R1"" ref=""DM"" />
		</Block>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 0x02, 0x01, 0x60 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[1], data);

			Assert.AreEqual(1, dom.dataModels[1].Count);
			Assert.IsTrue(dom.dataModels[1][0] is Peach.Core.Dom.Block);

			Peach.Core.Dom.Block outerBlock = (Peach.Core.Dom.Block)dom.dataModels[1][0];
			Assert.AreEqual(2, outerBlock.Count);
			Assert.IsTrue(outerBlock[0] is Peach.Core.Dom.Number);
			Assert.AreEqual(new byte[] { 0x02 }, ((Peach.Core.Dom.Number)outerBlock[0]).Value.ToArray());
			Assert.IsTrue(outerBlock[1] is Peach.Core.Dom.Block);

			Peach.Core.Dom.Block outerDataBlock = (Peach.Core.Dom.Block)outerBlock[1];
			Assert.AreEqual(1, outerDataBlock.Count);
			Assert.IsTrue(outerDataBlock[0] is Peach.Core.Dom.Block);
			Assert.AreEqual(1, ((Peach.Core.Dom.Block)outerDataBlock[0]).Count);
			Assert.IsTrue(((Peach.Core.Dom.Block)outerDataBlock[0])[0] is Peach.Core.Dom.Block);

			Peach.Core.Dom.Block innerBlock = (Peach.Core.Dom.Block)(((Peach.Core.Dom.Block)outerDataBlock[0])[0]);
			Assert.AreEqual(2, innerBlock.Count);
			Assert.IsTrue(innerBlock[0] is Peach.Core.Dom.Number);
			Assert.AreEqual(new byte[] { 0x01 }, ((Peach.Core.Dom.Number)innerBlock[0]).Value.ToArray());
			Assert.IsTrue(innerBlock[1] is Peach.Core.Dom.Blob);
			Assert.AreEqual(new byte[] { 0x60 }, ((Peach.Core.Dom.Blob)innerBlock[1]).Value.ToArray());


		}


		[Test]
		public void RecursiveSizeRelation2()
		{
			string xml = @"
<Peach>
	<DataModel name=""DM"">
		<Block name=""TheBlock"">
			<Number name=""Length"" size=""8"">
				<Relation type=""size"" of=""TheBlock""/>
			</Number>
			<Blob name=""data""/>
		</Block>
	</DataModel>

	<DataModel name=""DM2"" ref=""DM"">
		<Block name=""TheBlock.data"">
			<Block name=""R1"" ref=""DM"" />
		</Block>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 0x03, 0x02, 0x60 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[1], data);

			Assert.AreEqual(1, dom.dataModels[1].Count);
			Assert.IsTrue(dom.dataModels[1][0] is Peach.Core.Dom.Block);

			Peach.Core.Dom.Block outerBlock = (Peach.Core.Dom.Block)dom.dataModels[1][0];
			Assert.AreEqual(2, outerBlock.Count);
			Assert.IsTrue(outerBlock[0] is Peach.Core.Dom.Number);
			Assert.AreEqual(new byte[] { 0x03 }, ((Peach.Core.Dom.Number)outerBlock[0]).Value.ToArray());
			Assert.IsTrue(outerBlock[1] is Peach.Core.Dom.Block);

			Peach.Core.Dom.Block outerDataBlock = (Peach.Core.Dom.Block)outerBlock[1];
			Assert.AreEqual(1, outerDataBlock.Count);
			Assert.IsTrue(outerDataBlock[0] is Peach.Core.Dom.Block);
			Assert.AreEqual(1, ((Peach.Core.Dom.Block)outerDataBlock[0]).Count);
			Assert.IsTrue(((Peach.Core.Dom.Block)outerDataBlock[0])[0] is Peach.Core.Dom.Block);

			Peach.Core.Dom.Block innerBlock = (Peach.Core.Dom.Block)(((Peach.Core.Dom.Block)outerDataBlock[0])[0]);
			Assert.AreEqual(2, innerBlock.Count);
			Assert.IsTrue(innerBlock[0] is Peach.Core.Dom.Number);
			Assert.AreEqual(new byte[] { 0x02 }, ((Peach.Core.Dom.Number)innerBlock[0]).Value.ToArray());
			Assert.IsTrue(innerBlock[1] is Peach.Core.Dom.Blob);
			Assert.AreEqual(new byte[] { 0x60 }, ((Peach.Core.Dom.Blob)innerBlock[1]).Value.ToArray());


		}

		[Test]
		public void StringSizeRelation()
		{
			string xml = @"
<Peach>
	<DataModel name='StringLengthModel'>
		<String name='ALength'>
			<Relation type='size' of='A' />
		</String>
		<String token='true' value='\r\n' />
		<String name='A' />
	</DataModel>
</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "3\r\nabc");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("abc", (string)dom.dataModels[0][2].DefaultValue);
		}

		[Test]
		[Ignore("Expected to fail. See peach-pro issue #3")]
		public void SizeRefParentParent()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Block name='Header'>
			<Number size='16' endian='big' />

			<Block name='Contents'>
				<Number name='Length' size='16' endian='big'>
					<Relation type='size' of='Header' />
				</Number>

				<Number size='8' />
				<Number size='24' />

				<Block name='Array' minOccurs='1'>
					<Number size='32' endian='big' />
				</Block>
			</Block>
		</Block>

		<Number size='32' endian='big' />
	</DataModel>
</Peach>
";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:B16}{1:B16}{2:B8}{3:B24}{4:B32}{5:B32}{6:B32}",
				(uint)0xffff, 0x0010, 0, 0x414141, 1, 2, 3);

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], new BitStream(data));

			Assert.AreEqual(3, (int)dom.dataModels[0][1].DefaultValue);
		}
	}
}

// end
