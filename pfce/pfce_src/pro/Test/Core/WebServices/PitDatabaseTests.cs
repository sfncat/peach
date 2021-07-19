using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Pro.Core;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;
using File = System.IO.File;
using Monitor = Peach.Pro.Core.WebServices.Models.Monitor;
using MAgent = Peach.Pro.Core.WebServices.Models.Agent;
using Peach.Core.Test;
using Peach.Pro.Core.License;
using Moq;

namespace Peach.Pro.Test.Core.WebServices
{
	[TestFixture]
	[Quick]
	[Peach]
	public class PitDatabaseTests
	{
		/*
		 * ArgumentException for posting bad pit.config and pit.agents
		 * UnauthorizedAccessException for posting to locked pit
		 * Verify posting agents with same name/location properly get merged (wizard result)
		 * Verify Param StartMode gets translated properly
		 * POST monitor map name vs key
		 */

		TempDirectory _root;
		PitDatabase _db;
		string _originalPitPath;

		static string dataModelExample =
@"<?xml version='1.0' encoding='utf-8'?>
<Peach xmlns='http://peachfuzzer.com/2012/Peach'
       xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
       xsi:schemaLocation='http://peachfuzzer.com/2012/Peach peach.xsd'
       author='Pit Author Name'
       description='IMG PIT'
       version='0.0.1'>
	<DataModel name='DM'>
		<String value='Hello World' />
	</DataModel>
</Peach>
";

		static string stateModelExample =
@"<?xml version='1.0' encoding='utf-8'?>
<Peach xmlns='http://peachfuzzer.com/2012/Peach'
       xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
       xsi:schemaLocation='http://peachfuzzer.com/2012/Peach peach.xsd'
       author='Pit Author Name'
       description='IMG PIT'
       version='0.0.1'>

	<Include ns='DM' src='##PitLibraryPath##/_Common/Models/Image/IMG_Data.xml' />

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='call' method='StartIterationEvent' publisher='Peach.Agent' />
			<Action type='output'>
				<DataModel name='DM:DM'/>
			</Action>
			<Action type='call' method='ExitIterationEvent' publisher='Peach.Agent' />
		</State>
	</StateModel>
</Peach>
";

		static string pitExample =
@"<?xml version='1.0' encoding='utf-8'?>
<Peach xmlns='http://peachfuzzer.com/2012/Peach'
       xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
       xsi:schemaLocation='http://peachfuzzer.com/2012/Peach peach.xsd'
       author='Pit Author Name'
       version='0.0.1'>

	<Include ns='SM' src='##PitLibraryPath##/_Common/Models/Image/IMG_State.xml' />

	<Agent name='TheAgent'>
		<Monitor class='RunCommand'>
			<Param name='Command' value='Foo'/>
			<Param name='StartOnCall' value='Foo'/>
		</Monitor>
	</Agent>

	<Test name='Default'>
		<Agent ref='TheAgent'/>
		<Strategy class='Random'/>
		<StateModel ref='SM:SM' />
		<Publisher class='Null'/>
	</Test>
</Peach>
";

		static string configExample =
@"<?xml version='1.0' encoding='utf-8'?>
<PitDefines xmlns:xsd='http://www.w3.org/2001/XMLSchema'
            xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
            xmlns='http://peachfuzzer.com/2012/PitDefines'>
  <All>
    <Strategy key='Strategy' value='Random' name='Mutation Strategy' description='The mutation strategy to use when fuzzing.' />
    <String key='PitLibraryPath' value='.' name='Pit Library Path' description='The path to the root of the pit library.' />
    <String key='SomeMiscVariable' value='Foo' name='Misc Variable' description='Description goes here' />
  </All>
</PitDefines>
";

		const string pitNoConfig =
@"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='DM'>
		<String/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel name='DM:DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<Strategy class='Random'/>
		<StateModel ref='SM' />
		<Publisher class='Null'/>
	</Test>
</Peach>
";

		const string proxyExample =
@"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<Test name='Default'>
		<WebProxy />
		<Strategy class='WebProxy' />
		<Publisher class='WebApiProxy'/>
	</Test>
</Peach>
";

