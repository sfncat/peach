using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using NLog;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.IO;
using Peach.Core.Test;
using Peach.Pro.Core.OS;
using Encoding = Peach.Core.Encoding;
using Logger = NLog.Logger;
using SysProcess = System.Diagnostics.Process;

namespace Peach.Pro.Test.Core.Agent
{
	[TestFixture]
	[Quick]
	[Peach]
	public class AgentTests
	{
		ISingleInstance _si;
		SysProcess _process;
		TempDirectory _tmpDir;

		[SetUp]
		public void SetUp()
		{
			_si = Pal.SingleInstance(GetType().FullName);
			_si.Lock();
			_tmpDir = new TempDirectory();

			File.Copy(
				Assembly.GetExecutingAssembly().Location,
				Path.Combine(_tmpDir.Path, "Peach.Pro.Test.dll")
			);
		}

		[TearDown]
		public void TearDown()
		{
			_tmpDir.Dispose();
			_si.Dispose();
			_si = null;
		}

		class AgentKillerPublisher : Publisher
		{
			private readonly AgentTests _owner;
			private readonly string _protocol;
			private readonly RunContext _context;

			static readonly Logger ClassLogger = LogManager.GetCurrentClassLogger();

			public AgentKillerPublisher(AgentTests owner, string protocol, RunContext context)
				: base(new Dictionary<string, Variant>())
			{
				_owner = owner;
				_protocol = protocol;
				_context = context;
			}

			protected override Logger Logger
			{
				get { return ClassLogger; }
			}

			protected override void OnOpen()
			{
				base.OnOpen();

				if (!IsControlIteration && (Iteration % 2) == 1)
				{
					// Lame hack to make sure CrashableServer gets stopped
					_context.agentManager.IterationFinished();

					_owner.StopAgent();
					_owner.StartAgent(_protocol);
				}
			}
		}

		void StartAgent(string protocol)
		{
			_process = Helpers.StartAgent(protocol, _tmpDir.Path);
		}

		void StopAgent()
		{
			if (_process != null)
			{
				Helpers.StopAgent(_process);
				_process = null;
			}
		}

		static string CrashableServer
		{
			get
			{
				var ext = Platform.GetOS() == Platform.OS.Windows ? ".exe" : "";
				return Utilities.GetAppResourcePath("CrashableServer") + ext;
			}
		}

		static string PlatformMonitor
		{
			get
			{
				if (Platform.GetOS() != Platform.OS.Windows)
					return "Process";
				if (!Environment.Is64BitProcess && Environment.Is64BitOperatingSystem)
					return "Process";
				if (Environment.Is64BitProcess && !Environment.Is64BitOperatingSystem)
					return "Process";

				return "WindowsDebugger";
			}
		}

		public static string[] ChannelNames
		{
			get { return new[] {"legacy", "tcp"}; }
		}

		[Test]
		public void TestFailedReconnect([ValueSource("ChannelNames")]string protocol)
		{
			if (Platform.GetOS() != Platform.OS.Windows && protocol == "legacy")
				Assert.Ignore(".NET remoting doesn't work inside nunit on mono");

			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String value='Hello'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Agent name='RemoteAgent' location='{0}://127.0.0.1:9001'>
		<Monitor class='Null' />
	</Agent>

	<Test name='Default' targetLifetime='iteration'>
		<Agent ref='RemoteAgent'/>
		<StateModel ref='TheState'/>
		<Publisher class='Null' />
	</Test>
</Peach>".Fmt(protocol);

			try
			{
				StartAgent(protocol);

				var dom = DataModelCollector.ParsePit(xml);
				var config = new RunConfiguration { range = true, rangeStart = 0, rangeStop = 5 };
				var e = new Engine(null);

				e.TestStarting += ctx =>
				{
					ctx.ActionStarting += (c, a) =>
					{
						if (c.currentIteration == 3)
						{
							StopAgent();
						}
					};
				};

				e.startFuzzing(dom, config);

			}
			finally
			{
				StopAgent();
			}
		}

