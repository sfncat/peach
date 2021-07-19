using System;
using System.IO;
using System.Linq;
using System.Threading;
using Moq;
using NLog;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core;
using Peach.Pro.Core.License;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.Test.Core.Storage;
using Logger = NLog.Logger;
using Random = System.Random;
using SysProcess = System.Diagnostics.Process;
using TestStatus = Peach.Pro.Core.WebServices.Models.TestStatus;

namespace Peach.Pro.Test.Core.WebServices
{
	class BaseJobMonitorTests
	{
		protected IJobMonitor _monitor;
		protected ManualResetEvent _doneEvt;

		protected const string PitXml =
@"<?xml version='1.0' encoding='utf-8'?>
<Peach>

	<DataModel name='DM'>
		<String value='Hello World' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel name='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null'/>
	</Test>
</Peach>
";

		protected const string PitConfig = @"
{
	'OriginalPit': 'Test.xml',
	'Config': [],
	'Agents': [],
	'Weights': []
}
";

		protected const string PitXmlFail = PitXml + "xxx";

		protected const string PitConfigFail = @"
{
	'OriginalPit': 'TestFail.xml',
	'Config': [],
	'Agents': [],
	'Weights': []
}
";
		
		protected bool WaitUntil(params JobStatus[] status)
		{
			// waits up to 20 seconds
			for (var i = 0; i < 200; i++)
			{
				var job = _monitor.GetJob();
				Assert.IsNotNull(job);
				if (status.Contains(job.Status))
					return true;

				Thread.Sleep(100);
			}
			return false;
		}

		protected void WaitForFinish()
		{
			Assert.IsTrue(_doneEvt.WaitOne(TimeSpan.FromSeconds(20)), "Timeout waiting for job to finish");
		}

		protected void VerifyDatabase(Job job, bool required = true)
		{
			using (var db = new NodeDatabase())
			{
				Assert.IsNotNull(db.GetJob(job.Guid));
			}

			if (required)
			{
				Assert.IsTrue(File.Exists(job.DatabasePath));
			}
		}
	}

	interface IJobMonitorFactory
	{
		IJobMonitor Create();
	}

	class ExternalJobMonitorFactory : IJobMonitorFactory
	{
		public IJobMonitor Create()
		{
			return new ExternalJobMonitor();
		}
	}

	class InternalJobMonitorFactory : IJobMonitorFactory
	{
		public IJobMonitor Create()
		{
			var license = new Mock<ILicense>();
			return new InternalJobMonitor(license.Object);
		}
	}

