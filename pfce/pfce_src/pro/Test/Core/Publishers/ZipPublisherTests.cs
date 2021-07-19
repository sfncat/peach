using System.IO;
using System.Linq;
using Ionic.Zip;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Publishers
{
	[TestFixture]
	[Quick]
	[Peach]
	class ZipPublisherTests
	{
		[Test]
		public void TestSingleOutput()
		{
			var tmpName = Path.GetTempFileName();

			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Stream streamName='foo'>
			<String value='Hello'/>
		</Stream>
		<Stream streamName='bar'>
			<String value='World'/>
		</Stream>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Zip'>
			<Param name='FileName' value='{0}'/>
		</Publisher>
	</Test>

</Peach>
".Fmt(tmpName);

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration();
			config.singleIteration = true;

			var e = new Engine(null);
			e.startFuzzing(dom, config);

			VerifyZip(tmpName);
		}

		[Test]
		public void TestBlockSingleOutput()
		{
			var tmpName = Path.GetTempFileName();

			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Block>
			<Stream streamName='foo'>
				<String value='Hello'/>
			</Stream>
			<Stream streamName='bar'>
				<String value='World'/>
			</Stream>
		</Block>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Zip'>
			<Param name='FileName' value='{0}'/>
		</Publisher>
	</Test>

</Peach>
".Fmt(tmpName);

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration();
			config.singleIteration = true;

			var e = new Engine(null);
			e.startFuzzing(dom, config);

			VerifyZip(tmpName);
		}

		[Test]
		public void TestMultipleOutput()
		{
			var tmpName = Path.GetTempFileName();

			string xml = @"
<Peach>
	<DataModel name='DM1'>
		<Stream streamName='foo'>
			<String value='Hello'/>
		</Stream>
	</DataModel>

	<DataModel name='DM2'>
		<Stream streamName='bar'>
			<String value='World'/>
		</Stream>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM1'/>
			</Action>
			<Action type='output'>
				<DataModel ref='DM2'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Zip'>
			<Param name='FileName' value='{0}'/>
		</Publisher>
	</Test>

</Peach>
".Fmt(tmpName);

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration();
			config.singleIteration = true;

			var e = new Engine(null);
			e.startFuzzing(dom, config);

			VerifyZip(tmpName);
		}

		private static void VerifyZip(string tmpName)
		{
			Assert.True(File.Exists(tmpName));

			using (var f = ZipFile.Read(tmpName))
			{
				var entries = f.ToArray();
				Assert.AreEqual(2, entries.Length);

				Assert.AreEqual("foo", entries[0].FileName);
				using (var s = entries[0].OpenReader())
				{
					var rdr = new StreamReader(s);
					var val = rdr.ReadToEnd();
					Assert.AreEqual("Hello", val);
				}

				Assert.AreEqual("bar", entries[1].FileName);
				using (var s = entries[1].OpenReader())
				{
					var rdr = new StreamReader(s);
					var val = rdr.ReadToEnd();
					Assert.AreEqual("World", val);
				}
			}
		}
	}
}