		[SetUp]
		public void SetUp()
		{
			// On-disk directory structure:
			// ##PitLibraryPath## (tmpdir)
			//   _Common
			//     Models
			//       Image
			//         IMG_Data.xml
			//   Image
			//     IMG.xml
			//     IMG.xml.config
			//   Configs
			//     IMG.peach      OriginalPit = Image/IMG.xml

			_root = new TempDirectory();

			var cat = Path.Combine(_root.Path, "Image");
			Directory.CreateDirectory(cat);

			var mod = Path.Combine(_root.Path, "_Common", "Models", "Image");
			Directory.CreateDirectory(mod);

			_originalPitPath = Path.Combine(cat, "IMG.xml");

			File.WriteAllText(Path.Combine(mod, "IMG_State.xml"), stateModelExample);
			File.WriteAllText(Path.Combine(mod, "IMG_Data.xml"), dataModelExample);
			File.WriteAllText(_originalPitPath, pitExample);
			File.WriteAllText(_originalPitPath + ".config", configExample);

			var license = new Mock<ILicense>();
			license.Setup(x => x.CanUsePit(It.IsAny<string>())).Returns(new PitFeature { IsValid = true });

			_db = new PitDatabase(license.Object);
			_db.ValidationEventHandler += OnValidationEvent;
			_db.Load(_root.Path);
		}

		[TearDown]
		public void TearDown()
		{
			_root.Dispose();
			_root = null;
			_db = null;
		}

		private void OnValidationEvent(object sender, ValidationEventArgs args)
		{
			throw new PeachException("DB failed to load", args.Exception);
		}

		[Test]
		public void TestParse()
		{
			Assert.NotNull(_root);
			Assert.NotNull(_db);

			Assert.AreEqual(1, _db.Entries.Count());
			Assert.AreEqual(3, _db.Libraries.Count());

			var libs = _db.Libraries.ToList();

			// Pits
			Assert.True(libs[0].Locked);
			Assert.AreEqual("Pits", libs[0].Name);
			Assert.AreEqual(1, libs[0].Versions.Count);
			Assert.True(libs[0].Versions[0].Locked);
			Assert.AreEqual(2, libs[0].Versions[0].Version);
			Assert.AreEqual(1, libs[0].Versions[0].Pits.Count);
			Assert.AreEqual("IMG", libs[0].Versions[0].Pits[0].Name);

			var p = _db.GetPitByUrl(libs[0].Versions[0].Pits[0].PitUrl);
			Assert.NotNull(p);

			// Configs
			Assert.False(libs[1].Locked);
			Assert.AreEqual("Configurations", libs[1].Name);
			Assert.AreEqual(1, libs[1].Versions.Count);
			Assert.False(libs[1].Versions[0].Locked);
			Assert.AreEqual(2, libs[1].Versions[0].Version);

			// Legacy
			Assert.True(libs[2].Locked);
			Assert.AreEqual("Legacy", libs[2].Name);
			Assert.AreEqual(1, libs[2].Versions.Count);
			Assert.AreEqual(1, libs[2].Versions[0].Version);
			Assert.True(libs[2].Versions[0].Locked);
			Assert.AreEqual(0, libs[2].Versions[0].Pits.Count);

			// Metadata
			var expected = new string[] { "StartIterationEvent", "ExitIterationEvent" };
			var callParams = (from grp in p.Metadata.Monitors
							  from monitor in grp.Items
							  from paramGroup in monitor.Items
							  where paramGroup.Items != null
							  from param in paramGroup.Items
							  where param.Type == ParameterType.Call
							  select param).ToList();

			Assert.IsNotEmpty(callParams);
			foreach (var param in callParams)
			{
				CollectionAssert.AreEqual(expected, param.Options);
			}
		}

		[Test]
		public void TestNoConfig()
		{
			File.WriteAllText(Path.Combine(_root.Path, "Image", "IMG Copy.xml"), pitExample);

			_db.Load(_root.Path);

			var ent = _db.Entries.ToList();
			Assert.AreEqual(2, ent.Count);

			var img = ent.First(e => e.PitConfig.Name == "IMG");

			var pit1 = _db.GetPitByUrl(img.PitUrl);
			Assert.NotNull(pit1);

			var imgCopy = ent.First(e => e.PitConfig.Name == "IMG Copy");

			var pit2 = _db.GetPitByUrl(imgCopy.PitUrl);
			Assert.IsEmpty(pit2.Config);
			Assert.IsEmpty(pit2.Agents);
			Assert.IsEmpty(pit2.Weights);
		}

		[Test]
		public void TestNewConfig()
		{
			var lib = _db.Libraries.Single(x => x.Name == "Pits");
			var ent = _db.Entries.Single(x => x.Path == _originalPitPath);
			var pit = _db.GetPitById(ent.Id);

			var newName = "IMG Copy";
			var newDesc = "My copy of the img pit";

			var tuple = _db.NewConfig(pit.PitUrl, newName, newDesc);
			Assert.NotNull(tuple.Item1);
			Assert.NotNull(tuple.Item2);

			Assert.AreEqual(2, _db.Entries.Count());
			Assert.AreEqual(1, lib.Versions[0].Pits.Count);

			Assert.NotNull(_db.GetPitDetailByUrl(tuple.Item1.PitUrl));

			var expName = Path.Combine(_root.Path, PitDatabase.ConfigsDir, "Image", "IMG Copy.peach");
			Assert.AreEqual(expName, tuple.Item2.Path);
			Assert.True(File.Exists(expName));

			var newPit = PitDatabase.LoadPitConfig(tuple.Item2.Path);
			Assert.AreEqual(newName, newPit.Name);
			Assert.AreEqual(newDesc, newPit.Description);
		}

