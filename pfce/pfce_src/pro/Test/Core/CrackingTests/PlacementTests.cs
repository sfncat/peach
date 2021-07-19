

using System.IO;
using System.Linq;
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
	public class PlacementTests
	{
		[Test]
		public void BasicAfter()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Block name=\"Block1\">" +
				"			<Blob name=\"Data\">" +
				"				<Placement after=\"Block1\"/>" +
				"			</Blob>" +
				"		</Block>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Data", dom.dataModels[0][1].Name);
			Assert.AreEqual("Hello World", dom.dataModels[0][1].DefaultValue.BitsToString());
		}

		[Test]
		public void BasicBefore()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Block name=\"Block\">" +
				"			<Blob name=\"Data\">" +
				"				<Placement before=\"Block1\"/>" +
				"			</Blob>" +
				"		</Block>" +
				"		<Block name=\"Block1\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Data", dom.dataModels[0][1].Name);
			Assert.AreEqual("Hello World", dom.dataModels[0][1].DefaultValue.BitsToString());
		}

		[Test]
		public void SameName()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Block name=\"Block1\">" +
				"			<Blob name=\"Data\">" +
				"				<Placement after=\"Block1\"/>" +
				"			</Blob>" +
				"		</Block>" +
				"		<Block name=\"Data\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Data_1", dom.dataModels[0][1].Name);
			Assert.AreEqual("Hello World", dom.dataModels[0][1].DefaultValue.BitsToString());
		}

		[Test]
		public void RelationSameParentTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String name=\"TheString\" length=\"2\">" +
				"			<Relation type=\"size\" of=\"Data\"/>" +
				"		</String>" +
				"		<Block name=\"Block1\">" +
				"			<Blob name=\"Data\">" +
				"				<Placement after=\"Block2\"/>" +
				"			</Blob>" +
				"			<Block name=\"Block2\"/>" +
				"		</Block>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "11Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(1, dom.dataModels[0][0].relations.Count);
			Assert.AreEqual("Data", dom.dataModels[0][0].relations[0].OfName);
			Assert.AreEqual("TheDataModel.Block1.Data", dom.dataModels[0][0].relations[0].Of.fullName);
			Assert.AreEqual("Hello World", dom.dataModels[0][1].InternalValue.BitsToString());
		}

		[Test]
		public void RelationNewParentTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String name=\"TheString\" length=\"2\">" +
				"			<Relation type=\"size\" of=\"Data\"/>" +
				"		</String>" +
				"		<Block name=\"Block1\">" +
				"			<Blob name=\"Data\">" +
				"				<Placement after=\"Block1\"/>" +
				"			</Blob>" +
				"		</Block>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "11Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(1, dom.dataModels[0][0].relations.Count);
			Assert.AreEqual("Data", dom.dataModels[0][0].relations[0].OfName);
			Assert.AreEqual("TheDataModel.Data", dom.dataModels[0][0].relations[0].Of.fullName);
			Assert.AreEqual("Hello World", dom.dataModels[0][2].DefaultValue.BitsToString());
		}

		[Test]
		public void RelationRenameTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Block name=\"Block1\">" +
				"			<String name=\"TheString\" length=\"2\">" +
				"				<Relation type=\"size\" of=\"Data\"/>" +
				"			</String>" +
				"			<Blob name=\"Data\">" +
				"				<Placement after=\"Block1\"/>" +
				"			</Blob>" +
				"		</Block>" +
				"		<Blob name='Data'/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var blk = dom.dataModels[0][0] as Block;

			Assert.AreEqual(1, blk[0].relations.Count);
			Assert.AreEqual("Data", blk[0].relations[0].OfName);
			Assert.AreEqual("TheDataModel.Block1.Data", blk[0].relations[0].Of.fullName);

			var data = Bits.Fmt("{0}", "11Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(1, blk[0].relations.Count);
			Assert.AreEqual("Data_1", blk[0].relations[0].OfName);
			Assert.AreEqual("TheDataModel.Data_1", blk[0].relations[0].Of.fullName);
			Assert.AreEqual("Hello World", dom.dataModels[0][1].DefaultValue.BitsToString());
		}

		[Test]
		public void FixupTest()
		{
			// The ref'd half of the fixup is moved
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String name=\"TheString\" length=\"11\">" +
				"			<Fixup class=\"CopyValue\">" +
				"				<Param name=\"ref\" value=\"Data\"/>"+
				"			</Fixup>"+
				"		</String>" +
				"		<Block name=\"Block1\">" +
				"			<Blob name=\"Data\">" +
				"				<Placement after=\"Block1\"/>" +
				"			</Blob>" +
				"		</Block>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "HELLO WORLDHello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(1, dom.dataModels[0][0].fixup.references.Count());
			var item = dom.dataModels[0][0].fixup.references.First();
			Assert.AreEqual("ref", item.Item1);
			Assert.AreEqual("TheDataModel.Data", item.Item2);
			Assert.AreEqual("Hello World", dom.dataModels[0][0].InternalValue.BitsToString());
			Assert.AreEqual("Hello World", dom.dataModels[0][2].DefaultValue.BitsToString());
		}

		[Test]
		public void FixupTest2()
		{
			// The fixup is moved
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String name=\"TheString\" length=\"11\"/>" +
				"		<Block name=\"Block1\">" +
				"			<Blob name=\"Data\">" +
				"				<Placement after=\"Block1\"/>" +
				"				<Fixup class=\"CopyValue\">" +
				"					<Param name=\"ref\" value=\"TheString\"/>" +
				"				</Fixup>" +
				"			</Blob>" +
				"		</Block>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "HELLO WORLDHello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(1, dom.dataModels[0][2].fixup.references.Count());
			var item = dom.dataModels[0][2].fixup.references.First();
			Assert.AreEqual("ref", item.Item1);
			Assert.AreEqual("TheDataModel.TheString", item.Item2);
			Assert.AreEqual("HELLO WORLD", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("HELLO WORLD", (string)dom.dataModels[0][2].InternalValue);
		}

		[Test]
		public void RelationCloneTest()
		{
			// When the item is placed, it must be copied since an item of the same name will already exist
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Block name=\"Block1\">" +
				"			<String name=\"TheString\" length=\"2\">" +
				"				<Relation type=\"size\" of=\"Data\"/>" +
				"			</String>" +
				"			<Blob name=\"Data\">" +
				"				<Placement after=\"Block1\"/>" +
				"			</Blob>" +
				"		</Block>" +
				"		<Block name=\"Data\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "11Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(3, dom.dataModels[0].Count);
			Assert.AreEqual("Block1", dom.dataModels[0][0].Name);
			Assert.AreEqual("Data_1", dom.dataModels[0][1].Name);
			Assert.AreEqual("Data", dom.dataModels[0][2].Name);


			var Block1 = dom.dataModels[0][0] as DataElementContainer;
			Assert.NotNull(Block1);

			Assert.AreEqual(1, Block1.Count);
			Assert.AreEqual("TheString", Block1[0].Name);

			Assert.AreEqual(1, Block1[0].relations.Count);
			Assert.AreEqual("Data_1", Block1[0].relations[0].OfName);
			Assert.AreEqual("TheDataModel.Data_1", Block1[0].relations[0].Of.fullName);

			var final = dom.dataModels[0].Value;

			Assert.AreEqual(data.ToArray(), final.ToArray());
		}

		[Test]
		public void FixupInArray()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Block name=\"Block1\" occurs=\"2\">" +
				"			<String name=\"TheString\" length=\"5\"/>" +
				"			<String name=\"TheCopy\" length=\"5\">" +
				"				<Placement before=\"Marker\"/>" +
				"				<Fixup class=\"CopyValue\">" +
				"					<Param name=\"ref\" value=\"TheString\"/>" +
				"				</Fixup>" +
				"			</String>" +
				"		</Block>" +
				"		<Block name=\"Marker\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "helloworld1234567890");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(4, dom.dataModels[0].Count);

			var array = dom.dataModels[0][0] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(2, array.Count);

			var f1 = dom.dataModels[0][1].fixup.references.First();
			Assert.AreEqual("ref", f1.Item1);
			Assert.AreEqual("TheDataModel.Block1.Block1_0.TheString", f1.Item2);

			var f2 = dom.dataModels[0][2].fixup.references.First();
			Assert.AreEqual("ref", f2.Item1);
			Assert.AreEqual("TheDataModel.Block1.Block1_1.TheString", f2.Item2);

			Assert.AreEqual("helloworldhelloworld", dom.dataModels[0].InternalValue.BitsToString());
		}

		[Test]
		public void FixupCloneTest()
		{
			// Verify fixups remain intact when the item is cloned during placement
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Block name=\"Block0\">" +
				"			<Number name=\"TheCRC\" size=\"32\">" +
				"				<Fixup class=\"Crc32DualFixup\">" +
				"					<Param name=\"ref1\" value=\"TheString\"/>" +
				"					<Param name=\"ref2\" value=\"Data\"/>" +
				"				</Fixup>" +
				"			</Number>" +
				"			<String name=\"TheString\" length=\"2\">" +
				"				<Relation type=\"size\" of=\"Data\"/>" +
				"			</String>" +
				"			<String name=\"Data\">" +
				"				<Placement before=\"Placement\"/>" +
				"			</String>" +
				"		</Block>" +
				"		<Block name=\"Block1\">" +
				"			<Number name=\"TheCRC\" size=\"32\">" +
				"				<Fixup class=\"Crc32Fixup\">" +
				"					<Param name=\"ref\" value=\"Data\"/>" +
				"				</Fixup>" +
				"			</Number>" +
				"			<String name=\"TheString\" length=\"2\">" +
				"				<Relation type=\"size\" of=\"Data\"/>" +
				"			</String>" +
				"			<String name=\"Data\">" +
				"				<Placement before=\"Placement\"/>" +
				"			</String>" +
				"		</Block>" +
				"		<Block name=\"Placement\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "000011000011Hello WorldhELLO wORLD");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(5, dom.dataModels[0].Count);
			Assert.AreEqual("Block0", dom.dataModels[0][0].Name);
			Assert.AreEqual("Block1", dom.dataModels[0][1].Name);
			Assert.AreEqual("Data", dom.dataModels[0][2].Name);
			Assert.AreEqual("Data_1", dom.dataModels[0][3].Name);
			Assert.AreEqual("Placement", dom.dataModels[0][4].Name);

			var block0 = dom.dataModels[0][0] as DataElementContainer;
			var block1 = dom.dataModels[0][1] as DataElementContainer;
			Assert.NotNull(block0);
			Assert.NotNull(block1);
			Assert.AreEqual(2, block0.Count);
			Assert.AreEqual(2, block1.Count);
			Assert.AreEqual("TheCRC", block0[0].Name);
			Assert.AreEqual("TheString", block0[1].Name);
			Assert.AreEqual("TheCRC", block0[0].Name);
			Assert.AreEqual("TheString", block0[1].Name);

			var fixup0 = block0[0].fixup;
			var fixup1 = block1[0].fixup;
			Assert.NotNull(fixup0);
			Assert.NotNull(fixup1);

			Assert.AreEqual(2, fixup0.references.Count());
			var fixup0_first = fixup0.references.First();
			var fixup0_last = fixup0.references.Last();
			Assert.AreEqual("ref1", fixup0_first.Item1);
			Assert.AreEqual("TheDataModel.Block0.TheString", fixup0_first.Item2);
			Assert.AreEqual("ref2", fixup0_last.Item1);
			Assert.AreEqual("TheDataModel.Data", fixup0_last.Item2);

			Assert.AreEqual(1, fixup1.references.Count());
			var fixup1_first = fixup1.references.First();
			Assert.AreEqual("ref", fixup1_first.Item1);
			Assert.AreEqual("TheDataModel.Data_1", fixup1_first.Item2);
		}

		[Test]
		public void ArrayAfterPlacement()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Number name='NumPackets' size='8' >
			<Relation type='count' of='Packets'/>
		</Number>
		<Block name='Wrapper'>
			<Block name='Packets' maxOccurs='1024'>
				<Number name='PacketLength' size='8'>
					<Relation type='size' of='Packet'/>
				</Number>
				<String name='Packet'>
					<Placement after='Wrapper'/>
				</String>
			</Block>
		</Block>
	</DataModel>