	abstract class JobMonitorTests<T> : BaseJobMonitorTests 
		where T : IJobMonitorFactory, new()
	{
		static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		TempDirectory _tmpDir;
		string _pitXmlPath;
		string _pitConfigPath;
		string _pitXmlFailPath;
		string _pitConfigFailPath;
		bool _oldAsync;
		string _oldLogRoot;

		[SetUp]
		public void SetUp()
		{
			Logger.Trace(">>> Setup");

			_tmpDir = new TempDirectory();

			_oldAsync = Configuration.UseAsyncLogging;
			_oldLogRoot = Configuration.LogRoot;

			Configuration.UseAsyncLogging = false;
			Configuration.LogRoot = _tmpDir.Path;

			_doneEvt = new ManualResetEvent(false);

			var factory = new T();
			_monitor = factory.Create();
			_monitor.InternalEvent = (s, a) =>
			{
				Logger.Trace("InternalEvent");
				_doneEvt.Set();
			};

			_pitXmlPath = Path.Combine(_tmpDir.Path, "Test.xml");
			_pitConfigPath = Path.Combine(_tmpDir.Path, "Test.pit");

			File.WriteAllText(_pitXmlPath, PitXml);
			File.WriteAllText(_pitConfigPath, PitConfig);

			_pitXmlFailPath = Path.Combine(_tmpDir.Path, "TestFail.xml");
			_pitConfigFailPath = Path.Combine(_tmpDir.Path, "TestFail.pit");

			File.WriteAllText(_pitXmlFailPath, PitXmlFail);
			File.WriteAllText(_pitConfigFailPath, PitConfigFail);

			Logger.Trace("<<< Setup");
		}

		[TearDown]
		public void TearDown()
		{
			Logger.Trace(">>> TearDown");

			_monitor.Dispose();
			_monitor = null;

			_tmpDir.Dispose();
			_tmpDir = null;

			Configuration.UseAsyncLogging = _oldAsync;
			Configuration.LogRoot = _oldLogRoot;

			Logger.Trace("<<< TearDown");
		}

		public virtual void TestBasic()
		{
			var jobRequest = new JobRequest
			{
				RangeStop = 1,
			};

			var job = _monitor.Start(_tmpDir.Path, _pitConfigPath, jobRequest);
			Assert.IsNotNull(job);
			WaitForFinish();

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			Assert.IsNotNull(job.DatabasePath);
			Assert.AreEqual(JobStatus.Stopped, job.Status);

			VerifyDatabase(job);
		}

		public virtual void TestStop()
		{
			var jobRequest = new JobRequest();

			var job = _monitor.Start(_tmpDir.Path, _pitConfigPath, jobRequest);
			Assert.IsNotNull(job);

			job = _monitor.GetJob();
			Assert.IsNotNull(job);

			var duration = new Random().Next(1000);
			Logger.Trace("Sleep: {0}ms", duration);
			Thread.Sleep(duration);

			_monitor.Stop();

			WaitForFinish();

			job = _monitor.GetJob();
			Assert.AreEqual(JobStatus.Stopped, job.Status);

			VerifyDatabase(job, false);
		}

		[Test]
		public virtual void TestPauseContinue()
		{
			var jobRequest = new JobRequest();

			var job = _monitor.Start(_tmpDir.Path, _pitConfigPath, jobRequest);
			Assert.IsNotNull(job);
			Assert.IsTrue(WaitUntil(JobStatus.Running), "Timeout waiting for Running");

			job = _monitor.GetJob();
			Assert.IsNotNull(job);

			Assert.IsTrue(_monitor.Pause());
			Assert.IsTrue(WaitUntil(JobStatus.Paused), "Timeout waiting for Paused");

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			Assert.AreEqual(JobStatus.Paused, job.Status);

			Assert.IsTrue(_monitor.Continue());
			Assert.IsTrue(WaitUntil(JobStatus.Running), "Timeout waiting for Running");

			Assert.IsTrue(_monitor.Stop());
			WaitForFinish();

			job = _monitor.GetJob();
			Assert.AreEqual(JobStatus.Stopped, job.Status);

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			VerifyDatabase(job);
		}

		public virtual void TestKill()
		{
			Logger.Trace("TestKill");

			var jobRequest = new JobRequest();

			var job = _monitor.Start(_tmpDir.Path, _pitConfigPath, jobRequest);
			Assert.IsNotNull(job);

			job = _monitor.GetJob();
			Assert.IsNotNull(job);

			var duration = new Random().Next(1000);
			Logger.Trace("Sleep: {0}ms", duration);
			Thread.Sleep(duration);

			Assert.IsTrue(_monitor.Kill());
			WaitForFinish();

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			Assert.AreEqual(JobStatus.Stopped, job.Status);

			VerifyDatabase(job, false);
		}

		public virtual void TestPitTester()
		{
			var jobRequest = new JobRequest
			{
				DryRun = true,
			};

			var job = _monitor.Start(_tmpDir.Path, _pitConfigPath, jobRequest);
			Assert.IsNotNull(job);
			WaitForFinish();

			job = _monitor.GetJob();
			Assert.AreEqual(JobStatus.Stopped, job.Status);

			using (var db = new NodeDatabase())
			{
				job = db.GetJob(job.Guid);
				Assert.IsNotNull(job);

				DatabaseTests.AssertResult(db.GetTestEventsByJob(job.Guid), new[]
				{
					new TestEvent(1, job.Guid, TestStatus.Pass, "Loading pit file", 
						"Loading pit file '{0}'".Fmt(_pitXmlPath), null),
					new TestEvent(2, job.Guid, TestStatus.Pass, "Starting fuzzing engine", 
						"Starting fuzzing engine", null),
					new TestEvent(3, job.Guid, TestStatus.Pass, "Running iteration", 
						"Running the initial control record iteration", null),
					new TestEvent(4, job.Guid, TestStatus.Pass, 
						"Flushing logs.", "Flushing logs.", null),
				});
			}

			Assert.IsTrue(File.Exists(job.DebugLogPath));
		}

		public virtual void TestPitParseFailureDuringTest()
		{
			var jobRequest = new JobRequest
			{
				DryRun = true,
			};

			var job = _monitor.Start(_tmpDir.Path, _pitConfigFailPath, jobRequest);
			Assert.IsNotNull(job);
			WaitForFinish();

			job = _monitor.GetJob();
			Assert.AreEqual(JobStatus.Stopped, job.Status);

			using (var db = new NodeDatabase())
			{
				DatabaseTests.AssertResult(db.GetTestEventsByJob(job.Guid), new[]
				{
					new TestEvent(
						1, 
						job.Guid, 
						TestStatus.Fail, 
						"Loading pit file", 
						"Loading pit file '{0}'".Fmt(_pitXmlFailPath), 
						"Error: XML Failed to load: Data at the root level is invalid. Line 21, position 1."),
						new TestEvent(
							2, 
							job.Guid, 
							TestStatus.Pass, 
							"Flushing logs.", 
							"Flushing logs.", 
							null),
				});

				job = db.GetJob(job.Guid);
				Assert.IsNotNull(job);

				var logs = db.GetJobLogs(job.Guid).ToList();
				Assert.AreEqual(2, logs.Count, "Missing JobLogs");
			}

			Assert.IsFalse(File.Exists(job.DatabasePath), "job.DatabasePath should not exist");
			Assert.IsFalse(File.Exists(job.DebugLogPath), "job.DebugLogPath should not exist");
		}

		public virtual void TestPitParseFailureDuringRun()
		{
			var jobRequest = new JobRequest();
			var job = _monitor.Start(_tmpDir.Path, _pitConfigFailPath, jobRequest);
			Assert.IsNotNull(job);
			WaitForFinish();

			job = _monitor.GetJob();
			Assert.AreEqual(JobStatus.Stopped, job.Status);

			using (var db = new NodeDatabase())
			{
				DatabaseTests.AssertResult(db.GetTestEventsByJob(job.Guid), new[]
				{
					 new TestEvent(
						1, 
						job.Guid, 
						TestStatus.Fail, 
						"Loading pit file", 
						"Loading pit file '{0}'".Fmt(_pitXmlFailPath),
						"Error: XML Failed to load: Data at the root level is invalid. Line 21, position 1."),
						new TestEvent(
							2, 
							job.Guid, 
							TestStatus.Pass, 
							"Flushing logs.", 
							"Flushing logs.", 
							null),
				});

				job = db.GetJob(job.Guid);
				Assert.IsNotNull(job);

				var logs = db.GetJobLogs(job.Guid).ToList();
				Assert.AreEqual(2, logs.Count, "Missing JobLogs");

				Assert.IsFalse(File.Exists(job.DatabasePath), "job.DatabasePath should not exist");
				Assert.IsFalse(File.Exists(job.DebugLogPath), "job.DebugLogPath should not exist");
			}
		}

		public virtual void TestPid()
		{
			// The pid should always be set to the pid of the process that controls
			// the engine.  For the InternalJobMonitor it will be the same process
			// as the engine, but for the ExternalJobMonitor it will be the process
			// that manages the worker process.

			int pid;
			using (var p = SysProcess.GetCurrentProcess())
				pid = p.Id;

			var jobRequest = new JobRequest();

			var job = _monitor.Start(_tmpDir.Path, _pitConfigPath, jobRequest);
			Assert.IsNotNull(job);
			Assert.IsTrue(WaitUntil(JobStatus.Running), "Timeout waiting for Running");

			job = _monitor.GetJob();
			Assert.IsNotNull(job);

			Assert.AreEqual(pid, job.Pid);
			Assert.GreaterOrEqual(job.HeartBeat, job.StartDate);

			Assert.IsTrue(_monitor.Stop());
			WaitForFinish();

			job = _monitor.GetJob();
			Assert.AreEqual(JobStatus.Stopped, job.Status);

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			VerifyDatabase(job);
		}

		public virtual void TestHeartBeat()
		{
			var jobRequest = new JobRequest();

			var job = _monitor.Start(_tmpDir.Path, _pitConfigPath, jobRequest);
			Assert.IsNotNull(job);
			Assert.IsTrue(WaitUntil(JobStatus.Running), "Timeout waiting for Running");

			job = _monitor.GetJob();
			Assert.IsNotNull(job);

			Assert.GreaterOrEqual(job.HeartBeat, job.StartDate);

			Assert.IsTrue(_monitor.Pause());
			Assert.IsTrue(WaitUntil(JobStatus.Paused), "Timeout waiting for Paused");

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			Assert.AreEqual(JobStatus.Paused, job.Status);

			var time = job.HeartBeat;

			Thread.Sleep(2000);

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			Assert.AreEqual(JobStatus.Paused, job.Status);

			// Heartbeat should go up when we are paused
			Assert.Greater(job.HeartBeat, time);

			time = job.HeartBeat;

			Thread.Sleep(1000);

			Assert.IsTrue(_monitor.Stop());
			WaitForFinish();

			job = _monitor.GetJob();
			Assert.AreEqual(JobStatus.Stopped, job.Status);

			// Heartbeat should advance when job stops
			Assert.Greater(job.HeartBeat, time);
			Assert.GreaterOrEqual(job.HeartBeat, job.StopDate);

			VerifyDatabase(job);
		}
	}

