using System.Collections.Generic;
using System.Configuration;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Test.Core.Agent.Http;
using Peach.Pro.Test.Core.Publishers;
using Encoding = Peach.Core.Encoding;


namespace Peach.Pro.Test.Core.Agent
{
	[TestFixture]
	[Quick]
	[Peach]
	class HttpChannelTests : DataModelCollector
	{
		[Test]
		[Category("Peach")]
		public void HttpChannelPublisherTest()
		{
			var xml =
				"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
				"<Peach>\n" +
				"\t<DataModel name=\"Example1\">\n" +
				"\t\t<String value=\"Testing\" />\n" +
				"\t</DataModel>\n" +
				"\n" +
				"\t<StateModel name=\"TheStateModel\" initialState=\"initial\">\n" +
				"\t<State name=\"initial\">\n" +
				"\t  <Action type=\"output\">\n" +
				"\t\t<DataModel ref=\"Example1\"/>\n" +
				"\t  </Action>\n" +
				"\t</State>\n" +
				"\t</StateModel>\n" +
				"\n" +
				"\t<Agent name=\"TheAgent\" location=\"http://127.0.0.1:9000\">\n" +
				"\t\t<Monitor class=\"SaveFile\">\n" +
				"\t\t\t<Param name=\"Filename\" value=\"foo.txt\" />\n" +
				"\t\t</Monitor>\n" +
				"\t</Agent>\n" +
				"  \n" +
				"\t<Test name=\"Default\" maxOutputSize=\"200\">\n" +
				"\t\t<Agent ref=\"TheAgent\" />\n" +
				"\t\t<StateModel ref=\"TheStateModel\"/>\n" +
				"\t\t<Publisher class=\"Remote\">\n" +
				"\t\t\t<Param name=\"Agent\" value=\"TheAgent\" />\n" +
				"\t\t\t<Param name=\"Class\" value=\"ConsoleHex\"/>\n" +
				"\t\t</Publisher>\n" +
				"\t</Test>\n" +
				"</Peach>\n" +
				"";

			var expected = new string[]
			{
				"/Agent/AgentConnect",
				"/Agent/StartMonitor?name=Monitor&cls=SaveFile",
				"/Agent/SessionStarting",
				"/Agent/IterationStarting?iterationCount=0&isReproduction=False&lastWasFault=False",
				"/Agent/Publisher/CreatePublisher",
				"/Agent/Publisher/start",
				"/Agent/Publisher/Set_Iteration",
				"/Agent/Publisher/Set_IsControlIteration",
				"/Agent/Publisher/open",
				"/Agent/Publisher/output",
				"/Agent/Publisher/close",
				"/Agent/IterationFinished",
				"/Agent/DetectedFault",
				"/Agent/Publisher/stop",
				"/Agent/SessionFinished",
				"/Agent/StopAllMonitors",
				"/Agent/AgentDisconnect"
			};
			
			var started = new AutoResetEvent(false);
			var stopped = new AutoResetEvent(false);

			var server = new HttpServer();
			server.Started += (sender, args) => started.Set();
			server.Started += (sender, args) => stopped.Set();
			var agentThread = new Thread(() =>
			{
				server.Run(9000, 9500);
			});

			agentThread.Start();
			Assert.IsTrue(started.WaitOne(60000));

			var dom = ParsePit(xml);
			dom.tests[0].agents[0].location = "http://127.0.0.1:" + server.Uri.Port + "/";

			var config = new RunConfiguration()
			{
				singleIteration = true
			};

			var e = new Engine(null);

			e.startFuzzing(dom, config);

			// Verify calls are made in correct order and all calls are made

			Assert.AreEqual(expected.Length, server.RestCalls.Count);
			for (var cnt = 0; cnt < expected.Length; cnt++)
				Assert.AreEqual(expected[cnt], server.RestCalls[cnt]);

			server.Stop();
			Assert.IsTrue(stopped.WaitOne(60000));
		}

		[Test]
		[Category("Peach")]
		public void HttpChannelPublisherInputTest()
		{
			const string xml = @"
<Peach>
	<DataModel name='Example1'>
		<String length='5' />
		<String length='5' />
		<String />
	</DataModel>

	<StateModel name='TheStateModel' initialState='initial'>
		<State name='initial'>
			<Action type='input'>
				<DataModel ref='Example1'/>
			</Action>
		</State>
	</StateModel>
				
	<Agent name='TheAgent' location='http://127.0.0.1:9000' />

	<Test name='Default' maxOutputSize='200'>
		<Agent ref='TheAgent' />
		<StateModel ref='TheStateModel'/>
		<Publisher class='Foo' agent='TheAgent' >
			<Param name='Param1' value='Value1' />
		</Publisher>
	</Test>
</Peach>";

			var expected = new []
			{
				"/Agent/AgentConnect",
				"/Agent/Publisher/CreatePublisher",
				"/Agent/Publisher/start",
				"/Agent/Publisher/Set_Iteration",
				"/Agent/Publisher/Set_IsControlIteration",
				"/Agent/Publisher/open",
				"/Agent/Publisher/input",
				"/Agent/Publisher/WantBytes {\"count\":1}",
				"/Agent/Publisher/WantBytes {\"count\":4}",
				"/Agent/Publisher/WantBytes {\"count\":4}",
				"/Agent/Publisher/close",
				"/Agent/Publisher/stop",
				"/Agent/AgentDisconnect"
			};

			var data = new List<byte[]> {
				Encoding.ASCII.GetBytes("H"),       // Asks for 1, return 1
				Encoding.ASCII.GetBytes("elloW"),   // Needs 5, asks for 4 (has 1), return one extra
				Encoding.ASCII.GetBytes("orld!!!"), // Asks for enought to complete 2nd string, give extra
			};

			var started = new AutoResetEvent(false);
			var stopped = new AutoResetEvent(false);

			var server = new HttpServer();
			server.Started += (sender, args) => started.Set();
			server.Started += (sender, args) => stopped.Set();

			server.WantBytes = c =>
			{
				if (data.Count == 0)
					return null;

				var ret = data[0];
				data.RemoveAt(0);
				return ret;
			};

			var agentThread = new Thread(() =>
			{
				server.Run(9000, 9500);
			});

			agentThread.Start();
			Assert.IsTrue(started.WaitOne(60000));

			var dom = ParsePit(xml);
			dom.tests[0].agents[0].location = "http://127.0.0.1:" + server.Uri.Port + "/";

			var config = new RunConfiguration()
			{
				singleIteration = true
			};

			var e = new Engine(null);

			e.startFuzzing(dom, config);


			// Verify calls are made in correct order and all calls are made

			CollectionAssert.AreEqual(expected, server.RestCalls);

			Assert.AreEqual("HelloWorld!!!", dom.tests[0].stateModel.states[0].actions[0].dataModel.InternalValue.BitsToString());

			server.Stop();
			Assert.IsTrue(stopped.WaitOne(60000));
		}
	}
}
