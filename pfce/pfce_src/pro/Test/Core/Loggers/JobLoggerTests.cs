using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using NLog;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Publishers;
using Peach.Core.Test;
using Peach.Pro.Core;
using Peach.Pro.Core.License;
using Peach.Pro.Core.Loggers;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices.Models;
using FaultSummary = Peach.Pro.Core.WebServices.Models.FaultSummary;
using Logger = NLog.Logger;

namespace Peach.Pro.Test.Core.Loggers
{
	[TestFixture]
	[Quick]
	[Peach]
	class JobLoggerTests
	{
		TempDirectory _tmpDir;

		[SetUp]
		public void SetUp()
		{
			_tmpDir = new TempDirectory();

			Configuration.LogRoot = _tmpDir.Path;
		}

		[TearDown]
		public void TearDown()
		{
			_tmpDir.Dispose();
		}

		const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String value='Hello World' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default' faultWaitTime='0' controlIteration='2'>
		<StateModel ref='SM' />
		<Publisher class='Null' />
		<Logger class='File' />
		<Mutators mode='include'>
			<Mutator class='StringCaseRandom' />
		</Mutators>
	</Test>
</Peach>";

		void InitializeLicense(Peach.Core.Dom.Dom dom, RunConfiguration cfg)
		{
			var license = new Mock<ILicense>();
			license.Setup(x => x.CanUseMonitor("你好RandoFaulter")).Returns(true);
			var jobLicense = new Mock<IJobLicense>();

			var test = dom.tests["Default"];
			var jobLogger = test.loggers.OfType<JobLogger>().Single();
			jobLogger.Initialize(cfg, license.Object, jobLicense.Object);
		}

		[Test]
		public void TestRelativePaths()
		{
			// Job's Log path should be rooted
			// Fault's path should be relative to the Job's 
			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration
			{
				range = true,
				rangeStart = 1,
				rangeStop = 5,
				pitFile = "TestDirectoryExists",
			};
			var e = new Engine(null);

			e.IterationStarting += (ctx, it, tot) =>
			{
				if (!ctx.controlIteration && ctx.currentIteration == 3)
				{
					ctx.faults.Add(new Fault
					{
						title = "InjectedFault",
						folderName = "InjectedFault",
						description = "InjectedFault",
						type = FaultType.Fault
					});
				}
			};

			e.startFuzzing(dom, cfg);

			Job job;

			using (var db = new NodeDatabase())
			{
				var jobs = db.LoadTable<Job>().ToList();

				Assert.AreEqual(1, jobs.Count);

				job = jobs[0];
			}

			Assert.True(Path.IsPathRooted(job.LogPath), "Job's LogPath should be absolute");

			using (var db = new JobDatabase(job.DatabasePath))
			{
				var faults = db.LoadTable<FaultDetail>().ToList();

				Assert.Greater(faults.Count, 0);

				foreach (var fault in faults)
				{
					Assert.True(!Path.IsPathRooted(fault.FaultPath), "Fault's FaultPath should be relative");

					var detail = db.GetFaultById(fault.Id, NameKind.Machine);
					Assert.NotNull(detail);
					Assert.Greater(detail.Files.Count, 0);

					foreach (var file in detail.Files)
					{
						var fullPath = Path.Combine(job.LogPath, fault.FaultPath, file.FullName);
						Assert.True(File.Exists(fullPath), "File '{0}' should exist!".Fmt(fullPath));
					}
				}
			}
		}

