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
	class UDPChecksumFixupTests : DataModelCollector
	{
		[Test]
		public void Test1()
		{
			// standard test (Odd length string)

			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel\">" +
				"       <Number name=\"UDPChecksum\" endian=\"big\" size=\"16\">" +
				"           <Fixup class=\"UDPChecksumFixup\">" +
				"               <Param name=\"ref\" value=\"Data\"/>" +
				"               <Param name=\"src\" value=\"10.0.1.34\"/>" +
				"               <Param name=\"dst\" value=\"10.0.1.30\"/>" +
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
			byte[] precalcChecksum = new byte[] { 0xc5, 0xd7 };
			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(precalcChecksum, values[0].ToArray());
		}

		[Test]
		public void Test2()
		{
			// IPv6 test (Odd length string)

			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel\">" +
				"       <Number name=\"UDPChecksum\" endian=\"big\" size=\"16\">" +
				"           <Fixup class=\"UDPChecksumFixup\">" +
				"               <Param name=\"ref\" value=\"TheDataModel\"/>" +
				"               <Param name=\"src\" value=\"::1\"/>" +
				"               <Param name=\"dst\" value=\"::1\"/>" +
				"           </Fixup>" +
				"       </Number>" +
				"       <Number name=\"SrcPort\" size=\"16\" valueType=\"hex\" value=\"86 d6\"/>" +
                "       <Number name=\"DestPort\" size=\"16\" valueType=\"hex\" value=\"00 01\"/>" +
                "       <Number name=\"Length\" size=\"16\" valueType=\"hex\" value=\"00 0d\"/>"+
				"       <Blob name=\"Data\" value=\"hello\"/>" +
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
			byte[] precalcChecksum = new byte[] { 0x35, 0x29 };
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
			<Fixup class='TCPChecksumFixup'>
				<Param name='ref' value='DM' />
				<Param name='src' value='1.1.1.1' />
				<Param name='dst' value='1.1.1.1' />
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