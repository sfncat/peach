

//using NUnit.Framework.Constraints;

using System.IO;
using System.Net;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.PitParserTests
{
	[TestFixture]
	[Quick]
	[Peach]
	class BlobTests
	{
		[Test]
		public void BlobTest1()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob value=\"Hello World\" />" +
				"	</DataModel>" +
				"</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			Blob blob = dom.dataModels[0][0] as Blob;

			Assert.AreEqual(Variant.VariantType.BitStream, blob.DefaultValue.GetVariantType());
			Assert.AreEqual(Encoding.ASCII.GetBytes("Hello World"), ((BitwiseStream)blob.DefaultValue).ToArray());
		}

		[Test]
		public void BlobTest2()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob value=\"41 42 43 44\" valueType=\"hex\" />" +
				"	</DataModel>" +
				"</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			Blob blob = dom.dataModels[0][0] as Blob;

			Assert.AreEqual(Variant.VariantType.BitStream, blob.DefaultValue.GetVariantType());
			Assert.AreEqual(new byte[] { 0x41, 0x42, 0x43, 0x44 }, ((BitwiseStream)blob.DefaultValue).ToArray());
		}

		[Test]
		public void BlobTest3()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob value=\"1234\" />" +
				"	</DataModel>" +
				"</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			Blob blob = dom.dataModels[0][0] as Blob;

			Assert.AreEqual(Variant.VariantType.BitStream, blob.DefaultValue.GetVariantType());
			Assert.AreEqual(ASCIIEncoding.ASCII.GetBytes("1234"), ((BitwiseStream)blob.DefaultValue).ToArray());
		}

		[Test]
		public void BlobTest4()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob length=\"20\"/>" +
				"	</DataModel>" +
				"</Peach>";

			var dom = DataModelCollector.ParsePit(xml);

			var val = dom.dataModels[0].Value;
			Assert.NotNull(val);
			Assert.AreEqual(20, val.Length);
		}

		[Test]
		public void BlobTest5()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob value=\"127.0.0.1\" valueType=\"ipv4\"/>" +
				"	</DataModel>" +
				"</Peach>";

			var dom = DataModelCollector.ParsePit(xml);

			var val = dom.dataModels[0].Value.ToArray();
			var exp = IPAddress.Loopback.GetAddressBytes();
			Assert.AreEqual(exp, val);
		}

		[Test]
		public void BlobTest6()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob value=\"::1\" valueType=\"ipv6\"/>" +
				"	</DataModel>" +
				"</Peach>";

			var dom = DataModelCollector.ParsePit(xml);

			var val = dom.dataModels[0].Value.ToArray();
			var exp = IPAddress.IPv6Loopback.GetAddressBytes();
			Assert.AreEqual(exp, val);
		}

		[Test]
		public void BlobTest7()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob value=\"0.0.0.0\" valueType=\"ipv4\"/>" +
				"	</DataModel>" +
				"</Peach>";

			var dom = DataModelCollector.ParsePit(xml);

			var val = dom.dataModels[0].Value.ToArray();
			var exp = IPAddress.Any.GetAddressBytes();
			Assert.AreEqual(exp, val);
		}

		[Test]
		public void BlobTest8()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob value=\"00:11:22:33:44:55\" valueType=\"hex\"/>" +
				"	</DataModel>" +
				"</Peach>";

			var dom = DataModelCollector.ParsePit(xml);

			var val = dom.dataModels[0].Value.ToArray();
			var exp = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 };
			Assert.AreEqual(exp, val);
		}

		private void DoHexPad(bool throws, int length, string value, bool token = false)
		{
			string attr = value == null ? "" : string.Format("value=\"{0}\"", value);

			string template = "<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob length=\"{0}\" valueType=\"hex\" token=\"{2}\" {1}/>" +
				"	</DataModel>" +
				"</Peach>";

			string xml = string.Format(template, length, attr, token.ToString().ToLower());

			PitParser parser = new PitParser();

			if (throws)
			{
				Assert.Throws<PeachException>(delegate() {
					parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
				});
				return;
			}

			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Peach.Core.Dom.Blob blob = dom.dataModels[0][0] as Peach.Core.Dom.Blob;

			Assert.AreNotEqual(null, blob);
			Assert.NotNull(blob.DefaultValue);
			Assert.AreEqual(0, blob.lengthAsBits % 8);
			Assert.AreEqual(length, blob.lengthAsBits / 8);
			var type = blob.DefaultValue.GetVariantType();
			Assert.True(Variant.VariantType.BitStream == type ||Variant.VariantType.ByteString == type);
			BitStream bs = (BitStream)blob.DefaultValue;
			Assert.AreEqual(length, bs.Length);
		}

		[Test]
		public void TestHexPad()
		{
			DoHexPad(false, 4, null);
			DoHexPad(false, 4, "01 02 03 04");
			DoHexPad(false, 4, "01 02 03");
			DoHexPad(true, 4, "01 02 03 04 05");

			DoHexPad(true, 4, "01 02 03 04 05", true);
			DoHexPad(false, 4, "01 02 03", true);
			DoHexPad(false, 4, "", true);
		}

		[Test]
		public void ValueTypeHex()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Blob value='00-11-22-33-44-55' valueType='hex' />
	</DataModel>
</Peach>";
			var dom = DataModelCollector.ParsePit(xml);

			var val = dom.dataModels[0].Value.ToArray();
			var exp = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 };
			Assert.AreEqual(exp, val);
		}

	}
}