		[Test]
		public void TestDirectoryExists1()
		{
			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration
			{
				range = true,
				rangeStart = 1,
				rangeStop = 5,
				pitFile = "TestDirectoryExists",
			};
			var e = new Engine(null);
			var history = new List<string>();

			Job job = null;

			e.IterationStarting += (ctx, it, tot) =>
			{
				if (ctx.controlRecordingIteration)
				{
					using (var db = new NodeDatabase())
						job = db.GetJob(ctx.config.id);
				}

				var fault = "";

				if (!ctx.reproducingFault && ctx.currentIteration == 3)
				{
					ctx.faults.Add(new Fault
					{
						title = "InjectedFault",
						folderName = "InjectedFault",
						description = "InjectedFault",
						type = FaultType.Fault
					});

					fault = " Fault";
				}

				history.Add("{0} {1} {2}{3}".Fmt(
					ctx.reproducingFault ? "*" : " ",
					ctx.controlRecordingIteration ? "R" : (ctx.controlIteration ? "C" : " "),
					ctx.currentIteration,
					fault));
			};

			e.startFuzzing(dom, cfg);

			var expected = new[]
			{
				"  R 1",
				"    1",
				"    2",
				"  C 3 Fault",
				"* C 3",
				"*   1",
				"* C 2",
				"*   2",
				"* C 3",
				"    3 Fault",
				"* C 3",
				"*   3",
				"* C 3",
				"    4",
				"  C 5",
				"    5"
			};

			Assert.That(history, Is.EqualTo(expected));

			Assert.NotNull(job);

			using (var db = new JobDatabase(job.DatabasePath))
			{
				var faults = db.LoadTable<FaultDetail>().ToList();

				Assert.AreEqual(2, faults.Count);

				Assert.AreEqual(false, faults[0].Reproducible);
				Assert.AreEqual(Path.Combine("NonReproducible", "InjectedFault", "3C"), faults[0].FaultPath);
				Assert.AreEqual(false, faults[1].Reproducible);
				Assert.AreEqual(Path.Combine("NonReproducible", "InjectedFault", "3C_1"), faults[1].FaultPath);
			}
		}

		[Test]
		public void TestDirectoryExists2()
		{
			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration
			{
				range = true,
				rangeStart = 1,
				rangeStop = 5,
				pitFile = "TestDirectoryExists",
			};
			var e = new Engine(null);
			var history = new List<string>();

			Job job = null;

			e.IterationStarting += (ctx, it, tot) =>
			{
				if (ctx.controlRecordingIteration)
				{
					using (var db = new NodeDatabase())
						job = db.GetJob(ctx.config.id);
				}

				var fault = "";

				if ((!ctx.reproducingFault && ctx.currentIteration == 3 && !ctx.controlIteration) ||
					(ctx.reproducingFault && ctx.currentIteration == 3 && ctx.controlIteration && ctx.reproducingIterationJumpCount > 1) ||
					(!ctx.reproducingFault && ctx.currentIteration == 4 && !ctx.controlIteration))
				{
					ctx.faults.Add(new Fault
					{
						title = "InjectedFault",
						folderName = "InjectedFault",
						description = "InjectedFault",
						type = FaultType.Fault
					});

					fault = " Fault";
				}

				history.Add("{0} {1} {2}{3}".Fmt(
					ctx.reproducingFault ? "*" : " ",
					ctx.controlRecordingIteration ? "R" : (ctx.controlIteration ? "C" : " "),
					ctx.currentIteration,
					fault));
			};

			e.startFuzzing(dom, cfg);

			var expected = new[]
			{
				"  R 1",
				"    1",
				"    2",
				"  C 3",
				"    3 Fault",
				"* C 3",
				"*   3",
				"* C 3",
				"*   1",
				"* C 2",
				"*   2",
				"* C 3 Fault",
				"  C 4",
				"    4 Fault",
				"* C 4",
				"*   4",
				"* C 4",
				"  C 5",
				"    5"
			};

			Assert.That(history, Is.EqualTo(expected));

			Assert.NotNull(job);

			using (var db = new JobDatabase(job.DatabasePath))
			{
				var faults = db.LoadTable<FaultDetail>().ToList();

				Assert.AreEqual(2, faults.Count);

				Assert.AreEqual(true, faults[0].Reproducible);
				Assert.AreEqual(IterationFlags.Control, faults[0].Flags);
				Assert.AreEqual(Path.Combine("Faults", "InjectedFault", "3C"), faults[0].FaultPath);
				Assert.AreEqual(false, faults[1].Reproducible);
				Assert.AreEqual(IterationFlags.Control, faults[1].Flags);
				Assert.AreEqual(Path.Combine("NonReproducible", "InjectedFault", "4C"), faults[1].FaultPath);
			}
		}

