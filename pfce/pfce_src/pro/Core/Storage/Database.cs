using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Dapper;
using Peach.Core;
using Peach.Pro.Core.OS;
using FileInfo = System.IO.FileInfo;

namespace Peach.Pro.Core.Storage
{
	// This can not be an inner class of Database otherwise
	// assemblies that try to use the JobDatabase will fail
	// to compile on mono 2.10
	class TimeSpanHandler : SqlMapper.TypeHandler<TimeSpan>
	{
		public override TimeSpan Parse(object value)
		{
			// Use ticks to avoid floating point computation
			return TimeSpan.FromTicks(Convert.ToInt64(value) * TimeSpan.TicksPerSecond);
		}

		public override void SetValue(IDbDataParameter parameter, TimeSpan value)
		{
			// Use ticks to avoid floating point computation
			parameter.DbType = DbType.Int64;
			parameter.Value = value.Ticks / TimeSpan.TicksPerSecond;
		}
	}

	// This can not be an inner class of Database otherwise
	// assemblies that try to use the JobDatabase will fail
	// to compile on mono 2.10
	class DateTimeHandler : SqlMapper.TypeHandler<DateTime>
	{
		public override DateTime Parse(object value)
		{
			// Both Mono.Data.Sqlite and System.Data.SQLite ADO.NET implementations
			// return DateTimes with DateTimeKind.Unspecified.  However, if a timezone
			// is specified in the database, System.Data.SQLite will convert it
			// to local time (but leave the type as Unspecified) and Mono.Data.Sqlite
			// will fail to parse and throw an exception.

			// To work around this we are intentionally saving DateTimes w/o any time zone
			// indication, meaning on both ADO.NET providers they are going to come
			// in as DateTimeKind.Unspecified and actually be in UTC.
			// Explicitly mark them as UTC and then return them as Local.

			var dt = Convert.ToDateTime(value);

			Debug.Assert(dt.Kind == DateTimeKind.Unspecified);

			return DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime();
		}

		public override void SetValue(IDbDataParameter parameter, DateTime value)
		{
			// Mono.Data.Sqlite does not support time zones in its ISO8601
			// DateTime parsing code.  This means that Mono.Data.Sqlite does not
			// include the UTC timezone identifier in the database when saving, whereas
			// System.Data.SQLite does.  In order to make sure our database can be read
			// by both Mono.Data.Sqlite and System.Data.SQLite we need to manually convert
			// times to a compatible ISO8601 UTC time w/o a time zone marker and
			// insert as a string.

			parameter.DbType = DbType.String;
			parameter.Value = value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss");
		}
	}

	public delegate void MigrationHandler();

	public abstract class Database : IDisposable
	{
#if SQLITE_TRACE
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
#endif

		static Database()
		{
			// Store TimeSpan objects as number of elapsed seconds
			SqlMapper.AddTypeHandler(new TimeSpanHandler());

			// Use custom type handler for storing DateTime objects in sqlite.
			SqlMapper.AddTypeHandler(new DateTimeHandler());

			// Work around an issue with Dapper where custom type handlers
			// don't get called when sending values into sqlite (SetValue codepath).
			// https://github.com/StackExchange/dapper-dot-net/issues/206
			// https://github.com/StackExchange/dapper-dot-net/pull/177

			var fi = typeof(SqlMapper).GetField("typeMap", BindingFlags.Static | BindingFlags.NonPublic);
			if (fi == null)
				throw new InvalidOperationException("SqlMapper is missing typeMap member.");

			var typeMap = (Dictionary<Type, DbType>)fi.GetValue(null);

			typeMap.Remove(typeof(DateTime));
			typeMap.Remove(typeof(DateTime?));
			typeMap.Remove(typeof(TimeSpan));
			typeMap.Remove(typeof(TimeSpan?));

			SqliteTrace();
		}

		public string Path { get; private set; }
		protected IDbConnection Connection { get; private set; }
		
		protected abstract IEnumerable<Type> Schema { get; }
		protected abstract IEnumerable<string> Scripts { get; }

		protected virtual int RequiredVersion { get { return Migrations.Count; } }
		protected virtual IList<MigrationHandler> Migrations
		{ 
			get { return new List<MigrationHandler>(); } 
		}

		protected Database(string path, bool useWal)
		{
			Path = path;

			var builder = new SQLiteConnectionStringBuilder
			{
				DataSource = Path,
				ForeignKeys = true,
				JournalMode = useWal ? SQLiteJournalModeEnum.Wal : SQLiteJournalModeEnum.Default,
				SyncMode = SynchronizationModes.Normal,
			};

			var isInitialized = IsInitialized;

			var sqliteConnection = new SQLiteConnection(builder.ConnectionString);
			sqliteConnection.SetExtendedResultCodes(true);
			Connection = sqliteConnection;
			Connection.Open();
			
			if (!isInitialized)
				Initialize();
		}

