

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
	public class PaddingTests
	{
		static string template = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
			"	<DataModel name=\"TheDataModel\">" +
			"		<Block>" +
			"			<Blob name=\"blb\" length=\"{0}\" valueType=\"hex\" value=\"{1}\" />" +
			"			<Padding alignment=\"16\" /> " +
			"		</Block>" +
			"		<String/>" +
			"	</DataModel>" +
			"</Peach>";

		[Test]
		public void CrackPadding1()
		{
			string xml = template.Fmt(1, "00");

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 1, 2, 49, 50, 51 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(2, dom.dataModels[0].Count);
			var block = dom.dataModels[0][0] as Block;
			Assert.AreEqual(2, block.Count);
			Assert.AreEqual(new byte[] { 1 }, block[0].Value.ToArray());
			Assert.AreEqual(new byte[] { 1 }, block[0].DefaultValue.BitsToArray());
			Assert.AreEqual(8, ((BitStream)block[1].DefaultValue).LengthBits);
			Assert.AreEqual(8, ((BitStream)block[1].Value).LengthBits);
			Assert.AreEqual("123", (string)dom.dataModels[0][1].DefaultValue);

			var value = dom.dataModels[0].Value;
			value.Seek(0, SeekOrigin.Begin);
			Assert.AreEqual(5, value.Length);
			Assert.AreEqual(1, value.ReadByte());
			Assert.AreEqual(0, value.ReadByte());
		}

		[Test]
		public void CrackPadding2()
		{
			string xml = template.Fmt(2, "00 00");

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 1, 2, 49, 50, 51 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(2, dom.dataModels[0].Count);
			var block = dom.dataModels[0][0] as Block;
			Assert.AreEqual(2, block.Count);
			Assert.AreEqual(new byte[] { 1, 2 }, block[0].Value.ToArray());
			Assert.AreEqual(new byte[] { 1, 2 }, block[0].DefaultValue.BitsToArray());
			Assert.AreEqual(0, ((BitStream)block[1].DefaultValue).LengthBits);
			Assert.AreEqual(0, ((BitStream)block[1].Value).LengthBits);
			Assert.AreEqual("123", (string)dom.dataModels[0][1].DefaultValue);

			var value = dom.dataModels[0].Value;
			value.Seek(0, SeekOrigin.Begin);
			Assert.AreEqual(5, value.Length);
			Assert.AreEqual(1, value.ReadByte());
			Assert.AreEqual(2, value.ReadByte());
		}

		[Test]
		public void GeneratePadding1()
		{
			string xml = template.Fmt(1, "00");

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = dom.dataModels[0].Value;
			Assert.AreEqual(16, data.LengthBits);

			var block = dom.dataModels[0][0] as Block;
			var blob = block[0];
			Assert.AreEqual(8, blob.Value.LengthBits);

			var padding = block[1];
			Assert.AreEqual(8, padding.Value.LengthBits);
		}

		[Test]
		public void GeneratePadding2()
		{
			string xml = template.Fmt(0, "");

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = dom.dataModels[0].Value;
			Assert.AreEqual(0, data.LengthBits);

			var block = dom.dataModels[0][0] as Block;
			var blob = block[0];
			Assert.AreEqual(0, blob.Value.LengthBits);

			var padding = block[1];
			Assert.AreEqual(0, padding.Value.LengthBits);
		}

		[Test]
		public void GeneratePadding3()
		{
			string xml = template.Fmt(2, "11 22");

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = dom.dataModels[0].Value;
			Assert.AreEqual(16, data.LengthBits);

			var block = dom.dataModels[0][0] as Block;
			var blob = block[0];
			Assert.AreEqual(16, blob.Value.LengthBits);

			var padding = block[1];
			Assert.AreEqual(0, padding.Value.LengthBits);
		}
	}
}

// end