		[Test]
		public void TestDirectoryExists3()
		{
			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration
			{
				range = true,
				rangeStart = 1,
				rangeStop = 5,
				pitFile = "TestDirectoryExists",
			};
			var e = new Engine(null);
			var history = new List<string>();

			Job job = null;

			e.IterationStarting += (ctx, it, tot) =>
			{
				if (ctx.controlRecordingIteration)
				{
					// Create a dupplicate file on disk that will collide with
					// a file that the job logger will try and save
					using (var db = new NodeDatabase())
					{
						job = db.GetJob(ctx.config.id);
						var faultDir = Path.Combine(job.LogPath, "Faults", "InjectedFault", "3");
						Directory.CreateDirectory(faultDir);
						File.WriteAllText(Path.Combine(faultDir, "1.Initial.Action.bin"), "");
					}
				}

				var fault = "";

				if (ctx.currentIteration == 3 && !ctx.controlIteration)
				{
					ctx.faults.Add(new Fault
					{
						title = "InjectedFault",
						folderName = "InjectedFault",
						description = "InjectedFault",
						type = FaultType.Fault
					});

					fault = " Fault";
				}

				history.Add("{0} {1} {2}{3}".Fmt(
					ctx.reproducingFault ? "*" : " ",
					ctx.controlRecordingIteration ? "R" : (ctx.controlIteration ? "C" : " "),
					ctx.currentIteration,
					fault));
			};

			e.startFuzzing(dom, cfg);

			var expected = new[]
			{
				"  R 1",
				"    1",
				"    2",
				"  C 3",
				"    3 Fault",
				"* C 3",
				"*   3 Fault",
				"  C 4",
				"    4",
				"  C 5",
				"    5"
			};

			Assert.That(history, Is.EqualTo(expected));

			Assert.NotNull(job);

			using (var db = new JobDatabase(job.DatabasePath))
			{
				var faults = db.LoadTable<FaultDetail>().ToList();

				Assert.AreEqual(1, faults.Count);

				var f = faults[0];
				Assert.AreEqual(true, f.Reproducible);
				Assert.AreEqual(Path.Combine("Faults", "InjectedFault", "3_1"), f.FaultPath);
			}
		}

		[Test]
		public void TestNoReproRecord()
		{
			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration
			{
				range = true,
				rangeStart = 1,
				rangeStop = 5,
				pitFile = "TestDirectoryExists",
			};
			var e = new Engine(null);
			var history = new List<string>();

			Job job = null;

			e.IterationStarting += (ctx, it, tot) =>
			{
				var fault = "";

				if (ctx.controlRecordingIteration)
				{
					// Create a dupplicate file on disk that will collide with
					// a file that the job logger will try and save
					using (var db = new NodeDatabase())
					{
						job = db.GetJob(ctx.config.id);
						var faultDir = Path.Combine(job.LogPath, "Faults", "InjectedFault", "3");
						Directory.CreateDirectory(faultDir);
						File.WriteAllText(Path.Combine(faultDir, "1.Initial.Action.bin"), "");
					}

					if (!ctx.reproducingFault)
					{
						ctx.faults.Add(new Fault
						{
							title = "InjectedFault",
							folderName = "InjectedFault",
							description = "InjectedFault",
							type = FaultType.Fault
						});

						fault = " Fault";
					}
				}

				history.Add("{0} {1} {2}{3}".Fmt(
					ctx.reproducingFault ? "*" : " ",
					ctx.controlRecordingIteration ? "R" : (ctx.controlIteration ? "C" : " "),
					ctx.currentIteration,
					fault));
			};

			e.startFuzzing(dom, cfg);

			var expected = new[]
			{
				"  R 1 Fault",
				"* R 1",
				"    1",
				"    2",
				"  C 3",
				"    3",
				"    4",
				"  C 5",
				"    5"
			};

			Assert.That(history, Is.EqualTo(expected));

			Assert.NotNull(job);

			using (var db = new JobDatabase(job.DatabasePath))
			{
				var faults = db.LoadTable<FaultDetail>().ToList();

				Assert.AreEqual(1, faults.Count);

				var f = faults[0];
				Assert.AreEqual(false, f.Reproducible);
				Assert.AreEqual(IterationFlags.Record | IterationFlags.Control, f.Flags);
				Assert.AreEqual(Path.Combine("NonReproducible", "InjectedFault", "1R"), f.FaultPath);
			}
		}

