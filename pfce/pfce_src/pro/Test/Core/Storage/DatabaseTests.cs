using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core.Storage;

namespace Peach.Pro.Test.Core.Storage
{
	[TestFixture]
	[Quick]
	public class DatabaseTests
	{
		public static void AssertResult<T>(IEnumerable<T> actual, IEnumerable<T> expected)
		{
			var actualList = actual.ToList();
			var expectedList = expected.ToList();

			Database.Dump(actualList);

			Assert.AreEqual(expectedList.Count, actualList.Count, "Rows mismatch");

			var type = typeof(T);
			for (var i = 0; i < actualList.Count; i++)
			{
				var actualRow = actualList[i];
				var expectedRow = expectedList[i];
				foreach (var pi in type.GetProperties()
					.Where(x => !x.HasAttribute<NotMappedAttribute>()))
				{
					var actualValue = pi.GetValue(actualRow, null);
					var expectedValue = pi.GetValue(expectedRow, null);
					var msg = "Values mismatch on row {0} column {1}.".Fmt(i, pi.Name);
					Assert.AreEqual(expectedValue, actualValue, msg);
				}
			}
		}

		class TestTable
		{
			[Key]
			public long Id { get; set; }

			public string Value { get; set; }
		}

		class TestDatabase : Database
		{
			const string InsertData = "INSERT INTO TestTable (Id, Value) VALUES (@Id, @Value)";

			const string SelectData = "SELECT * FROM TestTable LIMIT 10";

			public TestDatabase(string path, bool useWal)
				: base(path, useWal)
			{
			}

			protected override IEnumerable<Type> Schema
			{
				get { return new[] { typeof(TestTable) }; }
			}

			protected override IEnumerable<string> Scripts { get { return null; } }

			protected override IList<MigrationHandler> Migrations
			{
				get { return TestMigrations; }
			}

			public readonly List<MigrationHandler> TestMigrations = new List<MigrationHandler>();

			public void Insert(TestTable data)
			{
				Connection.Execute(InsertData, data);
			}

			public IEnumerable<TestTable> Select()
			{
				return Connection.Query<TestTable>(SelectData);
			}
		}

		TempDirectory _tmp;

		[SetUp]
		public void SetUp()
		{
			_tmp = new TempDirectory();
		}

		[TearDown]
		public void TearDown()
		{
			_tmp.Dispose();
		}

		class ConsoleTracer : TraceListener
		{
			public override void Write(string message)
			{
				Console.Write(message);
			}

			public override void WriteLine(string message)
			{
				Console.WriteLine(message);
			}
		}

		[Test]
		public void Migration()
		{
			var path = Path.Combine(_tmp.Path, "test.db");
			var builder = new SQLiteConnectionStringBuilder
			{
				DataSource = path,
				ForeignKeys = true,
			};

			// Create Version 0
			using (var cnn = new SQLiteConnection(builder.ConnectionString))
			{
				cnn.Open();
			}

			var history = new List<string>();

			// Update to current (version 2)
			using (var db = new TestDatabase(path, false))
			{
				Assert.AreEqual(0, db.CurrentVersion);

				db.TestMigrations.Add(() => history.Add("1"));
				db.TestMigrations.Add(() => history.Add("2"));

				db.Migrate();

				Assert.AreEqual(2, db.CurrentVersion);
			}

			var expected = new[]
			{
				"1",
				"2",
			};

			Assert.That(history.ToArray(), Is.EqualTo(expected));

			// Add version 3 & 4
			history.Clear();

			using (var db = new TestDatabase(path, false))
			{
				Assert.AreEqual(2, db.CurrentVersion);

				db.TestMigrations.Add(() => history.Add("1"));
				db.TestMigrations.Add(() => history.Add("2"));
				db.TestMigrations.Add(() => history.Add("3"));
				db.TestMigrations.Add(() => history.Add("4"));

				db.Migrate();

				Assert.AreEqual(4, db.CurrentVersion);
			}

			expected = new[]
			{
				"3",
				"4",
			};

			Assert.That(history.ToArray(), Is.EqualTo(expected));
		}

		[Test]
		public void TestWalConcurrentWriters()
		{
			Console.WriteLine("TestWalConcurrentWriters");

			Trace.Listeners.Add(new ConsoleTracer());

			var path = Path.Combine(_tmp.Path, "test.db");
			using (new TestDatabase(path, true)) { }

			var writeTasks = new Task[10];
			for (var i = 0; i < writeTasks.Length; i++)
			{
				var index = i;
				writeTasks[i] = Task.Factory.StartNew(() => DoWrites(index), TaskCreationOptions.LongRunning);
			}

			var readTasks = new Task[10];
			for (var i = 0; i < readTasks.Length; i++)
			{
				readTasks[i] = Task.Factory.StartNew(DoReads, TaskCreationOptions.LongRunning);
			}

			Task.WaitAll(writeTasks.Concat(readTasks).ToArray());
		}

		private void DoReads()
		{
			const int reads = 1000;
			var path = Path.Combine(_tmp.Path, "test.db");
			for (var i = 0; i < reads; i++)
			{
				using (var db = new TestDatabase(path, true))
				{
					db.Select();
				}
			}
		}

		private void DoWrites(int index)
		{
			const int writes = 1000;
			var start = writes * index;
			var end = start + writes;
			var path = Path.Combine(_tmp.Path, "test.db");
			for (var i = start; i < end; i++)
			{
				using (var db = new TestDatabase(path, true))
				{
					db.Insert(new TestTable { Id = i, Value = "value" });
				}
			}
		}
	}
}
