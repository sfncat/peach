

using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.CrackingTests
{
	[TestFixture]
	[Quick]
	[Peach]
	public class BlobTests
	{
		[Test]
		public void CrackBlob1()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 1, 2, 3, 4, 5, 6, 0xff, 0xfe, 0xff });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6, 0xff, 0xfe, 0xff }, dom.dataModels[0][0].DefaultValue.BitsToArray());
		}

		[Test]
		public void CrackBlob2()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob length=\"5\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 1, 2, 3, 4, 5, 6, 0xff, 0xfe, 0xff });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, dom.dataModels[0][0].DefaultValue.BitsToArray());
		}

		[Test]
		public void CrackBlob3()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob />" +
				"		<Blob length=\"5\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 1, 2, 3, 4, 5, 6, 0xff, 0xfe, 0xff });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(new byte[] { 1, 2, 3, 4 }, dom.dataModels[0][0].DefaultValue.BitsToArray());
			Assert.AreEqual(new byte[] { 5, 6, 0xff, 0xfe, 0xff }, dom.dataModels[0][1].DefaultValue.BitsToArray());
		}

		[Test]
		public void CrackBlob4()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob />" +
				"		<Block>"+
				"			<Blob length=\"5\" />" +
				"		</Block>"+
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 1, 2, 3, 4, 5, 6, 0xff, 0xfe, 0xff });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(new byte[] { 1, 2, 3, 4 }, dom.dataModels[0][0].DefaultValue.BitsToArray());
			Assert.AreEqual(new byte[] { 5, 6, 0xff, 0xfe, 0xff }, ((DataElementContainer)dom.dataModels[0][1])[0].DefaultValue.BitsToArray());
		}

		[Test]
		public void BlobFields()
		{
			const string val = "\xff";
			var xml = @"
<Peach>
	<DataModel name='DM'>
		<Blob name='b1' />
		<Blob name='b2' />
		<Blob name='b3' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
				<Data>
					<Field name='b1' value='Hello' />
					<Field name='b2' value='57 6f 72 6c 64' valueType='hex' />
					<Field name='b3' value='{0}' />
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null'/>
	</Test>
</Peach>".Fmt(val);

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);

			e.startFuzzing(dom, cfg);

			var final = dom.tests[0].stateModel.states[0].actions[0].dataModel.Value.ToArray();
			var asStr = Encoding.ISOLatin1.GetString(final);

			Assert.AreEqual("HelloWorld\xff", asStr);
		}

		[Test]
		public void BlobBadFields()
		{
			const string val = "\x015a\x015a\x015a";
			var xml = @"
<Peach>
	<DataModel name='DM'>
		<Blob name='b1' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
				<Data>
					<Field name='b1' value='{0}' />
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null'/>
	</Test>
</Peach>".Fmt(val);

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);

			var ex = Assert.Throws<PeachException>(() => e.startFuzzing(dom, cfg));

			Assert.AreEqual("Error, Blob 'DM.b1' string value 'ŚŚŚ' contains unicode characters and could not be converted to a BitStream.", ex.Message);
		}
	}
}

// end
