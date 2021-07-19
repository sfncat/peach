using Peach.Pro.Core.WebServices.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dapper;

namespace Peach.Pro.Core.Storage
{
	public class JobHelper
	{
		public static void Fail(
			Guid id,
			Func<NodeDatabase, IEnumerable<TestEvent>> getEvents,
			string message)
		{
			using (var db = new NodeDatabase())
			{
				var job = db.GetJob(id);
				if (job == null)
					return;

				var events = getEvents(db).ToList();
				foreach (var testEvent in events)
				{
					if (testEvent.Status == TestStatus.Active)
					{
						testEvent.Status = TestStatus.Fail;
						testEvent.Resolve = message;
					}
				}

				job.StopDate = DateTime.Now;
				job.HeartBeat = job.StopDate;
				job.Mode = JobMode.Fuzzing;
				job.Status = JobStatus.Stopped;
				job.Result = message;

				db.Transaction(() =>
				{
					var testEvent = new TestEvent
					{
						JobId = id.ToString(),
						Status = TestStatus.Active,
						Short = "Flushing logs.",
						Description = "Flushing logs.",
					};

					db.UpdateTestEvents(events);
					db.InsertTestEvent(testEvent);
					db.UpdateJob(job);
				});
			}
		}
	}

	public class NodeDatabase : Database
	{
		static readonly IEnumerable<Type> StaticSchema = new[]
		{
			// job status
			typeof(Job),
			typeof(JobLog),

			// pit tester
			typeof(TestEvent),
		};

		protected override IEnumerable<Type> Schema
		{
			get { return StaticSchema; }
		}

		protected override IEnumerable<string> Scripts
		{
			get { return null; }
		}

		protected override IList<MigrationHandler> Migrations
		{
			get
			{
				return new MigrationHandler[]
				{
					() => { Connection.Execute(Sql.NodeMigrateV1); },
					() => { Connection.Execute(Sql.NodeMigrateV2); },
					() => { Connection.Execute(Sql.NodeMigrateV3); },
				};
			}
		}

		public NodeDatabase()
			: base(GetDatabasePath(), true)
		{
		}

		// used by unit tests
		internal static string GetDatabasePath()
		{
			var logRoot = Configuration.LogRoot;

			if (!Directory.Exists(logRoot))
				Directory.CreateDirectory(logRoot);

			return System.IO.Path.Combine(logRoot, "node.db");
		}

		public IEnumerable<JobLog> GetJobLogs(Guid id)
		{
			return Connection.Query<JobLog>(Sql.SelectJobLogs, new { Id = id.ToString() });
		}

		public void InsertJobLog(JobLog log)
		{
			Connection.Execute(Sql.InsertJobLog, log);
		}

		public Job GetJob(Guid id)
		{
			return Connection.Query<Job>(Sql.SelectJob, new { Id = id.ToString() })
				.SingleOrDefault();
		}

		public void InsertJob(Job job)
		{
			Connection.Execute(Sql.InsertJob, job);
		}

		public void UpdateJob(Job job)
		{
			Connection.Execute(Sql.UpdateJob, job);
		}

		public void UpdateRunningJob(Job job)
		{
			Connection.Execute(Sql.UpdateRunningJob, job);
		}

		public void DeleteJob(Guid id)
		{
			Connection.Execute(Sql.DeleteJob, new { Id = id.ToString() });
		}

		public void UpdateJobs(IEnumerable<Job> job)
		{
			Connection.Execute(Sql.UpdateJob, job);
		}

		public void DeleteJobs(IEnumerable<Job> jobs)
		{
			var ids = jobs.Select(x => new { x.Id });
			Connection.Execute(Sql.DeleteJob, ids);
		}

		public void InsertTestEvent(TestEvent testEvent)
		{
			testEvent.Id = Connection.ExecuteScalar<long>(Sql.InsertTestEvent, testEvent);
		}

		public void UpdateTestEvents(IEnumerable<TestEvent> testEvent)
		{
			Connection.Execute(Sql.UpdateTestEvent, testEvent);
		}

		public void PassPendingTestEvents(Guid id)
		{
			Connection.Execute(Sql.PassPendingTestEvents, new { JobId = id.ToString() });
		}

		public IEnumerable<TestEvent> GetTestEventsByJob(Guid jobId)
		{
			return Connection.Query<TestEvent>(
				Sql.SelectTestEvents,
				new { JobId = jobId.ToString() }
			);
		}
	}
}
