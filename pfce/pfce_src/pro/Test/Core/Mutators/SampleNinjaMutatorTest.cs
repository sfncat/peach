using System.IO;
using NUnit.Framework;
using Peach.Core.Test;
using Peach.Core;
using Peach.Pro.Core.Storage;
using Peach.Pro.Test.Core.Storage;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	[Peach]
	[Quick]
	class SampleNinjaMutatorTest : DataModelCollector
	{
		// 1. Create Ninja DB and verify
		// 2. Run some mutations and verify no exceptions

		readonly byte[] ninjaSample1 = {
			0x58, 0x02, 0x50, 0x65, 0x61, 0x63, 0x68, 0x79, 0x42, 0x6C, 0x6F, 0x62, 0x62, 0x79, 0x00, 0x58,
			0x02, 0x50, 0x65, 0x61, 0x63, 0x68, 0x79, 0x42, 0x6C, 0x6F, 0x62, 0x62, 0x79, 0x00, 0x58, 0x02,
			0x50, 0x65, 0x61, 0x63, 0x68, 0x79, 0x42, 0x6C, 0x6F, 0x62, 0x62, 0x79, 0x00, 0xA0, 0x06, 0x50,
			0x65, 0x61, 0x63, 0x68, 0x79, 0x58, 0x02, 0x7B, 0x22, 0x4E, 0x75, 0x6D, 0x31, 0x36, 0x22, 0x3A,
			0x36, 0x30, 0x30, 0x2C, 0x22, 0x53, 0x74, 0x72, 0x22, 0x3A, 0x22, 0x50, 0x65, 0x61, 0x63, 0x68,
			0x79, 0x22, 0x2C, 0x22, 0x42, 0x6C, 0x6F, 0x62, 0x22, 0x3A, 0x22, 0x51, 0x6D, 0x78, 0x76, 0x59,
			0x6D, 0x4A, 0x35, 0x22, 0x2C, 0x22, 0x6E, 0x75, 0x6C, 0x6C, 0x22, 0x3A, 0x6E, 0x75, 0x6C, 0x6C,
			0x2C, 0x22, 0x62, 0x6F, 0x6F, 0x6C, 0x22, 0x3A, 0x74, 0x72, 0x75, 0x65, 0x7D, 0x0A
		};

		readonly byte[] ninjaSample2 = {
			0x57, 0x02, 0x50, 0x65, 0x61, 0x63, 0x68, 0x79, 0x42, 0x6C, 0x6F, 0x62, 0x62, 0x79, 0x00, 0x58,
			0x02, 0x50, 0x65, 0x61, 0x63, 0x68, 0x79, 0x42, 0x6C, 0x6F, 0x62, 0x62, 0x79, 0x00, 0x58, 0x02,
			0x50, 0x65, 0x61, 0x63, 0x68, 0x79, 0x42, 0x6C, 0x6F, 0x62, 0x62, 0x79, 0x00, 0xA0, 0x06, 0x50,
			0x65, 0x61, 0x63, 0x68, 0x79, 0x58, 0x02, 0x7B, 0x22, 0x4E, 0x75, 0x6D, 0x31, 0x36, 0x22, 0x3A,
			0x36, 0x30, 0x30, 0x2C, 0x22, 0x53, 0x74, 0x72, 0x22, 0x3A, 0x22, 0x50, 0x65, 0x61, 0x63, 0x68,
			0x79, 0x22, 0x2C, 0x22, 0x42, 0x6C, 0x6F, 0x62, 0x22, 0x3A, 0x22, 0x51, 0x6D, 0x78, 0x76, 0x59,
			0x6D, 0x4A, 0x35, 0x22, 0x2C, 0x22, 0x6E, 0x75, 0x6C, 0x6C, 0x22, 0x3A, 0x6E, 0x75, 0x6C, 0x6C,
			0x2C, 0x22, 0x62, 0x6F, 0x6F, 0x6C, 0x22, 0x3A, 0x74, 0x72, 0x75, 0x65, 0x7D, 0x0A
		};

		readonly byte[] ninjaSample3 = {
			0x56, 0x02, 0x50, 0x65, 0x61, 0x63, 0x68, 0x79, 0x42, 0x6C, 0x6F, 0x62, 0x62, 0x79, 0x00, 0x58,
			0x02, 0x50, 0x65, 0x61, 0x63, 0x68, 0x79, 0x42, 0x6C, 0x6F, 0x62, 0x62, 0x79, 0x00, 0x58, 0x02,
			0x50, 0x65, 0x61, 0x63, 0x68, 0x79, 0x42, 0x6C, 0x6F, 0x62, 0x62, 0x79, 0x00, 0xA0, 0x06, 0x50,
			0x65, 0x61, 0x63, 0x68, 0x79, 0x58, 0x02, 0x7B, 0x22, 0x4E, 0x75, 0x6D, 0x31, 0x36, 0x22, 0x3A,
			0x36, 0x30, 0x30, 0x2C, 0x22, 0x53, 0x74, 0x72, 0x22, 0x3A, 0x22, 0x50, 0x65, 0x61, 0x63, 0x68,
			0x79, 0x22, 0x2C, 0x22, 0x42, 0x6C, 0x6F, 0x62, 0x22, 0x3A, 0x22, 0x51, 0x6D, 0x78, 0x76, 0x59,
			0x6D, 0x4A, 0x35, 0x22, 0x2C, 0x22, 0x6E, 0x75, 0x6C, 0x6C, 0x22, 0x3A, 0x6E, 0x75, 0x6C, 0x6C,
			0x2C, 0x22, 0x62, 0x6F, 0x6F, 0x6C, 0x22, 0x3A, 0x74, 0x72, 0x75, 0x65, 0x7D, 0x0A
		};

		string ninjaSampleXml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='Ninja'>
		<Block name='Block' maxOccurs='-1'>
			<Number name='Num16' size='16' value='600' />
			<String name='Str' value='Peachy' token='true' />
			<Blob name='Blob' value='Blobby' token='true' />
			
			<Flags name='Flags' size='8'>
				<Flag name='F1' size='1' position='1' />
				<Flag name='F2' size='1' position='2' />
			</Flags>
		</Block>
		
		<Asn1Type class='2' pc='1' tag='0' name='terminationID'>
			<String name='Value' value='Peachy'/>
		</Asn1Type>
		
		<Choice name='Choice'>
			<Number name='Num6' size='16' value='600' token='true' />
			<Number name='Num7' size='16' value='700' token='true' />
		</Choice>

		<String name='JsonTest'>
			<Analyzer class='Json'/>
		</String>
		
		<String value='\n' token='true' />
		
		<Padding />
	</DataModel>

	<StateModel name='TheStateModel' initialState='initial'>
		<State name='initial'>
		  <Action type='output'>
			<DataModel ref='Ninja'/>
		  </Action>
		</State>
	</StateModel>

	<Test name='Default' maxOutputSize='1024'>
		<StateModel ref='TheStateModel'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='SampleNinja' />
		</Mutators>
	</Test>
</Peach>
";

		// Use this model to generate bin file.
		//		string ninjaSampleXmlGenerateBin = @"<?xml version='1.0' encoding='utf-8'?>
		//<Peach>
		//	<!-- Used to generate bin file -->
		//	<DataModel name='NinjaOut'>
		//	
		//		<Block name='Block' maxOccurs='-1'>
		//			<Number name='Num16' size='16' value='600' />
		//			<String name='Str' value='Peachy' token='true' />
		//			<Blob name='Blob' value='Blobby' token='true' />
		//			
		//			<Flags name='Flags' size='8'>
		//				<Flag name='F1' size='1' position='1' />
		//				<Flag name='F2' size='1' position='2' />
		//			</Flags>
		//		</Block>
		//		
		//		<Asn1Type class='2' pc='1' tag='0' name='terminationID'>
		//			<String name='Value' value='Peachy'/>
		//		</Asn1Type>
		//		
		//		<Choice name='Choice'>
		//			<Number name='Num6' size='16' value='600' token='true' />
		//			<Number name='Num7' size='16' value='700' token='true' />
		//		</Choice>
		//
		//		<Json name='JsonTest'>
		//			<Number name='Num16' size='16' value='600' />
		//			<String name='Str' value='Peachy' />
		//			<Blob name='Blob' value='Blobby' />
		//			<Null name='null'/>
		//			<Bool name='bool' value='1'/>
		//		</Json>
		//		
		//		<String value='\n' token='true' />
		//		
		//		<Padding />
		//
		//	</DataModel>
		//
		//	<StateModel name='TheStateModel' initialState='initial'>
		//		<State name='initial'>
		//		  <Action type='output'>
		//			<DataModel ref='Ninja'/>
		//   			<Data>
		//				<Field name='Block[0]' value='' />
		//				<Field name='Block[1]' value='' />
		//				<Field name='Block[2]' value='' />
		//			</Data>
		//		  </Action>
		//		</State>
		//	</StateModel>
		//
		//	<Test name='Default' maxOutputSize='200'>
		//		<StateModel ref='TheStateModel'/>
		//		<Publisher class='File'>
		//			<Param name='FileName' value='ninja.bin' />
		//		</Publisher>
		//	</Test>
		//</Peach>
		//";
		string _pitFile;
		string _dbPath;
		List<Sample> _samples;

		TempDirectory _tmpDir;

		[SetUp]
		public void ThisSetUp()
		{
			_tmpDir = new TempDirectory();

			_pitFile = Path.Combine(_tmpDir.Path, "ninja.xml");
			File.WriteAllText(_pitFile, ninjaSampleXml);

			var samplesPath = Path.Combine(_tmpDir.Path, "samples");
			Directory.CreateDirectory(samplesPath);

			_samples = new List<Sample>
			{
				MakeSample(1, samplesPath, ninjaSample1),
				MakeSample(2, samplesPath, ninjaSample2),
				MakeSample(3, samplesPath, ninjaSample3),
			};

			_dbPath = SampleNinjaDatabase.Create(_tmpDir.Path, _pitFile, "Ninja", samplesPath);
		}

		Sample MakeSample(ulong id, string dir, byte[] buf)
		{
			var name = "ninja{0}.bin".Fmt(id + 1);
			var path = Path.Combine(dir, name);
			File.WriteAllBytes(path, buf);
			var hash = SHA1.Create().ComputeHash(buf);
			return new Sample(id, path, hash);
		}

		[TearDown]
		public void TearDown()
		{
			_tmpDir.Dispose();
		}

		[Test]
		public void VerifyNinjaDatabase()
		{
			using (var db = new SampleNinjaDatabase(_dbPath))
			{
				DatabaseTests.AssertResult(db.LoadTable<Sample>(), _samples);

				DatabaseTests.AssertResult(db.LoadTable<Element>(), new[] {
					new Element { ElementId = 1, Name = "Ninja" },
					new Element { ElementId = 2, Name = "Block" },
					new Element { ElementId = 3, Name = "Num16" },
					new Element { ElementId = 4, Name = "Str" },
					new Element { ElementId = 5, Name = "Blob" },
					new Element { ElementId = 6, Name = "Flags" },
					new Element { ElementId = 7, Name = "F1" },
					new Element { ElementId = 8, Name = "F2" },
					new Element { ElementId = 9, Name = "terminationID" },
					new Element { ElementId = 10, Name = "class" },
					new Element { ElementId = 11, Name = "pc" },
					new Element { ElementId = 12, Name = "tag" },
					new Element { ElementId = 13, Name = "length" },
					new Element { ElementId = 14, Name = "Value" },
					new Element { ElementId = 15, Name = "Choice" },
					new Element { ElementId = 16, Name = "Num6" },
					new Element { ElementId = 17, Name = "JsonTest" },
					new Element { ElementId = 18, Name = "null" },
					new Element { ElementId = 19, Name = "bool" },
					new Element { ElementId = 20, Name = "DataElement_0" },
					new Element { ElementId = 21, Name = "DataElement_1" },
				});

				Assert.AreEqual(117, db.LoadTable<SampleElement>().Count());
			}
		}

		[Test]
		public void TestMutations()
		{
			RunEngine(ninjaSampleXml, _pitFile);
			Assert.AreEqual(123, mutatedDataModels.Count);
		}
	}
}
