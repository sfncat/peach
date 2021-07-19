using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Fixups
{
	[TestFixture]
	[Quick]
	[Peach]
	class IcmpV6ChecksumFixupTests : DataModelCollector
	{
		[Test]
		public void Test1()
		{
			// standard test (Even length string)

			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel\">" +
				"     <Number name=\"ICMPv6Checksum\" endian=\"big\" size=\"16\">" +
				"           <Fixup class=\"IcmpV6ChecksumFixup\">" +
				"               <Param name=\"ref\" value=\"TheDataModel\"/>" +
				"               <Param name=\"src\" value=\"::1\"/>" +
				"               <Param name=\"dst\" value=\"::1\"/>" +
				"           </Fixup>" +
				"     </Number>" +
				"     <Number name=\"Type\" size=\"8\" valueType=\"hex\" value=\"80\"/>" +
				"     <Number name=\"Code\" size=\"8\"/>" +

				"     <Number name=\"Identifier\" endian=\"big\" size=\"16\" valueType=\"hex\" value=\"08 69\" />" +
				"     <Number name=\"Sequence\" endian=\"big\" size=\"16\" valueType=\"hex\" value=\"00 05\" />" +
				"     <Blob name=\"Data\" valueType=\"hex\" value=\"d6a8f05000000000b5c40c0000000000101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f3031323334353637\"/>" +
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
			byte[] precalcChecksum = new byte[] { 0x2f, 0x84 };
			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(precalcChecksum, values[0].ToArray());
		}

		[Test]
		public void TestPacket()
		{
			// Wireshark sample
			// http://wiki.wireshark.org/SampleCaptures?action=AttachFile&do=get&target=v6.pcap
			// Packet #3
			// Source: fe80::200:86ff:fe05:80da (fe80::200:86ff:fe05:80da)
			// Destination: fe80::260:97ff:fe07:69ea (fe80::260:97ff:fe07:69ea)
			// Packet: 8700 68bd 00000000fe80000000000000026097fffe0769ea01010000860580da
			// Expected checksum: 0x68bd

			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Block name='icmpv6'>
			<Number name='type' size='8' signed='false' value='0x87'/>
			<Number name='code' size='8' signed='false' />
			<Number name='csum' size='16' signed='false'>
				<Fixup class='IcmpV6ChecksumFixup'>
					<Param name='ref' value='icmpv6'/>
					<Param name='src' value='fe80::200:86ff:fe05:80da'/>
					<Param name='dst' value='fe80::260:97ff:fe07:69ea'/>
				</Fixup>
			</Number>
			<Blob valueType='hex' value='00000000fe80000000000000026097fffe0769ea01010000860580da'/>
		</Block>
	</DataModel>
</Peach>
";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var csum = ((Block)dom.dataModels[0][0])[2];
			Assert.AreEqual("csum", csum.Name);

			var val = csum.InternalValue;
			Assert.AreEqual(0x68bd, (int)val);
		}

		[Test]
		public void TestRoundTrip()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Number size='16' signed='false' endian='big'>
			<Fixup class='IcmpV6ChecksumFixup'>
				<Param name='ref' value='DM' />
				<Param name='src' value='::1' />
				<Param name='dst' value='::1' />
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