	namespace JobMonitorTests
	{
		[TestFixture]
		[Peach]
		[Quick]
		class External : JobMonitorTests<ExternalJobMonitorFactory>
		{
			[OneTimeSetUp]
			public void EnableTrace()
			{
				SetUpFixture.EnableTrace();
			}

			[Test]
			[Repeat(10)]
			public override void TestBasic()
			{
				base.TestBasic();
			}

			[Test]
			[Repeat(10)]
			public override void TestStop()
			{
				base.TestStop();
			}

			[Test]
			public override void TestPauseContinue()
			{
				base.TestPauseContinue();
			}

			[Test]
			[Repeat(30)]
			public override void TestKill()
			{
				base.TestKill();
			}

			[Test]
			[Repeat(2)]
			public override void TestPitTester()
			{
				base.TestPitTester();
			}

			[Test]
			public override void TestPitParseFailureDuringTest()
			{
				base.TestPitParseFailureDuringTest();
			}

			[Test]
			public override void TestPitParseFailureDuringRun()
			{
				base.TestPitParseFailureDuringRun();
			}

			[Test]
			public override void TestPid()
			{
				base.TestPid();
			}

			[Test]
			public override void TestHeartBeat()
			{
				base.TestHeartBeat();
			}
		}

		[TestFixture]
		[Peach]
		[Quick]
		class Internal : JobMonitorTests<InternalJobMonitorFactory>
		{
			[Test]
			[Repeat(10)]
			public override void TestBasic()
			{
				base.TestBasic();
			}

