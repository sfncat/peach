

using System.IO;
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
	class PaddingTests
	{
		[Test]
		public void PaddingTest1()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Padding alignment=\"8\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Padding padding = dom.dataModels[0][0] as Padding;

			Assert.AreEqual(0, ((BitStream)padding.DefaultValue).LengthBits);
		}

		[Test]
		public void PaddingTest2()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"8\" />"+
				"		<Padding alignment=\"16\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Padding padding = dom.dataModels[0][1] as Padding;

			Assert.AreEqual(8, ((BitStream)padding.DefaultValue).LengthBits);
		}

		[Test]
		public void PaddingTest3()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Blob name='header' length='10'/>
		<Block name='blk'>
			<Blob name='payload' length='10' valueType='hex' value='11 22 33 44 55 66 77 88 99 aa' />
			<Padding name='padding' alignment='128'>
				<Fixup class='FillValue'>
					<Param name='ref' value='padding'/>
					<Param name='start' value='1'/>
					<Param name='stop' value='255'/>
				</Fixup>
			</Padding>
			<Number name='len' size='8' signed='false'>
				<Relation type='size' of='padding'/>
			</Number>
			<Number name='next' size='8' value='255' signed='false'/>
		</Block>
	</DataModel>
</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var val = dom.dataModels[0].Value;
			Assert.NotNull(val);

			val.Seek(0, SeekOrigin.Begin);
			string str = Utilities.HexDump(val);
			Assert.NotNull(str);

			Assert.AreEqual(80 + 128, val.LengthBits);

			byte[] expected = new byte[] {
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // header
				0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xaa, // payload
				0x01, 0x02, 0x03, 0x04,                                     // padding
				0x04,                                                       // length of padding
				0xff                                                        // next
			};

			Assert.AreEqual(expected, val.ToArray());
		}

		[Test]
		public void PaddingTest4()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"33\" />" +
				"		<Padding alignment=\"16\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Padding padding = dom.dataModels[0][1] as Padding;

			Assert.AreEqual(15, ((BitStream)padding.DefaultValue).LengthBits);
		}

		[Test]
		public void PaddingTest5()
		{
			string xml = @"
<Peach>
	<DataModel name='data'>
		<Block name='block1'>
			<String value='top' />
		</Block>
		<Block name='block2'>
			<Padding alignment='32' alignedTo='block1' />
			<String value='middle' />
		</Block>
		<Block name='block3'>
			<String value='bottom' />
		</Block>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Padding padding = ((Block)dom.dataModels[0][1])[0] as Padding;

			Assert.AreEqual(8, ((BitStream)padding.DefaultValue).LengthBits);
		}

		[Test]
		public void PaddingTest6()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String/>
		<Padding alignedTo='missing'/>
	</DataModel>
</Peach>";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));
			Assert.AreEqual("Error, unable to resolve alignedTo 'missing'.", ex.Message);
		}

		[Test]
		public void PaddingTest7()
		{
			string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<Blob length='3'/>
		<Padding minSize='32'/>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Padding padding = dom.dataModels[0][1] as Padding;

			Assert.AreEqual(8, ((BitStream)padding.DefaultValue).LengthBits);
		}

		[Test]
		public void PaddingTest8()
		{
			string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<Blob length='3'/>
		<Padding minSize='32' alignment='48' />
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Padding padding = dom.dataModels[0][1] as Padding;

			Assert.AreEqual(24, ((BitStream)padding.DefaultValue).LengthBits);
		}

		[Test]
		public void PaddingTest9()
		{
			string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<Blob length='3'/>
		<Padding minSize='32' alignment='32' />
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Padding padding = dom.dataModels[0][1] as Padding;

			Assert.AreEqual(8, ((BitStream)padding.DefaultValue).LengthBits);
		}

		[Test]
		public void PaddingTest10()
		{
			string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<Blob length='3'/>
		<Padding minSize='24'/>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Padding padding = dom.dataModels[0][1] as Padding;

			Assert.AreEqual(0, ((BitStream)padding.DefaultValue).LengthBits);
		}

		[Test]
		public void PaddingTest11()
		{
			string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<Blob length='3'/>
		<Blob length='3'/>
		<Padding minSize='64'/>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Padding padding = dom.dataModels[0][2] as Padding;

			Assert.AreEqual(16, ((BitStream)padding.DefaultValue).LengthBits);
		}

		[Test]
		public void PaddingTest12()
		{
			string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<Blob length='3'/>
		<Blob name='b' length='3'/>
		<Padding minSize='64' alignedTo='b'/>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Padding padding = dom.dataModels[0][2] as Padding;

			Assert.AreEqual(40, ((BitStream)padding.DefaultValue).LengthBits);
		}

		[Test]
		public void PaddingTest13()
		{
			string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<Blob length='3'/>
		<Blob name='b' length='3'/>
		<Padding minSize='24' alignedTo='b' alignment='64'/>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Padding padding = dom.dataModels[0][2] as Padding;

			Assert.AreEqual(40, ((BitStream)padding.DefaultValue).LengthBits);
		}

	}
}
