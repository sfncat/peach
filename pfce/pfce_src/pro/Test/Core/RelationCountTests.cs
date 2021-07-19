

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
	class RelationCountTests
	{
		[Test]
		public void BasicTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number name=\"TheNumber\" size=\"8\">" +
				"			<Relation type=\"count\" of=\"Array\" />" +
				"		</Number>" +
				"		<String name=\"Array\" value=\"1\" maxOccurs=\"100\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.NotNull(dom.dataModels[0].Value);

			Number num = dom.dataModels[0][0] as Number;

			Assert.AreEqual(1, (int)num.InternalValue);

			Peach.Core.Dom.Array array = dom.dataModels[0][1] as Peach.Core.Dom.Array;
			array.OriginalElement = array[0];

			array.Add(new Peach.Core.Dom.String("Child2") { DefaultValue = new Variant("2") });
			array.Add(new Peach.Core.Dom.String("Child3") { DefaultValue = new Variant("3") });

			Assert.AreEqual(3, (int)num.InternalValue);
			Assert.AreEqual("123", ASCIIEncoding.ASCII.GetString(array.Value.ToArray()));
		}

		[Test]
		public void ExpressionSetTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number name=\"TheNumber\" size=\"8\">" +
				"			<Relation type=\"count\" of=\"Array\" expressionSet=\"count + 1\" />" +
				"		</Number>" +
				"		<String name=\"Array\" value=\"1\" maxOccurs=\"100\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var val = dom.dataModels[0].Value;
			Assert.NotNull(val);

			Assert.AreEqual(new byte[] { 2, (byte)'1' }, val.ToArray());

			Number num = dom.dataModels[0][0] as Number;

			Peach.Core.Dom.Array array = dom.dataModels[0][1] as Peach.Core.Dom.Array;
			array.OriginalElement = array[0];

			array.Add(new Peach.Core.Dom.String("Child2") { DefaultValue = new Variant("2") });
			array.Add(new Peach.Core.Dom.String("Child3") { DefaultValue = new Variant("3") });

			Assert.AreEqual(4, (int)num.InternalValue);
			Assert.AreEqual("123", ASCIIEncoding.ASCII.GetString(array.Value.ToArray()));
		}

		[Test]
		public void ExpressionGetSetUlong()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM1'>
		<Number size='64' signed='false' endian='big'>
			<Relation type='count' of='Items' expressionGet='count &amp; 0x00ffffffffffffff' expressionSet='count | 0xff00000000000000' />
		</Number>
		<Number name='Items' size='8' minOccurs='3' />
		<Blob />
	</DataModel>

	<DataModel name='DM2' ref='DM1' />
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);

			var val = dom.dataModels[0].Value.ToArray();

			Assert.AreEqual(new byte[] { 0xff, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0}, val);

			var data = new BitStream(new byte[] { 0xff, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0, 0 });

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[1], data);

			var array = (Array)dom.dataModels[1][1];

			Assert.AreEqual(4, array.Count);
		}
	}
}

// end