		[Test]
		public void TestCopyDotInName()
		{
			var pit = _db.Entries.First(x => x.Path == _originalPitPath);

			var newName = "Foo.Mine";
			var newDesc = "My copy of the img pit";

			var tuple = _db.NewConfig(pit.PitUrl, newName, newDesc);
			var newPit = _db.GetPitByUrl(tuple.Item1.PitUrl);

			Assert.AreEqual(newName, newPit.Name);
		}

		[Test]
		public void TestCopyPathInFilename()
		{
			var pit = _db.Entries.First(x => x.Path == _originalPitPath);

			var newName = "../../../Foo";
			var newDesc = "My copy of the img pit";

			var ex = Assert.Throws<ArgumentException>(() => _db.NewConfig(pit.PitUrl, newName, newDesc));
			Assert.AreEqual("name", ex.ParamName);
		}

		[Test]
		public void TestCopyBadFilename()
		{
			var pit = _db.Entries.First(x => x.Path == _originalPitPath);

			var newName = "****";
			var newDesc = "My copy of the img pit";

			try
			{
				// Linux lets everything but '/' be in a file name
				_db.NewConfig(pit.PitUrl, newName, newDesc);

				if (Platform.GetOS() == Platform.OS.Windows)
					Assert.Fail("Should throw");
				else
					Assert.Pass();
			}
			catch (ArgumentException)
			{
				if (Platform.GetOS() == Platform.OS.Windows)
					Assert.Pass();
				else
					Assert.Fail("Should not throw");
			}
		}

		[Test]
		public void TestSetPitConfig()
		{
			var ent = _db.Entries.First();
			var tuple = _db.NewConfig(ent.PitUrl, "IMG Copy", "Desc");
			var cfg = new PitConfig
			{
				Agents = new List<MAgent>(),
				Config = new List<Param> {
					new Param { Key = "SomeMiscVariable", Value = "Foo Bar Baz" }
				},
			};

			var pit = _db.UpdatePitByUrl(tuple.Item1.PitUrl, cfg);
			Assert.NotNull(pit);
			Assert.NotNull(pit.Config);
			Assert.AreEqual("SomeMiscVariable", pit.Config[0].Key);
			Assert.AreEqual("Foo Bar Baz", pit.Config[0].Value);

			var savedPit = PitDatabase.LoadPitConfig(tuple.Item2.Path);
			Assert.NotNull(savedPit);
			Assert.AreEqual(1, savedPit.Config.Count);

			Assert.AreEqual("SomeMiscVariable", savedPit.Config[0].Key);
			Assert.AreEqual("Foo Bar Baz", savedPit.Config[0].Value);
		}

