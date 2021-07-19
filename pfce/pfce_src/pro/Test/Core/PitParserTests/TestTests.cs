

using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.PitParserTests
{
	[TestFixture]
	[Quick]
	[Peach]
	class TestTests
	{
		[Test]
		public void Default()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob />" +
				"		<Blob name=\"Blob2\" />" +
				"		<Blob />" +
				"	</DataModel>" +
				"	<StateModel name=\"TheStateModel\" initialState=\"TheState\">" +
				"		<State name=\"TheState\">" +
				"			<Action type=\"output\">" +
				"				<DataModel ref=\"TheDataModel\"/>" +
				"			</Action>" +
				"		</State>" +
				"	</StateModel>" +
				"	<Test name=\"Default\">" +
				"		<StateModel ref=\"TheStateModel\" />" +
				"		<Publisher class=\"Null\" />" +
				"		<Exclude/>" +
				"	</Test> " +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration() { singleIteration = true };
			var engine = new Engine(null);
			engine.startFuzzing(dom, config);

			Assert.AreEqual(true, dom.dataModels[0][0].isMutable);
			Assert.AreEqual(true, dom.dataModels[0][1].isMutable);
			Assert.AreEqual(true, dom.dataModels[0][2].isMutable);
		}

		[Test]
		public void ExcludeAll()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob />" +
				"		<Blob name=\"Blob2\" />" +
				"		<Blob />" +
				"	</DataModel>" +
				"	<StateModel name=\"TheStateModel\" initialState=\"TheState\">" +
				"		<State name=\"TheState\">" +
				"			<Action type=\"output\">" +
				"				<DataModel ref=\"TheDataModel\"/>" +
				"			</Action>" +
				"		</State>" +
				"	</StateModel>" +
				"	<Test name=\"Default\">" +
				"		<StateModel ref=\"TheStateModel\" />" +
				"		<Publisher class=\"Null\" />" +
				"		<Exclude/>" +
				"	</Test> " +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration() { singleIteration = true };
			var engine = new Engine(null);
			engine.startFuzzing(dom, config);

			Assert.AreEqual(false, dom.tests[0].stateModel.states[0].actions[0].dataModel[0].isMutable);
			Assert.AreEqual(false, dom.tests[0].stateModel.states[0].actions[0].dataModel[1].isMutable);
			Assert.AreEqual(false, dom.tests[0].stateModel.states[0].actions[0].dataModel[2].isMutable);
		}

		[Test]
		public void ExcludeThenIncludeAll()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob />" +
				"		<Blob name=\"Blob2\" />" +
				"		<Blob />" +
				"	</DataModel>" +
				"	<StateModel name=\"TheStateModel\" initialState=\"TheState\">" +
				"		<State name=\"TheState\">" +
				"			<Action type=\"output\">" +
				"				<DataModel ref=\"TheDataModel\"/>" +
				"			</Action>" +
				"		</State>" +
				"	</StateModel>" +
				"	<Test name=\"Default\">" +
				"		<StateModel ref=\"TheStateModel\" />" +
				"		<Publisher class=\"Null\" />" +
				"		<Exclude/>" +
				"		<Include xpath=\"//Blob2\"/>" +
				"	</Test> " +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration() { singleIteration = true };
			var engine = new Engine(null);
			engine.startFuzzing(dom, config);

			Assert.AreEqual(false, dom.tests[0].stateModel.states[0].actions[0].dataModel[0].isMutable);
			Assert.AreEqual(true,  dom.tests[0].stateModel.states[0].actions[0].dataModel[1].isMutable);
			Assert.AreEqual(false, dom.tests[0].stateModel.states[0].actions[0].dataModel[2].isMutable);
		}

		[Test]
		public void ExcludeThenIncludeBlock()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob />" +
				"		<Block name=\"Block2\">" +
				"			<Block>" +
				"				<Blob/>" +
				"			</Block>" +
				"		</Block>" +
				"		<Blob />" +
				"	</DataModel>" +
				"	<StateModel name=\"TheStateModel\" initialState=\"TheState\">" +
				"		<State name=\"TheState\">" +
				"			<Action type=\"output\">" +
				"				<DataModel ref=\"TheDataModel\"/>" +
				"			</Action>" +
				"		</State>" +
				"	</StateModel>" +
				"	<Test name=\"Default\">" +
				"		<StateModel ref=\"TheStateModel\" />" +
				"		<Publisher class=\"Null\" />" +
				"		<Exclude/>" +
				"		<Include xpath=\"//Block2\"/>" +
				"	</Test> " +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration() { singleIteration = true };
			var engine = new Engine(null);
			engine.startFuzzing(dom, config);

			Assert.AreEqual(false, dom.tests[0].stateModel.states[0].actions[0].dataModel[0].isMutable);
			Assert.AreEqual(true, dom.tests[0].stateModel.states[0].actions[0].dataModel[1].isMutable);
			Assert.AreEqual(false, dom.tests[0].stateModel.states[0].actions[0].dataModel[2].isMutable);

			var cont = dom.tests[0].stateModel.states[0].actions[0].dataModel[1] as DataElementContainer;
			Assert.NotNull(cont);
			Assert.AreEqual(1, cont.Count);
			cont = cont[0] as DataElementContainer;
			Assert.NotNull(cont);
			Assert.AreEqual(1, cont.Count);
			Assert.AreEqual(true, cont.isMutable);
			Assert.AreEqual(true, cont[0].isMutable);
		}

		[Test]
		public void ExcludeSpecific()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob />" +
				"		<Blob name=\"Blob2\" />" +
				"		<Blob />" +
				"	</DataModel>" +
				"	<StateModel name=\"TheStateModel\" initialState=\"TheState\">" +
				"		<State name=\"TheState\">" +
				"			<Action type=\"output\">" +
				"				<DataModel ref=\"TheDataModel\"/>" +
				"			</Action>" +
				"		</State>" +
				"	</StateModel>" +
				"	<Test name=\"Default\">" +
				"		<StateModel ref=\"TheStateModel\" />" +
				"		<Publisher class=\"Null\" />" +
				"		<Exclude xpath=\"//Blob2\"/>" +
				"	</Test> " +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration() { singleIteration = true };
			var engine = new Engine(null);
			engine.startFuzzing(dom, config);

			Assert.AreEqual(true, dom.tests[0].stateModel.states[0].actions[0].dataModel[0].isMutable);
			Assert.AreEqual(false, dom.tests[0].stateModel.states[0].actions[0].dataModel[1].isMutable);
			Assert.AreEqual(true, dom.tests[0].stateModel.states[0].actions[0].dataModel[2].isMutable);
		}

		[Test]
		public void ExcludeBlock()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob />" +
				"		<Block name=\"Block2\">" +
				"			<Block>" +
				"				<Blob/>" +
				"			</Block>" +
				"		</Block>" +
				"		<Blob />" +
				"	</DataModel>" +
				"	<StateModel name=\"TheStateModel\" initialState=\"TheState\">" +
				"		<State name=\"TheState\">" +
				"			<Action type=\"output\">" +
				"				<DataModel ref=\"TheDataModel\"/>" +
				"			</Action>" +
				"		</State>" +
				"	</StateModel>" +
				"	<Test name=\"Default\">" +
				"		<StateModel ref=\"TheStateModel\" />" +
				"		<Publisher class=\"Null\" />" +
				"		<Exclude xpath=\"//Block2\"/>" +
				"	</Test> " +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration() { singleIteration = true };
			var engine = new Engine(null);
			engine.startFuzzing(dom, config);

			Assert.AreEqual(true, dom.tests[0].stateModel.states[0].actions[0].dataModel[0].isMutable);
			Assert.AreEqual(false, dom.tests[0].stateModel.states[0].actions[0].dataModel[1].isMutable);
			Assert.AreEqual(true, dom.tests[0].stateModel.states[0].actions[0].dataModel[2].isMutable);

			var cont = dom.tests[0].stateModel.states[0].actions[0].dataModel[1] as DataElementContainer;
			Assert.NotNull(cont);
			Assert.AreEqual(1, cont.Count);
			cont = cont[0] as DataElementContainer;
			Assert.NotNull(cont);
			Assert.AreEqual(1, cont.Count);
			Assert.AreEqual(false, cont.isMutable);
			Assert.AreEqual(false, cont[0].isMutable);
		}

		[Test]
		public void IncludeExcludeScope()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str'/>
		<Number name='num' size='32'/>
		<Blob name='blob'/>
	</DataModel>

	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action> 
		</State>
	</StateModel>

	<Test name='Test0'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
		<Exclude xpath='//str'/>
	</Test>

	<Test name='Test1'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
		<Exclude xpath='//blob'/>
	</Test>