		[Test]
		public void TestMetricKindMachine()
		{
			const string xml = @"
<Peach>
	<!-- Only fieldIds on elements used by the state model effect Job.MetricKind -->
	<DataModel name='Unused'>
		<String fieldId='Test' />
	</DataModel>

	<DataModel name='DM'>
		<String value='Hello World'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default' faultWaitTime='0' controlIteration='2'>
		<StateModel ref='SM' />
		<Publisher class='Null' />
		<Logger class='File' />
	</Test>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration { singleIteration = true, pitFile = "test" };

			var e = new Engine(null);

			Job job = null;

			e.IterationStarting += (ctx, it, tot) =>
			{
				// Job.MetricKind is set in TestStarting so ensure it is correct
				// by the first call to IterationStarting
				using (var db = new NodeDatabase())
				{
					job = db.GetJob(cfg.id);
				}

			};

			e.startFuzzing(dom, cfg);

			Assert.NotNull(job);
			Assert.AreEqual(NameKind.Machine, job.MetricKind);
		}

		[Test]
		public void TestMetricKindHuman()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String value='Hello World' fieldId='Test' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default' faultWaitTime='0' controlIteration='2'>
		<StateModel ref='SM' />
		<Publisher class='Null' />
		<Logger class='File' />
	</Test>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration { singleIteration = true, pitFile = "test" };

			var e = new Engine(null);

			Job job = null;

			e.IterationStarting += (ctx, it, tot) =>
			{
				// Job.MetricKind is set in TestStarting so ensure it is correct
				// by the first call to IterationStarting
				using (var db = new NodeDatabase())
				{
					job = db.GetJob(cfg.id);
				}

			};

			e.startFuzzing(dom, cfg);

			Assert.NotNull(job);
			Assert.AreEqual(NameKind.Human, job.MetricKind);
		}

		[Test]
		public void TestBadUnicodeDescription()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String value='Hello World' fieldId='Test' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default' faultWaitTime='0'>
		<StateModel ref='SM' />
		<Publisher class='Null' />
		<Logger class='File' />
	</Test>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration
			{
				range = true,
				rangeStart = 1,
				rangeStop = 1,
				pitFile = "Test"
			};

			var e = new Engine(null);

			e.IterationStarting += (ctx, it, tot) =>
			{
				if (ctx.controlIteration)
					return;

				ctx.Fault("Title", "Desc\xd800", "MajorHash", "MinorHash", "Exploitability", "Source");
			};

			e.startFuzzing(dom, cfg);

