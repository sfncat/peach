

using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.Dom.XPath;
using Peach.Core.IO;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
	class ReportedTests
	{
		/// <summary>
		/// From Sirus.
		/// </summary>
		/// <remarks>
		/// input data attached.   For some reason stalls in the ObjectCopier clone method 
		/// trying to deserialze a 120 megabyte (!) memory stream of a Dom.Number... Wonder 
		/// what the heck the serializer is doing..
		/// </remarks>
		[Test]
		public void ObjectCopierExplode()
		{
			string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
<DataModel name=""GeneratedModel"">
            <Block name=""a"">
                <Number name=""b"" signed=""false"" size=""32""/>
                <Block name=""c"">
                    <Number name=""d"" signed=""false"" size=""32""/>
                    <Number name=""e"" maxOccurs=""9999"" minOccurs=""0"" signed=""false"" size=""32"">
                        <Relation type=""count"" of=""f""/>
                    </Number>
                    <Blob name=""f""/>
                </Block>
            </Block>
    </DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.NotNull(dom);

			var newDom = dom.dataModels[0].Clone();

			Assert.NotNull(newDom);
		}

		/// <summary>
		/// Reported by Sirus
		/// </summary>
		[Test]
		public void CrackExplode()
		{
			string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
<DataModel name=""GeneratedModel"">
            <Block name=""a"">
                <Number name=""b"" size=""32""/>
                <Block name=""c"">
                    <Number name=""d"" size=""32""/>
                    <Number name=""e"" size=""32"">
                        <Relation type=""size"" of=""c""/>
                    </Number>
                </Block>
            </Block>
    </DataModel>
</Peach>
";
			byte[] dataBytes = new byte[] { 
						0x5f,						0xfa,
						0x8a,						0x68,
						0x09,						0x00,
						0x00,						0x00,
						0x9a,						0x3d,
						0x19,						0x28,
						0xc7,						0x1c,
						0x03,						0x9a,
						0xa6 };

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			BitStream data = new BitStream();
			data.Write(dataBytes, 0, dataBytes.Length);
			data.SeekBits(0, SeekOrigin.Begin);

			DataCracker cracker = new DataCracker();
			var ex = Assert.Throws<CrackingFailure>(() => cracker.CrackData(dom.dataModels[0], data));
			Assert.AreEqual("Block 'GeneratedModel.a.c' failed to crack. Read 64 of 5381942480 bits but buffer only has 40 bits left.", ex.Message);
		}

		/// <summary>
		/// Proper data bytes for model of CrackExplode
		/// </summary>
		[Test]
		public void CrackNoExplode()
		{
			string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
<DataModel name=""GeneratedModel"">
            <Block name=""a"">
                <Number name=""b"" size=""32""/>
                <Block name=""c"">
                    <Number name=""d"" size=""32""/>
                    <Number name=""e"" size=""32"">
                        <Relation type=""size"" of=""c""/>
                    </Number>
                </Block>
            </Block>
    </DataModel>
</Peach>
";
			byte[] dataBytes = new byte[] { 
						0x5f,						0xfa,
						0x8a,						0x68,
						0x9a,						0x3d,
						0x19,						0x28,
						0x09,						0x00,
						0x00,						0x00,
						0xc7,						0x1c,
						0x03,						0x9a,
						0xa6 };

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			BitStream data = new BitStream();
			data.Write(dataBytes, 0, dataBytes.Length);
			data.SeekBits(0, SeekOrigin.Begin);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.NotNull(dom);

			var newDom = dom.dataModels[0].Clone();

			Assert.NotNull(newDom);
		}

		/// <summary>
		/// Reported by Sirus
		/// </summary>
		[Test]
		public void CrackExplode2()
		{
			string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
<DataModel name=""GeneratedModel"">
            <Block name=""a"">
                <Block name=""b"">
                    <Number name=""d"" size=""32"">
                        <Relation type=""size"" of=""c""/>
                    </Number>
                    <Number name=""e"" size=""32"">
                        <Relation type=""size"" of=""b""/>
                    </Number>
                </Block>
                <Blob name=""c"">
                </Blob>
            </Block>
    </DataModel>
</Peach>";
			byte[] dataBytes = new byte[] { 
						0x1b,
						0x53,    0xcb,
						0x22,    0x01,
						0x00,    0x00,
						0x00,    0x05 };

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			BitStream data = new BitStream();
			data.Write(dataBytes, 0, dataBytes.Length);
			data.SeekBits(0, SeekOrigin.Begin);

			DataCracker cracker = new DataCracker();
			var ex = Assert.Throws<CrackingFailure>(() => cracker.CrackData(dom.dataModels[0], data));
			Assert.AreEqual("Block 'GeneratedModel.a.b' failed to crack. Length is 8 bits but already read 64 bits.", ex.Message);
		}


		[Test]
		public void BugUtf16Length()
		{
			string xml = @"<?xml version='1.0' encoding='UTF-8'?>
<Peach>
	<DataModel name='bug_utf16_length'>
		<String name='signature' token='true' value='START_MARKER'/>

		<Number name='FILENAME_LENGTH' endian='little' size='16' signed='false' occurs='1'>
			<Relation of='FILENAME' type='size'/>
		</Number>

		<Number name='OBJECT_LENGTH' endian='little' size='32' signed='false' occurs='1'>
			<Relation of='OBJECT' type='size'/>
		</Number>

		<String name='FILENAME' occurs='1' type='utf16' nullTerminated='false'/>

		<Block name='OBJECT' occurs='1'>
			<String value='END_MARKER'/>
		</Block>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			byte[] FILENAME = Encoding.Unicode.GetBytes("sample_mpeg4.mp4");
			byte[] OBJECT = Encoding.ASCII.GetBytes("END_MARKER");

			var data = Bits.Fmt("{0}{1:L16}{2:L32}{3}{4}", "START_MARKER", FILENAME.Length, OBJECT.Length, FILENAME, OBJECT);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(5, dom.dataModels[0].Count);
			Assert.AreEqual("START_MARKER", (string)dom.dataModels[0][0].DefaultValue);

			var array = dom.dataModels[0][1] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(1, array.Count);
			Assert.AreEqual(FILENAME.Length, (int)array[0].DefaultValue);

			array = dom.dataModels[0][2] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(1, array.Count);
			Assert.AreEqual(OBJECT.Length, (int)array[0].DefaultValue);

			array = dom.dataModels[0][3] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(1, array.Count);
			Assert.AreEqual("sample_mpeg4.mp4", (string)array[0].DefaultValue);

			array = dom.dataModels[0][4] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(1, array.Count);
			var block = array[0] as Block;
			Assert.NotNull(block);
			Assert.AreEqual(1, block.Count);
			Assert.AreEqual("END_MARKER", (string)block[0].DefaultValue);

		}

		[Test]
		public void SlurpArray()
		{
			string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String name='str' value='A'/>
		<Block name='blk' minOccurs='1'>
			<String name='val'/>
		</Block>
	</DataModel>

	<StateModel name='TheState' initialState='State1'>
		<State name='State1'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher class='Console'/>
	</Test>
</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var model = dom.tests[0].stateModel.states["State1"].actions[0].dataModel.Value;
			Assert.NotNull(model);

			Peach.Core.Dom.Array array = dom.tests[0].stateModel.states["State1"].actions[0].dataModel[1] as Peach.Core.Dom.Array;

			PeachXPathNavigator navi = new PeachXPathNavigator(dom.tests[0].stateModel);
			var iter = navi.Select("//str");
			if (!iter.MoveNext())
				Assert.Fail();

			DataElement valueElement = ((PeachXPathNavigator)iter.Current).CurrentNode as DataElement;
			if (valueElement == null)
				Assert.Fail();

			if (iter.MoveNext())
				Assert.Fail();

			iter = navi.Select("//val");

			if (!iter.MoveNext())
				Assert.Fail();

			int count = 0;
			do
			{
				var setElement = ((PeachXPathNavigator)iter.Current).CurrentNode as DataElement;
				if (setElement == null)
					Assert.Fail();

				setElement.DefaultValue = valueElement.DefaultValue;
				++count;
			}
			while (iter.MoveNext());

			// When Array.CountOverride is used, it duplicates the last element
			// over and over, so there are really only 1 elements in the array...
			// xpath will navigate over the original element and all real array elements.
			Assert.AreEqual(2, count);
			Assert.AreEqual(1, array.Count);

			array.SetCountOverride(50, array[0].Value, 0);
			//array.CountOverride = 50;
			Assert.AreEqual(50, array.GetCountOverride());

			var val = array.InternalValue.BitsToString();
			var exp = new string('A', 50);

			Assert.AreEqual(exp, val);
		}

		[Test]
		public void NumericString()
		{
			// Ensure numeric strings of -1 don't cause exceptions
			// And are mutated as numbers
			string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String name='str' value='-1'/>
	</DataModel>

	<StateModel name='TheState' initialState='State1'>
		<State name='State1'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<Mutators mode='include'>
			<Mutator class='StringCaseMutator'/>
			<Mutator class='NumericalEdgeCaseMutator'/>
			<Mutator class='NumericalVarianceMutator'/>
		</Mutators>
		<Strategy class='Sequential'/>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();

			Engine e = new Engine(null);
			e.IterationFinished += new Engine.IterationFinishedEventHandler(e_IterationFinished);
			e.startFuzzing(dom, config);

			// Numeric mutators should be used
			Assert.AreNotEqual(3, engineCount);
		}

		void e_IterationFinished(RunContext context, uint currentIteration)
		{
			engineCount = currentIteration;
		}

		uint engineCount = 0;

		[Test]
		[Ignore("Data to crack is incorrect.")]
		public void PlacementWithOffset()
		{
			string xml = @"
<Peach>
	<DataModel name='entry_model'>
		<Number size='8' name='blob_offset'>
			<Relation type='offset' of='blob' />
		</Number>
		<Number name='blob_size' size='8'>
			<Relation type='size' of='blob' />
		</Number>

		<Block name='blob'>
			<Placement before='end_entries' />
			<Blob name='blobdata' />
		</Block>
	</DataModel>

	<DataModel name='DataEntry'>
		<Block name='dataEntry_header'>
			<Number size='8' name='data_size'>
				<Relation type='size' of='dataentry_data' />
			</Number>
			<Number size='8' name='data_offset'>
				<Relation type='offset' of='dataentry_data' />
			</Number>
		</Block>
		<Block name='Holder'>
			<Placement before='EndPlaceBlock'/>
			<Block name='dataentry_data'>
				<Number name='entry_count' size='8'>
					<Relation type='count' of='entriesf' />
				</Number>
				<Number name='entry_offset' size='8'>
					<Relation type='offset' of='entriesf' />
				</Number>
			</Block>
			<Block name='entriesf' minOccurs='0' ref='entry_model' />
		</Block>


		<Block name='end_entries' />
	</DataModel>

	<DataModel name='EntryList'>
		<Blob name='ELToken' valueType='string' value='EL' token='true' />
		<Number size='8' name='entry_count'>
			<Relation type='count' of='Entries' />
		</Number>
		
		<Choice name='Entries' minOccurs='0'>
			<Block name='ADataEntry' ref='DataEntry' />
		</Choice>

		<Block name='EndPlaceBlock'/>
		
		<Blob name='ENDToken' valueType='string' value='ND' token='true' />
	</DataModel>
</Peach>";

			var bytes = HexString.Parse("45 4c 01 03 09 41 41 41 41 01 0e 00 00 00 10 42 41 41 4e 44").Value;

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[2], new BitStream(bytes));


		}
	}
}