</Peach>
";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration();
			config.singleIteration = true;
			config.runName = "Test1";

			var engine = new Engine(null);
			engine.startFuzzing(dom, config);

			var dm = dom.tests[1].stateModel.states["initial"].actions[0].dataModel;
			Assert.AreEqual(3, dm.Count);
			Assert.True(dm[0].isMutable);
			Assert.True(dm[1].isMutable);
			Assert.False(dm[2].isMutable);
		}

		[Test]
		public void ExcludeNonSelected()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Choice name='q'>
			<String name='str'/>
			<Number name='num' size='32'/>
			<Blob name='blob'/>
		</Choice>
	</DataModel>

	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data>
					<Field name='q.blob' value='00 00 00 00' valueType='hex' />
				</Data>
			</Action> 
		</State>
	</StateModel>

	<Test name='Test0'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
		<Exclude xpath='//str'/>
	</Test>

	<Test name='Test1'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
		<Exclude xpath='//blob'/>
	</Test>
</Peach>
";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration();
			config.singleIteration = true;
			config.runName = "Test1";

			var engine = new Engine(null);
			engine.startFuzzing(dom, config);

			var dm = dom.tests[1].stateModel.states["initial"].actions[0].dataModel;
			Assert.AreEqual(1, dm.Count);
			var c = dm[0] as Choice;
			Assert.NotNull(c);
			Assert.False(c.SelectedElement.isMutable);
			Assert.AreEqual(3, c.choiceElements.Count);
			Assert.True(c.choiceElements[0].isMutable);
			Assert.True(c.choiceElements[1].isMutable);
			Assert.False(c.choiceElements[2].isMutable);
		}

		[Test]
		public void WaitTime()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob />" +
				"		<Blob name=\"Blob2\" />" +
				"		<Blob />" +
				"	</DataModel>" +
				"	<StateModel name=\"TheStateModel\" initialState=\"TheState\">" +
				"		<State name=\"TheState\">" +
				"			<Action type=\"output\">" +
				"				<DataModel ref=\"TheDataModel\"/>" +
				"			</Action>" +
				"		</State>" +
				"	</StateModel>" +
				"	<Test name=\"Default\" waitTime=\"10.5\" faultWaitTime=\"99.9\">" +
				"		<StateModel ref=\"TheStateModel\" />" +
				"		<Publisher class=\"File\">" +
				"			<Param name=\"FileName\" value=\"test.fuzzed.txt\" /> " +
				"		</Publisher>" +
				"	</Test> " +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(10.5, dom.tests[0].waitTime);
			Assert.AreEqual(99.9, dom.tests[0].faultWaitTime);
		}

		[Test]
		public void NonDeterministic()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str'/>
	</DataModel>

	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default' nonDeterministicActions='true'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.True(dom.tests[0].nonDeterministicActions);
		}

		[Test]
		public void DefaultMaxOuptputSize()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str'/>
	</DataModel>

	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default' nonDeterministicActions='true'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			var exp = 1024 * 1024 * 1024;

			Assert.AreEqual(exp, dom.tests[0].maxOutputSize);

			var config = new RunConfiguration() { singleIteration = true };
			var e = new Engine(null);
			var count = 0;
			e.TestStarting += (c) =>
			{
				c.ActionStarting += (ctx, act) =>
				{
					foreach (var i in act.allData)
					{
						++count;
						Assert.AreEqual(exp, i.MaxOutputSize);
					}
				};
			};

			e.startFuzzing(dom, config);

			Assert.AreEqual(1, count);
		}

		[Test]
		public void MaxOuptputSize()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str'/>
	</DataModel>

	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default' nonDeterministicActions='true' maxOutputSize='65535'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(65535, dom.tests[0].maxOutputSize);

			var config = new RunConfiguration() { singleIteration = true };
			var e = new Engine(null);
			var count = 0;
			e.TestStarting += (c) =>
			{
				c.ActionStarting += (ctx, act) =>
				{
					foreach (var i in act.allData)
					{
						++count;
						Assert.AreEqual(65535, i.MaxOutputSize);
					}
				};
			};

			e.startFuzzing(dom, config);

			Assert.AreEqual(1, count);
		}

		[Test]
		public void TestTargetLifetime()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str'/>
	</DataModel>

	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default' {0}>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";

			var dom1 = DataModelCollector.ParsePit(xml.Fmt(""));
			Assert.AreEqual(Peach.Core.Dom.Test.Lifetime.Session, dom1.tests[0].TargetLifetime);

			var dom2 = DataModelCollector.ParsePit(xml.Fmt("targetLifetime='session'"));
			Assert.AreEqual(Peach.Core.Dom.Test.Lifetime.Session, dom2.tests[0].TargetLifetime);

			var dom3 = DataModelCollector.ParsePit(xml.Fmt("targetLifetime='iteration'"));
			Assert.AreEqual(Peach.Core.Dom.Test.Lifetime.Iteration, dom3.tests[0].TargetLifetime);

			var ex = Assert.Throws<PeachException>(() =>
				DataModelCollector.ParsePit(xml.Fmt("targetLifetime='foo'"))
			);
			StringAssert.StartsWith("Error, Pit file failed to validate", ex.Message);
		}

		[Test]
		public void TestAgentPlatform()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str'/>
	</DataModel>

	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action> 
		</State>
	</StateModel>

	<Agent name='NoneAgent' />
	<Agent name='AllAgent' />
	<Agent name='WindowsAgent' />
	<Agent name='OsxAgent' />
	<Agent name='LinuxAgent' />

	<Test name='Default'>
		<Agent ref='NoneAgent' platform='none' />
		<Agent ref='AllAgent' platform='all' />
		<Agent ref='WindowsAgent' platform='windows' />
		<Agent ref='OsxAgent' platform='osx' />
		<Agent ref='LinuxAgent' platform='linux' />

		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";

			var dom1 = DataModelCollector.ParsePit(xml);
			Assert.AreEqual(5, dom1.tests[0].agents.Count);

			Assert.AreEqual("NoneAgent", dom1.tests[0].agents[0].Name);
			Assert.AreEqual(Platform.OS.None, dom1.tests[0].agents[0].platform);

			Assert.AreEqual("AllAgent", dom1.tests[0].agents[1].Name);
			Assert.AreEqual(Platform.OS.All, dom1.tests[0].agents[1].platform);

			Assert.AreEqual("WindowsAgent", dom1.tests[0].agents[2].Name);
			Assert.AreEqual(Platform.OS.Windows, dom1.tests[0].agents[2].platform);

			Assert.AreEqual("OsxAgent", dom1.tests[0].agents[3].Name);
			Assert.AreEqual(Platform.OS.OSX, dom1.tests[0].agents[3].platform);

			Assert.AreEqual("LinuxAgent", dom1.tests[0].agents[4].Name);
			Assert.AreEqual(Platform.OS.Linux, dom1.tests[0].agents[4].platform);
		}

		[Test]
		public void TestWeights()
		{
			const string xml = @"
<Peach>
	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel name='DM'/>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>

		<Include xpath='//foo' />
		<Include xpath='//bar' />
		<Weight xpath='//*/*' weight='BelowNormal' />
		<Weight xpath='/foo' weight='Highest' />
		<Weight xpath='/foo/*' weight='Off' />

	</Test>
</Peach>
";

			var dom1 = DataModelCollector.ParsePit(xml);
			Assert.AreEqual(5, dom1.tests[0].mutables.Count);
		}

		[Test]
		public void TestWeightTwo()
		{
			const string xml = @"
<Peach>
	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel name='DM'>
					<Block name='foo'>
						<String name='bar' />
					</Block>
					<Block name='bar'>
						<String name='baz' />
					</Block>
				</DataModel>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>

		<Weight xpath='//bar' weight='Lowest' />
	</Test>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);

			e.startFuzzing(dom, cfg);

			var dm = dom.tests[0].stateModel.states[0].actions[0].dataModel;
			foreach (var item in dm.PreOrderTraverse())
			{
				if (item.Name == "bar" || item.Name == "baz")
					Assert.AreEqual(ElementWeight.Lowest, item.Weight);
				else
					Assert.AreEqual(ElementWeight.Normal, item.Weight);
			}
		}
	}
}