			[Test]
			[Repeat(10)]
			public override void TestStop()
			{
				base.TestStop();
			}

			[Test]
			public override void TestPauseContinue()
			{
				base.TestPauseContinue();
			}

			[Test]
			[Repeat(30)]
			public override void TestKill()
			{
				base.TestKill();
			}

			[Test]
			[Repeat(2)]
			public override void TestPitTester()
			{
				base.TestPitTester();
			}

			[Test]
			public override void TestPitParseFailureDuringTest()
			{
				base.TestPitParseFailureDuringTest();
			}

			[Test]
			public override void TestPitParseFailureDuringRun()
			{
				base.TestPitParseFailureDuringRun();
			}

			[Test]
			public override void TestPid()
			{
				base.TestPid();
			}

			[Test]
			public override void TestHeartBeat()
			{
				base.TestHeartBeat();
			}
		}
	}


	[TestFixture]
	[Peach]
	[Quick]
	class ExternalJobMonitorTests2 : BaseJobMonitorTests
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		protected TempDirectory _tmpDir;
		protected string _pitXmlPath;
		protected string _pitConfigPath;
		protected string _pitXmlCrashPath;
		protected string _pitConfigCrashPath;
		protected bool _oldAsync;
		protected string _oldLogRoot;

		protected const string PitXmlCrash =
@"<?xml version='1.0' encoding='utf-8'?>
<Peach>

