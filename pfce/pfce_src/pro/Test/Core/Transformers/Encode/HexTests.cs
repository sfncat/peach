using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Transformers.Encode
{
	[TestFixture]
	[Quick]
	[Peach]
	class HexTests : DataModelCollector
	{
		byte[] precalcResult = new byte[] { 0x34, 0x38, 0x36, 0x35, 0x36, 0x63, 0x36, 0x63, 0x36, 0x66 };

		[Test]
		public void Test1()
		{
			// standard test

			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel\">" +
				"       <Block name=\"TheBlock\">" +
				"           <Transformer class=\"Hex\"/>" +
				"           <Blob name=\"Data\" value=\"Hello\"/>" +
				"       </Block>" +
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
			// -- this is the pre-calculated result from Peach2.3 on the blob: "Hello"
			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(precalcResult, values[0].ToArray());
		}

		[Test]
		public void TestUppercase()
		{
			// standard test

			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel\">" +
				"       <Block name=\"TheBlock\">" +
				"           <Transformer class=\"Hex\">" +
				"               <Param name=\"lowercase\" value=\"false\" />" +
				"           </Transformer>" +
				"           <Blob name=\"Data\" value=\"Hello\"/>" +
				"       </Block>" +
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
			// -- this is the pre-calculated result from Peach2.3 on the blob: "Hello"
			byte[] precalcResult = new byte[] { 0x34, 0x38, 0x36, 0x35, 0x36, 0x43, 0x36, 0x43, 0x36, 0x46 };
			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(precalcResult, values[0].ToArray());
		}

		[Test]
		public void CrackTest()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String/>
		<Transformer class='Hex'/>
	</DataModel>
</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", precalcResult);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello", (string)dom.dataModels[0][0].DefaultValue);
		}

		[Test]
		public void CrackBadLengthTest()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String/>
		<Transformer class='Hex'/>
	</DataModel>
</Peach>
";

			var dom = ParsePit(xml);
			var data = Bits.Fmt("{0}", (byte)'0');

			DataCracker cracker = new DataCracker();
			var ex = Assert.Throws<SoftException>(() => cracker.CrackData(dom.dataModels[0], data));
			Assert.AreEqual("Hex decode failed, invalid length.", ex.Message);
		}
	}
}

// end