		[Test]
		public void TestReconnect([ValueSource("ChannelNames")]string protocol)
		{
			if (Platform.GetOS() != Platform.OS.Windows && protocol == "legacy")
				Assert.Ignore(".NET remoting doesn't work inside nunit on mono");

			var port = TestBase.MakePort(20000, 21000);
			var tmp = Path.GetTempFileName();

			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String value='Hello'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output' publisher='Remote'>
				<DataModel ref='TheDataModel'/>
			</Action>

			<Action type='call' method='ScoobySnacks' publisher='Peach.Agent'/>

			<Action type='open' publisher='Killer'/>
		</State>
	</StateModel>

	<Agent name='RemoteAgent' location='{0}://127.0.0.1:9001'>
		<Monitor class='{1}'>
			<Param name='Executable' value='{2}'/>
			<Param name='Arguments' value='127.0.0.1 {3}'/>
			<Param name='RestartOnEachTest' value='true'/>
			<Param name='FaultOnEarlyExit' value='true'/>
		</Monitor>
		<Monitor name='M' class='Null'>
			<Param name='LogFile' value='{4}'/>
			<Param name='OnCall' value='ScoobySnacks'/>
		</Monitor>
	</Agent>

	<Test name='Default' targetLifetime='iteration'>
		<Agent ref='RemoteAgent'/>
		<StateModel ref='TheState'/>
		<Publisher name='Remote' class='Remote'>
			<Param name='Agent' value='RemoteAgent' />
			<Param name='Class' value='Tcp'/>
			<Param name='Host' value='127.0.0.1' />
			<Param name='Port' value='{3}' />
		</Publisher>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='StringStatic' />
		</Mutators>
	</Test>
</Peach>".Fmt(protocol, PlatformMonitor, CrashableServer, port, tmp);

			try
			{
				StartAgent(protocol);

				var parser = new PitParser();
				var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));
				var config = new RunConfiguration { range = true, rangeStart = 83, rangeStop = 86 };
				var e = new Engine(null);

				e.TestStarting += ctx =>
				{
					var pub = new AgentKillerPublisher(this, protocol, ctx) { Name = "Killer" };
					ctx.test.publishers.Add(pub);
				};

				e.Fault += e_Fault;

				e.startFuzzing(dom, config);

				Assert.Greater(_faults.Count, 0, "Test should have found faults");

				var contents = File.ReadAllLines(tmp);
				var expected = new[] {
// Iteration 83 (Control & Record)
"M.StartMonitor", "M.SessionStarting", "M.IterationStarting False False", "M.Message ScoobySnacks", "M.IterationFinished", "M.DetectedFault", 
// Iteration 83 - Agent is killed (IterationFinished is a hack to kill CrashableServer)
"M.IterationStarting False False", "M.Message ScoobySnacks", "M.IterationFinished", 
// Agent is restarted & fault is not detected
"M.StartMonitor", "M.SessionStarting", "M.IterationStarting False False", "M.Message ScoobySnacks", "M.IterationFinished", "M.DetectedFault",
// Agent is killed
"M.IterationStarting False False", "M.Message ScoobySnacks", "M.IterationFinished", 
// Agent is restarted & fault is detected
"M.StartMonitor", "M.SessionStarting", "M.IterationStarting False False", "M.Message ScoobySnacks", "M.IterationFinished", "M.DetectedFault", "M.GetMonitorData",
// Reproduction occurs & fault is detected
"M.IterationStarting True True", "M.Message ScoobySnacks", "M.IterationFinished", "M.DetectedFault", "M.GetMonitorData",
// Fussing stops
"M.SessionFinished", "M.StopMonitor"
				};

