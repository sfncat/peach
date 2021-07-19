using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Test;
using Peach.Core.Fixups;
using Peach.Pro.Core.Fixups;

namespace Peach.Pro.Test.Core.Fixups
{
	[TestFixture]
	[Quick]
	[Peach]
	class IsoFletcher16FixupTests : DataModelCollector
	{
		private ushort CalcSum(byte[] data, uint offset)
		{
			var sum = new IsoFletcher16Fixup.IsoFletcher16(offset);
			sum.Update(data, data.Length);
			return sum.Final();
		}

		[Test]
		public void IsoFletcherTest1()
		{
			// check that the checksum is correct regardless of
			// initial value the checksum bytes in data (indices 14 and 15)
			var correct = 0xe91d;
			ushort offset = 14;
			var data = new byte[] {
				0x02, 0x01, 0x0a, 0x63, 0x09, 0x0e, 0x0a, 0x63,
				0x09, 0x0e, 0x80, 0x00, 0x00, 0x29, 0x00, 0x00,
				0x00, 0x24, 0x00, 0x00, 0x00, 0x01, 0xc0, 0xa8,
				0x7a, 0x00, 0xff, 0xff, 0xff, 0x00, 0x03, 0x00,
				0x27, 0x10
			};

			// indices 14, 15 => 0x00, 0x00
			Assert.AreEqual(correct, CalcSum(data, offset));

			data[14] = 0xff;
			data[15] = 0xff;
			Assert.AreEqual(correct, CalcSum(data, offset));

			data[14] = 0xe9;
			data[15] = 0xd1;
			Assert.AreEqual(correct, CalcSum(data, offset));
		}

		public static string ByteArrayToString(byte[] ba)
		{
			var hex = new System.Text.StringBuilder(ba.Length * 2);
			foreach (byte b in ba)
				hex.AppendFormat("{0:x2}", b);
			return hex.ToString();
		}

		public void OspfTest(byte[] checksum, byte[] prefix, byte[] tail)
		{
			var prefix_str = ByteArrayToString(prefix);
			var tail_str = ByteArrayToString(tail);
			var format = @"
                <Peach>
                    <DataModel name='TheDataModel'>
                        <Block name='LSACheckSumArea'>
                            <Blob name='HeaderStuff' length='14' />
                            <Number name='LSAChecksum' size='16' endian='big' value='0'>
                                <Fixup class='IsoFletcher16Checksum'>
                                    <Param name='ref' value='LSACheckSumArea'/>
                                </Fixup>
                            </Number>
                            <Blob name='Tail' />
                        </Block>
                    </DataModel>

                    <StateModel name='TheState' initialState='Initial'>
                        <State name='Initial'>
                            <Action type='output'>
                                <DataModel ref='TheDataModel'/>
                                <Data>
                                    <Field name='LSACheckSumArea.HeaderStuff'
                                           valueType='hex'
                                           value='{0}' />
                                    <Field name='LSACheckSumArea.Tail'
                                           valueType='hex'
                                           value='{1}' />
                                </Data>
                            </Action>
                        </State>
                    </StateModel>

                    <Test name='Default'>
                        <StateModel ref='TheState'/>
                        <Publisher class='Null'/>
                    </Test>
                </Peach>";

			var xml = string.Format(format, prefix_str, tail_str);

			var parser = new PitParser();

			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration();
			config.singleIteration = true;

			var e = new Engine(this);
			e.startFuzzing(dom, config);

			// verify values
			var correct = new byte[prefix.Length + checksum.Length + tail.Length];
			System.Buffer.BlockCopy(prefix, 0, correct, 0, prefix.Length);
			System.Buffer.BlockCopy(checksum, 0, correct, prefix.Length, checksum.Length);
			System.Buffer.BlockCopy(tail, 0, correct, prefix.Length + checksum.Length, tail.Length);

			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(correct, values[0].ToArray());
		}

