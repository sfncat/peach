using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Fixups
{
	[TestFixture]
	[Quick]
	[Peach]
	class LRCFixupTests : DataModelCollector
	{
		[Test]
		public void Test1()
		{
			// standard test

			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel\">" +
				"       <Number name=\"CRC\" size=\"32\" signed=\"false\">" +
				"           <Fixup class=\"LRCFixup\">" +
				"               <Param name=\"ref\" value=\"Data\"/>" +
				"           </Fixup>" +
				"       </Number>" +
				"       <Blob name=\"Data\" value=\"12345\"/>" +
				"   </DataModel>" +

				"   <StateModel name=\"TheState\" initialState=\"Initial\">" +
				"       <State name=\"Initial\">" +
				"           <Action type=\"output\">" +
				"               <DataModel ref=\"TheDataModel\"/>" +
				"           </Action>" +
				"       </State>" +
				"   </StateModel>" +

				"   <Test name=\"Default\">" +
				"       <StateModel ref=\"TheState\"/>" +
				"       <Publisher class=\"Null\"/>" +
				"   </Test>" +
				"</Peach>";

			PitParser parser = new PitParser();

			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);

			// verify values
			// -- this is the pre-calculated result from Peach2.3 on the blob: "12345"
			byte[] precalcResult = new byte[] { 0x01, 0x00, 0x00, 0x00 };
			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(precalcResult, values[0].ToArray());
		}

		[Test]
		public void TestTypes()
		{
			string xml = @"
<Peach>
	<DataModel name=""TheDataModel"">
		<Blob name=""blob"" valueType=""hex"" value=""00 01 10""/>

		<Blob length=""1"">
			<Fixup class=""LRCFixup"">
				<Param name=""ref"" value=""blob""/>
			</Fixup>
		</Blob>

		<String>
			<Fixup class=""LRCFixup"">
				<Param name=""ref"" value=""blob""/>
			</Fixup>
		</String>

		<Number size=""32"" endian=""little"">
			<Fixup class=""LRCFixup"">
				<Param name=""ref"" value=""blob""/>
			</Fixup>
		</Number>

		<Number size=""32"" endian=""big"">
			<Fixup class=""LRCFixup"">
				<Param name=""ref"" value=""blob""/>
			</Fixup>
		</Number>

		<Flags size=""16"" endian=""big"">
			<Flag position=""4"" size=""8"">
				<Fixup class=""LRCFixup"">
					<Param name=""ref"" value=""blob""/>
				</Fixup>
			</Flag>
		</Flags>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var actual = dom.dataModels[0].Value.ToArray();
			byte[] expected = { 0x00, 0x01, 0x10, 0xef, 0x32, 0x33, 0x39, 0xef, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xef, 0x0e, 0xf0 };
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void TestRoundTrip()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Number size='16' signed='false' endian='big'>
			<Fixup class='LRCFixup'>
				<Param name='ref' value='DM' />
			</Fixup>
		</Number>
		<Blob value='Hello' />
	</DataModel>
</Peach>
";

			VerifyRoundTrip(xml);
		}

		[Test]
		public void TestModbus1()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel\">" +
				"       <Number name=\"CRC\" size=\"8\" signed=\"false\">" +
				"           <Fixup class=\"LRCFixup\">" +
				"               <Param name=\"ref\" value=\"Data\"/>" +
				"           </Fixup>" +
				"       </Number>" +
				"       <Blob name=\"Data\" valueType=\"hex\" value=\"010300000001\"/>" +
				"   </DataModel>" +

				"   <StateModel name=\"TheState\" initialState=\"Initial\">" +
				"       <State name=\"Initial\">" +
				"           <Action type=\"output\">" +
				"               <DataModel ref=\"TheDataModel\"/>" +
				"           </Action>" +
				"       </State>" +
				"   </StateModel>" +

				"   <Test name=\"Default\">" +
				"       <StateModel ref=\"TheState\"/>" +
				"       <Publisher class=\"Null\"/>" +
				"   </Test>" +
				"</Peach>";

			PitParser parser = new PitParser();

			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);

			// verify values
			// -- this is the pre-calculated result from Peach2.3 on the blob: "12345"
			byte[] precalcResult = new byte[] { 0xFB };
			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(precalcResult, values[0].ToArray());
		}

		[Test]
		public void TestModbus2()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel\">" +
						"<Block name=\"HexBlock\">" +
							"<Block name=\"LrcBlock\">" +
								"<Number name=\"Address\" size=\"8\" value=\"1\"/>" +
								"<Number name=\"Function\" size=\"8\" value=\"3\"/>" +
								"<Blob name=\"Data\" valueType=\"hex\" value=\"00000001\"/>" +
							"</Block>" +
							"<Number name=\"LRC\" size=\"8\" signed=\"false\">" +
								"<Fixup class=\"LRCFixup\">" +
									"<Param name=\"ref\" value=\"LrcBlock\"/>" +
								"</Fixup>" +
							"</Number>" +
							"<Transformer class=\"Hex\">" +
								"<Param name=\"lowercase\" value=\"false\" />" +
							"</Transformer>" +
						"</Block>" +
				"   </DataModel>" +

				"   <StateModel name=\"TheState\" initialState=\"Initial\">" +
				"       <State name=\"Initial\">" +
				"           <Action type=\"output\">" +
				"               <DataModel ref=\"TheDataModel\"/>" +
				"           </Action>" +
				"       </State>" +
				"   </StateModel>" +

				"   <Test name=\"Default\">" +
				"       <StateModel ref=\"TheState\"/>" +
				"       <Publisher class=\"Null\"/>" +
				"   </Test>" +
				"</Peach>";

			PitParser parser = new PitParser();

			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(this);
			e.startFuzzing(dom, config);

			// verify values
			byte[] expected = new byte[] { 0x30, 0x31, 0x30, 0x33, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x31, 0x46, 0x42 };
			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(expected, values[0].ToArray());
		}
	}
}
