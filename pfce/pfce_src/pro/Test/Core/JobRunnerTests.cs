using System;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.Test.Core.Storage;
using TestStatus = Peach.Pro.Core.WebServices.Models.TestStatus;
using Peach.Pro.Core.WebServices;
using System.Collections.Generic;
using NLog;
using NLog.Config;
using NLog.Targets;
using MAgent = Peach.Pro.Core.WebServices.Models.Agent;
using MMonitor = Peach.Pro.Core.WebServices.Models.Monitor;
using Moq;
using Peach.Pro.Core.License;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Peach]
	[Quick]
	class JobRunnerTests
	{
		TempDirectory _tmpDir;
		LoggingConfiguration _loggingConfig;
		string _pitXmlPath;
		string _pitXmlFailPath;

		const string PitXml =
			@"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action name='output1' type='output'>
				<DataModel name='DM'>
					<String name='off' />
					<String name='lowest' />
					<String name='low' />
					<String name='normal' />
					<String name='high' />
					<String name='highest' />
				</DataModel>
			</Action>
			<Action name='output2' type='output'>
				<DataModel name='DM'>
					<Block name='array' occurs='1'>
						<String name='off' />
						<String name='lowest' />
						<String name='low' />
						<String name='normal' />
						<String name='high' />
						<String name='highest' />
					</Block>
				</DataModel>
			</Action>
		</State>
	</StateModel>

	<Test name='Default' maxOutputSize='##MaxOutputSize##'>
		<StateModel ref='StateModel' />
		<Publisher class='Null'/>
		<Strategy class='Random'>
			<Param name='MaxFieldsToMutate' value='1' />
		</Strategy>
	</Test>
</Peach>
";

		const string PitXmlFail = "xxx" + PitXml;

		static readonly PitConfig PitDefault = new PitConfig
		{
			OriginalPit = "Test.xml",
			Config = new List<Param>
			{
				new Param { Key = "MaxOutputSize", Value = "100" }
			},
			Agents = new List<MAgent>(),
			Weights = new List<PitWeight>(),
		};

		static readonly PitConfig PitFail = new PitConfig
		{
			OriginalPit = "TestFail.xml",
			Config = new List<Param>
			{
				new Param { Key = "MaxOutputSize", Value = "100" }
			},
			Agents = new List<MAgent>(),
			Weights = new List<PitWeight>(),
		};

		static readonly PitConfig PitWithMonitors = new PitConfig 
		{
			OriginalPit = "Test.xml",
			Config = new List<Param>
			{
				new Param { Key = "MaxOutputSize", Value = "100" }
			},
			Agents = new List<MAgent>
			{
				new MAgent()
				{
					AgentUrl = "local://",
					Monitors = new List<MMonitor>
					{
						new MMonitor()
						{
							MonitorClass = "你好RandoFaulter",
							Map = new List<Param>
							{
								new Param
								{
									Key = "Fault",
									Value = "-1"
								}
							}
						}
					}
				}
			},
			Weights = new List<PitWeight>(),
		};

		[SetUp]
		public void SetUp()
		{
			_tmpDir = new TempDirectory();
			Configuration.LogRoot = _tmpDir.Path;

			_pitXmlPath = Path.Combine(_tmpDir.Path, "Test.xml");
			File.WriteAllText(_pitXmlPath, PitXml);

			_pitXmlFailPath = Path.Combine(_tmpDir.Path, "TestFail.xml");
			File.WriteAllText(_pitXmlFailPath, PitXmlFail);

			_loggingConfig = LogManager.Configuration;

			var target = new ColoredConsoleTarget
			{
				Layout = "${time} ${logger} ${message} ${exception:format=tostring}"
			};

			var config = new LoggingConfiguration();
			var rule = new LoggingRule("*", LogLevel.Debug, target);
			config.AddTarget("console", target);
			config.LoggingRules.Add(rule);
			LogManager.Configuration = config;
		}

		[TearDown]
		public void TearDown()
		{
			_tmpDir.Dispose();

			LogManager.Configuration = _loggingConfig;
		}

		class SafeRunner : IDisposable
		{
			readonly Job _job;

			public JobRunner JobRunner { get; private set; }

			readonly Thread _thread;
			Exception _caught;
			readonly AutoResetEvent _evtReady = new AutoResetEvent(false);

			public SafeRunner(
				string pitLibraryPath, 
				PitConfig pitConfig, 
				JobRequest jobRequest, 
				Action<Engine> hooker = null,
				Mock<ILicense> license = null)
			{
				var pitPath = Path.Combine(pitLibraryPath, "Test.peach");
				PitDatabase.SavePitConfig(pitPath, pitConfig);

				if (license == null)
					license = new Mock<ILicense>();

				_job = new Job(jobRequest, pitPath);
				JobRunner = new JobRunner(license.Object, _job, pitLibraryPath, pitPath);
				_thread = new Thread(() =>
				{
					try
					{
						JobRunner.Run(_evtReady, hooker);
					}
					catch (Exception ex)
					{
						if (ex.GetBaseException() is ThreadAbortException)
							Thread.ResetAbort();
						_caught = ex;
					}
				});
				_thread.Start();
				if (!_evtReady.WaitOne(1000))
					throw new PeachException("Timeout waiting for job to start");
			}

			public Guid Id { get { return _job.Guid; } }

			public void WaitUntil(params JobStatus[] status)
			{
				Console.WriteLine("WaitUntil({0})", string.Join(",", status));

				// waits up to 20 seconds
				for (var i = 0; i < 40; i++)
				{
					var job = GetJob();
					Assert.IsNotNull(job);
					if (status.Contains(job.Status))
						return;

					Thread.Sleep(500);
				}
				Console.WriteLine("Timeout");
				Assert.Fail("Timeout");
			}

			public void WaitForFinish(int timeout = 20)
			{
				Console.WriteLine("Waiting for finish");
				var ret = _thread.Join(TimeSpan.FromSeconds(timeout));
				Console.WriteLine("Done");
				Assert.IsTrue(ret, "Timeout waiting for job to finish");
				Assert.AreEqual(JobStatus.Stopped, GetJob().Status);
				if (_caught != null)
					throw new AggregateException(_caught);
			}

			public Job GetJob()
			{
				using (var db = new NodeDatabase())
				{
					var job = db.GetJob(Id);
					Assert.IsNotNull(job);
					return job;
				}
			}

			public void VerifyDatabase(int expectedLogs)
			{
				using (var db = new NodeDatabase())
				{
					var job = db.GetJob(Id);
					Assert.IsNotNull(job);

					var logs = db.GetJobLogs(job.Guid).ToList();
					Console.WriteLine("JobLog>");
					foreach (var line in logs)
					{
						Console.WriteLine(line);
					}
					Assert.AreEqual(expectedLogs, logs.Count, "JobLog mismatch");
				}
			}

			public void Dispose()
			{
				if (!_thread.Join(TimeSpan.FromSeconds(2)))
					_thread.Abort();
			}
		}

		[Test]
		public void TestBasic()
		{
			var jobRequest = new JobRequest {
				RangeStop = 1,
			};
			using (var runner = new SafeRunner(_tmpDir.Path, PitDefault, jobRequest))
			{
				runner.WaitForFinish();
				runner.VerifyDatabase(2);
			}
		}

		[Test]
		public void TestStop()
		{
			var jobRequest = new JobRequest();
			using (var runner = new SafeRunner(_tmpDir.Path, PitDefault, jobRequest))
			{
				runner.WaitUntil(JobStatus.Running);
				Console.WriteLine("Stop");
				runner.JobRunner.Stop();
				runner.WaitForFinish();
				Console.WriteLine("VerifyDatabase");
				runner.VerifyDatabase(1);
			}
		}

		[Test]
		public void TestPauseContinue()
		{
			var jobRequest = new JobRequest();
			using (var runner = new SafeRunner(_tmpDir.Path, PitDefault, jobRequest))
			{
				runner.WaitUntil(JobStatus.Running);
				runner.JobRunner.Pause();
				runner.WaitUntil(JobStatus.Paused);
				runner.JobRunner.Continue();
				runner.WaitUntil(JobStatus.Running);
				runner.JobRunner.Stop();
				runner.WaitForFinish();
				runner.VerifyDatabase(1);
			}
		}

		[Test]
		[Repeat(2)]
		public void TestPitTester()
		{
			var jobRequest = new JobRequest {
				DryRun = true,
			};
			using (var runner = new SafeRunner(_tmpDir.Path, PitDefault, jobRequest))
			{
				runner.WaitForFinish();

				using (var db = new NodeDatabase())
				{
					DatabaseTests.AssertResult(db.GetTestEventsByJob(runner.Id), new[] {
						new TestEvent(1, runner.Id, TestStatus.Pass, 
							"Loading pit file", "Loading pit file '{0}'".Fmt(_pitXmlPath), null),
						new TestEvent(2, runner.Id, TestStatus.Pass, 
							"Starting fuzzing engine", "Starting fuzzing engine", null),
						new TestEvent(3, runner.Id, TestStatus.Pass, 
							"Running iteration", "Running the initial control record iteration", null),
						new TestEvent(4, runner.Id, TestStatus.Pass, 
							"Flushing logs.", "Flushing logs.", null),
					});
				}

				var job = runner.GetJob();
				Assert.IsTrue(File.Exists(job.DebugLogPath));
				Console.Write(File.ReadAllText(job.DebugLogPath));
			}
		}

		[Test]
		public void TestPitParseFailure()
		{
			var jobRequest = new JobRequest {
				DryRun = true,
			};
			using (var runner = new SafeRunner(_tmpDir.Path, PitFail, jobRequest))
			{
				runner.WaitForFinish();

				using (var db = new NodeDatabase())
				{
					DatabaseTests.AssertResult(db.GetTestEventsByJob(runner.Id), new[] {
						new TestEvent(
							1, 
							runner.Id, 
							TestStatus.Fail, 
							"Loading pit file", 
							"Loading pit file '{0}'".Fmt(_pitXmlFailPath), 
							"Error: XML Failed to load: Data at the root level is invalid. Line 1, position 1."),
						new TestEvent(
							2, 
							runner.Id, 
							TestStatus.Pass, 
							"Flushing logs.", 
							"Flushing logs.", 
							null),
					});

					var logs = db.GetJobLogs(runner.Id).ToList();
					Assert.AreEqual(2, logs.Count, "Missing JobLogs");
					foreach (var log in logs)
						Console.WriteLine(log.Message);
				}

				var job = runner.GetJob();
				Assert.IsFalse(File.Exists(job.DebugLogPath));
			}
		}

		[Test]
		public void TestUnhandledException()
		{
			var jobRequest = new JobRequest
			{
				RangeStart = 1,
				RangeStop = 1
			};

			Action<Engine> hook = e =>
			{
				e.IterationStarting += (ctx, it, tot) =>
				{
					if (ctx.controlIteration)
						return;

					throw new InvalidOperationException("Message Goes Here");
				};
			};

			using (var runner = new SafeRunner(_tmpDir.Path, PitDefault, jobRequest, hook))
			{
				runner.WaitUntil(JobStatus.Stopped);

				var job = runner.GetJob();

				Assert.NotNull(job);
				Assert.AreEqual("Message Goes Here", job.Result);
			}
		}

		[Test]
		public void TestWithMonitorsNoLicense()
		{
			var jobRequest = new JobRequest
			{
				RangeStop = 1,
			};
			using (var runner = new SafeRunner(_tmpDir.Path, PitWithMonitors, jobRequest))
			{
				runner.WaitForFinish();
				runner.VerifyDatabase(2);

				using (var db = new NodeDatabase())
				{
					var licenseError =
						"The 你好RandoFaulter monitor is not supported with your current license. " + 
						"Contact Peach Fuzzer sales for more information.";
					DatabaseTests.AssertResult(db.GetTestEventsByJob(runner.Id), new[] {
						new TestEvent(1, runner.Id, TestStatus.Pass,
							"Loading pit file", "Loading pit file '{0}'".Fmt(_pitXmlPath), null),
						new TestEvent(2, runner.Id, TestStatus.Fail,
							"Starting fuzzing engine", "Starting fuzzing engine", licenseError),
						new TestEvent(3, runner.Id, TestStatus.Pass,
							"Connecting to agent", "Connecting to agent 'local://'", null),
						new TestEvent(4, runner.Id, TestStatus.Fail,
							"Starting monitor", "Starting monitor '你好RandoFaulter' named 'Monitor'", licenseError),
						new TestEvent(5, runner.Id, TestStatus.Pass,
							"Flushing logs.", "Flushing logs.", null),
					});
				}
			}
		}

		[Test]
		public void TestWithMonitors()
		{
			var license = new Mock<ILicense>();
			license.Setup(x => x.CanUseMonitor("你好RandoFaulter")).Returns(true);

			var jobRequest = new JobRequest
			{
				RangeStop = 1,
			};

			using (var runner = new SafeRunner(_tmpDir.Path, PitWithMonitors, jobRequest, license: license))
			{
				runner.WaitForFinish();
				runner.VerifyDatabase(2);

				using (var db = new NodeDatabase())
				{
					DatabaseTests.AssertResult(db.GetTestEventsByJob(runner.Id), new[] {
						new TestEvent(1, runner.Id, TestStatus.Pass,
							"Loading pit file", "Loading pit file '{0}'".Fmt(_pitXmlPath), null),
						new TestEvent(2, runner.Id, TestStatus.Pass,
							"Starting fuzzing engine", "Starting fuzzing engine", null),
						new TestEvent(3, runner.Id, TestStatus.Pass,
							"Connecting to agent", "Connecting to agent 'local://'", null),
						new TestEvent(4, runner.Id, TestStatus.Pass,
							"Starting monitor", "Starting monitor '你好RandoFaulter' named 'Monitor'", null),
						new TestEvent(5, runner.Id, TestStatus.Pass,
							"Starting fuzzing session", "Notifying agent 'local://' that the fuzzing session is starting", null),
						new TestEvent(6, runner.Id, TestStatus.Pass,
							"Running iteration", "Running the initial control record iteration", null),
						new TestEvent(7, runner.Id, TestStatus.Pass,
							"Flushing logs.", "Flushing logs.", null),
					});
				}
			}
		}
	}
}