			using (var db = new NodeDatabase())
			{
				var job = db.GetJob(cfg.id);
				Assert.NotNull(job);
				Assert.AreEqual(1, job.FaultCount);

				using (var jobDb = new JobDatabase(job.DatabasePath))
				{
					var faults = jobDb.LoadTable<FaultDetail>().ToList();

					Assert.AreEqual(1, faults.Count);
					Assert.AreEqual("Desc�", faults[0].Description);
				}
			}
		}

		[Test]
		public void TestTwoCoreFaults()
		{
			const string pit = @"
<Peach>
	<DataModel name='DM'>
		<String name='Value' value='Hello' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action name='act1' type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Agent name='Agent1'>
		<Monitor name='mon1' class='你好RandoFaulter'>
			<Param name='Fault' value='1' />
		</Monitor>
		<Monitor name='mon2' class='你好RandoFaulter'>
			<Param name='Fault' value='1' />
		</Monitor>
	</Agent>

	<Test name='Default' faultWaitTime='0'>
		<Publisher class='Null'/>
		<StateModel ref='SM'/>
		<Logger class='File' />
		<Agent ref='Agent1' />
	</Test>
</Peach>";

			var dom = DataModelCollector.ParsePit(pit);
			dom.tests[0].publishers[0] = new TestPub();

			var config = new RunConfiguration
			{
				range = true,
				rangeStart = 1,
				rangeStop = 1,
				pitFile = "TestTwoCoreFaults"
			};

			InitializeLicense(dom, config);

			var e = new Engine(null);

			e.IterationStarting += (ctx, it, tot) =>
			{
				ctx.agentManager.Message(ctx.controlIteration.ToString());

				if (!ctx.controlIteration)
					ctx.InjectFault();
			};

			e.Fault += (ctx, it, sm, faults) =>
			{
				foreach (var f in faults)
				{
					f.title = f.monitorName + ": " + f.title;
				}
			};

			e.startFuzzing(dom, config);

			Job job;

			using (var db = new NodeDatabase())
			{
				job = db.GetJob(config.id);
			}

			Assert.AreEqual(1, job.FaultCount);

			using (var db = new JobDatabase(job.DatabasePath))
			{
				var faults = db.LoadTable<FaultSummary>().ToList();

				Assert.AreEqual(job.FaultCount, faults.Count);

				var f = db.GetFaultById(faults[0].Id, NameKind.Machine);

				Assert.AreEqual("mon1: 你好 from RandoFaulter", f.Title);
			}
		}

		[Test]
		public void TestFaultFile()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='Value' value='Hello' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action name='act1' type='output'>
				<DataModel ref='DM'/>
			</Action>
			<Action name='act2' type='input'>
				<DataModel ref='DM'/>
			</Action>
			<Action name='act3' type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Agent name='Agent1'>
		<Monitor class='你好RandoFaulter'>
			<Param name='Fault' value='-1' />
		</Monitor>
	</Agent>

	<Test name='Default' faultWaitTime='0'>
		<Publisher class='Null'/>
		<StateModel ref='SM'/>
		<Logger class='File' />
		<Agent ref='Agent1' />
	</Test>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			dom.tests[0].publishers[0] = new TestPub();

			var config = new RunConfiguration
			{
				range = true,
				rangeStart = 1,
				rangeStop = 3,
				pitFile = "LoggerTest"
			};

			InitializeLicense(dom, config);

			var e = new Engine(null);

			e.IterationStarting += (ctx, it, tot) =>
			{
				if (!ctx.controlIteration && it == 2)
					ctx.InjectFault();
			};

			e.startFuzzing(dom, config);

			Job job;

			using (var db = new NodeDatabase())
			{
				job = db.GetJob(config.id);
			}

			Assert.AreEqual(1, job.FaultCount);

			using (var db = new JobDatabase(job.DatabasePath))
			{
				var faults = db.LoadTable<FaultSummary>().ToList();

				Assert.AreEqual(job.FaultCount, faults.Count);

				var f = db.GetFaultById(faults[0].Id, NameKind.Machine);

				var files = f.Files.ToList();

				// When only DetectionSource is specified, the logger can't provide
				// AgentName/MonitorClass/MonitorName grouping for the asset

				var expected = new[]
				{
					new SavedFile("1.Initial.act1.bin",
						null,
						null,
						null,
						"Initial.act1",
						false, FaultFileType.Ouput),
					new SavedFile("2.Initial.act2.bin",
						null,
						null,
						null,
						"Initial.act2",
						false, FaultFileType.Input),
					new SavedFile("3.Initial.act3.bin",
						null,
						null,
						null,
						"Initial.act3",
						false, FaultFileType.Ouput),
					new SavedFile("Agent1.Monitor.你好RandoFaulter.description.txt",
						"Agent1",
						"你好RandoFaulter",
						"Monitor",
						"description.txt",
						false, FaultFileType.Asset),
					new SavedFile("Agent1.Monitor.你好RandoFaulter.NetworkCapture1.pcap",
						"Agent1",
						"你好RandoFaulter",
						"Monitor",
						"NetworkCapture1.pcap",
						false, FaultFileType.Asset),
					new SavedFile("Agent1.Monitor.你好RandoFaulter.NetworkCapture2.pcapng",
						"Agent1",
						"你好RandoFaulter",
						"Monitor",
						"NetworkCapture2.pcapng",
						false, FaultFileType.Asset),
					new SavedFile("Agent1.Monitor.你好RandoFaulter.BinaryData.bin",
						"Agent1",
						"你好RandoFaulter",
						"Monitor",
						"BinaryData.bin",
						false, FaultFileType.Asset),
					new SavedFile("Agent1.Monitor.你好RandoFaulter.機除拍禁響地章手棚国歳違不.pcap",
						"Agent1",
						"你好RandoFaulter",
						"Monitor",
						"機除拍禁響地章手棚国歳違不.pcap",
						false, FaultFileType.Asset),
					new SavedFile("UnitTest.description.txt",
						null,
						null,
						null,
						"UnitTest.description.txt",
						false, FaultFileType.Asset),
					new SavedFile("fault.json",
						null,
						null,
						null,
						"fault.json",
						false, FaultFileType.Asset),
					new SavedFile("Initial\\2\\1.Initial.act1.bin",
						null,
						null,
						null,
						"Initial.act1",
						true, FaultFileType.Ouput),
					new SavedFile("Initial\\2\\2.Initial.act2.bin",
						null,
						null,
						null,
						"Initial.act2",
						true, FaultFileType.Input),
					new SavedFile("Initial\\2\\3.Initial.act3.bin",
						null,
						null,
						null,
						"Initial.act3",
						true, FaultFileType.Ouput),
					new SavedFile("Initial\\2\\Agent1.Monitor.你好RandoFaulter.description.txt",
						"Agent1",
						"你好RandoFaulter",
						"Monitor",
						"description.txt",
						true, FaultFileType.Asset),
					new SavedFile("Initial\\2\\Agent1.Monitor.你好RandoFaulter.NetworkCapture1.pcap",
						"Agent1",
						"你好RandoFaulter",
						"Monitor",
						"NetworkCapture1.pcap",
						true, FaultFileType.Asset),
					new SavedFile("Initial\\2\\Agent1.Monitor.你好RandoFaulter.NetworkCapture2.pcapng",
						"Agent1",
						"你好RandoFaulter",
						"Monitor",
						"NetworkCapture2.pcapng",
						true, FaultFileType.Asset),
					new SavedFile("Initial\\2\\Agent1.Monitor.你好RandoFaulter.BinaryData.bin",
						"Agent1",
						"你好RandoFaulter",
						"Monitor",
						"BinaryData.bin",
						true, FaultFileType.Asset),
					new SavedFile("Initial\\2\\Agent1.Monitor.你好RandoFaulter.機除拍禁響地章手棚国歳違不.pcap",
						"Agent1",
						"你好RandoFaulter",
						"Monitor",
						"機除拍禁響地章手棚国歳違不.pcap",
						true, FaultFileType.Asset),
					new SavedFile("Initial\\2\\UnitTest.description.txt",
						null,
						null,
						null,
						"UnitTest.description.txt",
						true, FaultFileType.Asset),
					new SavedFile("Initial\\2\\fault.json",
						null,
						null,
						null,
						"fault.json",
						true, FaultFileType.Asset),
				};


				Assert.AreEqual(expected.Length, files.Count);

				for (var i = 0; i < expected.Length; ++i)
				{
					Assert.AreEqual(expected[i].FullName.Replace('\\', Path.DirectorySeparatorChar), files[i].FullName);
					Assert.AreEqual(expected[i].AgentName, files[i].AgentName);
					Assert.AreEqual(expected[i].MonitorClass, files[i].MonitorClass);
					Assert.AreEqual(expected[i].MonitorName, files[i].MonitorName);
					Assert.AreEqual(expected[i].Name, files[i].Name);
					Assert.AreEqual(expected[i].Initial, files[i].Initial);
					Assert.AreEqual(expected[i].Type, files[i].Type);
				}
			}
		}

		class SavedFile
		{
			public readonly string FullName;
			public readonly string AgentName;
			public readonly string MonitorClass;
			public readonly string MonitorName;
			public readonly string Name;
			public readonly bool Initial;
			public readonly FaultFileType Type;

			public SavedFile(string fullName,
			                 string agentName,
			                 string monitorClass,
			                 string monitorName,
			                 string name,
			                 bool initial,
			                 FaultFileType type)
			{
				FullName = fullName;
				AgentName = agentName;
				MonitorClass = monitorClass;
				MonitorName = monitorName;
				Name = name;
				Initial = initial;
				Type = type;
			}
		}

		[Test]
		public void TestSaveActionData()
		{
			string xml = @"
<Peach>
	<DataModel name='CallModel'>
		<String name='Value' value='Hello' mutable='false'/>
	</DataModel>

	<DataModel name='RequestModel'>
		<Number name='Sequence' size='8'/>
		<String name='Method' length='1'/>
	</DataModel>

	<DataModel name='XModel'>
		<Number name='Sequence' size='8'/>
		<String name='Response' value='X Response'/>
	</DataModel>

	<DataModel name='YModel'>
		<Number name='Sequence' size='8'/>
		<String name='Response' value='Y Response'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='open'/>

			<Action name='DoCall' type='call' method='foo'>
				<Param>
					<DataModel ref='CallModel'/>
				</Param>
				<Param name='MyParam2'>
					<DataModel ref='CallModel'/>
				</Param>
				<Param name='MyParam3' type='inout'>
					<DataModel ref='CallModel'/>
					<Data>
						<Field name='Value' value='inout'/>
					</Data>
				</Param>
				<Param name='MyParam4' type='out'>
					<DataModel ref='CallModel'/>
				</Param>
				<Result>
					<DataModel ref='CallModel'/>
				</Result>
			</Action>

			<Action type='changeState' ref='Request'/>
		</State>

		<State name='Request'>
			<Action name='RecvReq' type='input'>
				<DataModel ref='RequestModel'/>
			</Action>

			<Action type='changeState' 
				ref='XResponse' 
				when=""str(getattr(StateModel.states['Request'].actions[0].dataModel.find('Method'), 'DefaultValue', None)) == 'X'"" />

			<Action type='changeState' 
				ref='YResponse' 
				when=""str(getattr(StateModel.states['Request'].actions[0].dataModel.find('Method'), 'DefaultValue', None)) == 'Y'"" />
		</State>

		<State name='XResponse'>
			<Action type='slurp' 
				valueXpath='//Request//RecvReq//RequestModel//Sequence' setXpath='//Sequence' />

			<Action type='output' name='OutputX'>
				<DataModel ref='XModel' />
			</Action>

			<Action type='changeState' ref='Request' />
		</State>

		<State name='YResponse'>
			<Action type='slurp' 
				valueXpath='//Request//RecvReq//RequestModel//Sequence' setXpath='//Sequence' />

			<Action type='output' name='OutputY'>
				<DataModel ref='YModel' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default' faultWaitTime='0'>
		<Publisher class='Null'/>
		<StateModel ref='SM'/>
		<Logger class='File'>
			<Param name='Path' value='{0}'/>
		</Logger>
	</Test>
</Peach>".Fmt(Configuration.LogRoot);

			var dom = DataModelCollector.ParsePit(xml);
			dom.tests[0].publishers[0] = new TestPub();

			var config = new RunConfiguration
			{
				range = true,
				rangeStart = 1,
				rangeStop = 2,
				pitFile = "LoggerTest"
			};

			var e = new Engine(null);

			e.TestStarting += ctx =>
			{
				e.Fault += e_Fault;
				e.ReproFault += e_ReproFault;
			};

			e.IterationStarting += (ctx, it, tot) =>
			{
				if (!ctx.controlIteration && it == 2)
					ctx.InjectFault();
			};

			e.startFuzzing(dom, config);
		}

		void e_ReproFault(RunContext context, uint currentIteration, Peach.Core.Dom.StateModel stateModel, Fault[] faultData)
		{
			VerifyFaults("Reproducing", context, currentIteration);
		}

		void e_Fault(RunContext context, uint currentIteration, Peach.Core.Dom.StateModel stateModel, Fault[] faultData)
		{
			VerifyFaults("Faults", context, currentIteration);
		}

		void VerifyFaults(string dir, RunContext context, uint currentIteration)
		{
			var pub = context.test.publishers[0] as TestPub;
			Assert.NotNull(pub);
			Assert.AreEqual(6, pub.outputs.Count);

			var logger = context.test.loggers[0] as JobLogger;
			Assert.NotNull(logger);

			var subdir = Directory.EnumerateDirectories(logger.BasePath).FirstOrDefault();
			Assert.NotNull(subdir);

			var fullPath = Path.Combine(logger.BasePath, subdir, dir, "UnitTest", currentIteration.ToString());

			var files = Directory.EnumerateFiles(fullPath, "*.bin").ToList();
			Assert.AreEqual(12, files.Count);

			var actual = File.ReadAllBytes(Path.Combine(fullPath, "1.Initial.DoCall.Param.In.bin"));
			Assert.AreEqual(actual, pub.outputs[0]);

			actual = File.ReadAllBytes(Path.Combine(fullPath, "2.Initial.DoCall.MyParam2.In.bin"));
			Assert.AreEqual(actual, pub.outputs[1]);

			// In half of param
			actual = File.ReadAllBytes(Path.Combine(fullPath, "3.Initial.DoCall.MyParam3.In.bin"));
			Assert.AreEqual(actual, Encoding.ASCII.GetBytes("inout"));
			Assert.AreEqual(actual, pub.outputs[2]);

			// Out half of param
			actual = File.ReadAllBytes(Path.Combine(fullPath, "4.Initial.DoCall.MyParam3.Out.bin"));
			Assert.AreEqual(actual, Encoding.ASCII.GetBytes("MyParam3"));

			// Out param
			actual = File.ReadAllBytes(Path.Combine(fullPath, "5.Initial.DoCall.MyParam4.Out.bin"));
			Assert.AreEqual(actual, Encoding.ASCII.GetBytes("MyParam4"));

			actual = File.ReadAllBytes(Path.Combine(fullPath, "6.Initial.DoCall.Result.bin"));
			Assert.AreEqual(actual, Encoding.ASCII.GetBytes("Result!"));

			actual = File.ReadAllBytes(Path.Combine(fullPath, "7.Request.RecvReq.bin"));
			Assert.AreEqual(new byte[] { 1, (byte)'X' }, actual);

			actual = File.ReadAllBytes(Path.Combine(fullPath, "8.XResponse.OutputX.bin"));
			Assert.AreEqual(pub.outputs[3], actual);

			actual = File.ReadAllBytes(Path.Combine(fullPath, "9.Request.RecvReq.bin"));
			Assert.AreEqual(new byte[] { 2, (byte)'X' }, actual);

			actual = File.ReadAllBytes(Path.Combine(fullPath, "10.XResponse.OutputX.bin"));
			Assert.AreEqual(pub.outputs[4], actual);

			actual = File.ReadAllBytes(Path.Combine(fullPath, "11.Request.RecvReq.bin"));
			Assert.AreEqual(new byte[] { 3, (byte)'Y' }, actual);

			actual = File.ReadAllBytes(Path.Combine(fullPath, "12.YResponse.OutputY.bin"));
			Assert.AreEqual(pub.outputs[5], actual);
		}

		class TestPub : StreamPublisher
		{
			private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

			protected override Logger Logger { get { return logger; } }

			private int cnt;

			public readonly List<byte[]> outputs = new List<byte[]>();

			public TestPub()
				: base(new Dictionary<string, Variant>())
			{
				Name = "Pub";
				stream = new MemoryStream();
			}

			protected override void OnOpen()
			{
				cnt = 0;
				outputs.Clear();
			}

			protected override void OnInput()
			{
				++cnt;

				stream.SetLength(0);
				stream.WriteByte((byte)cnt);

				if (cnt == 3)
					stream.WriteByte((byte)'Y');
				else
					stream.WriteByte((byte)'X');

				stream.Position = 0;
			}

			protected override void OnOutput(BitwiseStream data)
			{
				outputs.Add(data.ToArray());
			}

			protected override Variant OnCall(string method, List<ActionParameter> args)
			{
				foreach (var item in args)
				{
					if (item.type != ActionParameter.Type.Out)
						outputs.Add(item.dataModel.Value.ToArray());
					if (item.type != ActionParameter.Type.In)
						item.Crack(new BitStream(Encoding.ASCII.GetBytes(item.Name)));
				}
				return new Variant(new BitStream(Encoding.ASCII.GetBytes("Result!")));
			}
		}
	}
}