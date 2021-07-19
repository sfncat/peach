

using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.PitParserTests
{
	[TestFixture]
	[Quick]
	[Peach]
	public class FlagTests
	{
		[Test]
		public void SimpleFlags()
		{
			string xml = @"
<Peach>
	<DataModel name=""DM"">
		<Flags size=""8"">
			<Flag position=""0"" size=""1""/>
			<Flag position=""1"" size=""1""/>
			<Flag position=""2"" size=""1""/>
			<Flag position=""3"" size=""1""/>
			<Flag position=""4"" size=""1""/>
			<Flag position=""5"" size=""1""/>
			<Flag position=""6"" size=""1""/>
			<Flag position=""7"" size=""1""/>
		</Flags>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(1, dom.dataModels.Count);
			Assert.AreEqual(1, dom.dataModels[0].Count);

			Flags flags = dom.dataModels[0][0] as Flags;

			Assert.NotNull(flags);
			Assert.AreEqual(8, flags.Count);
		}

		private void RunOverlap(int pos1, int size1, int pos2, int size2)
		{
			string template = @"
<Peach>
	<DataModel name=""DM"">
		<Flags size=""8"">
			<Flag position=""{0}"" size=""{1}""/>
			<Flag position=""{2}"" size=""{3}""/>
		</Flags>
	</DataModel>
</Peach>";

			string xml = string.Format(template, pos1, size1, pos2, size2);

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(1, dom.dataModels.Count);
			Assert.AreEqual(1, dom.dataModels[0].Count);

			Flags flags = dom.dataModels[0][0] as Flags;

			Assert.NotNull(flags);
			Assert.AreEqual(2, flags.Count);
		}

		[Test]
		public void TestOverlap()
		{
			Assert.Throws<PeachException>(delegate() { RunOverlap(0, 1, 8, 1); } );
			Assert.Throws<PeachException>(delegate() { RunOverlap(0, 1, 7, 2); });
			Assert.Throws<PeachException>(delegate() { RunOverlap(0, 4, 3, 1); });
			Assert.Throws<PeachException>(delegate() { RunOverlap(0, 4, 3, 2); });
			Assert.Throws<PeachException>(delegate() { RunOverlap(0, 4, 3, 3); });
			Assert.Throws<PeachException>(delegate() { RunOverlap(0, 8, 3, 3); });
			Assert.Throws<PeachException>(delegate() { RunOverlap(5, 2, 3, 3); });

			RunOverlap(0, 6, 7, 1);
			RunOverlap(7, 1, 0, 6);
		}

		private void DoEndian(string endian, byte[] expected)
		{
			string template = @"
<Peach>
	<DataModel name=""DM"">
		<Flags size=""16"" endian=""{0}"">
			<Flag position=""0""  size=""4"" valueType=""hex"" value=""0x0a""/>
			<Flag position=""4""  size=""4"" valueType=""hex"" value=""0x0b""/>
			<Flag position=""8""  size=""4"" valueType=""hex"" value=""0x0c""/>
			<Flag position=""12"" size=""4"" valueType=""hex"" value=""0x0d""/>
		</Flags>
	</DataModel>
</Peach>";

			string xml = string.Format(template, endian);
			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(1, dom.dataModels.Count);


			Flags flags = dom.dataModels[0][0] as Flags;

			Assert.NotNull(flags);
			Assert.AreEqual(4, flags.Count);

			var value = dom.dataModels[0].Value;

			Assert.NotNull(value);
			Assert.AreEqual(2, value.Length);

			byte[] actual = value.ToArray();
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void TestEndian()
		{
			DoEndian("little", new byte[] { 0xba, 0xdc });
			DoEndian("big", new byte[] { 0xab, 0xcd });
		}

		[Test]
		public void TestRelation()
		{
			string xml = @"
<Peach>
	<DataModel name=""DM"">
		<Flags size=""16"" endian=""big"">
			<Flag position=""0"" size=""7"" value=""1""/>
			<Flag position=""7"" size=""9"">
				<Relation type=""size"" of=""blob""/>
			</Flag>
		</Flags>
		<Blob name=""blob"" length=""100""/>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(1, dom.dataModels.Count);

			var value = dom.dataModels[0].Value.ToArray();
			Assert.NotNull(value);
			Assert.AreEqual(102, value.Length);

			Assert.AreEqual(2, value[0]);
			Assert.AreEqual(100, value[1]);
		}

		byte[] RunEndian(string how)
		{
			string xml = @"
<Peach>
	<DataModel name=""DM"">
		<Flags size=""64"" endian=""{0}"">
			<Flag position=""0"" size=""1"" value=""0x0""/>
			<Flag position=""1"" size=""2"" value=""0x1""/>
			<Flag position=""3"" size=""1"" value=""0x1""/>
			<Flag position=""4"" size=""8"" value=""0x3e""/>
			<Flag position=""12"" size=""12"" value=""0x12c""/>
			<Flag position=""24"" size=""32"" value=""0xffeeddcc""/>
			<Flag position=""56"" size=""8"" value=""0x0""/>
		</Flags>
	</DataModel>
</Peach>".Fmt(how);

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(1, dom.dataModels.Count);

			var value = dom.dataModels[0].Value;
			value.SeekBits(0, SeekOrigin.Begin);

			Assert.NotNull(value);
			Assert.AreEqual(64, value.LengthBits);

			var bytes = value.ToArray();

			return bytes;
		}

		byte[] RunEndianSimple(string how)
		{
			string xml = @"
<Peach>
	<DataModel name=""DM"">
		<Flags size=""32"" endian=""{0}"">
			<Flag position=""0"" size=""32"" value=""0xffeeddcc""/>
		</Flags>
	</DataModel>
</Peach>".Fmt(how);

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(1, dom.dataModels.Count);

			var value = dom.dataModels[0].Value;
			value.SeekBits(0, SeekOrigin.Begin);

			Assert.NotNull(value);
			Assert.AreEqual(32, value.LengthBits);

			var bytes = value.ToArray();

			return bytes;
		}

		byte[] RunEndianShort(string how)
		{
			string xml = @"
<Peach>
	<DataModel name=""DM"">
		<Flags size=""28"" endian=""{0}"">
			<Flag position=""0"" size=""28"" value=""0xffeeddc""/>
		</Flags>
	</DataModel>
</Peach>".Fmt(how);

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(1, dom.dataModels.Count);

			var value = dom.dataModels[0].Value;
			value.SeekBits(0, SeekOrigin.Begin);

			Assert.NotNull(value);
			Assert.AreEqual(28, value.LengthBits);

			var bytes = value.ToArray();

			return bytes;
		}

		[Test]
		public void TestBigEndian()
		{
			// Big Endian
			// C bitfield output: 33 e1 2c ff ee dd cc 00
			var bytes = RunEndian("big");
			var expected = new byte[] { 0x33, 0xe1, 0x2c, 0xff, 0xee, 0xdd, 0xcc, 0x00 };

			Assert.AreEqual(bytes, expected);
		}

		[Test]
		public void TestBigEndian1()
		{

			// Big Endian
			// 0xffeeddcc -> ff, ee, dd, cc
			var bytes = RunEndianSimple("big");
			var expected = new byte[] { 0xff, 0xee, 0xdd, 0xcc };

			Assert.AreEqual(bytes, expected);
		}

		[Test]
		public void TestBigEndian2()
		{

			// Big Endian
			// 0xffeeddc -> ff, ee, dd, c0
			var bytes = RunEndianShort("big");
			var expected = new byte[] { 0xff, 0xee, 0xdd, 0xc0 };
			Assert.AreEqual(bytes, expected);
		}

		[Test]
		public void TestLittleEndian()
		{
			// Little Endian
			// C bitfield output: ea c3 12 cc dd ee ff 00
			var bytes = RunEndian("little");
			var expected = new byte[] { 0xea, 0xc3, 0x12, 0xcc, 0xdd, 0xee, 0xff, 0x00 };

			Assert.AreEqual(bytes, expected);
		}

		[Test]
		public void TestLittleEndian1()
		{

			// Little Endian
			// 0xffeeddcc -> cc, dd, ee, ff
			var bytes = RunEndianSimple("little");
			var expected = new byte[] { 0xcc, 0xdd, 0xee, 0xff };

			Assert.AreEqual(bytes, expected);
		}

		[Test]
		public void TestLittleEndian2()
		{

			// Little Endian
			// 0xffeeddc -> dc, ed, fe, f0
			var bytes = RunEndianShort("little");
			var expected = new byte[] { 0xdc, 0xed, 0xfe, 0xf0 };
			Assert.AreEqual(bytes, expected);
		}


		[Test]
		public void TestEndianDefaults()
		{
			const string xml = @"
<Peach>
	<Defaults>
		<Flags endian=""{0}"" />
	</Defaults>

	<DataModel name=""DM"">
		<Flags size=""8"">
			<Flag position=""0"" size=""1""/>
			<Flag position=""1"" size=""1""/>
			<Flag position=""2"" size=""1""/>
			<Flag position=""3"" size=""1""/>
			<Flag position=""4"" size=""1""/>
			<Flag position=""5"" size=""1""/>
			<Flag position=""6"" size=""1""/>
			<Flag position=""7"" size=""1""/>
		</Flags>
	</DataModel>
</Peach>";

			var dom1 = DataModelCollector.ParsePit(xml.Fmt("little"));
			var flags1 = (Flags)dom1.dataModels[0][0];
			Assert.True(flags1.LittleEndian, "Should be little endian");

			var dom2 = DataModelCollector.ParsePit(xml.Fmt("big"));
			var flags2 = (Flags)dom2.dataModels[0][0];
			Assert.False(flags2.LittleEndian, "Should be big endian");

		}

		[Test]
		public void OverrideFlags()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM1'>
		<Block name='B'>
			<Flags name='F' size='8'>
				<Flag name='F0' position='0' size='1'/>
				<Flag name='F1' position='1' size='1'/>
				<Flag name='F2' position='2' size='1'/>
				<Flag name='F3' position='3' size='1'/>
				<Flag name='F4' position='4' size='1'/>
				<Flag name='F5' position='5' size='1'/>
				<Flag name='F6' position='6' size='1'/>
				<Flag name='F7' position='7' size='1'/>
			</Flags>
		</Block>
	</DataModel>

	<DataModel name='DM2' ref='DM1'>
		<Flag name='B.F.F4' position='4' size='1' value='1' />
	</DataModel>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);

			Assert.AreEqual(new byte[] { 0x00 }, dom.dataModels[0].Value.ToArray());
			Assert.AreEqual(new byte[] { 0x10 }, dom.dataModels[1].Value.ToArray());
		}

		[Test]
		public void BadOverrideFlags()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM1'>
		<Block name='B'>
			<Flags name='F' size='8'>
				<Flag name='F0' position='0' size='1'/>
				<Flag name='F1' position='1' size='1'/>
				<Flag name='F2' position='2' size='1'/>
				<Flag name='F3' position='3' size='1'/>
				<Flag name='F4' position='4' size='1'/>
				<Flag name='F5' position='5' size='1'/>
				<Flag name='F6' position='6' size='1'/>
				<Flag name='F7' position='7' size='1'/>
			</Flags>
		</Block>
	</DataModel>

	<DataModel name='DM2' ref='DM1'>
		<Flag name='B.F4' position='4' size='1' value='1' />
	</DataModel>
</Peach>";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));
			Assert.AreEqual("Error, Block 'DM2.B' has unsupported child element 'Flag'.", ex.Message);
		}
	}
}