				Assert.That(contents, Is.EqualTo(expected));
			}
			finally
			{
				StopAgent();

				File.Delete(tmp);
			}
		}

		readonly Dictionary<uint, Fault[]> _faults = new Dictionary<uint, Fault[]>();

		void e_Fault(RunContext context, uint currentIteration, Peach.Core.Dom.StateModel stateModel, Fault[] faultData)
		{
			_faults[currentIteration] = faultData;
		}

		[Test]
		public void TestSoftException([ValueSource("ChannelNames")]string protocol)
		{
			if (Platform.GetOS() != Platform.OS.Windows && protocol == "legacy")
				Assert.Ignore(".NET remoting doesn't work inside nunit on mono");

			var port = TestBase.MakePort(20000, 21000);

			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String value='Hello'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output' publisher='Remote'>
				<DataModel ref='TheDataModel'/>
			</Action>
			<Action type='output' publisher='Remote'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Agent name='RemoteAgent' location='{0}://127.0.0.1:9001'>
		<Monitor class='{1}'>
			<Param name='Executable' value='{2}'/>
			<Param name='Arguments' value='127.0.0.1 {3}'/>
			<Param name='FaultOnEarlyExit' value='true'/>
		</Monitor>
	</Agent>

	<Test name='Default' targetLifetime='iteration'>
		<Agent ref='RemoteAgent'/>
		<StateModel ref='TheState'/>
		<Publisher name='Remote' class='Remote'>
			<Param name='Agent' value='RemoteAgent' />
			<Param name='Class' value='Tcp'/>
			<Param name='Host' value='127.0.0.1' />
			<Param name='Port' value='{3}' />
		</Publisher>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='StringStatic' />
		</Mutators>
	</Test>
</Peach>".Fmt(protocol, PlatformMonitor, CrashableServer, port);

			try
			{
				StartAgent(protocol);

				var parser = new PitParser();
				var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));
				var config = new RunConfiguration { range = true, rangeStart = 83, rangeStop = 86 };

				var e = new Engine(null);
				e.Fault += e_Fault;
				e.startFuzzing(dom, config);

				Assert.Greater(_faults.Count, 0);
			}
			finally
			{
				StopAgent();
			}
		}

		[Test]
		public void TestBadProcess([ValueSource("ChannelNames")]string protocol)
		{
			if (Platform.GetOS() != Platform.OS.Windows && protocol == "legacy")
				Assert.Ignore(".NET remoting doesn't work inside nunit on mono");

			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String value='Hello'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Agent name='RemoteAgent' location='{0}://127.0.0.1:9001'>
		<Monitor class='{1}'>
			<Param name='Executable' value='MissingProgram'/>
		</Monitor>
	</Agent>

	<Test name='Default'>
		<Agent ref='RemoteAgent'/>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
	</Test>
</Peach>".Fmt(protocol, PlatformMonitor);

			try
			{
				StartAgent(protocol);

				var dom = DataModelCollector.ParsePit(xml);
				var cfg = new RunConfiguration();
				var e = new Engine(null);

				var ex = Assert.Throws<PeachException>(() => e.startFuzzing(dom, cfg));

				var msg = PlatformMonitor == "Process"
					? "Could not start process 'MissingProgram'."
					: "System debugger could not start process 'MissingProgram'.";

				StringAssert.StartsWith(msg, ex.Message);
			}
			finally
			{
				StopAgent();
			}
		}

		[Test]
		public void TestSessionStarting([ValueSource("ChannelNames")]string protocol)
		{
			// If session starting throws a SoftException, it should come out of
			// the engine as a PeachException

			if (Platform.GetOS() != Platform.OS.Windows && protocol == "legacy")
				Assert.Ignore(".NET remoting doesn't work inside nunit on mono");

			var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));

			var port = ((IPEndPoint)socket.LocalEndPoint).Port;

			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String value='Hello'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Agent name='RemoteAgent' location='{0}://127.0.0.1:9001'>
		<Monitor class='TcpPort'>
			<Param name='Host' value='127.0.0.1'/>
			<Param name='Port' value='{1}'/>
			<Param name='Action' value='Automation'/>
			<Param name='When' value='OnStart'/>
			<Param name='Timeout' value='1'/>
			<Param name='State' value='Open'/>
		</Monitor>
	</Agent>

	<Test name='Default'>
		<Agent ref='RemoteAgent'/>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
	</Test>
