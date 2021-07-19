using NUnit.Framework;
using Peach.Core;
using Peach.Core.Cracker;
using Peach.Core.IO;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Dom
{
	[TestFixture]
	[Quick]
	[Category("Peach")]
	internal class XmlFragmenttTests : DataModelCollector
	{
		[Test]
		public void TestCrack()
		{
			const string data = @"<?xml version='1.0' encoding='utf-8' ?>
<catalog>
	<book id='bk101'>
		<author>Gambardella, Matthew</author>
		<title>XML Developer's Guide</title>
		<genre>Computer</genre>
		<price>44.95</price>
		<publish_date>2000-10-01</publish_date>
		<description>An in-depth look at creating applications with XML.</description>
	</book>
</catalog>
<catalog>
	<book id='bk102'>
		<author>Gambardella, Matthew</author>
		<title>XML Developer's Guide</title>
		<genre>Computer</genre>
		<price>44.95</price>
		<publish_date>2000-10-01</publish_date>
		<description>An in-depth look at creating applications with XML.</description>
	</book>
</catalog>
Foo
";

			const string pit = @"
<Peach>
	<DataModel name='DM'>
		<XmlFragment name='doc1' />
		<XmlFragment name='doc2' />
		<String name='trailer' />
	</DataModel>
</Peach>";

			var dom = ParsePit(pit);
			var cracker = new DataCracker();
			var bs = new BitStream(Encoding.UTF8.GetBytes(data));

			cracker.CrackData(dom.dataModels[0], bs);

			var part1 = (string) dom.dataModels[0][0].DefaultValue;
			StringAssert.StartsWith("<?xml version='1.0' encoding='utf-8' ?>", part1);
			StringAssert.Contains("<book id='bk101'>", part1);

			var part2 = (string)dom.dataModels[0][1].DefaultValue;
			StringAssert.DoesNotStartWith("<?xml version='1.0' encoding='utf-8' ?>", part2);
			StringAssert.Contains("<book id='bk102'>", part2);

			var part3 = (string)dom.dataModels[0][2].DefaultValue;
			StringAssert.Contains("Foo", part3);
		}

		[Test]
		public void TestCrackFail()
		{
			const string data = @"<This Is Not Xml";

			const string pit = @"
<Peach>
	<DataModel name='DM'>
		<XmlFragment name='doc1' />
	</DataModel>
</Peach>";

			var dom = ParsePit(pit);
			var cracker = new DataCracker();
			var bs = new BitStream(Encoding.UTF8.GetBytes(data));

			Assert.Throws<CrackingFailure>(() => cracker.CrackData(dom.dataModels[0], bs));
		}
	}
}