using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.PitParserTests
{
	[TestFixture]
	[Quick]
	[Peach]
	class StreamTests
	{
		/*
		 * Stream not child of <DataModel>
		 * Stream ref
		 * Stream ref override
		 * Stream ref deep override
		 */

		[Test]
		public void TestParse()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>

		<Stream streamName='foo'>
			<String value='Hello'/>
		</Stream>

		<Stream streamName='bar'>
			<String value='World'/>
			<String value='!'/>
		</Stream>

	</DataModel>
</Peach>
";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.NotNull(dom);
			Assert.AreEqual(1, dom.dataModels.Count);
			Assert.NotNull(dom.dataModels[0]);
			Assert.AreEqual(2, dom.dataModels[0].Count);
			Assert.NotNull(dom.dataModels[0][0]);
		}
	}
}
