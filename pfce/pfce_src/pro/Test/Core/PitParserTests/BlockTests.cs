

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
	class BlockTests
	{
		[Test]
		public void TestOverWrite()
		{
			string xml = @"
<Peach>
	<DataModel name='Base'>
		<Block name='b'>
			<String name='s1' value='Hello'/>
			<String name='s2' value='World'/>
		</Block>
	</DataModel>

	<DataModel name='Derived' ref='Base'>
		<String name='b.s2' value='Hello'/>
	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(2, dom.dataModels.Count);
			Assert.AreEqual("HelloWorld", Encoding.ASCII.GetString(dom.dataModels[0].Value.ToArray()));
			Assert.AreEqual("HelloHello", Encoding.ASCII.GetString(dom.dataModels[1].Value.ToArray()));
		}

		[Test]
		public void TestAddNewChild()
		{
			string xml = @"
<Peach>
	<DataModel name='Base'>
		<Block name='b'>
			<String name='s1' value='Hello'/>
			<String name='s2' value='World'/>
		</Block>
		<String value='.'/>
	</DataModel>

	<DataModel name='Derived' ref='Base'>
		<String name='b.s3' value='Hello'/>
	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(2, dom.dataModels.Count);
			Assert.AreEqual("HelloWorld.", Encoding.ASCII.GetString(dom.dataModels[0].Value.ToArray()));
			Assert.AreEqual("HelloWorldHello.", Encoding.ASCII.GetString(dom.dataModels[1].Value.ToArray()));
		}
	}
}
