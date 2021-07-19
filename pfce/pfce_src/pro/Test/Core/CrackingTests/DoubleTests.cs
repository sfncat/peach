using System;
using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.IO;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.CrackingTests
{
	[TestFixture]
	[Category("Peach")]
	[Quick]
	class DoubleTests
	{
		[Test]
		public void CrackDouble1()
		{
			const string xml = @"<?xml version='1.0' encoding='utf-8'?>
                <Peach>
                	<DataModel name='TheDataModel'>
                		<Double size='64' />
                	</DataModel>
                </Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var bs = new BitStream(BitConverter.GetBytes(1.0));
			bs.Seek(0, SeekOrigin.Begin);

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], bs);

			Assert.AreEqual(1.0, (double)dom.dataModels[0][0].DefaultValue);
		}

		[Test]
		public void CrackDouble2()
		{
			const string xml = @"<?xml version='1.0' encoding='utf-8'?>
                <Peach>
                	<DataModel name='TheDataModel'>
                		<Double size='64' endian='big'/>
                	</DataModel>
                </Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var bs = new BitStream(BitConverter.GetBytes(3.0386519416174186E-319d));
			bs.Seek(0, SeekOrigin.Begin);

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], bs);

			Assert.AreEqual(1.0, (double)dom.dataModels[0][0].DefaultValue);
		}
	}
}