		[Test]
		public void TestSaveMonitorsOmitDefaults()
		{
			// Ensure default monitor parameters are not written to xml

			var ent = _db.Entries.First();
			Assert.NotNull(ent);

			var tuple = _db.NewConfig(ent.PitUrl, "IMG Copy", "Desc");

			var cfg = new PitConfig
			{
				Config = new List<Param>(),
				Agents = new List<MAgent> {
					new MAgent {
						Name = "Agent0",
						AgentUrl = "local://",
						Monitors = new List<Monitor> {
							new Monitor {
								MonitorClass = "Process",
								Map = new List<Param> {
									new Param { Key = "Executable", Value = "foo" },
									new Param { Key = "StartOnCall", Value = "" },
									new Param { Key = "WaitForExitOnCall", Value = null },
									new Param { Key = "NoCpuKill", Value = "false" },
								}
							}
						}
					}
				}
			};

			var pit = _db.UpdatePitByUrl(tuple.Item1.PitUrl, cfg);

			var parser = new PitParser();

			var defs = new Dictionary<string, string>
			{
				{"PitLibraryPath", _root.Path},
				{"Strategy", "Random"}
			};

			var opts = new Dictionary<string, object>
			{
				{PitParser.DEFINED_VALUES, defs}
			};

			var dom = parser.asParser(opts, Path.Combine(_root.Path, tuple.Item2.PitConfig.OriginalPit));
			PitInjector.InjectAgents(cfg, defs, dom);

			Assert.AreEqual(1, dom.tests[0].agents.Count);

			Assert.AreEqual("Agent0", dom.tests[0].agents[0].Name);
			Assert.AreEqual("local://", dom.tests[0].agents[0].location);
			Assert.AreEqual(1, dom.tests[0].agents[0].monitors.Count);

			var mon = dom.tests[0].agents[0].monitors[0];
			Assert.AreEqual("Process", mon.cls);
			Assert.AreEqual(2, mon.parameters.Count);
			Assert.True(mon.parameters.ContainsKey("Executable"), "Should contain key Executable");
			Assert.AreEqual("foo", mon.parameters["Executable"].ToString());
			Assert.True(mon.parameters.ContainsKey("NoCpuKill"), "Should contain key NoCpuKill");
			Assert.AreEqual("false", mon.parameters["NoCpuKill"].ToString());

			Assert.NotNull(pit);
			Assert.NotNull(pit.Agents);

			Assert.AreEqual(1, pit.Agents.Count);
			Assert.AreEqual("Agent0", pit.Agents[0].Name);
			Assert.AreEqual("local://", pit.Agents[0].AgentUrl);
			Assert.AreEqual(1, pit.Agents[0].Monitors.Count);
			Assert.AreEqual(2, pit.Agents[0].Monitors[0].Map.Count);
			Assert.AreEqual("Executable", pit.Agents[0].Monitors[0].Map[0].Key);
			Assert.AreEqual("foo", pit.Agents[0].Monitors[0].Map[0].Value);
			Assert.AreEqual("NoCpuKill", pit.Agents[0].Monitors[0].Map[1].Key);
			Assert.AreEqual("false", pit.Agents[0].Monitors[0].Map[1].Value);

			// Only Key/Value are expected to be set
			// Name/Description come from pit.metadata.monitors
			Assert.AreEqual(null, pit.Agents[0].Monitors[0].Map[0].Name);
			Assert.AreEqual(null, pit.Agents[0].Monitors[0].Map[0].Description);
		}

		[Test]
		public void TestSaveMonitors()
		{
			var json = @"
[
	{
		'name':'Agent0',
		'agentUrl':'local://',
		'monitors': [
			{
				'monitorClass':'PageHeap',
				'map': [
					{ 'name':'Executable', 'value':'Foo.exe' },
					{ 'name':'WinDbgPath', 'value':'C:\\WinDbg'  }
				],
			},
			{
				'monitorClass':'WindowsDebugger',
				'map': [
					{ 'name':'Executable', 'value':'Foo.exe' },
					{ 'name':'Arguments', 'value':'--arg' },
					{ 'name':'IgnoreFirstChanceGuardPage', 'value':'true' }
				],
			},
			{
				'monitorClass':'CanaKit',
				'map': [
					{'name':'SerialPort', 'value':'COM1' },
					{'name':'RelayNumber', 'value':'1' }
				]
			}
		]
	},
	{
		'name':'Agent1',
		'agentUrl':'tcp://remotehostname',
		'monitors': [
			{
				'monitorClass':'Pcap',
				'map': [
					{'name':'Device', 'value':'eth0' },
					{'name':'Filter', 'value':'tcp port 80' }
				],
			},
			{
				'monitorClass':'Pcap',
				'map': [
					{'name':'Device', 'value':'eth0' },
					{'name':'Filter', 'value':'tcp port 8080' }
				],
			},
		]
	},
	{
		'name':'Agent2',
		'agentUrl':'tcp://remotehostname2',
		'monitors': [
			{
				'monitorClass':'Pcap',
				'map': [
					{'name':'Device', 'value':'eth0' },
					{'name':'Filter', 'value':'tcp port 80' }
				],
			}
		]
	}
]";

			var ent = _db.Entries.First();
			Assert.NotNull(ent);

			var tuple = _db.NewConfig(ent.PitUrl, "IMG Copy", "Desc");

			var agents = JsonConvert.DeserializeObject<List<MAgent>>(json);
			Assert.NotNull(agents);

			var cfg = new PitConfig
			{
				Agents = agents,
				Config = new List<Param>(),
			};
			var pit = _db.UpdatePitByUrl(tuple.Item1.PitUrl, cfg);
			Assert.NotNull(pit);

			var parser = new PitParser();

			var defs = new Dictionary<string, string>
			{
				{"PitLibraryPath", _root.Path},
				{"Strategy", "Random"}
			};

			var opts = new Dictionary<string, object>
			{
				{PitParser.DEFINED_VALUES, defs}
			};

			var dom = parser.asParser(opts, Path.Combine(_root.Path, tuple.Item2.PitConfig.OriginalPit));
			PitInjector.InjectAgents(cfg, defs, dom);

			Assert.AreEqual(3, dom.tests[0].agents.Count);

			Assert.AreEqual("Agent0", dom.tests[0].agents[0].Name);
			Assert.AreEqual("local://", dom.tests[0].agents[0].location);
			Assert.AreEqual(3, dom.tests[0].agents[0].monitors.Count);

			VerifyMonitor(agents[0].Monitors[0], dom.tests[0].agents[0].monitors[0]);
			VerifyMonitor(agents[0].Monitors[1], dom.tests[0].agents[0].monitors[1]);
			VerifyMonitor(agents[0].Monitors[2], dom.tests[0].agents[0].monitors[2]);

			Assert.AreEqual("Agent1", dom.tests[0].agents[1].Name);
			Assert.AreEqual("tcp://remotehostname", dom.tests[0].agents[1].location);
			Assert.AreEqual(2, dom.tests[0].agents[1].monitors.Count);

			VerifyMonitor(agents[1].Monitors[0], dom.tests[0].agents[1].monitors[0]);
			VerifyMonitor(agents[1].Monitors[1], dom.tests[0].agents[1].monitors[1]);

			Assert.AreEqual("Agent2", dom.tests[0].agents[2].Name);
			Assert.AreEqual("tcp://remotehostname2", dom.tests[0].agents[2].location);
			Assert.AreEqual(1, dom.tests[0].agents[2].monitors.Count);

			VerifyMonitor(agents[2].Monitors[0], dom.tests[0].agents[2].monitors[0]);
		}

