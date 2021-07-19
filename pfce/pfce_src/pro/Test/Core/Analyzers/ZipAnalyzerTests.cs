using System.IO;
using Ionic.Zip;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.IO;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Analyzers
{
	[TestFixture]
	[Quick]
	[Peach]
	class ZipAnalyzerTests
	{
		[Test]
		public void Crack1()
		{
			var bs = new BitStream();

			using (var z = new ZipFile())
			{
				z.AddEntry("foo", new MemoryStream(Encoding.ASCII.GetBytes("Hello")));
				z.AddEntry("bar", new MemoryStream(Encoding.ASCII.GetBytes("World")));

				z.Save(bs);
			}

			bs.Seek(0, SeekOrigin.Begin);

			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Blob>
			<Analyzer class='Zip'/>
		</Blob>
	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], bs);

			Assert.AreEqual(1, dom.dataModels[0].Count);
			var block = dom.dataModels[0][0] as Peach.Core.Dom.Block;
			Assert.NotNull(block);


		}

		[Test]
		public void Crack2()
		{
			var bs = new BitStream();

			using (var z = new ZipFile())
			{
				z.AddEntry("foo.xml", new MemoryStream(Encoding.ASCII.GetBytes("<Elem>Hello</Elem>")));
				z.AddEntry("bar.bin", new MemoryStream(Encoding.ASCII.GetBytes("World")));

				z.Save(bs);
			}

			bs.Seek(0, SeekOrigin.Begin);

			string xml = @"
<Peach>
	<DataModel name='XmlModel'>
		<String>
			<Analyzer class='Xml'/>
		</String>
	</DataModel>

	<DataModel name='BinModel'>
		<Blob constraint='1 == 1'>
			<Analyzer class='Binary'/>
		</Blob>
	</DataModel>

	<DataModel name='DM'>
		<Blob>
			<Analyzer class='Zip'>
				<Param name='Map' value='/\.xml$/XmlModel/,/\.bin$/BinModel/' />
			</Analyzer>
		</Blob>
	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[2], bs);

			Assert.AreEqual(1, dom.dataModels[2].Count);
			var block = dom.dataModels[2][0] as Peach.Core.Dom.Block;
			Assert.NotNull(block);
			Assert.AreEqual(2, block.Count);
		}

		[Test]
		public void Crack3()
		{
			var inner = new MemoryStream();

			using (var z = new ZipFile())
			{
				z.AddEntry("foo.xml", new MemoryStream(Encoding.ASCII.GetBytes("<Elem>Hello</Elem>")));
				z.AddEntry("bar.bin", new MemoryStream(Encoding.ASCII.GetBytes("World")));

				z.Save(inner);
			}

			inner.Seek(0, SeekOrigin.Begin);

			var bs = new BitStream();

			using (var z = new ZipFile())
			{
				z.AddEntry("bar.zip", inner);

				z.Save(bs);
			}

			bs.Seek(0, SeekOrigin.Begin);

			string xml = @"
<Peach>
	<DataModel name='ZipModel'>
		<Blob>
			<Analyzer class='Zip' />
		</Blob>
	</DataModel>

	<DataModel name='BinModel'>
		<Blob>
			<Analyzer class='Zip'>
				<Param name='Map' value='/\.zip$/ZipModel/' />
			</Analyzer>
		</Blob>
	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[1], bs);

			Assert.AreEqual(1, dom.dataModels[1].Count);
			var block = dom.dataModels[1][0] as Peach.Core.Dom.Block;
			Assert.NotNull(block);
			Assert.AreEqual(1, block.Count);
		}
	}
}