</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			// When using placement with after, the order gets reversed.  This is because
			// each placement puts the element directly after the target.
			var expected = Encoding.ASCII.GetBytes("\x02\x05\x07!fuzzerpeach");

			var data = Bits.Fmt("{0}", expected);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			var final = dom.dataModels[0].Value.ToArray();
			Assert.AreEqual(expected, final);

			Assert.AreEqual(4, dom.dataModels[0].Count);
			Assert.AreEqual("!fuzzer", (string)dom.dataModels[0][2].DefaultValue);
			Assert.AreEqual("peach", (string)dom.dataModels[0][3].DefaultValue);
		}

		[Test]
		public void ArrayBeforePlacement()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Number name='NumPackets' size='8' >
			<Relation type='count' of='Packets'/>
		</Number>
		<Block name='Packets' maxOccurs='1024'>
			<Number name='PacketLength' size='8'>
				<Relation type='size' of='Packet'/>
			</Number>
			<String name='Packet'>
				<Placement before='Marker'/>
			</String>
		</Block>
		<Block name='Marker'/>
	</DataModel>
</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			// When using placement with before, the order is maintained.  This is because
			// each placement puts the element directly before the target.
			var expected = Encoding.ASCII.GetBytes("\x02\x05\x07peach!fuzzer");

			var data = Bits.Fmt("{0}", expected);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			var final = dom.dataModels[0].Value.ToArray();
			Assert.AreEqual(expected, final);

			Assert.AreEqual(5, dom.dataModels[0].Count);
			Assert.AreEqual("peach", (string)dom.dataModels[0][2].DefaultValue);
			Assert.AreEqual("!fuzzer", (string)dom.dataModels[0][3].DefaultValue);
		}

		[Test]
		public void SizedArrayPlacement()
		{
			string xml = @"
<Peach>
	<Defaults>
		<Number endian='big' signed='false'/>
	</Defaults>
			
	<DataModel name='DM'>
		<Number name='NumEntries' size='16'>
			<Relation type='count' of='Entries'/>
		</Number>
		
		<Block name='Entries' minOccurs='1'>
			<Number name='Offset' size='16'>
				<Relation type='offset' of='Data'/>
			</Number>
			<Number name='Size' size='16'>
				<Relation type='size' of='Data'/>
			</Number>
			<String name='Data'>
				<Placement before='Marker'/>
			</String>
		</Block>

		<Block name='Marker'/>
	</DataModel>
</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:B16}{1:B16}{2:B16}{3:B16}{4:B16}{5}",
				2, 14, 5, 27, 7, "junkpeachmorejunk!fuzzerevenmorejunk");

			var expected = data.ToArray();
			Assert.NotNull(expected);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(5, dom.dataModels[0].Count);
			Assert.AreEqual("peach", (string)dom.dataModels[0][2].DefaultValue);
			Assert.AreEqual("!fuzzer", (string)dom.dataModels[0][3].DefaultValue);
		}


		[Test]//, Ignore("See Issue #417")]
		public void BeforeAndAfterPlacement()
		{
			string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
				<Peach>
					<DataModel name=""TheDataModel"">
						<Number size=""8"" name=""Offset1"">
							<Relation type=""offset"" of=""Block1"" />
						</Number>

						<Block name=""Block1"">
							<Placement before=""PlaceHolder""/>	
							
							<Number size=""8"" name=""Offset2"">
								<Relation type=""offset"" of=""Block2"" />
							</Number>

							<Block name=""Block2"">
								<Placement after=""PlaceHolder""/>
								<Blob name=""DataPlaced"" length=""1"" />
							</Block>							
						</Block>				
						
						<Blob name=""Data"" />

						<Block name=""PlaceHolder""/>

					</DataModel>
				</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 0x03, 0x41, 0x41, 0x04, 0x42 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Offset1", dom.dataModels[0][0].Name);
			Assert.AreEqual(3, (int)dom.dataModels[0][0].DefaultValue);

			var Blob1 = (Peach.Core.Dom.Blob)dom.dataModels[0][1];
			Assert.AreEqual("Data", Blob1.Name);
			Assert.AreEqual(new byte[] { 0x41, 0x41 }, Blob1.DefaultValue.BitsToArray());

			var Block1 = (Peach.Core.Dom.Block)dom.dataModels[0][2];
			Assert.AreEqual("Block1", Block1.Name);

			Assert.AreEqual(1, Block1.Count);
			Assert.AreEqual(4, (int)Block1[0].DefaultValue);

			var PlaceHolder = (Peach.Core.Dom.Block)dom.dataModels[0][3];
			Assert.AreEqual("PlaceHolder", PlaceHolder.Name);
			Assert.AreEqual(0, PlaceHolder.Count);

			var Block2 = (Peach.Core.Dom.Block)dom.dataModels[0][4];
			Assert.AreEqual("Block2", Block2.Name);
			Assert.AreEqual(1, Block2.Count);

			var DataPlaced = (Peach.Core.Dom.Blob)Block2[0];
			Assert.AreEqual("DataPlaced", DataPlaced.Name);
			Assert.AreEqual(new byte[] { 0x42 }, DataPlaced.DefaultValue.BitsToArray());
		}


		[Test]
		public void BeforeAndAfterPlacementRelativeTo()
		{
			string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
				<Peach>
					<DataModel name=""TheDataModel"">
						<Number size=""8"" name=""Offset1"">
							<Relation type=""offset"" of=""Block1"" relative=""true"" relativeTo=""TheDataModel""/>
						</Number>

						<Block name=""Block1"">
							<Placement before=""PlaceHolder""/>	
							
							<Number size=""8"" name=""Offset2"">
								<Relation type=""offset"" of=""Block2"" relative=""true"" relativeTo=""TheDataModel""/>
							</Number>

							<Block name=""Block2"">
								<Placement after=""PlaceHolder""/>
								<Blob name=""DataPlaced"" length=""1"" />
							</Block>							
						</Block>				
						
						<Blob name=""Data"" />

						<Block name=""PlaceHolder""/>

					</DataModel>
				</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 0x03, 0x41, 0x41, 0x04, 0x42 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Offset1", dom.dataModels[0][0].Name);
			Assert.AreEqual(3, (int)dom.dataModels[0][0].DefaultValue);

			var Blob1 = (Peach.Core.Dom.Blob)dom.dataModels[0][1];
			Assert.AreEqual("Data", Blob1.Name);
			Assert.AreEqual(new byte[] { 0x41, 0x41 }, Blob1.DefaultValue.BitsToArray());

			var Block1 = (Peach.Core.Dom.Block)dom.dataModels[0][2];
			Assert.AreEqual("Block1", Block1.Name);

			Assert.AreEqual(1, Block1.Count);
			Assert.AreEqual(4, (int)Block1[0].DefaultValue);

			var PlaceHolder = (Peach.Core.Dom.Block)dom.dataModels[0][3];
			Assert.AreEqual("PlaceHolder", PlaceHolder.Name);
			Assert.AreEqual(0, PlaceHolder.Count);

			var Block2 = (Peach.Core.Dom.Block)dom.dataModels[0][4];
			Assert.AreEqual("Block2", Block2.Name);
			Assert.AreEqual(1, Block2.Count);

			var DataPlaced = (Peach.Core.Dom.Blob)Block2[0];
			Assert.AreEqual("DataPlaced", DataPlaced.Name);
			Assert.AreEqual(new byte[] { 0x42 }, DataPlaced.DefaultValue.BitsToArray());
		}

		[Test]
		public void SizedPlaced()
		{
			// Ensure relations to elements inside a moved block are maintained when placement occurs
			string xml = @"
<Peach>
  <DataModel name='repro'>

    <Number size='8' name='size_of_element'>
      <Relation type='size' of='element' />
    </Number>

    <Number size='8' name='offset_of_element_container'>
      <Relation type='offset' of='element_placement' />
    </Number>

    <Block name='element_placement'>
      <Placement before='tail_placement'/>
      <String name='element' />
    </Block>
  </DataModel>


  <DataModel name='file'>
    <Block name='the_repro' ref='repro' />
    <Block name='tail_placement' />
  </DataModel>
</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:b8}{1:b8}{2}", 2, 5, "AAABBCCC");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[1], data);

			Assert.AreEqual(3, dom.dataModels[1].Count);
			var blk = dom.dataModels[1][1] as Peach.Core.Dom.Block;
			Assert.AreEqual(1, blk.Count);
			var str = blk[0] as Peach.Core.Dom.String;
			Assert.NotNull(str);
			Assert.AreEqual("BB", (string)str.DefaultValue);
		}

		[Test]
		public void BacktrackPlacementBefore()
		{
			const string xml = @"
<Peach>
  <DataModel name='repro'>
    <Number size='8' name='array_offset'>
      <Relation type='offset' of='array' />
    </Number>

    <Number size='8' name='array_count'>
      <Relation type='count' of='array' />
    </Number>

    <Block name='array' minOccurs='0'>
      <Number name='len' size='8'>
        <Relation type='size' of='value' />
      </Number>
      <String name='value'>
        <Placement before='array' />
      </String>
    </Block>
  </DataModel>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);
			var data = Bits.Fmt("{0}{1}{2}", new byte[] {12, 4}, "ABBCCCDDDD", new byte[] {1, 2, 3, 4});
			var cracker = new DataCracker();

			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(2 + 4 + 1, dom.dataModels[0].Count);

			Assert.AreEqual("A", (string)dom.dataModels[0][2].DefaultValue);
			Assert.AreEqual("BB", (string)dom.dataModels[0][3].DefaultValue);
			Assert.AreEqual("CCC", (string)dom.dataModels[0][4].DefaultValue);
			Assert.AreEqual("DDDD", (string)dom.dataModels[0][5].DefaultValue);
		}

		[Test]
		public void BacktrackPlacementBeforeTwoPass()
		{
			const string xml = @"
<Peach>
  <DataModel name='repro'>
    <Number size='8' name='offset1'>
      <Relation type='offset' of='offset2' />
    </Number>

    <Number size='8' name='offset2'>
      <Relation type='offset' of='block' />
    </Number>

    <Block name='block'>
      <Block name='value'>
        <Placement before='block' />
          <String name='string1'>
            <Placement before='offset2' />
          </String>
          <String name='string2'/>
      </Block>
      <String name='string3' />
    </Block>
  </DataModel>
</Peach>
";
			var dom = DataModelCollector.ParsePit(xml);
			var data = Bits.Fmt("{0}", "\x0004abc\x0008defghi");
			var cracker = new DataCracker();

			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(5, dom.dataModels[0].Count);

			Assert.AreEqual("abc", (string)dom.dataModels[0][1].DefaultValue);
			Assert.AreEqual("def", dom.dataModels[0][3].InternalValue.BitsToString());
			Assert.AreEqual("ghi", dom.dataModels[0][4].InternalValue.BitsToString());
		}

		[Test]
		public void BacktrackPlacementAfter()
		{
			const string xml = @"
<Peach>
  <DataModel name='repro'>
    <Number size='8' name='array_offset'>
      <Relation type='offset' of='array' />
    </Number>

    <Number size='8' name='array_count'>
      <Relation type='count' of='array' />
    </Number>

    <Block name='array' minOccurs='0'>
      <Number name='len' size='8'>
        <Relation type='size' of='value' />
      </Number>
      <String name='value'>
        <Placement after='array_count' />
      </String>
    </Block>
  </DataModel>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);
			var data = Bits.Fmt("{0}{1}{2}", new byte[] { 12, 4 }, "DDDDCCCBBA", new byte[] { 1, 2, 3, 4 });
			var cracker = new DataCracker();

			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(2 + 4 + 1, dom.dataModels[0].Count);

			Assert.AreEqual("DDDD", (string)dom.dataModels[0][2].DefaultValue);
			Assert.AreEqual("CCC", (string)dom.dataModels[0][3].DefaultValue);
			Assert.AreEqual("BB", (string)dom.dataModels[0][4].DefaultValue);
			Assert.AreEqual("A", (string)dom.dataModels[0][5].DefaultValue);
		}

		[Test]
		public void BacktrackPlacementSimpleNotLast()
		{
			const string xml = @"
<Peach>
  <DataModel name='repro'>
    <Number size='8' name='block_offset'>
      <Relation type='offset' of='block' />
    </Number>

    <Block name='block'>
      <String name='dot' value='.' token='true' />
      <String name='payload'>
        <Placement before='dot' />
      </String>
      <String name='end' value='end' token='true' />
    </Block>
  </DataModel>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);
			var data = Bits.Fmt("{0:b8}{1}", 8, "abcdefg.end");
			var cracker = new DataCracker();

			cracker.CrackData(dom.dataModels[0], data);

			var payload = dom.dataModels[0].find("payload");
			Assert.NotNull(payload);
			Assert.AreEqual("abcdefg", (string)payload.DefaultValue);
		}

		[Test]
		public void BacktrackPlacementSimpleLast()
		{
			const string xml = @"
<Peach>
  <DataModel name='repro'>
    <Number size='8' name='block_offset'>
      <Relation type='offset' of='block' />
    </Number>

    <Block name='block'>
      <String name='end' value='.end' token='true' />
      <String name='payload'>
        <Placement before='end' />
      </String>
    </Block>
  </DataModel>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);
			var data = Bits.Fmt("{0:b8}{1}", 8, "abcdefg.end");
			var cracker = new DataCracker();

			cracker.CrackData(dom.dataModels[0], data);

			var payload = dom.dataModels[0].find("payload");
			Assert.NotNull(payload);
			Assert.AreEqual("abcdefg", (string)payload.DefaultValue);
		}

		[Test]
		public void AbsolutePlacementNoOffset()
		{
			const string xml = @"
<Peach>
  <DataModel name='DM'>
    <String name='value'>
      <Placement />
    </String>
  </DataModel>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);
			var data = Bits.Fmt("{0}", "abcdefg");
			var cracker = new DataCracker();

			var ex = Assert.Throws<CrackingFailure>(() => cracker.CrackData(dom.dataModels[0], data));

			Assert.AreEqual("Placement requires before/after attribute or an offset relation.", ex.ShortMessage);
		}

		[Test]
		public void AbsolutePlacementBackwards()
		{
			const string xml = @"
<Peach>
  <DataModel name='repro'>
    <Number size='8' name='array_offset'>
      <Relation type='offset' of='array' />
    </Number>

    <Number size='8' name='array_count'>
      <Relation type='count' of='array' />
    </Number>

    <Block name='array' minOccurs='0'>
      <Number name='offset' size='8'>
        <Relation type='offset' of='value' />
      </Number>
      <Number name='length' size='8'>
        <Relation type='size' of='value' />
      </Number>
      <String name='value'>
        <Placement />
      </String>
    </Block>
  </DataModel>
</Peach>
";
			// 8 3 a bb ccc  5 3 2 1 3 2
			var dom = DataModelCollector.ParsePit(xml);
			var data = Bits.Fmt("{0}{1}{2}", "\x8\x3", "abbccc", "\x5\x3\x2\x1\x3\x2");
			var cracker = new DataCracker();

			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(6, dom.dataModels[0].Count);
			Assert.AreEqual("a", (string)dom.dataModels[0][2].InternalValue);
			Assert.AreEqual("bb", (string)dom.dataModels[0][3].InternalValue);
			Assert.AreEqual("ccc", (string)dom.dataModels[0][4].InternalValue);
		}

		[Test]
		public void AbsolutePlacementForwards()
		{
			const string xml = @"
<Peach>
  <DataModel name='repro'>
    <Number size='8' name='array_count'>
      <Relation type='count' of='array' />
    </Number>

    <Block name='array' minOccurs='0'>
      <Number name='offset' size='8'>
        <Relation type='offset' of='value' />
      </Number>
      <Number name='length' size='8'>
        <Relation type='size' of='value' />
      </Number>
      <String name='value'>
        <Placement />
      </String>
    </Block>
  </DataModel>
</Peach>
";
			// 3 7 1 8 2 10 3 a bb ccc
			var dom = DataModelCollector.ParsePit(xml);
			var data = Bits.Fmt("{0}{1}", "\x3\x7\x1\x8\x2\xa\x3", "abbccc");
			var cracker = new DataCracker();

			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(5, dom.dataModels[0].Count);
			Assert.AreEqual("a", (string)dom.dataModels[0][2].InternalValue);
			Assert.AreEqual("bb", (string)dom.dataModels[0][3].InternalValue);
			Assert.AreEqual("ccc", (string)dom.dataModels[0][4].InternalValue);
		}

		[Test]
		public void SimpleAbsolutePlacement()
		{
			const string xml = @"
<Peach>
  <DataModel name='repro'>
    <Block name='header'>
      <Number size='8' name='value_offset'>
        <Relation type='offset' of='value' />
      </Number>

      <Number size='8' name='value_size'>
        <Relation type='size' of='value' />
      </Number>

      <Block name='value'>
        <Placement />
        <String name='str' />
      </Block>
    </Block>

  </DataModel>
</Peach>
";
			var dom = DataModelCollector.ParsePit(xml);
			var data = Bits.Fmt("{0}{1}", "\x3\x3", "_abc");
			var cracker = new DataCracker();

			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(2, dom.dataModels[0].Count);
			Assert.AreEqual("abc", dom.dataModels[0][1].InternalValue.BitsToString());
		}
	}
}

// end