		private void VerifyMonitor(Monitor jsonMon, Peach.Core.Dom.Monitor domMon)
		{
			Assert.AreEqual(jsonMon.MonitorClass, domMon.cls);
			Assert.AreEqual(jsonMon.Map.Count, domMon.parameters.Count);

			foreach (var item in jsonMon.Map)
			{
				var key = item.Key ?? item.Name;
				Assert.True(domMon.parameters.ContainsKey(key));
				Assert.AreEqual(item.Value, (string)domMon.parameters[key]);
			}
		}

		[Test]
		public void AddRemovePit()
		{
			Assert.AreEqual(1, _db.Entries.Count());
			Assert.AreEqual(3, _db.Libraries.Count());

			var path = Path.Combine(_root.Path, "Image", "My.xml");
			File.WriteAllText(path, pitNoConfig);

			_db.Load(_root.Path);
			Assert.AreEqual(2, _db.Entries.Count());

			var file = _db.Entries.FirstOrDefault(e => e.PitConfig.Name == "My");
			Assert.NotNull(file);

			var pit = _db.GetPitByUrl(file.PitUrl);
			Assert.NotNull(pit);

			File.Delete(path);

			_db.Load(_root.Path);
			Assert.AreEqual(1, _db.Entries.Count());

			Assert.Null(_db.Entries.FirstOrDefault(e => e.PitConfig.Name == "My"));
		}

		[Test]
		public void TestConfigInjection()
		{
			var cwd = Directory.GetCurrentDirectory();
			try
			{
				Directory.SetCurrentDirectory(_root.Path);

				var path = Path.Combine(_root.Path, "Image", "inject.xml");
				File.WriteAllText(path, pitExample);

				var asm = Assembly.GetExecutingAssembly();
				var json = Utilities.LoadStringResource(asm, "Peach.Pro.Test.Core.Resources.pit.json");
				var cfg = JsonConvert.DeserializeObject<PitConfig>(json);

				var cfgFile = Path.Combine(_root.Path, "Image", "IMG.xml.config");
				var extras = new List<KeyValuePair<string, string>>();
				var defs = PitDefines.ParseFile(cfgFile, extras, Guid.Empty);

				var evaluated = defs.Evaluate();
				PitInjector.InjectDefines(cfg, defs, evaluated);

				var opts = new Dictionary<string, object>
				{
					{PitParser.DEFINED_VALUES, evaluated}
				};

				var parser = new PitParser();
				var dom = parser.asParser(opts, path);

				PitInjector.InjectAgents(cfg, evaluated, dom);

				var agent = dom.agents.First();
				var monitor = agent.monitors.First();
				Assert.AreEqual("local://", agent.location);
				Assert.AreEqual(false, monitor.parameters.Any(x => x.Key == "WaitForExitTimeout"), "WaitForExitTimeout should be omitted");
				Assert.AreEqual("true", (string)monitor.parameters.Single(x => x.Key == "UseNLog").Value);

				var config = new RunConfiguration
				{
					singleIteration = true,
				};

				var e = new Engine(null);
				e.startFuzzing(dom, config);
			}
			finally
			{
				Directory.SetCurrentDirectory(cwd);
			}
		}

