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
    class IcmpChecksumFixupTests : DataModelCollector
    {
        [Test]
        public void Test1()
        {
            // standard test (Odd length string)

            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
                "<Peach>" +
                "   <DataModel name=\"TheDataModel\">" +
                "       <Number name=\"ICMPChecksum\" signed=\"false\" endian=\"big\" size=\"16\">" +
                "           <Fixup class=\"IcmpChecksumFixup\">" +
                "               <Param name=\"ref\" value=\"Data\"/>" +
                "           </Fixup>" +
                "       </Number>" +
                "       <Blob name=\"Data\" value=\"Hello\"/>" +
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
            // -- this is the pre-calculated checksum from Peach2.3 on the blob: "Hello"
            byte[] precalcChecksum = new byte[] { 0xDC, 0x2D };
            Assert.AreEqual(1, values.Count);
            Assert.AreEqual(precalcChecksum, values[0].ToArray());
        }

        [Test]
        public void Test2()
        {
            // standard test (Even length string)

            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
                "<Peach>" +
                "   <DataModel name=\"TheDataModel\">" +
				"       <Number name=\"ICMPChecksum\" signed=\"false\" endian=\"big\" size=\"16\">" +
                "           <Fixup class=\"IcmpChecksumFixup\">" +
                "               <Param name=\"ref\" value=\"Data\"/>" +
                "           </Fixup>" +
                "       </Number>" +
                "       <Blob name=\"Data\" value=\"1234\"/>" +
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
            // -- this is the pre-calculated checksum from Peach2.3 on the blob: "Hello"
            byte[] precalcChecksum = new byte[] { 0x9b, 0x99 };
            Assert.AreEqual(1, values.Count);
            Assert.AreEqual(precalcChecksum, values[0].ToArray());
        }

		[Test]
		public void TestRoundTrip()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Number size='16' signed='false' endian='big'>
			<Fixup class='IcmpChecksumFixup'>
				<Param name='ref' value='DM' />
			</Fixup>
		</Number>
		<Blob value='Hello' />
	</DataModel>
</Peach>
";

			VerifyRoundTrip(xml);
		}
    }
}

// end