		[Test]
		public void OspfTest1()
		{
			var prefix = new byte[] {
				0x02, 0x01, 0x0a, 0x63, 0x09, 0x0e, 0x0a, 0x63,
				0x09, 0x0e, 0x80, 0x00, 0x00, 0x29
			};
			var checksum = new byte[] { 0xe9, 0x1d };
			var tail = new byte[] {
				0x00, 0x24, 0x00, 0x00, 0x00, 0x01, 0xc0, 0xa8,
				0x7a, 0x00, 0xff, 0xff, 0xff, 0x00, 0x03, 0x00,
				0x27, 0x10
			};

			OspfTest(checksum, prefix, tail);
		}

		[Test]
		public void OspfTest2()
		{
			var prefix = new byte[] {
				0x02, 0x01, 0xc0, 0xa8, 0x7a, 0x01, 0xc0, 0xa8,
				0x7a, 0x01, 0x80, 0x00, 0x00, 0x1b
			};
			var checksum = new byte[] { 0x18, 0xfc };
			var tail = new byte[] {
				0x00, 0x3c, 0x00, 0x00, 0x00, 0x03, 0xc0, 0xa8,
				0x7a, 0x01, 0xc0, 0xa8, 0x7a, 0x01, 0x02, 0x00,
				0xff, 0xff, 0x0a, 0x00, 0x01, 0x00, 0xff, 0xff,
				0xff, 0x00, 0x03, 0x00, 0x00, 0x0a, 0x0a, 0x09,
				0x09, 0x09, 0xff, 0xff, 0xff, 0xff, 0x03, 0x00,
				0x27, 0x10
			};

			OspfTest(checksum, prefix, tail);
		}

		[Test]
		public void OspfTest3()
		{
			var prefix = new byte[] {
				0x02, 0x02, 0xc0, 0xa8, 0x7a, 0x01, 0xc0, 0xa8,
				0x7a, 0x01, 0x80, 0x00, 0x00, 0x07
			};
			var checksum = new byte[] { 0x3f, 0xe1 };
			var tail = new byte[] {
				0x00, 0x20, 0xff, 0xff, 0xff, 0x00, 0xc0, 0xa8,
				0x7a, 0x01, 0x0a, 0x63, 0x09, 0x0e
			};

			OspfTest(checksum, prefix, tail);
		}

		[Test]
		public void OspfTest4()
		{
			var prefix = new byte[] {
				0x02, 0x01, 0x0a, 0x63, 0x09, 0x0e, 0x0a, 0x63,
				0x09, 0x0e, 0x80, 0x00, 0x00, 0x2a
			};
			var checksum = new byte[] { 0x2a, 0x31 };
			var tail = new byte[] {
				0x00, 0x24, 0x00, 0x00, 0x00, 0x01, 0xc0, 0xa8,
				0x7a, 0x01, 0xc0, 0xa8, 0x7a, 0xc6, 0x02, 0x00,
				0x27, 0x10
			};

			OspfTest(checksum, prefix, tail);
		}

		[Test]
		public void OspfTest5()
		{
			var prefix = new byte[] {
				0x02, 0x01, 0xc0, 0xa8, 0x7a, 0x01, 0xc0, 0xa8,
				0x7a, 0x01, 0x80, 0x00, 0x00, 0x1c
			};
			var checksum = new byte[] { 0xc0, 0x91 };
			var tail = new byte[] {
				0x00, 0x24, 0x01, 0x00, 0x00, 0x01, 0xc0, 0xa8,
				0x7a, 0x00, 0xff, 0xff, 0xff, 0x00, 0x03, 0x00,
				0x27, 0x10
			};

			OspfTest(checksum, prefix, tail);
		}

		[Test]
		public void OspfTest6()
		{
			var prefix = new byte[] {
				0x02, 0x02, 0xc0, 0xa8, 0x7a, 0x01, 0xc0, 0xa8,
				0x7a, 0x01, 0x80, 0x00, 0x00, 0x01
			};
			var checksum = new byte[] { 0x4b, 0xdb };
			var tail = new byte[] {
				0x00, 0x20, 0xff, 0xff, 0xff, 0x00, 0xc0, 0xa8,
				0x7a, 0x01, 0x0a, 0x63, 0x09, 0x0e
			};

			OspfTest(checksum, prefix, tail);
		}
	}
}