	<DataModel name='DM'>
		<String value='Hello World' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel name='DM'/>
			</Action>
		</State>
	</StateModel>

	<Agent name='LocalAgent'>
		<Monitor class='你好RandoFaulter'>
			<Param name='CrashAfter' value='2000'/>
		</Monitor>
	</Agent>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null'/>
		<Agent ref='LocalAgent' />
	</Test>
</Peach>
";

		protected const string PitConfigCrash = @"
{
	'OriginalPit': 'TestCrash.xml',
	'Config': [],
	'Agents': [],
	'Weights': []
}
";

		[SetUp]
		public void SetUp()
		{
			Logger.Trace(">>> Setup");

			_tmpDir = new TempDirectory();

			_oldAsync = Configuration.UseAsyncLogging;
			_oldLogRoot = Configuration.LogRoot;

			Configuration.UseAsyncLogging = false;
			Configuration.LogRoot = _tmpDir.Path;

			_doneEvt = new ManualResetEvent(false);
			_monitor = new ExternalJobMonitor
			{
				InternalEvent = (s, a) =>
				{
					Logger.Trace("InternalEvent");
					_doneEvt.Set();
				}
			};

			_pitXmlPath = Path.Combine(_tmpDir.Path, "Test.xml");
			_pitConfigPath = Path.Combine(_tmpDir.Path, "Test.pit");

			File.WriteAllText(_pitXmlPath, PitXml);
			File.WriteAllText(_pitConfigPath, PitConfig);

			_pitXmlCrashPath = Path.Combine(_tmpDir.Path, "TestCrash.xml");
			_pitConfigCrashPath = Path.Combine(_tmpDir.Path, "TestCrash.pit");

			File.WriteAllText(_pitXmlCrashPath, PitXmlCrash);
			File.WriteAllText(_pitConfigCrashPath, PitConfigCrash);

			Logger.Trace("<<< Setup");
		}

		[TearDown]
		public void TearDown()
		{
			Logger.Trace(">>> TearDown");

			_monitor.Dispose();
			_monitor = null;
			_tmpDir.Dispose();
			_tmpDir = null;

			Configuration.UseAsyncLogging = _oldAsync;
			Configuration.LogRoot = _oldLogRoot;

			Logger.Trace("<<< TearDown");
		}

		[Test]
		public void TestRestart()
		{
			var jobRequest = new JobRequest();

			var job = _monitor.Start(_tmpDir.Path, _pitConfigPath, jobRequest);
			Assert.IsNotNull(job);
			Assert.IsTrue(WaitUntil(JobStatus.Running), "Timeout waiting for Running");

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			var count = job.IterationCount;

			// kill the worker without setting _pendingKill
			((ExternalJobMonitor)_monitor).Terminate();

			Thread.Sleep(TimeSpan.FromSeconds(5));

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			Assert.AreEqual(JobStatus.Running, job.Status);

			Assert.IsTrue(_monitor.Stop());
			WaitForFinish();

			job = _monitor.GetJob();
			Assert.Greater(job.IterationCount, count);

			VerifyDatabase(job);
		}

		[Test]
		public void TestCrash()
		{
			var jobRequest = new JobRequest();

			var job = _monitor.Start(_tmpDir.Path, _pitConfigCrashPath, jobRequest);
			Assert.IsNotNull(job);
			Assert.IsTrue(WaitUntil(JobStatus.Running), "Timeout waiting for Running");

			job = _monitor.GetJob();
			Assert.IsNotNull(job);

			var count = job.IterationCount;

			Thread.Sleep(TimeSpan.FromSeconds(5));

			// should have crashed at least once by now

			job = _monitor.GetJob();
			Assert.IsNotNull(job);
			Assert.AreEqual(JobStatus.Running, job.Status);

			Assert.Greater(job.IterationCount, count);

			Assert.IsTrue(_monitor.Kill(), "Kill failed");
			WaitForFinish();

			job = _monitor.GetJob();
			Assert.AreEqual(JobStatus.Stopped, job.Status);

			var logPath = Path.Combine(Configuration.LogRoot, job.Id, "error.log");
			Assert.IsTrue(File.Exists(logPath));

			VerifyDatabase(job);
		}
	}
}
