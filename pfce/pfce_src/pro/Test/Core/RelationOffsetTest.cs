

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
	class RelationOffsetTest : DataModelCollector
	{
		[Test]
		public void BasicTest()
		{
			var xml = @"
<Peach>
	<DataModel name='Block'>
		<Number size='32' endian='big'>
			<Relation type='offset' of='StringData' relative='true' relativeTo='TheDataModel'/>
		</Number>
		<Number size='32' endian='big'>
			<Relation type='size' of='StringData'/>
		</Number>
		<String name='StringData' value='test'/>
	</DataModel>

	<DataModel name='TheDataModel'>
		<String value='1234'/>
		<Block ref='Block'/>
	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(2, dom.dataModels.Count);

			var dm = dom.dataModels[1];
			Assert.AreEqual("TheDataModel", dm.Name);

			// "1234   12    4    test"
			var expected = new byte[] { 49, 50, 51, 52, 0, 0, 0, 12, 0, 0, 0, 4, 116, 101, 115, 116 };
			var actual = dm.Value.ToArray();
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void BasicBlockTest()
		{
			var xml = @"
<Peach>
	<DataModel name='Block'>
		<Number size='32' endian='big'>
			<Relation type='offset' of='BlockData' relative='true' relativeTo='TheDataModel'/>
		</Number>
		<Number size='32' endian='big'>
			<Relation type='size' of='BlockData'/>
		</Number>
		<Block name='BlockData'>
			<String name='StringData' value='test'/>
		</Block>
	</DataModel>

	<DataModel name='TheDataModel'>
		<String value='1234'/>
		<Block ref='Block'/>
	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(2, dom.dataModels.Count);

			var dm = dom.dataModels[1];
			Assert.AreEqual("TheDataModel", dm.Name);

			// "1234   12    4    test"
			var expected = new byte[] { 49, 50, 51, 52, 0, 0, 0, 12, 0, 0, 0, 4, 116, 101, 115, 116 };
			var actual = dm.Value.ToArray();
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void BasicBlockTest2()
		{
			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>

	<Number size='32'> <Relation type='offset' of='Blk1'/> </Number>
	<Number size='32'> <Relation type='size' of='Blk1'/> </Number>

	<Block name='Blk1'>
		<Number size='32' value='0xffffffff'/>
	</Block>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(1, dom.dataModels.Count);

			var dm = dom.dataModels[0];
			Assert.AreEqual("TheDataModel", dm.Name);

			var expected = new byte[] { 8, 0, 0, 0, 4, 0, 0, 0, 0xFF, 0xFF, 0xFF, 0xFF };
			var actual = dm.Value.ToArray();
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void FlagsTest()
		{
			var xml = @"
<Peach>
	<DataModel name='Block'>
		<Number size='32' endian='big'>
			<Relation type='offset' of='StringData' relative='true' relativeTo='TheDataModel'/>
		</Number>
		<Flags size='16' endian='big'>
		</Flags>
		<Number size='16' endian='big'>
			<Relation type='size' of='StringData'/>
		</Number>
		<String name='StringData' value='test'/>
	</DataModel>

	<DataModel name='TheDataModel'>
		<String value='1234'/>
		<Block ref='Block'/>
	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(2, dom.dataModels.Count);

			var dm = dom.dataModels[1];
			Assert.AreEqual("TheDataModel", dm.Name);

			// "1234   12    4    test"
			var expected = new byte[] { 49, 50, 51, 52, 0, 0, 0, 12, 0, 0, 0, 4, 116, 101, 115, 116 };
			var actual = dm.Value.ToArray();
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void RefTest()
		{
			var xml = @"
<Peach>
	<DataModel name='Block'>
		<Number size='32' endian='big'>
			<Relation type='offset' of='StringData' relative='true' relativeTo='Block'/>
		</Number>

		<Number size='32' endian='big'>
			<Relation type='size' of='StringData'/>
		</Number>

		<String name='StringData' value='test'/>
	</DataModel>

	<DataModel name='TheDataModel'>
		<String value='1234'/>
		<Block ref='Block'/>
	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(2, dom.dataModels.Count);

			var dm = dom.dataModels[1];
			Assert.AreEqual("TheDataModel", dm.Name);

			// "1234   12    4    test"
			var expected = new byte[] { 49, 50, 51, 52, 0, 0, 0, 8, 0, 0, 0, 4, 116, 101, 115, 116 };
			var actual = dm.Value.ToArray();
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void RefTest2()
		{
			var xml = @"
<Peach>
	<DataModel name='Block'>
		<Number size='32' endian='big'>
			<Relation type='offset' of='StringData' relative='true' relativeTo='Proxy'/>
		</Number>

		<Number size='32' endian='big'>
			<Relation type='size' of='StringData'/>
		</Number>

		<String name='StringData' value='test'/>
	</DataModel>

	<DataModel name='Proxy'>
		<Block ref='Block'/>
	</DataModel>

	<DataModel name='TheDataModel'>
		<String value='1234'/>
		<Block ref='Proxy'/>
	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(3, dom.dataModels.Count);

			var dm = dom.dataModels[2];
			Assert.AreEqual("TheDataModel", dm.Name);

			// "1234   12    4    test"
			var expected = new byte[] { 49, 50, 51, 52, 0, 0, 0, 8, 0, 0, 0, 4, 116, 101, 115, 116 };
			var actual = dm.Value.ToArray();
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void RefTest3()
		{
			var xml = @"
<Peach>
	<DataModel name='Block'>
		<Number name='BlockSize' size='32' signed='false' endian='big'>
			<Relation type='size' of='Block0' expressionGet='size' expressionSet='size+4'/>
		</Number>

		<Block name='Block0'>
			<Block name='TagTable'>
				<Number name='TagCount' size='32' signed='false' endian='big'>
					<Relation type='size' of='Tags' expressionGet='size' expressionSet='size/12'/>
				</Number>
				<Block name='Tags'>
					<Block name='Tag0'>
						<String value='Tag0'/>
						<Number size='32' signed='false' endian='big'>
							<Relation type='offset' of='Data' relative='true' relativeTo='BlockSize'/>
						</Number>
						<Number size='32' signed='false' endian='big'>
							<Relation type='size' of='Data'/>
						</Number>
					</Block>
					<Block name='Tag1'>
						<String value='Tag1'/>
						<Number size='32' signed='false' endian='big'>
							<Relation type='offset' of='Data' relative='true' relativeTo='BlockSize'/>
						</Number>
						<Number size='32' signed='false' endian='big'>
							<Relation type='size' of='Data'/>
						</Number>
					</Block>
				</Block>
			</Block>

			<Block name='TagData'>
				<Block name='Data'>
					<String value='test'/>
				</Block>
			</Block>
		</Block>
	</DataModel>

	<DataModel name='TheDataModel'>
		<Block ref='Block'/>
	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(2, dom.dataModels.Count);

			var dm = dom.dataModels[1];
			Assert.AreEqual("TheDataModel", dm.Name);

			// "1234   12    4    test"
			var expected = new byte[] {
				0,  0,  0, 36,    0,  0,  0,  2,   84, 97,103, 48,
				0,  0,  0, 32,    0,  0,  0,  4,   84, 97,103, 49,
				0,  0,  0, 32,    0,  0,  0,  4,  116,101,115,116,
			};
			var actual = dm.Value.ToArray();
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void TestFuzz()
		{
			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<Number name='num' size='32' signed='false' endian='big'>
			<Relation type='offset' of='blob'/>
		</Number>
		<String name='str'/>
		<Blob name='blob'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
	</Test>
</Peach>";

			var parser = new PitParser();

			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));
			dom.tests[0].includedMutators = new List<string> {"StringStatic"};

			var config = new RunConfiguration();

			var e = new Engine(this);
			e.startFuzzing(dom, config);

			Assert.AreEqual(1661, dataModels.Count);

			foreach (var dm in dataModels)
			{
				var val = dm.Value;
				var len = val.LengthBits;
				Assert.GreaterOrEqual(len, 32);

				val.Seek(0, SeekOrigin.Begin);
				var rdr = new BitReader(val);
				rdr.BigEndian();
				var offset = rdr.ReadUInt32();

				Assert.AreEqual(len, offset * 8);
			}
		}

		[Test]
		public void TestRelativeTo()
		{
			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<Number name='len' size='32' signed='false' endian='big'>
			<Relation type='size' of='begin'/>
		</Number>
		<String name='begin'/>
		<String name='eol' mutable='false' value='\r\n'/>
		<Number name='num' size='32' signed='false' endian='big'>
			<Relation type='offset' of='blob' relativeTo='eol'/>
		</Number>
		<String name='str'/>
		<Blob name='blob'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
	</Test>
</Peach>";

			var parser = new PitParser();

			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));
			dom.tests[0].includedMutators = new List<string> {"StringStatic"};

			var config = new RunConfiguration();

			var e = new Engine(this);
			e.startFuzzing(dom, config);

			Assert.AreEqual(3321, dataModels.Count);

			foreach (var dm in dataModels)
			{
				var val = dm.Value;
				var len = val.LengthBits;
				Assert.GreaterOrEqual(len, 32);

				val.Seek(0, SeekOrigin.Begin);
				var rdr = new BitReader(val);
				rdr.BigEndian();
				var beginLen = rdr.ReadUInt32();

				val.Seek(beginLen + 2, SeekOrigin.Current);
				var offset = rdr.ReadUInt32();

				Assert.AreEqual((4 + beginLen + offset) * 8, len);
			}
		}

		[Test]
		public void TestRelative()
		{
			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<Number name='len' size='32' signed='false' endian='big'>
			<Relation type='size' of='begin'/>
		</Number>
		<String name='begin'/>
		<String name='eol' mutable='false' value='\r\n'/>
		<Number name='num' size='32' signed='false' endian='big'>
			<Relation type='offset' of='blob' relative='true'/>
		</Number>
		<String name='str'/>
		<Blob name='blob'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
	</Test>
</Peach>";

			var parser = new PitParser();

			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));
			dom.tests[0].includedMutators = new List<string> {"StringStatic"};

			var config = new RunConfiguration();

			var e = new Engine(this);
			e.startFuzzing(dom, config);

			Assert.AreEqual(3321, dataModels.Count);

			foreach (var dm in dataModels)
			{
				var val = dm.Value;
				var len = val.LengthBits;
				Assert.GreaterOrEqual(len, 32);

				val.Seek(0, SeekOrigin.Begin);
				var rdr = new BitReader(val);
				rdr.BigEndian();
				var beginLen = rdr.ReadUInt32();

				val.Seek(beginLen + 2, SeekOrigin.Current);
				var offset = rdr.ReadUInt32();

				Assert.AreEqual((4 + beginLen + 2 + offset) * 8, len);
			}
		}

		[Test]
		public void TestAbsolute()
		{
			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<Number name='len' size='32' signed='false' endian='big'>
			<Relation type='size' of='begin'/>
		</Number>
		<String name='begin'/>
		<String name='eol' mutable='false' value='\r\n'/>
		<Block>
			<Number name='num' size='32' signed='false' endian='big'>
				<Relation type='offset' of='blob'/>
			</Number>
			<String name='str'/>
			<Blob name='blob'/>
		</Block>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
	</Test>
</Peach>";

			var parser = new PitParser();

			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));
			dom.tests[0].includedMutators = new List<string> {"StringStatic"};

			var config = new RunConfiguration();

			var e = new Engine(this);
			e.startFuzzing(dom, config);

			Assert.AreEqual(3321, dataModels.Count);

			foreach (var dm in dataModels)
			{
				var val = dm.Value;
				var len = val.LengthBits;
				Assert.GreaterOrEqual(len, 32);

				val.Seek(0, SeekOrigin.Begin);
				var rdr = new BitReader(val);
				rdr.BigEndian();
				var beginLen = rdr.ReadUInt32();

				val.Seek(beginLen + 2, SeekOrigin.Current);
				var offset = rdr.ReadUInt32();

				Assert.AreEqual(len, offset * 8);
			}
		}

		[Test]
		public void TestAbsoluteChoice()
		{
			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<Number name='len' size='32' signed='false' endian='big'>
			<Relation type='offset' of='item'/>
		</Number>
		<Choice name='c'>
			<String name='item' value='foo' />
		</Choice>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
	</Test>
</Peach>";

			var parser = new PitParser();

			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));
			dom.tests[0].includedMutators = new List<string> {"StringStatic"};

			var config = new RunConfiguration {singleIteration = true};

			var e = new Engine(this);
			e.startFuzzing(dom, config);

			Assert.AreEqual(1, dataModels.Count);

			foreach (var dm in dataModels)
			{
				var val = dm.Value;
				var len = val.LengthBits;
				Assert.GreaterOrEqual(len, 32);

				val.Seek(0, SeekOrigin.Begin);
				var rdr = new BitReader(val);
				rdr.BigEndian();
				var offset = rdr.ReadUInt32();

				Assert.AreEqual(4, offset);
			}
		}


		[Test]
		public void ExpressionGetSetUlong()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM1'>
		<Number size='64' signed='false' endian='big'>
			<Relation type='offset' of='Item' expressionGet='offset &amp; 0x00ffffffffffffff' expressionSet='offset | 0xff00000000000000' />
		</Number>
		<Blob name='Item' length='1' />
	</DataModel>

	<DataModel name='DM2' ref='DM1' />
</Peach>
";

			var dom = ParsePit(xml);

			var val = dom.dataModels[0].Value.ToArray();

			Assert.AreEqual(new byte[] { 0xff, 0, 0, 0, 0, 0, 0, 8, 0 }, val);

			var data = new BitStream(new byte[] { 0xff, 0, 0, 0, 0, 0, 0, 10, 0, 0, 0xff });

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[1], data);

			var blob = (Blob)dom.dataModels[1][1];

			Assert.AreEqual(new byte[] { 0xff }, ((BitwiseStream)blob.DefaultValue).ToArray());
		}
	}
}
// end
