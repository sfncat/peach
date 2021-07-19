using System.IO;
using System.Net;
using System.Net.Sockets;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Test;
using Peach.Pro.Core.Publishers;

namespace Peach.Pro.Test.Core.Publishers
{
	[TestFixture]
	[Quick]
	[Peach]
	class TcpListenerPublisherTests : DataModelCollector
	{
		[Test]
		public void TcpListenerTest1()
		{
			ushort port = TestBase.MakePort(51000, 52000);
			string xml = @"
				<Peach>
					<DataModel name='input'>
						<Choice>
							<String length='100'/>
							<String />
						</Choice>
					</DataModel>

					<DataModel name='output'>
						<String name='str'/>
					</DataModel>

					<StateModel name='SM' initialState='InitialState'>
						<State name='InitialState'>
							<Action type='open' publisher='Server'/>

							<Action type='output' publisher='Client'>
								<DataModel ref='output'/>
								<Data>
									<Field name='str' value='Hello'/>
								</Data>
							</Action>
			
							<Action type='accept' publisher='Server'/>

							<Action type='input' publisher='Server'>
								<DataModel ref='input'/>
							</Action>

							<Action type='output' publisher='Client'>
								<DataModel ref='output'/>
								<Data>
									<Field name='str' value='World'/>
								</Data>
							</Action>

							<Action type='input' publisher='Server'>
								<DataModel ref='input'/>
							</Action>
						</State>
					</StateModel>

					<Test name='Default'>
						<StateModel ref='SM'/>

						<Publisher class='TcpListener' name='Server'>
							<Param name='Interface' value='127.0.0.1'/>
							<Param name='Port' value='{0}'/>
							<Param name='Timeout' value='1000'/>
						</Publisher>
		
						<Publisher class='Tcp' name='Client'>
							<Param name='Host' value='127.0.0.1'/>
							<Param name='Port' value='{0}'/>
						</Publisher>
					</Test>
				</Peach>".Fmt(port);

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(this);

			e.startFuzzing(dom, config);

			Assert.AreEqual("Hello", dom.tests[0].stateModel.states["InitialState"].actions[1].dataModel.InternalValue.BitsToString());
			Assert.AreEqual("Hello", dom.tests[0].stateModel.states["InitialState"].actions[3].dataModel.InternalValue.BitsToString());
			Assert.AreEqual("World", dom.tests[0].stateModel.states["InitialState"].actions[4].dataModel.InternalValue.BitsToString());
			Assert.AreEqual("World", dom.tests[0].stateModel.states["InitialState"].actions[5].dataModel.InternalValue.BitsToString());
			
		}

		[Test]
		public void FailingInputTest1()
		{
			string xml = @"
				<Peach>
					<DataModel name='input'>
						<String />
					</DataModel>

					<StateModel name='SM' initialState='InitialState'>
						<State name='InitialState'>
							<Action type='input' publisher='Server'>
								<DataModel ref='input'/>
							</Action>
						</State>
					</StateModel>

					<Test name='Default'>
						<StateModel ref='SM'/>

						<Publisher class='TcpListener' name='Server'>
							<Param name='Interface' value='127.0.0.1'/>
							<Param name='Port' value='55555'/>
							<Param name='Timeout' value='1000'/>
						</Publisher>
					</Test>
				</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(this);

			var ex = Assert.Throws<PeachException>(() => e.startFuzzing(dom, config));
			Assert.AreEqual("Error on data input, the buffer is not initalized.", ex.Message);
		}

		[Test]
		public void FailingOutputTest1()
		{
			string xml = @"
				<Peach>
					<DataModel name='output'>
						<String name='str'/>
					</DataModel>

					<StateModel name='SM' initialState='InitialState'>
						<State name='InitialState'>
							<Action type='output' publisher='Server'>
								<DataModel ref='output'/>
							</Action>
						</State>
					</StateModel>

					<Test name='Default'>
						<StateModel ref='SM'/>

						<Publisher class='TcpListener' name='Server'>
							<Param name='Interface' value='127.0.0.1'/>
							<Param name='Port' value='0'/>
							<Param name='Timeout' value='1000'/>
						</Publisher>
					</Test>
				</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(this);

			var ex = Assert.Throws<PeachException>(() => e.startFuzzing(dom, config));
			Assert.AreEqual("Error on data output, the client is not initalized.", ex.Message);
		}

		[Test]
		public void TestSessionStop()
		{
			const string xml = @"
<Peach>
	<StateModel name='SM' initialState='InitialState'>
		<State name='InitialState'>
			<Action type='open' name='open' />
			<Action type='accept' name='accept' />
			<Action type='input' name='input'>
				<DataModel name='DM'>
					<Blob />
				</DataModel>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>

		<Publisher class='TcpListener'>
			<Param name='Interface' value='127.0.0.1'/>
			<Param name='Port' value='0'/>
			<Param name='Lifetime' value='session'/>
		</Publisher>
	</Test>
</Peach>";

			var dom = ParsePit(xml);
			var cfg = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);
			var localEp = new IPEndPoint(IPAddress.Loopback, 0);
			var tcp = new TcpClient();

			e.TestStarting += ctx =>
			{
				ctx.ActionFinished += (c, a) =>
				{
					if (a.Name == "open")
					{
						localEp.Port = ((TcpListenerPublisher)ctx.test.publishers[0]).Port;

						tcp.Connect(localEp);
					}
					else if (a.Name == "accept")
					{
						tcp.Client.Send(Encoding.ASCII.GetBytes("Hello"));
					}
				};
			};

			e.startFuzzing(dom, cfg);

			tcp.Close();

			Assert.AreNotEqual(0, localEp.Port, "Port should be non-zero");

			// Ensure port was properly closed, bind should be successful

			using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
			{
				s.Bind(localEp);
			}
		}
	}
}
