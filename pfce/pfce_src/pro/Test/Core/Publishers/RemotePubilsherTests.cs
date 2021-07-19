using System.IO;
using System.Net.Sockets;
using System.Reflection;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Test;
using Peach.Pro.Core.OS;

namespace Peach.Pro.Test.Core.Publishers
{
	[TestFixture]
	[Quick]
	[Peach]
	class RemotePublisherTests
	{
		ISingleInstance _si;
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

		private const string template = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">
		<String value=""Hello""/>
	</DataModel>

	<StateModel name=""TheStateModel"" initialState=""InitialState"">
		<State name=""InitialState"">
			<Action name=""Action1"" type=""output"">
				<DataModel ref=""TheDataModel""/>
			</Action>
		</State>
	</StateModel>

	<Agent name=""LocalAgent"">
	</Agent>

	<Test name=""Default"">
		<Agent ref=""LocalAgent""/>
		<StateModel ref=""TheStateModel""/>
		<Publisher class=""Remote"">
			<Param name=""Agent"" value=""LocalAgent""/>
			<Param name=""Class"" value=""File""/>
			<Param name=""FileName"" value=""{0}""/>
		</Publisher>
	</Test>
</Peach>";

		private const string raw_eth = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">
		<Blob value=""Hello!Hello!001122334455""/>
	</DataModel>

	<StateModel name=""TheStateModel"" initialState=""InitialState"">
		<State name=""InitialState"">
			<Action name=""Action1"" type=""output"">
				<DataModel ref=""TheDataModel""/>
			</Action>
			<Action name=""Action2"" type=""input"">
				<DataModel ref=""TheDataModel""/>
			</Action>
		</State>
	</StateModel>

	<Agent name=""LocalAgent"" location=""{0}://127.0.0.1:9001"">
	</Agent>

	<Test name=""Default"">
		<Agent ref=""LocalAgent""/>
		<StateModel ref=""TheStateModel""/>
		<Publisher class=""Remote"">
			<Param name=""Agent"" value=""LocalAgent""/>
			<Param name=""Class"" value=""RawEther""/>
			<Param name=""Interface"" value=""{1}""/>
		</Publisher>
	</Test>
</Peach>";

		private const string udp = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">
		<String value=""Hello!Hello!001122334455"" token=""true""/>
	</DataModel>

	<StateModel name=""TheStateModel"" initialState=""InitialState"">
		<State name=""InitialState"">
			<Action name=""Action1"" type=""output"">
				<DataModel ref=""TheDataModel""/>
			</Action>
			<Action name=""Action2"" type=""input"">
				<DataModel ref=""TheDataModel""/>
			</Action>
		</State>
	</StateModel>

	<Agent name=""LocalAgent"" location=""{0}://127.0.0.1:9001"">
	</Agent>

	<Test name=""Default"">
		<Agent ref=""LocalAgent""/>
		<StateModel ref=""TheStateModel""/>
		<Publisher class=""Remote"">
			<Param name=""Agent"" value=""LocalAgent""/>
			<Param name=""Class"" value=""Udp""/>
			<Param name=""Host"" value=""127.0.0.1""/>
			<Param name=""SrcPort"" value=""{1}""/>
			<Param name=""Port"" value=""{1}""/>
		</Publisher>
	</Test>
</Peach>";

		[Test]
		public void TestCreate()
		{
			var xml = string.Format(template, "tile.txt");

			var parser = new PitParser();
			parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));
		}

		[Test]
		public void TestRun()
		{
			var tempFile = Path.GetTempFileName();

			var xml = string.Format(template, tempFile);

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration {singleIteration = true};

			var e = new Engine(null);
			e.startFuzzing(dom, config);

			var output = File.ReadAllLines(tempFile);

			Assert.AreEqual(1, output.Length);
			Assert.AreEqual("Hello", output[0]);
		}

		public void RunRemote(string protocol, string xml)
		{
			var process = Helpers.StartAgent(protocol, _tmpDir.Path);

			try
			{
				var parser = new PitParser();
				var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

				var config = new RunConfiguration {singleIteration = true};

				var e = new Engine(null);
				e.startFuzzing(dom, config);
			}
			finally
			{
				Helpers.StopAgent(process);
			}
		}

		public static string[] ChannelNames
		{
			get { return new[] { "legacy", "tcp" }; }
		}

		[Test]
		public void TestRaw([ValueSource("ChannelNames")] string protocol)
		{
			if (Platform.GetOS() != Platform.OS.Windows && protocol == "legacy")
				Assert.Ignore(".NET remoting doesn't work inside nunit on mono");

			var iface = Helpers.GetPrimaryIface(AddressFamily.InterNetwork).Item1;
			RunRemote(protocol, raw_eth.Fmt(protocol, iface));
		}

		[Test]
		public void TestUdp([ValueSource("ChannelNames")] string protocol)
		{
			if (Platform.GetOS() != Platform.OS.Windows && protocol == "legacy")
				Assert.Ignore(".NET remoting doesn't work inside nunit on mono");

			var port = TestBase.MakePort(12000, 13000);
			RunRemote(protocol, udp.Fmt(protocol, port));
		}
	}
}
