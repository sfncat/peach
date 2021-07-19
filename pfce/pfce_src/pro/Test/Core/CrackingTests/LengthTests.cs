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
	public class LengthTests
	{
		string cont_template = @"
<Peach>
	<DataModel name=""TheDataModel"">
		<{0} lengthType=""{1}"" {2}=""{3}"">
			<Blob/>
		</{0}>
		<!-- ensure we don't fall into the isLastUnsizedElement case -->
		<Blob />
	</DataModel>
</Peach>";

		string elem_template = @"
<Peach>
	<DataModel name=""TheDataModel"">
		<{0} lengthType=""{1}"" {2}=""{3}""/>
		<!-- ensure we don't fall into the isLastUnsizedElement case -->
		<Blob />
	</DataModel>
</Peach>";

		BitwiseStream Crack(string template, string elem, string units, string lengthType, string length)
		{
			string xml = string.Format(template, elem, units, lengthType, length);

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));
			var data = Bits.Fmt("{0}", new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(1, dom.dataModels.Count);
			Assert.AreEqual(2, dom.dataModels[0].Count);
			var de = dom.dataModels[0][0];

			var cont = de as DataElementContainer;
			if (cont != null)
			{
				Assert.AreEqual(1, cont.Count);
				de = cont[0];
			}
			
			var value = de.Value;
			return value;
		}

		BitwiseStream CrackElement(string elem, string units, string lengthType, string length)
		{
			return Crack(elem_template, elem, units, lengthType, length);
		}

		BitwiseStream CrackContainer(string elem, string units, string lengthType, string length)
		{
			return Crack(cont_template, elem, units, lengthType, length);
		}

		[Test]
		public void BlobChars()
		{
			Assert.Throws<PeachException>(() =>
				CrackElement("Blob", "chars", "length", "5"));
		}

		[Test]
		public void BlobBytes()
		{
			var bs = CrackElement("Blob", "bytes", "length", "5");
			Assert.AreEqual(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55 }, bs.ToArray());
		}

		[Test]
		public void BlobBits()
		{
			var bs = CrackElement("Blob", "bits", "length", "36");
			Assert.AreEqual(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x50 }, bs.ToArray());
		}

		[Test]
		public void BlockChars()
		{
			Assert.Throws<PeachException>(() =>
				CrackContainer("Block", "chars", "length", "5"));
		}

		[Test]
		public void BlockBytes()
		{
			var bs = CrackContainer("Block", "bytes", "length", "5");
			Assert.AreEqual(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55 }, bs.ToArray());
		}

		[Test]
		public void BlockBits()
		{
			var bs = CrackContainer("Block", "bits", "length", "36");
			Assert.AreEqual(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x50 }, bs.ToArray());
		}

		[Test]
		public void ChoiceChars()
		{
			Assert.Throws<PeachException>(() =>
				CrackContainer("Choice", "chars", "length", "5"));
		}

		[Test]
		public void ChoiceBytes()
		{
			var bs = CrackContainer("Choice", "bytes", "length", "5");
			Assert.AreEqual(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55 }, bs.ToArray());
		}

		[Test]
		public void ChoiceBits()
		{
			var bs = CrackContainer("Choice", "bits", "length", "36");
			Assert.AreEqual(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x50 }, bs.ToArray());
		}

		[Test]
		public void FlagsChars()
		{
			Assert.Throws<PeachException>(() =>
				CrackContainer("Flags", "chars", "length", "2"));
		}

		[Test]
		public void FlagsBytes()
		{
			Assert.Throws<PeachException>(() =>
				CrackElement("Flags", "bytes", "length", "2"));
		}

		[Test]
		public void FlagsBits()
		{
			Assert.Throws<PeachException>(() =>
				CrackElement("Flags", "bits", "length", "16"));
		}

		[Test]
		public void StringChars()
		{
			var bs = CrackElement("String type=\"utf16\"", "chars", "length", "2");
			Assert.AreEqual(new byte[] { 0x11, 0x22, 0x33, 0x44 }, bs.ToArray());
		}

		[Test]
		public void StringBytes()
		{
			var bs = CrackElement("String type=\"utf16\"", "bytes", "length", "4");
			Assert.AreEqual(new byte[] { 0x11, 0x22, 0x33, 0x44 }, bs.ToArray());
		}

		[Test]
		public void StringBits()
		{
			var bs = CrackElement("String type=\"utf16\"", "bits", "length", "32");
			Assert.AreEqual(new byte[] { 0x11, 0x22, 0x33, 0x44 }, bs.ToArray());
		}
	}
}