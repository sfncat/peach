

using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.OutputTests
{
	[TestFixture]
	[Quick]
	[Peach]
	class SizeRelationTests
	{
		[Test]
		public void OutputSizeOf1()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"8\">" +
				"			<Relation type=\"size\" of=\"Data\" />" +
				"		</Number>" +
				"		<Blob name=\"Data\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:L8}{1}", 11, "Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			byte[] newData = ASCIIEncoding.ASCII.GetBytes("This is a much longer value than before!");
			dom.dataModels[0][1].DefaultValue = new Variant(newData);

			Assert.AreEqual(newData.Length, (int)dom.dataModels[0][0].InternalValue);
		}

		void RunRelation(string len, string value, string encoding, string lengthType, byte[] expected, bool throws)
		{
			// Use expressionGet/expressionSet so we can have negative numbers
			string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String name='Length' length='{0}' lengthType='{1}' type='{2}' padCharacter='0'>
			<Relation type='size' of='Data' expressionSet='size - 3' expressionGet='size + 3'/>
			<Hint name='NumericalString' value='true' />
		</String>
		<String name='Data' value='{3}'/>
	</DataModel>
</Peach>".Fmt(len, lengthType, encoding, value);

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			try
			{
				var final = dom.dataModels[0].Value.ToArray();
				Assert.AreEqual(expected, final);
			}
			catch (SoftException se)
			{
				Assert.True(throws);
				string msg = "Error, String 'TheDataModel.Length' numeric value '{0}' could not be converted to a {1}-{2} {3} string.".Fmt(
					value.Length - 3, len, lengthType.TrimEnd('s'), encoding);
				Assert.AreEqual(msg, se.Message);
			}
		}

		[Test]
		public void TestStringSize()
		{
			// For 0-9, utf7, utf8 and utf9 are all single byte per char
			var expected = Encoding.ASCII.GetBytes("01013 Byte Len *");

			RunRelation("3", "13 Byte Len *", "ascii", "bytes", expected, false);
			RunRelation("1", "13 Byte Len *", "ascii", "bytes", expected, true);
			RunRelation("3", "13 Byte Len *", "ascii", "chars", expected, false);
			RunRelation("1", "13 Byte Len *", "ascii", "chars", expected, true);
			RunRelation("24", "13 Byte Len *", "ascii", "bits", expected, false);
			RunRelation("8", "13 Byte Len *", "ascii", "bits", expected, true);

			RunRelation("3", "13 Byte Len *", "utf7", "bytes", expected, false);
			RunRelation("1", "13 Byte Len *", "utf7", "bytes", expected, true);
			RunRelation("3", "13 Byte Len *", "utf7", "chars", expected, false);
			RunRelation("1", "13 Byte Len *", "utf7", "chars", expected, true);
			RunRelation("24", "13 Byte Len *", "utf7", "bits", expected, false);
			RunRelation("8", "13 Byte Len *", "utf7", "bits", expected, true);

			RunRelation("3", "13 Byte Len *", "utf8", "bytes", expected, false);
			RunRelation("1", "13 Byte Len *", "utf8", "bytes", expected, true);
			RunRelation("3", "13 Byte Len *", "utf8", "chars", expected, false);
			RunRelation("1", "13 Byte Len *", "utf8", "chars", expected, true);
			RunRelation("24", "13 Byte Len *", "utf8", "bits", expected, false);
			RunRelation("8", "13 Byte Len *", "utf8", "bits", expected, true);

			expected = Bits.Fmt("{0:utf16}{1:ascii}", "010", "13 Byte Len *").ToArray();

			RunRelation("6", "13 Byte Len *", "utf16", "bytes", expected, false);
			RunRelation("2", "13 Byte Len *", "utf16", "bytes", expected, true);
			RunRelation("3", "13 Byte Len *", "utf16", "chars", expected, false);
			RunRelation("1", "13 Byte Len *", "utf16", "chars", expected, true);
			RunRelation("48", "13 Byte Len *", "utf16", "bits", expected, false);
			RunRelation("16", "13 Byte Len *", "utf16", "bits", expected, true);

			expected = Bits.Fmt("{0:utf16be}{1:ascii}", "010", "13 Byte Len *").ToArray();

			RunRelation("6", "13 Byte Len *", "utf16be", "bytes", expected, false);
			RunRelation("2", "13 Byte Len *", "utf16be", "bytes", expected, true);
			RunRelation("3", "13 Byte Len *", "utf16be", "chars", expected, false);
			RunRelation("1", "13 Byte Len *", "utf16be", "chars", expected, true);
			RunRelation("48", "13 Byte Len *", "utf16be", "bits", expected, false);
			RunRelation("16", "13 Byte Len *", "utf16be", "bits", expected, true);

			expected = Bits.Fmt("{0:utf32}{1:ascii}", "010", "13 Byte Len *").ToArray();

			RunRelation("12", "13 Byte Len *", "utf32", "bytes", expected, false);
			RunRelation("4", "13 Byte Len *", "utf32", "bytes", expected, true);
			RunRelation("3", "13 Byte Len *", "utf32", "chars", expected, false);
			RunRelation("1", "13 Byte Len *", "utf32", "chars", expected, true);
			RunRelation("96", "13 Byte Len *", "utf32", "bits", expected, false);
			RunRelation("32", "13 Byte Len *", "utf32", "bits", expected, true);
		}

		[Test]
		public void TestNegative()
		{
			var expected = Encoding.ASCII.GetBytes("-02.");

			RunRelation("3", ".", "ascii", "bytes", expected, false);
			RunRelation("1", ".", "ascii", "bytes", expected, true);
			RunRelation("3", ".", "ascii", "chars", expected, false);
			RunRelation("1", ".", "ascii", "chars", expected, true);
			RunRelation("24", ".", "ascii", "bits", expected, false);
			RunRelation("8", ".", "ascii", "bits", expected, true);

			RunRelation("3", ".", "utf7", "bytes", expected, false);
			RunRelation("1", ".", "utf7", "bytes", expected, true);
			RunRelation("3", ".", "utf7", "chars", expected, false);
			RunRelation("1", ".", "utf7", "chars", expected, true);
			RunRelation("24", ".", "utf7", "bits", expected, false);
			RunRelation("8", ".", "utf7", "bits", expected, true);

			RunRelation("3", ".", "utf8", "bytes", expected, false);
			RunRelation("1", ".", "utf8", "bytes", expected, true);
			RunRelation("3", ".", "utf8", "chars", expected, false);
			RunRelation("1", ".", "utf8", "chars", expected, true);
			RunRelation("24", ".", "utf8", "bits", expected, false);
			RunRelation("8", ".", "utf8", "bits", expected, true);
		}


		[Test]
		public void RelationAndFixup()
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach xmlns='http://peachfuzzer.com/2012/Peach'
       xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
       xsi:schemaLocation='http://peachfuzzer.com/2012/Peach ../peach.xsd'>

	<DataModel name='DM1'>
		<Block name='ToSize'>
			<Block name='ToCrc'>
				<Number name='Length' size='32'>
					<Relation type='size' of='ToSize'/>
				</Number>
				<Number name='Checksum' size='32'>
					<Fixup class='Crc'>
						<Param name='ref' value='ToCrc' />
					</Fixup>
				</Number>
			</Block>
			<Number name='Data' size='32' value='0'/>
		</Block>
	</DataModel>

	<StateModel name='State' initialState='State1' >
		<State name='State1'  >
			<Action name='A1' type='output'>
				<DataModel ref='DM1'/>
			</Action>
			<Action type='slurp' valueXpath='//A1//Data' setXpath='//A2//Data' />
			<Action name='A2' type='output'>
					<DataModel ref='DM1'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='State'/>
		<Publisher class='Null' />
	</Test>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var c = new RunConfiguration();
			c.singleIteration = true;

			var e = new Engine(null);
			e.startFuzzing(dom, c);

			var m1 = dom.tests[0].stateModel.states[0].actions[0].dataModel.Value.ToArray();
			var m2 = dom.tests[0].stateModel.states[0].actions[2].dataModel.Value.ToArray();

			Assert.AreEqual(m1, m2);
		}

		[Test]
		public void TestInvalidate()
		{
			string xml = @"
<Peach>
	<DataModel name='DM1'>
		<Block name='ToSize'>
			<Block name='ToCrc'>
				<Number name='Length' size='32'>
					<Relation type='size' of='ToSize'/>
				</Number>
				<Number name='Checksum' size='32'>
					<Fixup class='Crc'>
						<Param name='ref' value='ToCrc' />
					</Fixup>
				</Number>
			</Block>
			<Number name='Data' size='32' value='0'/>
		</Block>
	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var val = dom.dataModels[0].Value.ToArray();

			dom.dataModels[0].find("Data").Invalidate();

			var iv = dom.dataModels[0].find("Length").InternalValue;
			Assert.AreEqual(12, (int)iv);

			var val2 = dom.dataModels[0].Value.ToArray();

			Assert.AreEqual(val, val2);

			dom.dataModels[0].find("Data").Invalidate();

			var csum = dom.dataModels[0].find("Checksum").Value;
			Assert.NotNull(csum);

			var val3 = dom.dataModels[0].Value.ToArray();

			Assert.AreEqual(val2, val3);
		}
	}
}

// end