</Peach>".Fmt(protocol, port);

			try
			{
				StartAgent(protocol);

				var dom = DataModelCollector.ParsePit(xml);
				var cfg = new RunConfiguration { singleIteration = true };
				var e = new Engine(null);

				var ex = Assert.Throws<PeachException>(() => e.startFuzzing(dom, cfg));

				var msg = "TcpPort monitor timeout reached checking 127.0.0.1:{0} for state of Open.".Fmt(port);

				Assert.AreEqual(msg, ex.Message);

				Assert.NotNull(ex.InnerException, "InnerException should be non-null");
				Assert.True(ex.InnerException is SoftException, "InnerException should be a SoftException");
			}
			finally
			{
				StopAgent();

				socket.Dispose();
			}
		}

		[Test]
		public void TestDefaultPort([ValueSource("ChannelNames")]string protocol)
		{
			if (Platform.GetOS() != Platform.OS.Windows && protocol == "legacy")
				Assert.Ignore(".NET remoting doesn't work inside nunit on mono");

			var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String value='Hello'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
		</State>
	</StateModel>

	<Agent name='RemoteAgent' location='{0}://127.0.0.1'>
		<Monitor class='{1}'>
			<Param name='Executable' value='{2}'/>
			<Param name='Arguments' value='127.0.0.1 0'/>
			<Param name='RestartOnEachTest' value='true'/>
			<Param name='FaultOnEarlyExit' value='true'/>
		</Monitor>
	</Agent>

	<Test name='Default'>
		<Agent ref='RemoteAgent'/>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Strategy class='RandomDeterministic'/>
	</Test>
</Peach>".Fmt(protocol, PlatformMonitor, CrashableServer);

			try
			{
				StartAgent(protocol);

				var parser = new PitParser();
				var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

				var config = new RunConfiguration { singleIteration = true };

				var e = new Engine(null);

				e.startFuzzing(dom, config);
			}
			finally
			{
				StopAgent();
			}
		}

		[Test]
		public void TestAgentOrder()
		{
			var tmp = Path.GetTempFileName();

			const string template = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String value='Hello'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
			</Action>
			<Action type='call' method='Foo' publisher='Peach.Agent'/>
		</State>
	</StateModel>

	<Agent name='Local1'>
		<Monitor name='Local1.mon1' class='Null'>
			<Param name='LogFile' value='{0}' />
		</Monitor>
		<Monitor name='Local1.mon2' class='Null'>
			<Param name='LogFile' value='{0}' />
		</Monitor>
	</Agent>

	<Agent name='Local2'>
		<Monitor name='Local2.mon1' class='Null'>
			<Param name='LogFile' value='{0}' />
		</Monitor>
		<Monitor name='Local2.mon2' class='Null'>
			<Param name='LogFile' value='{0}' />
		</Monitor>
	</Agent>

	<Test name='Default'>
		<Agent ref='Local1'/>
		<Agent ref='Local2'/>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Strategy class='Random'/>
	</Test>
</Peach>";

			string[] actual;

			try
			{
				var xml = template.Fmt(tmp);
				var dom = DataModelCollector.ParsePit(xml);
				var config = new RunConfiguration {singleIteration = true};
				var e = new Engine(null);

				e.startFuzzing(dom, config);

				actual = File.ReadAllLines(tmp);
			}
			finally
			{
				File.Delete(tmp);
			}

			string[] expected =
			{
				"Local1.mon1.StartMonitor",
				"Local1.mon2.StartMonitor",
				"Local1.mon1.SessionStarting",
				"Local1.mon2.SessionStarting",
				"Local2.mon1.StartMonitor",
				"Local2.mon2.StartMonitor",
				"Local2.mon1.SessionStarting",
				"Local2.mon2.SessionStarting",
				"Local1.mon1.IterationStarting False False",
				"Local1.mon2.IterationStarting False False",
				"Local2.mon1.IterationStarting False False",
				"Local2.mon2.IterationStarting False False",
				"Local1.mon1.Message Foo",
				"Local1.mon2.Message Foo",
				"Local2.mon1.Message Foo",
				"Local2.mon2.Message Foo",
				"Local2.mon2.IterationFinished",
				"Local2.mon1.IterationFinished",
				"Local1.mon2.IterationFinished",
				"Local1.mon1.IterationFinished",
				"Local1.mon1.DetectedFault",
				"Local1.mon2.DetectedFault",
				"Local2.mon1.DetectedFault",
				"Local2.mon2.DetectedFault",
				"Local2.mon2.SessionFinished",
				"Local2.mon1.SessionFinished",
				"Local1.mon2.SessionFinished",
				"Local1.mon1.SessionFinished",
				"Local2.mon2.StopMonitor",
				"Local2.mon1.StopMonitor",
				"Local1.mon2.StopMonitor",
				"Local1.mon1.StopMonitor"
			};

			Assert.That(actual, Is.EqualTo(expected));
		}

		[Publisher("TestRemoteFile", Scope = PluginScope.Internal)]
		[Parameter("FileName", typeof(string), "Name of file to open for reading/writing")]
		public class TestRemoteFilePublisher : Peach.Core.Publishers.StreamPublisher
		{
			private static readonly Logger ClassLogger = LogManager.GetCurrentClassLogger();
			protected override Logger Logger { get { return ClassLogger; } }

			public string FileName { get; set; }

			static readonly byte[] Data = Encoding.ASCII.GetBytes("0123456789");

			public TestRemoteFilePublisher(Dictionary<string, Variant> args)
				: base(args)
			{
				stream = new MemoryStream();
			}

			void Log(string msg, params object[] args)
			{
				using (var writer = new StreamWriter(FileName, true))
				{
					writer.WriteLine(msg, args);
				}
			}

			protected override void OnStart()
			{
				Log("OnStart");
			}

			protected override void OnStop()
			{
				Log("OnStop");
			}

			protected override void OnOpen()
			{
				Log("OnOpen");

				stream = new MemoryStream();
			}

			protected override void OnClose()
			{
				Log("OnClose");

				stream.Dispose();
				stream = null;
			}

			protected override void OnAccept()
			{
				Log("OnAccept");
			}

			protected override void OnInput()
			{
				Log("OnInput");

				// Write some bytes
				stream.Write(Data, 0, 5);
				stream.Seek(0, SeekOrigin.Begin);
			}

			public override void WantBytes(long count)
			{
				Log("WantBytes {0}", count);

				var pos = stream.Position;

				for (var i = 0; i < count; ++i)
				{
					stream.WriteByte(Data[stream.Length % Data.Length]);
				}

				stream.Position = pos;
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				var ret = base.Read(buffer, offset, count);
				Log("Read, Want: {0}, Got: {1}", count - offset, ret);
				return ret;
			}

			protected override void OnOutput(BitwiseStream data)
			{
				// Do a copy to ensure it is remoted properly
				var strm = new BitStream();
				data.CopyTo(strm);

				Log("OnOutput {0}/{1}", strm.Length, strm.LengthBits);
			}

			protected override Variant OnGetProperty(string property)
			{
				Log("GetProperty: {0}", property);

				switch (property)
				{
					case "long":
						return new Variant(long.MinValue);
					case "ulong":
						return new Variant(ulong.MaxValue);
					case "int":
						return new Variant(100);
					case "string":
						return new Variant("This is a string");
					case "bytes":
						return new Variant(new byte[] { 0xff, 0x00, 0xff });
					case "bits":
						var bs = new BitStream();
						bs.Write(new byte[] { 0xfe, 0x00, 0xfe }, 0, 3);
						bs.Seek(0, SeekOrigin.Begin);
						return new Variant(bs);
					default:
						return new Variant();
				}
			}

			protected override void OnSetProperty(string property, Variant value)
			{
				Log("SetProperty {0} {1} {2}", property, value.GetVariantType(), value.ToString());
			}

			protected override Variant OnCall(string method, List<BitwiseStream> args)
			{
				var sb = new StringBuilder();

				sb.AppendFormat("Call: {0}", method);
				sb.AppendLine();

				for (var i = 0; i < args.Count; ++i)
				{
					sb.AppendFormat(" Param '{0}': {1}", args[i].Name, Encoding.ASCII.GetString(args[i].ToArray()));
					if (i < (args.Count - 1))
						sb.AppendLine();
				}

				Log(sb.ToString());

				if (method == "null")
					return null;

				return new Variant(Bits.Fmt("{0:L8}{1}", 7, "Success"));
			}
		}

		const string RemotePublisherXml = @"