		[Test]
		public void TestMigrateConfig()
		{
			// Extract .peach from .xml
			//
			// Before:
			//   ##PitLibraryPath##
			//     Image
			//       IMG.xml
			//       IMG.xml.config
			//     User
			//       Image
			//         IMG.xml
			// ----
			// After:
			//   ##PitLibraryPath##
			//     Image
			//       IMG.xml
			//       IMG.xml.config
			//     Configs
			//       Image
			//         IMG.xml         (backup)
			//         IMG.xml.config  (backup)
			//         IMG.peach       OriginalPit = file://##PitLibraryPath##/Image/IMG.xml
			// ----
			var category = "Image";
			var legacyDir = Path.Combine(_root.Path, PitDatabase.LegacyDir, category);
			Directory.CreateDirectory(legacyDir);

			var srcPath = Path.Combine(legacyDir, "IMG.xml");

			File.WriteAllText(srcPath, pitExample);
			File.WriteAllText(srcPath + ".config", configExample);

			_db.Load(_root.Path);

			// Number of legacy entries is double for backwards compatible absolute pitUrls
			Assert.AreEqual(3, _db.Entries.Count());
			Assert.AreEqual(3, _db.Libraries.Count());

			var libs = _db.Libraries.ToList();
			Assert.AreEqual(1, libs[2].Versions[0].Pits.Count);

			var pit = _db.Entries.First(x => x.Path == srcPath);
			var pitDetail = _db.GetPitDetailByUrl(pit.PitUrl);
			Assert.NotNull(pitDetail);

			var originalPit = _db.Entries.ElementAt(0);

			var tuple = _db.MigratePit(pit.PitUrl, originalPit.PitUrl);

			var expectedPath = Path.Combine(_root.Path, PitDatabase.ConfigsDir, category, "IMG.peach");
			Assert.AreEqual(expectedPath, tuple.Item2.Path);
			Assert.True(File.Exists(expectedPath));
			Assert.True(File.Exists(Path.Combine(_root.Path, PitDatabase.ConfigsDir, category, "IMG.xml")));
			Assert.True(File.Exists(Path.Combine(_root.Path, PitDatabase.ConfigsDir, category, "IMG.xml.config")));
			Assert.False(File.Exists(Path.Combine(_root.Path, PitDatabase.LegacyDir, category, "IMG.xml")));
			Assert.False(File.Exists(Path.Combine(_root.Path, PitDatabase.LegacyDir, category, "IMG.xml.config")));

			var newPit = PitDatabase.LoadPitConfig(expectedPath);
			Assert.NotNull(newPit);
			Assert.AreEqual(Path.Combine(category, "IMG.xml"), newPit.OriginalPit);

			Assert.AreEqual(2, newPit.Config.Count);
			Assert.AreEqual("Strategy", newPit.Config[0].Key);
			Assert.AreEqual("Random", newPit.Config[0].Value);
			Assert.AreEqual("SomeMiscVariable", newPit.Config[1].Key);
			Assert.AreEqual("Foo", newPit.Config[1].Value);

			Assert.AreEqual(1, newPit.Agents.Count);
			Assert.AreEqual("TheAgent", newPit.Agents[0].Name);
			Assert.IsNull(newPit.Agents[0].AgentUrl);
			Assert.AreEqual(1, newPit.Agents[0].Monitors.Count);
			Assert.IsNull(newPit.Agents[0].Monitors[0].Name);
			Assert.AreEqual("RunCommand", newPit.Agents[0].Monitors[0].MonitorClass);
			Assert.AreEqual(2, newPit.Agents[0].Monitors[0].Map.Count);
			Assert.AreEqual("Command", newPit.Agents[0].Monitors[0].Map[0].Key);
			Assert.AreEqual("Foo", newPit.Agents[0].Monitors[0].Map[0].Value);
			Assert.AreEqual("StartOnCall", newPit.Agents[0].Monitors[0].Map[1].Key);
			Assert.AreEqual("Foo", newPit.Agents[0].Monitors[0].Map[1].Value);
		}

