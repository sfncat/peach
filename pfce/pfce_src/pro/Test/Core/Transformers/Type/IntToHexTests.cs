using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Transformers.Type
{
	[TestFixture]
	[Quick]
	[Peach]
    class IntToHexTests : DataModelCollector
    {
        [Test]
        public void Test1()
        {
            // standard test

            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
                "<Peach>" +
                "   <DataModel name=\"TheDataModel\">" +
                "       <Block name=\"TheBlock\">" +
                "           <Transformer class=\"IntToHex\"/>" +
                "           <Blob name=\"blob1\" value=\"12345\"/>" +
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
            // -- this is the pre-calculated result from Peach2.3 on the blob: "12345"
            byte[] precalcResult = new byte[] { 0x33, 0x30, 0x33, 0x39 };
            Assert.AreEqual(1, values.Count);
            Assert.AreEqual(precalcResult, values[0].ToArray());
        }

		[Test]
		public void Test2()
		{
			// standard test

			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel\">" +
				"       <String name=\"TheString\" value=\"15\">" +
				"           <Transformer class=\"IntToHex\"/>" +
				"       </String>" +
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
			byte[] precalcResult = new byte[] { 0x46 }; //ascii for 'F'
			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(precalcResult, values[0].ToArray());
		}

		[Test]
		public void Test3()
		{
			// Test to show that this should be a SoftException
			// Since a numeric string can be fuzzed into a non-numeric string

			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel\">" +
				"       <String name=\"TheString\" value=\"15\">" +
				"           <Transformer class=\"IntToHex\"/>" +
				"       </String>" +
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
				 "      <Strategy class=\"Sequential\"/>" +
				"   </Test>" +
				"</Peach>";

			PitParser parser = new PitParser();

			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			dom.tests[0].includedMutators = new List<string>();
			dom.tests[0].includedMutators.Add("StringStatic");

			RunConfiguration config = new RunConfiguration();
			config.range = true;
			config.rangeStart = 1;
			config.rangeStop = 1;
			
			Engine e = new Engine(this);
			e.startFuzzing(dom, config);
			string val1 = "Peach";
			Assert.AreEqual(val1, (string)mutations[0]);
		}

        [Test]
        public void CrackTest()
        {
            string xml = @"
<Peach>
	<DataModel name='DM'>
		<String/>
		<Transformer class='IntToHex'/>
	</DataModel>
</Peach>
";

            PitParser parser = new PitParser();
            Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

            var data = Bits.Fmt("{0}", new byte[] {0x31, 0x30 });

            DataCracker cracker = new DataCracker();
            cracker.CrackData(dom.dataModels[0], data);

            var val = (string)dom.dataModels[0][0].DefaultValue;

            Assert.AreEqual("16", val);
        }
    }
}

// end