<Peach>
	<DataModel name='Input'>
		<String length='10'/>
	</DataModel>

	<DataModel name='Param1'>
		<Number size='8' value='0x7c'/>
	</DataModel>

	<DataModel name='Param2'>
		<String value='Hello'/>
	</DataModel>

	<DataModel name='Param3'>
		<Blob value='World'/>
	</DataModel>

	<DataModel name='Result'>
		<Number size='8'>
			<Relation type='size' of='str'/>
		</Number>
		<String name='str'/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='Param2'/>
			</Action>

			<Action type='accept'/>

			<Action name='input' type='input'>
				<DataModel ref='Input'/>
			</Action>

			<Action type='setProperty' property='int'>
				<DataModel ref='Param1'/>
			</Action>

			<Action type='getProperty' property='int'>
				<DataModel ref='Param1'/>
			</Action>

			<Action type='getProperty' property='string'>
				<DataModel ref='Param2'/>
			</Action>

			<Action type='getProperty' property='bytes'>
				<DataModel ref='Param2'/>
			</Action>

			<Action type='getProperty' property='bits'>
				<DataModel ref='Param2'/>
			</Action>

			<!--Action name='call' type='call' method='foo'>
				<Param>
					<DataModel ref='Param1'/>
				</Param>
				<Param>
					<DataModel ref='Param2'/>
				</Param>
				<Param>
					<DataModel ref='Param3'/>
				</Param>
				<Result>
					<DataModel ref='Result'/>
				</Result>
			</Action-->
		</State>
	</StateModel>

	<Agent name='RemoteAgent' location='{0}://127.0.0.1:9001'/>

	<Test name='Default'>
		<Agent ref='RemoteAgent'/>
		<StateModel ref='TheState'/>
{1}
		<Strategy class='RandomDeterministic'/>
	</Test>
