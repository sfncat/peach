using System;
using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Publishers
{
	[TestFixture]
	[Quick]
	[Peach]
	class FilePublisherTests
	{
		private string _xml = @"<?xml version='1.0' encoding='utf-8'?>
				<Peach>
				   <DataModel name='TheDataModel'>
				       <String value='Hello'/>
				   </DataModel>

				   <StateModel name='TheStateModel' initialState='InitialState'>
				       <State name='InitialState'>
				           <Action name='Action1' type='output'>
				               <DataModel ref='TheDataModel'/>
				           </Action>
				       </State>
				   </StateModel>

				   <Test name='Default'>
				       <StateModel ref='TheStateModel'/>
				       <Publisher class='File'>
				           <Param name='FileName' value='{0}'/>
				       </Publisher>
				   </Test>

				</Peach>";

		[Test]
		public void Test1()
		{
			using (var tmp = new TempFile())
			{
				var xml = _xml.Fmt(tmp.Path);

				var dom = DataModelCollector.ParsePit(xml);

				var config = new RunConfiguration { singleIteration = true };

				var e = new Engine(null);
				e.startFuzzing(dom, config);

				var output = File.ReadAllLines(tmp.Path);

				Assert.AreEqual(1, output.Length);
				Assert.AreEqual("Hello", output[0]);
			}
		}

		[Test]
		public void TestCreateDirectory()
		{
			using (var tmpDir = new TempDirectory())
			{
				var tmpFile = Path.Combine(tmpDir.Path, "Some", "Dir", "File.xml");

				var xml = _xml.Fmt(tmpFile);

				var dom = DataModelCollector.ParsePit(xml);

				var config = new RunConfiguration { singleIteration = true };

				var e = new Engine(null);
				e.startFuzzing(dom, config);

				var output = File.ReadAllLines(tmpFile);

				Assert.AreEqual(1, output.Length);
				Assert.AreEqual("Hello", output[0]);
			}
		}

		[Test]
		public void TestCreateDirectoryNoDirectory()
		{
			var dir = Environment.CurrentDirectory;

			try
			{
				using (var tmpDir = new TempDirectory())
				{
					Environment.CurrentDirectory = tmpDir.Path;

					var xml = _xml.Fmt("foo.bin");

					var dom = DataModelCollector.ParsePit(xml);

					var config = new RunConfiguration { singleIteration = true };

					var e = new Engine(null);
					e.startFuzzing(dom, config);

					Assert.DoesNotThrow(() => e.startFuzzing(dom, config));
				}
			}
			finally
			{
				Environment.CurrentDirectory = dir;
			}
		}
	}
}
