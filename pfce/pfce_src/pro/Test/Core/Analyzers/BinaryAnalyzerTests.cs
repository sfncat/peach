

using System.IO;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Analyzers
{
	[TestFixture]
	[Quick]
	[Peach]
    class BinaryAnalyzerTests
    {
        [Test]
        public void BasicTest()
        {
            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
                "	<DataModel name=\"TheDataModel\">" +
                "       <Blob name=\"TheBlob\">" +
                "           <Analyzer class=\"Binary\"> "+
                "               <Param name=\"AnalyzeStrings\" value=\"false\"/> "+
                "           </Analyzer> "+
                "       </Blob>"+
                "	</DataModel>" +
                "</Peach>";

            PitParser parser = new PitParser();
            Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

            Random rnd = new Random(123);

            BitStream bs = new BitStream();
            BitWriter data = new BitWriter(bs);
            data.LittleEndian();

            for (int cnt = 0; cnt < 100; cnt++)
                data.WriteInt32(rnd.NextInt32());

            data.WriteString("Hello World");

            for (int cnt = 0; cnt < 100; cnt++)
                data.WriteInt32(rnd.NextInt32());

            data.WriteString("Peach Fuzzer");

            for (int cnt = 0; cnt < 100; cnt++)
                data.WriteInt32(rnd.NextInt32());

            bs.SeekBits(0, SeekOrigin.Begin);

            DataCracker cracker = new DataCracker();
            cracker.CrackData(dom.dataModels[0], bs);
            bs.Seek(0, SeekOrigin.Begin);

            Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Block);
			Assert.AreEqual("TheBlob", dom.dataModels["TheDataModel"][0].Name);
            Assert.AreEqual(bs.ToArray(), dom.dataModels["TheDataModel"].Value.ToArray());

            var block = dom.dataModels["TheDataModel"][0] as Block;
            Assert.IsTrue(block[5] is Peach.Core.Dom.String);
            Assert.AreEqual("Hello WorldYY&", (string)block[5].InternalValue);

            Assert.IsTrue(block[11] is Peach.Core.Dom.String);
            Assert.AreEqual("Peach Fuzzer|", (string)block[11].InternalValue);
        }

		[Test]
		public void StableNames()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Blob name='Val'>
			<Analyzer class='Binary'/>
		</Blob>
	</DataModel>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			var bs = Bits.Fmt("{0:B16}{1}{2:B16}{3}{4:B16}", 0, "HelloWorld", 1, "FooBar", 2);
			var cracker = new DataCracker();

			cracker.CrackData(dom.dataModels[0], bs);

			var names = dom.dataModels[0].Walk().Select(e => e.Name).ToList();

			var expected = new[]
			{
				"DM",
				"Val",
				"Elem_0",
				"Elem_1",
				"Elem_2",
				"Elem_3",
				"Elem_4"
			};

			Assert.AreEqual(expected, names);
		}

    }
}

// end