</Peach>";

		[Test]
		public void TestRemotePublisher([ValueSource("ChannelNames")]string protocol)
		{
			if (Platform.GetOS() != Platform.OS.Windows && protocol == "legacy")
				Assert.Ignore(".NET remoting doesn't work inside nunit on mono");

			var tmp = Path.GetTempFileName();

			var pub = @"
		<Publisher class='Remote'>
			<Param name='Class' value='TestRemoteFile'/>
			<Param name='Agent' value='RemoteAgent'/>
			<Param name='FileName' value='{0}'/>
		</Publisher>
".Fmt(tmp);

			var xml = RemotePublisherXml.Fmt(protocol, pub);

			try
			{
				StartAgent(protocol);

				var parser = new PitParser();
				var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

				var config = new RunConfiguration { singleIteration = true };

				var e = new Engine(null);
				e.startFuzzing(dom, config);

				Verify(tmp, dom);
			}
			finally
			{
				StopAgent();
			}
		}

		[Test]
		public void TestNewRemotePublisher([ValueSource("ChannelNames")]string protocol)
		{
			if (Platform.GetOS() != Platform.OS.Windows && protocol == "legacy")
				Assert.Ignore(".NET remoting doesn't work inside nunit on mono");

			var tmp = Path.GetTempFileName();

			var pub = @"
		<Publisher class='TestRemoteFile' agent='RemoteAgent'>
			<Param name='FileName' value='{0}'/>
		</Publisher>
".Fmt(tmp);

			var xml = RemotePublisherXml.Fmt(protocol, pub);

			try
			{
				StartAgent(protocol);

				var parser = new PitParser();
				var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

				var config = new RunConfiguration { singleIteration = true };

				var e = new Engine(null);
				e.startFuzzing(dom, config);

				Verify(tmp, dom);
			}
			finally
			{
				if (_process != null)
					StopAgent();
			}
		}

		private static void Verify(string tmp, Peach.Core.Dom.Dom dom)
		{
			var contents = File.ReadAllLines(tmp);
			var expected = new[] {
					"OnStart",
					"OnOpen",
					"OnOutput 5/40",
					"OnAccept",
					"OnInput",
					"Read, Want: \\d+, Got: 5",
					"Read, Want: \\d+, Got: 0",
					"WantBytes 5",
					"Read, Want: \\d+, Got: 5",
					"Read, Want: \\d+, Got: 0",
					"SetProperty int BitStream 7c",
					"GetProperty: int",
					"GetProperty: string",
					"GetProperty: bytes",
					"GetProperty: bits",
					"OnClose",
					"OnStop"
				};

			if (contents.Length != expected.Length)
			{
				// Generate nice nunit failure message
				Assert.That(contents, Is.EqualTo(expected));
			}
			else
			{
				for (int i = 0; i < expected.Length; ++i)
				{
					StringAssert.IsMatch(expected[i], contents[i]);
				}
			}

			var st = dom.tests[0].stateModel.states[0];
			//var act = st.actions["call"] as Dom.Actions.Call;
			//Assert.NotNull(act);
			//Assert.NotNull(act.result);
			//Assert.NotNull(act.result.dataModel);
			//Assert.AreEqual(2, act.result.dataModel.Count);
			//Assert.AreEqual(7, (int)act.result.dataModel[0].DefaultValue);
			//Assert.AreEqual("Success", (string)act.result.dataModel[1].DefaultValue);

			var inp = st.actions["input"];
			Assert.AreEqual("0123456789", inp.dataModel.InternalValue.BitsToString());
		}
	}
}
