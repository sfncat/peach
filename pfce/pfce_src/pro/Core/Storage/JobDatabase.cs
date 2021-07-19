using System;
using System.Collections.Generic;
using System.Reflection;
using Peach.Pro.Core.WebServices.Models;
using Dapper;
using Peach.Core;
using System.Linq;

namespace Peach.Pro.Core.Storage
{
	public class JobDatabase : Database
	{
		static readonly IEnumerable<Type> StaticSchema = new[]
		{
			// fault data
			typeof(FaultDetail),
			typeof(FaultFile),

			// metrics
			typeof(NamedItem),
			typeof(State),
			typeof(Mutation),
			typeof(FaultMetric),
		};

		static readonly string[] StaticScripts =
		{
			Utilities.LoadStringResource(
				Assembly.GetExecutingAssembly(), 
				"Peach.Pro.Core.Resources.Metrics.sql"
			)
		};

		protected override IEnumerable<Type> Schema
		{
			get { return StaticSchema; }
		}

		protected override IEnumerable<string> Scripts
		{
			get { return StaticScripts; }
		}

		protected override IList<MigrationHandler> Migrations
		{
			get
			{
				return new MigrationHandler[]
				{
					() => { Connection.Execute(Sql.JobMigrateV1); },
					() => { Connection.Execute(Sql.JobMigrateV2); },
					() => { Connection.Execute(Sql.JobMigrateV3); },
					() => { Connection.Execute(Sql.JobMigrateV4); },
				};
			}
		}

		public JobDatabase(string path)
			: base(path, true)
		{
		}

		public void InsertNames(IEnumerable<NamedItem> items)
		{
			Connection.Execute(Sql.InsertNames, items);
		}

		public void UpdateStates(IEnumerable<State> states)
		{
			Connection.Execute(Sql.UpdateStates, states);
		}

		public void UpsertStates(IEnumerable<State> states)
		{
			Connection.Execute(Sql.UpsertState, states);
		}

		public void UpsertMutations(IEnumerable<Mutation> mutations)
		{
			Connection.Execute(Sql.UpsertMutation, mutations);
		}

		public void InsertFaultMetrics(IEnumerable<FaultMetric> faults)
		{
			Connection.Execute(Sql.InsertFaultMetric, faults);
		}

		public void InsertFault(FaultDetail fault)
		{
			fault.Id = Connection.ExecuteScalar<long>(Sql.InsertFaultDetail, fault);

			foreach (var file in fault.Files)
			{
				file.FaultDetailId = fault.Id;
			}
			Connection.Execute(Sql.InsertFaultFile, fault.Files);
		}

		public FaultDetail GetFaultById(long id, NameKind kind, bool loadFiles = true)
		{
			FaultDetail fault;

			if (loadFiles)
			{
				const string sql =
					Sql.SelectFaultDetailById +
					Sql.SelectMutationByFaultIdAndKind +
					Sql.SelectFaultFilesByFaultId;

				using (var multi = Connection.QueryMultiple(sql, new { Id = id, Kind = kind }))
				{
					fault = multi.Read<FaultDetail>().SingleOrDefault();

					if (fault != null)
					{
						fault.Mutations = multi.Read<FaultMutation>().ToList();
						fault.Files = multi.Read<FaultFile>().ToList();
					}
				}
			}
			else
			{
				const string sql =
					Sql.SelectFaultDetailById +
					Sql.SelectMutationByFaultIdAndKind;

				using (var multi = Connection.QueryMultiple(sql, new { Id = id, Kind= kind }))
				{
					fault = multi.Read<FaultDetail>().SingleOrDefault();

					if (fault != null)
					{
						fault.Mutations = multi.Read<FaultMutation>().ToList();
					}
				}
			}

			return fault;
		}

		public FaultFile GetFaultFileById(long id)
		{
			return Connection.Query<FaultFile>(Sql.SelectFaultFilesById, new { Id = id })
				.SingleOrDefault();
		}

		public IEnumerable<FaultMutation> GetFaultMutations(long iteration, NameKind kind)
		{
			return Connection.Query<FaultMutation>(
				Sql.SelectMutationByIterationAndKind,
				new { Iteration = iteration, Kind = kind }
			);
		}

		public Report GetReport(Job job)
		{
			var report = new Report
			{
				Job = job,
				BucketCount = Connection.ExecuteScalar<int>(Sql.SelectBucketCount),
				BucketDetails = LoadTable<BucketDetail>()
					.Select(m =>
					{
						m.Mutations = GetFaultMutations(m.Iteration, job.MetricKind);
						return m;
					}),
				MutatorMetrics = LoadTable<MutatorMetric>(),
				ElementMetrics = LoadTableKind<ElementMetric>(job.MetricKind),
				StateMetrics = LoadTableKind<StateMetric>(job.MetricKind),
				DatasetMetrics = LoadTableKind<DatasetMetric>(job.MetricKind),
				BucketMetrics = LoadTableKind<BucketMetric>(job.MetricKind),
			};

			return report;
		}
	}
}
