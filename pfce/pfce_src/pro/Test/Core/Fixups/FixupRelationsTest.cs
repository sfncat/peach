using System;
using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.IO;
using Peach.Core.Test;
using Peach.Pro.Core.Fixups.Libraries;

namespace Peach.Pro.Test.Core.Fixups
{
	[TestFixture]
	[Quick]
	[Peach]
	class FixupRelationsTest : DataModelCollector
	{
		[Test]
		public void TestFixupAfter()
		{
			// Verify that in a DOM with Fixups after Relations, the fixup runs
			// after the relation has.

			// In this case the data model is:
			// Len, 4 byte number whose value is the size of the data model
			// CRC, 4 byte number whose value is the CRC of the data model
			// Data, 5 byte string.

			// The CRC should include the computed size relation.

			string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<Number name='len' size='32' signed='false'>
			<Relation type='size' of='TheDataModel' />
		</Number>
		<Block>
			<Number name='CRC' size='32' signed='false'>
				<Fixup class='Crc32Fixup'>
					<Param name='ref' value='TheDataModel' />
				</Fixup>
			</Number>
			<String name='Data' value='Hello' />
		</Block>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState' />
		<Publisher class='Null' />
		<Strategy class='Sequential' />
		<Mutators mode='include'>
			<Mutator class='StringLengthVariance' />
		</Mutators>
	</Test>
</Peach>";

			RunEngine(xml);

			// StringLengthVariance goes +/- 50 from length or [0,55] except 5
			// Expect 1 control & 55 mutations
			Assert.AreEqual(56, dataModels.Count);

			var dm1 = dataModels[0].Value.ToArray();

			// Mutations lengths will go 0, 1, 2, 3, 4, 6, 7, 8, 9, 10
			var dm2 = dataModels[10].Value.ToArray();

			Assert.AreEqual(4 + 4 + 5, dm1.Length);
			Assert.AreEqual(dm1.Length + 5, dm2.Length);

			Func<string, byte[]> final = str =>
			{
				var data = Bits.Fmt("{0:L32}{1:L32}{2}", 8 + str.Length, 0, str);

				var crc = new CRCTool();
				crc.Init(CRCTool.CRCCode.CRC32);
				data.Seek(4, SeekOrigin.Begin);
				data.WriteBits(Endian.Little.GetBits((uint)crc.crctablefast(data.ToArray()), 32), 32);

				var ret = data.ToArray();
				return ret;
			};

			var final1 = final("Hello");
			Assert.AreEqual(final1, dm1);

			var final2 = final("HelloHello");
			Assert.AreEqual(final2, dm2);
		}

		[Test]
		public void TestFixupBefore()
		{
			// Verify that in a DOM with Fixups before Relations, the fixup runs
			// after the relation has.

			// In this case the data model is:
			// CRC, 4 byte number whose value is the CRC of the data model
			// Len, 4 byte number whose value is the size of the data model
			// Data, 5 byte string.

			// The CRC should include the computed size relation.

			string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<Block>
			<Number name='CRC' size='32' signed='false'>
				<Fixup class='Crc32Fixup'>
					<Param name='ref' value='TheDataModel' />
				</Fixup>
			</Number>
		</Block>
		<Number name='len' size='32' signed='false'>
			<Relation type='size' of='TheDataModel' />
		</Number>
		<String name='Data' value='Hello' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState' />
		<Publisher class='Null' />
		<Strategy class='Sequential' />
		<Mutators mode='include'>
			<Mutator class='StringLengthVariance' />
		</Mutators>
	</Test>
</Peach>";

			RunEngine(xml);

			// StringLengthVariance goes +/- 50 from length or [0,55] except 5
			// Expect 1 control & 55 mutations
			Assert.AreEqual(56, dataModels.Count);

			var dm1 = dataModels[0].Value.ToArray();

			// Mutations lengths will go 0, 1, 2, 3, 4, 6, 7, 8, 9, 10
			var dm2 = dataModels[10].Value.ToArray();

			Assert.AreEqual(4 + 4 + 5, dm1.Length);
			Assert.AreEqual(dm1.Length + 5, dm2.Length);

			Func<string, byte[]> final = str =>
			{
				var data = Bits.Fmt("{0:L32}{1:L32}{2}", 0, 8 + str.Length, str);

				var crc = new CRCTool();
				crc.Init(CRCTool.CRCCode.CRC32);
				data.Seek(0, SeekOrigin.Begin);
				data.WriteBits(Endian.Little.GetBits((uint)crc.crctablefast(data.ToArray()), 32), 32);

				var ret = data.ToArray();
				return ret;
			};

			var final1 = final("Hello");
			Assert.AreEqual(final1, dm1);

			var final2 = final("HelloHello");
			Assert.AreEqual(final2, dm2);
		}