		[Test]
		public void TestMigrateCustom()
		{
			// Split .xml into (.peach, .xml)
			//
			// Before:
			//   ##PitLibraryPath##
			//     User
			//       Customer
			//         IMG.xml
			//         IMG.xml.config
			// ----
			// After:
			//   ##PitLibraryPath##
			//     Customer
			//       IMG.xml
			//       IMG.xml.config
			//     Configs
			//       Customer
			//         IMG.peach       OriginalPit = file://##PitLibraryPath##/Customer/IMG.xml
			// ----
			var category = "Customer";
			var legacyDir = Path.Combine(_root.Path, PitDatabase.LegacyDir, category);
			Directory.CreateDirectory(legacyDir);

			var srcPath = Path.Combine(legacyDir, "IMG.xml");
			File.WriteAllText(srcPath, pitExample);
			File.WriteAllText(srcPath + ".config", configExample);

			_db.Load(_root.Path);

			// Number of legacy entries is double for backwards compatible absolute pitUrls
			Assert.AreEqual(3, _db.Entries.Count());
			Assert.AreEqual(3, _db.Libraries.Count());

			var libs = _db.Libraries.ToList();
			Assert.AreEqual(1, libs[2].Versions[0].Pits.Count);

			var pit = _db.Entries.First(x => x.Path == srcPath);
			var pitDetail = _db.GetPitDetailByUrl(pit.PitUrl);
			Assert.NotNull(pitDetail);

			var tuple = _db.MigratePit(pit.PitUrl, pit.PitUrl);

			var expectedPath = Path.Combine(_root.Path, PitDatabase.ConfigsDir, category, "IMG.peach");
			Assert.AreEqual(expectedPath, tuple.Item2.Path);
			Assert.True(File.Exists(expectedPath));

			Assert.True(File.Exists(Path.Combine(_root.Path, category, "IMG.xml")));
			Assert.True(File.Exists(Path.Combine(_root.Path, category, "IMG.xml.config")));
			Assert.False(File.Exists(Path.Combine(_root.Path, PitDatabase.LegacyDir, category, "IMG.xml")));
			Assert.False(File.Exists(Path.Combine(_root.Path, PitDatabase.LegacyDir, category, "IMG.xml.config")));

			var newPit = PitDatabase.LoadPitConfig(expectedPath);
			Assert.NotNull(newPit);
			Assert.AreEqual(Path.Combine(category, "IMG.xml"), newPit.OriginalPit);
			Assert.AreEqual(2, newPit.Config.Count);
			Assert.AreEqual("Strategy", newPit.Config[0].Key);
			Assert.AreEqual("Random", newPit.Config[0].Value);
			Assert.AreEqual("SomeMiscVariable", newPit.Config[1].Key);
			Assert.AreEqual("Foo", newPit.Config[1].Value);

			Assert.AreEqual(1, newPit.Agents.Count);
			Assert.AreEqual("TheAgent", newPit.Agents[0].Name);
			Assert.IsNull(newPit.Agents[0].AgentUrl);
			Assert.AreEqual(1, newPit.Agents[0].Monitors.Count);
			Assert.IsNull(newPit.Agents[0].Monitors[0].Name);
			Assert.AreEqual("RunCommand", newPit.Agents[0].Monitors[0].MonitorClass);
			Assert.AreEqual(2, newPit.Agents[0].Monitors[0].Map.Count);
			Assert.AreEqual("Command", newPit.Agents[0].Monitors[0].Map[0].Key);
			Assert.AreEqual("Foo", newPit.Agents[0].Monitors[0].Map[0].Value);
			Assert.AreEqual("StartOnCall", newPit.Agents[0].Monitors[0].Map[1].Key);
			Assert.AreEqual("Foo", newPit.Agents[0].Monitors[0].Map[1].Value);
		}

		[Test]
		public void TestMigrateDotInName()
		{
			var srcPath = Path.Combine(_root.Path, "Image", "Foo.Mine.xml");
			File.WriteAllText(srcPath, pitExample);

			_db.Load(_root.Path);

			var pit = _db.Entries.First(x => x.Path == srcPath);
			var originalPit = _db.Entries.First(x => x.Path == _originalPitPath);
			var tuple = _db.MigratePit(pit.PitUrl, originalPit.PitUrl);
			Assert.AreEqual("Foo.Mine", tuple.Item2.PitConfig.Name);
		}

