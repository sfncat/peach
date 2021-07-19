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
    class CountRelationTests
    {
        [Test]
        public void CrackCountOf1()
        {
            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
                "	<DataModel name=\"TheDataModel\">" +
                "		<Number size=\"8\">" +
                "			<Relation type=\"count\" of=\"numbers\" />" +
                "		</Number>" +
                "		<Number size=\"8\" name=\"numbers\" minOccurs=\"0\" maxOccurs=\"-1\"/>" +
                "	</DataModel>" +
                "</Peach>";

            PitParser parser = new PitParser();
            Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

            var data = Bits.Fmt("{0}", new byte[] { 0x05, 0x01, 0x02, 0x03, 0x04, 0x05 });

            DataCracker cracker = new DataCracker();
            cracker.CrackData(dom.dataModels[0], data);

            Assert.AreEqual(5, (int)dom.dataModels[0][0].DefaultValue);
            Assert.IsInstanceOf<Peach.Core.Dom.Array>(dom.dataModels[0][1]);
            Assert.AreEqual(5, ((Peach.Core.Dom.Array)dom.dataModels[0][1]).Count);
            Assert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, dom.dataModels[0][1].InternalValue.BitsToArray());
        }

        [Test]
        public void CrackCountOf2()
        {
            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
                "	<DataModel name=\"TheDataModel\">" +
                "		<Number size=\"8\" name=\"numbers\" occurs=\"5\"/>" +
                "	</DataModel>" +
                "</Peach>";

            PitParser parser = new PitParser();
            Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

            var data = Bits.Fmt("{0}", new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0xFF, 0xFF, 0xFF, 0xFF });

            DataCracker cracker = new DataCracker();
            cracker.CrackData(dom.dataModels[0], data);

            Assert.IsInstanceOf<Peach.Core.Dom.Array>(dom.dataModels[0][0]);
            Assert.AreEqual(5, ((Peach.Core.Dom.Array)dom.dataModels[0][0]).Count);
            Assert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, dom.dataModels[0][0].InternalValue.BitsToArray());
        }


        [Test]
        public void CrackCountOf3()
        {
            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
                "	<DataModel name=\"TheDataModel\">" +
                "		<Number size=\"8\" name=\"numbers\" minOccurs=\"5\" maxOccurs=\"5\"/>" +
                "	</DataModel>" +
                "</Peach>";

            PitParser parser = new PitParser();
            Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

            var data = Bits.Fmt("{0}", new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0xFF, 0xFF, 0xFF, 0xFF });

            DataCracker cracker = new DataCracker();
            cracker.CrackData(dom.dataModels[0], data);

            Assert.IsInstanceOf<Peach.Core.Dom.Array>(dom.dataModels[0][0]);
            Assert.AreEqual(5, ((Peach.Core.Dom.Array)dom.dataModels[0][0]).Count);
            Assert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, dom.dataModels[0][0].InternalValue.BitsToArray());
        }

        [Test]
        public void CrackCountOf4()
        {
            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
                "	<DataModel name=\"TheDataModel\">" +
                "		<Number size=\"8\">" +
                "			<Relation type=\"count\" of=\"numbers\" expressionGet=\"count/2\"/>" +
                "		</Number>" +
                "		<Number size=\"8\" name=\"numbers\" minOccurs=\"0\" maxOccurs=\"-1\"/>" +
                "	</DataModel>" +
                "</Peach>";

            PitParser parser = new PitParser();
            Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

            var data = Bits.Fmt("{0}", new byte[] { 0x0A, 0x01, 0x02, 0x03, 0x04, 0x05 });

            DataCracker cracker = new DataCracker();
            cracker.CrackData(dom.dataModels[0], data);

            Assert.AreEqual(10, (int)dom.dataModels[0][0].DefaultValue);
            Assert.AreEqual(5, (int)dom.dataModels[0][0].InternalValue);
            Assert.IsInstanceOf<Peach.Core.Dom.Array>(dom.dataModels[0][1]);
            Assert.AreEqual(5, ((Peach.Core.Dom.Array)dom.dataModels[0][1]).Count);
            Assert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, dom.dataModels[0][1].InternalValue.BitsToArray());
        }

		[Test]
		public void CrackCountOf5()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String length=\"8\">" +
				"			<Relation type=\"count\" of=\"Data\" />" +
				"			<Hint name='NumericalString' value='true' />" +
				"		</String>" +
				"		<String name=\"Data\" length=\"1\" minOccurs=\"0\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "000000088ByteLen");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(8, (int)dom.dataModels[0][0].DefaultValue);
			var array = dom.dataModels[0][1] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(8, array.Count);
		}

		[Test]
		public void CrackCountOfLayers()
		{
			string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
				<Peach>
					<DataModel name=""TheDataModel"">
						<Number size=""8"">
							<Relation type=""count"" of=""x1""/>
						</Number>
						<Number size=""8"">
							<Relation type=""count"" of=""y1""/>
						</Number>
						<Block name=""x1"" minOccurs=""0"">
							<Block name=""y1"" minOccurs=""0"">
								<String length=""1""/>
							</Block>
						</Block>
					</DataModel>
				</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 0x02, 0x03, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(2, (int)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual(3, (int)dom.dataModels[0][1].DefaultValue);
			Peach.Core.Dom.Array ArrayX1 = (Peach.Core.Dom.Array)dom.dataModels[0][2];
			Assert.AreEqual(2, ArrayX1.Count);

			Peach.Core.Dom.Array ArrayY1 = (Peach.Core.Dom.Array)((Peach.Core.Dom.Block)ArrayX1[0])[0];
			Assert.AreEqual(3, ArrayY1.Count);
			Assert.AreEqual("0", (string)((Peach.Core.Dom.String)((Peach.Core.Dom.Block)ArrayY1[0])[0]).DefaultValue);
			Assert.AreEqual("1", (string)((Peach.Core.Dom.String)((Peach.Core.Dom.Block)ArrayY1[1])[0]).DefaultValue);
			Assert.AreEqual("2", (string)((Peach.Core.Dom.String)((Peach.Core.Dom.Block)ArrayY1[2])[0]).DefaultValue);

			Peach.Core.Dom.Array ArrayY2 = (Peach.Core.Dom.Array)((Peach.Core.Dom.Block)ArrayX1[1])[0];
			Assert.AreEqual(3, ArrayY2.Count);
			Assert.AreEqual("3", (string)((Peach.Core.Dom.String)((Peach.Core.Dom.Block)ArrayY2[0])[0]).DefaultValue);
			Assert.AreEqual("4", (string)((Peach.Core.Dom.String)((Peach.Core.Dom.Block)ArrayY2[1])[0]).DefaultValue);
			Assert.AreEqual("5", (string)((Peach.Core.Dom.String)((Peach.Core.Dom.Block)ArrayY2[2])[0]).DefaultValue);
		}

		[Test]
		public void CrackLeftovers()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number name=\"count\" size=\"8\">" +
				"			<Relation type=\"count\" of=\"num\" />" +
				"		</Number>" +
				"		<Number name=\"num\" size=\"8\" minOccurs=\"2\"/>" +
				"		<String name=\"str\" length=\"9\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "\x06QWERTYleftoversextrajunk");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(3, dom.dataModels[0].Count);
			Peach.Core.Dom.Array array = (Peach.Core.Dom.Array)dom.dataModels[0][1];
			Assert.AreEqual(6, array.Count);
			Assert.AreEqual("leftovers", (string)dom.dataModels[0][2].DefaultValue);
		}
	}
}