		[Conditional("SQLITE_TRACE")]
		private static void SqliteTrace()
		{
#if SQLITE_TRACE
			SQLiteLog.Log += (o, args) =>
			{
				Logger.Trace("SQL> {0}", args.Message);
			};
#endif
		}

		public void Dispose()
		{
			if (Connection != null)
				Connection.Dispose();
		}

		public void Transaction(Action action)
		{
			using (var xact = Connection.BeginTransaction())
			{
				action();
				xact.Commit();
			}
		}

		public bool IsInitialized
		{
			get
			{
				var fi = new FileInfo(Path);
				return fi.Exists && fi.Length > 0;
			}
		}

		public int CurrentVersion
		{
			get
			{
				return Convert.ToInt32(Connection.ExecuteScalar("PRAGMA user_version;"));
			}
			private set
			{
				Connection.Execute("PRAGMA user_version = {0};".Fmt(value));
			}
		}

		public void Initialize()
		{
			SqliteInitializer.InitializeDatabase(Connection, Schema, Scripts);
			CurrentVersion = RequiredVersion;
		}

		public void Migrate()
		{
			using (var si = Pal.SingleInstance(Path))
			{
				si.Lock();

				if (CurrentVersion < RequiredVersion)
				{
					for (var i = CurrentVersion; i < RequiredVersion; i++)
					{
						Migrations[i]();
						CurrentVersion = i + 1;
					}

					if (Scripts != null)
						Transaction(() => Scripts.ForEach(s => Connection.Execute(s)));
				}

				if (CurrentVersion != RequiredVersion)
				{
					throw new PeachException("Invalid {0} version {1}, expected version {2}".Fmt(
						GetType().Name,
						CurrentVersion,
						RequiredVersion
					));
				}
			}
		}

		public IEnumerable<T> LoadTable<T>()
		{
			var type = typeof(T);

			var attr = type.GetCustomAttributes(typeof(TableAttribute), true)
				.OfType<TableAttribute>()
				.FirstOrDefault();
	
			var table = (attr != null) ? attr.Name : type.Name;
			var sql = "SELECT * FROM {0}".Fmt(table);
			return Connection.Query<T>(sql);
		}

		public IEnumerable<T> LoadTableKind<T>(NameKind kind)
		{
			var type = typeof(T);

			var attr = type.GetCustomAttributes(typeof(TableAttribute), true)
				.OfType<TableAttribute>()
				.FirstOrDefault();

			var table = (attr != null) ? attr.Name : type.Name;
			var sql = "SELECT * FROM {0} WHERE Kind=@Kind".Fmt(table);
			return Connection.Query<T>(sql, new { Kind = kind });
		}

		public static void Dump<T>(IEnumerable<T> data)
		{
			var type = typeof(T);

			var columns = type.GetProperties()
				.Where(pi => !pi.HasAttribute<NotMappedAttribute>())
				.ToList();

			var maxWidth = new int[columns.Count];
			var header = new string[columns.Count];
			var rows = new List<string[]> { header };

			for (var i = 0; i < columns.Count; i++)
			{
				var pi = columns[i];
				header[i] = pi.Name;
				maxWidth[i] = pi.Name.Length;
			}

			foreach (var item in data)
			{
				var row = new string[columns.Count];
				var values = columns.Select(pi =>
				{
					var value = pi.GetValue(item, null);
					return value == null ? "<NULL>" : ToString(value);
				}).ToArray();
				for (var i = 0; i < values.Length; i++)
				{
					var value = values[i];
					row[i] = value;
					maxWidth[i] = Math.Max(maxWidth[i], value.Length);
				}
				rows.Add(row);
			}

			var fmts = maxWidth
				.Select((t, i) => "{0},{1}".Fmt(i, t))
				.Select(fmt => "{" + fmt + "}")
				.ToList();
			var finalFmt = string.Join("|", fmts);
			foreach (object[] row in rows)
			{
				Console.WriteLine(finalFmt, row);
			}
		}

		static string ToString(object value)
		{
			var bytes = value as byte[];
			if (bytes != null)
				return BitConverter.ToString(bytes);
			return value.ToString();
		}

		#region Unit Test Helpers

		internal DateTime SelectDateTime(string commandText)
		{
			using (var cmd = Connection.CreateCommand())
			{
				cmd.CommandText = commandText;
				var obj = cmd.ExecuteScalar();
				return Convert.ToDateTime(obj);
			}
		}

		internal long SelectLong(string commandText)
		{
			using (var cmd = Connection.CreateCommand())
			{
				cmd.CommandText = commandText;
				var obj = cmd.ExecuteScalar();
				return Convert.ToInt64(obj);
			}
		}

		internal string SelectString(string commandText)
		{
			using (var cmd = Connection.CreateCommand())
			{
				cmd.CommandText = commandText;
				var obj = cmd.ExecuteScalar();
				return Convert.ToString(obj);
			}
		}

		#endregion
	}
}