		[Test]
		public void TestMigrateDuplicate()
		{
			var category = "Image";
			var legacyDir = Path.Combine(_root.Path, PitDatabase.LegacyDir, category);
			Directory.CreateDirectory(legacyDir);

			var srcPath = Path.Combine(legacyDir, "IMG.xml");
			File.WriteAllText(srcPath, pitExample);
			File.WriteAllText(srcPath + ".config", configExample);

			_db.Load(_root.Path);

			// Number of legacy entries is double for backwards compatible absolute pitUrls
			Assert.AreEqual(3, _db.Entries.Count());
			Assert.AreEqual(3, _db.Libraries.Count());

			var libs = _db.Libraries.ToList();
			Assert.AreEqual(1, libs[2].Versions[0].Pits.Count);

			var pit = _db.Entries.First(x => x.Path == srcPath);
			Assert.NotNull(_db.GetPitDetailByUrl(pit.PitUrl));

			var tuple = _db.MigratePit(pit.PitUrl, pit.PitUrl);

			var expectedPath = Path.Combine(_root.Path, PitDatabase.ConfigsDir, category, "IMG.peach");
			Assert.AreEqual(expectedPath, tuple.Item2.Path);
			Assert.True(File.Exists(expectedPath));

			Assert.True(File.Exists(Path.Combine(_root.Path, category, "IMG.xml")));
			Assert.True(File.Exists(Path.Combine(_root.Path, category, "IMG.xml.config")));
			Assert.True(File.Exists(Path.Combine(_root.Path, category, "IMG-Legacy-1.xml")));
			Assert.True(File.Exists(Path.Combine(_root.Path, category, "IMG-Legacy-1.xml.config")));
			Assert.False(File.Exists(Path.Combine(_root.Path, PitDatabase.LegacyDir, category, "IMG.xml")));
			Assert.False(File.Exists(Path.Combine(_root.Path, PitDatabase.LegacyDir, category, "IMG.xml.config")));

			var newPit = PitDatabase.LoadPitConfig(expectedPath);
			Assert.NotNull(newPit);
			Assert.AreEqual(Path.Combine(category, "IMG-Legacy-1.xml"), newPit.OriginalPit);
			Assert.AreEqual(2, newPit.Config.Count);
			Assert.AreEqual("Strategy", newPit.Config[0].Key);
			Assert.AreEqual("Random", newPit.Config[0].Value);
			Assert.AreEqual("SomeMiscVariable", newPit.Config[1].Key);
			Assert.AreEqual("Foo", newPit.Config[1].Value);

			Assert.AreEqual(1, newPit.Agents.Count);
			Assert.AreEqual("TheAgent", newPit.Agents[0].Name);
			Assert.IsNull(newPit.Agents[0].AgentUrl);
			Assert.AreEqual(1, newPit.Agents[0].Monitors.Count);
			Assert.IsNull(newPit.Agents[0].Monitors[0].Name);
			Assert.AreEqual("RunCommand", newPit.Agents[0].Monitors[0].MonitorClass);
			Assert.AreEqual(2, newPit.Agents[0].Monitors[0].Map.Count);
			Assert.AreEqual("Command", newPit.Agents[0].Monitors[0].Map[0].Key);
			Assert.AreEqual("Foo", newPit.Agents[0].Monitors[0].Map[0].Value);
			Assert.AreEqual("StartOnCall", newPit.Agents[0].Monitors[0].Map[1].Key);
			Assert.AreEqual("Foo", newPit.Agents[0].Monitors[0].Map[1].Value);
		}

		[Test]
		public void TestDeletePitConfig()
		{
			var ent = _db.Entries.Single(x => x.Path == _originalPitPath);
			var pit = _db.GetPitById(ent.Id);

			var newName = "Delete";
			var newDesc = "My copy of the img pit";

			var tuple = _db.NewConfig(pit.PitUrl, newName, newDesc);
			var expName = Path.Combine(_root.Path, PitDatabase.ConfigsDir, "Image", "Delete.peach");
			Assert.IsTrue(File.Exists(expName));

			_db.DeletePitById(tuple.Item2.Id);
			Assert.IsFalse(File.Exists(expName));
			Assert.IsNull(_db.GetPitById(tuple.Item2.Id));
		}

		[Test]
		public void TestRenamePitConfig()
		{
			var ent = _db.Entries.Single(x => x.Path == _originalPitPath);
			var pit = _db.GetPitById(ent.Id);

			var newName = "Start";
			var newDesc = "My copy of the img pit";

			// create a new config
			var start = _db.NewConfig(pit.PitUrl, newName, newDesc);
			var startPath = Path.Combine(_root.Path, PitDatabase.ConfigsDir, "Image", "Start.peach");
			Assert.IsTrue(File.Exists(startPath));

			// update the new config
			var cfg = new PitConfig
			{
				Agents = new List<MAgent>(),
				Config = new List<Param> {
					new Param { Key = "SomeMiscVariable", Value = "Foo Bar Baz" }
				},
			};
			_db.UpdatePitByUrl(start.Item1.PitUrl, cfg);

			// copy the config to another
			var finish = _db.NewConfig(start.Item2.PitUrl, "Finish", newDesc);
			Assert.IsTrue(File.Exists(Path.Combine(_root.Path, PitDatabase.ConfigsDir, "Image", "Finish.peach")));

			// delete the original config
			_db.DeletePitById(start.Item2.Id);
			Assert.IsFalse(File.Exists(startPath));
			Assert.IsNull(_db.GetPitById(start.Item2.Id));

			// check that the config was copied
			var saved = PitDatabase.LoadPitConfig(finish.Item2.Path);
			Assert.AreEqual("SomeMiscVariable", saved.Config[0].Key);
			Assert.AreEqual("Foo Bar Baz", saved.Config[0].Value);
		}
	}
}