		[Test]
		public void TestFixupSiblingBefore()
		{
			// Verify that in a DOM with Fixups before Relations, the fixup runs
			// after the relation has.

			// In this case the data model is:
			// Len, 4 byte number whose value is the size of the crc
			// CRC, 4 byte number whose value is the CRC of the length

			// The CRC should include the computed size relation.

			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel\">" +
				"       <Block>" +
				"           <Number name=\"CRC\" size=\"32\" endian=\"big\" signed=\"false\">" +
				"               <Fixup class=\"Crc32Fixup\">" +
				"                   <Param name=\"ref\" value=\"LEN\"/>" +
				"               </Fixup>" +
				"           </Number>" +
				"       </Block>" +
				"       <Number name=\"LEN\" size=\"32\" endian=\"big\" signed=\"false\">" +
				"           <Relation type=\"size\" of=\"CRC\" />" +
				"       </Number>" +
				"   </DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var actual = dom.dataModels[0].Value.ToArray();
			byte[] expected = new byte[] { 38, 41, 27, 5, 0, 0, 0, 4 };
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void TestFixupSiblingAfter()
		{
			// Verify that in a DOM with Fixups before Relations, the fixup runs
			// after the relation has.

			// In this case the data model is:
			// Len, 4 byte number whose value is the size of the crc
			// CRC, 4 byte number whose value is the CRC of the length

			// The CRC should include the computed size relation.

			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel\">" +
				"       <Number name=\"LEN\" size=\"32\" endian=\"big\" signed=\"false\">" +
				"           <Relation type=\"size\" of=\"CRC\" />" +
				"       </Number>" +
				"       <Block>" +
				"           <Number name=\"CRC\" size=\"32\" endian=\"big\" signed=\"false\">" +
				"               <Fixup class=\"Crc32Fixup\">" +
				"                   <Param name=\"ref\" value=\"LEN\"/>" +
				"               </Fixup>" +
				"           </Number>" +
				"       </Block>" +
				"   </DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var actual = dom.dataModels[0].Value.ToArray();
			byte[] expected = new byte[] { 0, 0, 0, 4, 38, 41, 27, 5 };
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void TestFixupChildRelation()
		{
			// Verify that in a DOM with Fixups that are siblings of a Relation,
			// where the fixup ref's the parent of the parent of the relation,
			// the fixup runs after the relation has.

			// In this case the data model is:
			// Len, 4 byte number whose value is the size of the crc
			// CRC, 4 byte number whose value is the CRC of the length

			// The CRC should include the computed size relation.

			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel\">" +
				"       <Block name=\"Data\">" +
				"           <Number name=\"LEN\" size=\"32\" endian=\"big\" signed=\"false\">" +
				"               <Relation type=\"size\" of=\"Data\" />" +
				"           </Number>" +
				"           <Block>" +
				"               <Number name=\"CRC\" size=\"32\" endian=\"big\" signed=\"false\">" +
				"                   <Fixup class=\"Crc32Fixup\">" +
				"                       <Param name=\"ref\" value=\"TheDataModel\"/>" +
				"                   </Fixup>" +
				"               </Number>" +
				"           </Block>" +
				"       </Block>" +
				"   </DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var actual = dom.dataModels[0].Value.ToArray();
			byte[] expected = new byte[] { 0, 0, 0, 8, 85, 82, 148, 168 };
			Assert.AreEqual(expected, actual);
		}
	}
}

// end
