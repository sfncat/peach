

using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.CrackingTests
{
	[TestFixture]
	[Quick]
	[Peach]
	class NumberTests
	{
		[Test]
		public void CrackNumber1()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"8\" signed=\"true\"/>" +
				"		<Number size=\"16\" signed=\"true\"/>" +
				"		<Number size=\"8\" signed=\"true\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:L8}{1:L16}{2:L8}", 0x10, 0xbb8, 0x19);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(0x10, (int)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual(0xbb8, (int)dom.dataModels[0][1].DefaultValue);
			Assert.AreEqual(0x19, (int)dom.dataModels[0][2].DefaultValue);
		}

		[Test]
		public void CrackNumber2()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"2\" signed=\"true\"/>" +
				"		<Number size=\"2\" signed=\"false\"/>" +
				"		<Number size=\"3\" signed=\"true\"/>" +
				"		<Number size=\"9\" signed=\"false\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", (ushort)0xffff);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(-1, (int)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual(3, (int)dom.dataModels[0][1].DefaultValue);
			Assert.AreEqual(-1, (int)dom.dataModels[0][2].DefaultValue);
			Assert.AreEqual(511, (int)dom.dataModels[0][3].DefaultValue);
		}
	}
}

// end
