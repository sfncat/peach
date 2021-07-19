using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices.Models;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Storage
{
	[TestFixture]
	[Peach]
	[Quick]
	class MetricsTests
	{
		Job _job;
		TempDirectory _tmp;
		DateTime _now;

		public static void MakeSampleCache(DateTime now, Job job)
		{
			var cache = new AsyncDbCache(job);
			
			// NORMAL
			cache.IterationStarting(JobMode.Fuzzing);
			cache.StateStarting("S1", "", 1);
			cache.StateStarting("S2", "", 1);
			cache.ActionStarting("A1", "");
			cache.ActionStarting("A2", "");
			cache.DataMutating(NameKind.Machine, "P1", "E1", "M1", "");
			cache.DataMutating(NameKind.Human, "", "", "M1", "");
			cache.ActionStarting("A3", "");
			cache.StateStarting("S3", "", 1);
			cache.ActionStarting("A3", "");
			cache.DataMutating(NameKind.Machine, "P1", "E1", "M1", "");
			cache.DataMutating(NameKind.Human, "", "", "M1", "");
			cache.DataMutating(NameKind.Machine, "P2", "E2", "M2", "D2");
			cache.DataMutating(NameKind.Human, "", "Field1", "M2", "D2");
			cache.IterationFinished();

			// NORMAL
			cache.IterationStarting(JobMode.Fuzzing);
			cache.StateStarting("S1", "", 1);
			cache.StateStarting("S2", "", 1);
			cache.ActionStarting("A1", "");
			cache.ActionStarting("A2", "");
			cache.DataMutating(NameKind.Machine, "P1", "E1", "M3", "D1");
			cache.DataMutating(NameKind.Human, "", "", "M3", "D1");
			cache.IterationFinished();

			// REPRO FAIL
			cache.IterationStarting(JobMode.Reproducing);
			cache.StateStarting("S1", "", 1);
			cache.StateStarting("S2", "", 1);
			cache.ActionStarting("A1", "");
			cache.ActionStarting("A2", "");
			cache.DataMutating(NameKind.Machine, "P1", "E1", "M3", "D1");
			cache.DataMutating(NameKind.Human, "", "", "M3", "D1");
			// no iteration finished because we're reproducing

			// REPRO SUCCESS
			cache.IterationStarting(JobMode.Searching);
			cache.StateStarting("S1", "", 1);
			cache.StateStarting("S2", "", 1);
			cache.ActionStarting("A1", "");
			cache.ActionStarting("A2", "");
			cache.DataMutating(NameKind.Machine, "P1", "E1", "M1", "");
			cache.DataMutating(NameKind.Human, "", "", "M1", "");
			cache.ActionStarting("A3", "");
			// Simulate S2_A3 soft exception, so we don't run S3
			cache.OnFault(new FaultDetail
			{
				Iteration = 1,
				Title = "Fault Title Goes Here",
				MajorHash = "AAA",
				MinorHash = "BBB",
				TimeStamp = now,
				Files = new List<FaultFile>(),
				Source = "WindowsDebugger",
				Exploitability = "UNKNOWN",
				Description = "Fault Description Goes Here",
				Reproducible = false,
			});

			// NORMAL
			cache.IterationStarting(JobMode.Fuzzing);
			cache.StateStarting("S3", "", 1);
			cache.ActionStarting("A3", "");
			cache.DataMutating(NameKind.Machine, "P3", "E3", "M3", "D3");
			cache.DataMutating(NameKind.Human, "", "", "M3", "");
			cache.IterationFinished();

			// REPRO SUCCESS
			cache.IterationStarting(JobMode.Reproducing);
			cache.StateStarting("S3", "", 1);
			cache.ActionStarting("A3", "");
			cache.DataMutating(NameKind.Machine, "P3", "E3", "M3", "D3");
			cache.DataMutating(NameKind.Human, "", "", "M3", "");
			cache.OnFault(new FaultDetail
			{
				Iteration = 3,
				Title = "Fault Title Goes Here",
				MajorHash = "AAA",
				MinorHash = "BBB",
				TimeStamp = now,
				Files = new List<FaultFile>(),
				Source = "WindowsDebugger",
				Exploitability = "UNKNOWN",
				Description = "Fault Description Goes Here",
				Reproducible = false,
			});

			// NORMAL
			cache.IterationStarting(JobMode.Fuzzing);
			cache.StateStarting("S4", "", 1);
			cache.ActionStarting("A4", "");
			cache.DataMutating(NameKind.Machine, "P4", "E4", "M4", "D4");
			cache.DataMutating(NameKind.Human, "", "Field2", "M4", "");
			cache.IterationFinished();

			// REPRO SUCCESS
			cache.IterationStarting(JobMode.Reproducing);
			cache.StateStarting("S4", "", 1);
			cache.ActionStarting("A4", "");
			cache.DataMutating(NameKind.Machine, "P4", "E4", "M4", "D4");
			cache.DataMutating(NameKind.Human, "", "Field2", "M4", "");
			cache.ActionStarting("A5", "");
			cache.DataMutating(NameKind.Machine, "P4", "E5", "M9", "D9");
			cache.DataMutating(NameKind.Human, "", "", "M9", "");
			cache.OnFault(new FaultDetail
			{
				Iteration = 4,
				Title = "Fault 你好 Goes Here",
				MajorHash = "XXX",
				MinorHash = "YYY",
				TimeStamp = now + TimeSpan.FromHours(1),
				Files = new List<FaultFile>(),
				Source = "WindowsDebugger",
				Exploitability = "UNKNOWN",
				Description = "Fault 機除拍禁響地章手棚国歳違不 Goes Here",
				Reproducible = true,
			});

			// NORMAL
			cache.IterationStarting(JobMode.Fuzzing);
			cache.StateStarting("S5", "", 1);
			cache.ActionStarting("A5", "");
			cache.DataMutating(NameKind.Machine, "P5", "E5", "M5", "D5");
			cache.DataMutating(NameKind.Human, "", "Field2", "M5", "");
			cache.StateStarting("S5", "", 2);
			cache.ActionStarting("A5", "");
			cache.DataMutating(NameKind.Machine, "P5", "E5", "M5", "D5");
			cache.DataMutating(NameKind.Human, "", "Field2", "M5", "");
			cache.IterationFinished();

			// REPRO SUCCESS
			cache.IterationStarting(JobMode.Reproducing);
			cache.StateStarting("S5", "", 1);
			cache.ActionStarting("A5", "");
			cache.DataMutating(NameKind.Machine, "P5", "E5","M5", "D5");
			cache.DataMutating(NameKind.Human, "", "Field2", "M5", "");
			cache.StateStarting("S5", "", 2);
			cache.ActionStarting("A5", "");
			cache.DataMutating(NameKind.Machine, "P5", "E5", "M5", "D5");
			cache.DataMutating(NameKind.Human, "", "Field2", "M5", "");
			cache.OnFault(new FaultDetail
			{
				Iteration = 5,
				Title = "Fault Title Goes Here",
				MajorHash = "AAA",
				MinorHash = "YYY",
				TimeStamp = now + TimeSpan.FromHours(2),
				Files = new List<FaultFile>(),
				Source = "WindowsDebugger",
				Exploitability = "UNKNOWN",
				Description = "Fault Description Goes Here",
				Reproducible = false,
			});

			// NORMAL
			cache.IterationStarting(JobMode.Fuzzing);
			cache.StateStarting("S3", "", 1);
			cache.ActionStarting("A3", "");
			cache.DataMutating(NameKind.Machine, "P3", "E3", "M3", "D8");
			cache.DataMutating(NameKind.Human, "", "", "M3", "");
			cache.IterationFinished();

			cache.IterationStarting(JobMode.Reproducing);
			cache.StateStarting("S3", "", 1);
			cache.ActionStarting("A3", "");
			cache.DataMutating(NameKind.Machine, "P3", "E3", "M3", "D8");
			cache.DataMutating(NameKind.Human, "", "", "M3", "");
			cache.OnFault(new FaultDetail
			{
				Iteration = 6,
				Title = "Fault Title Goes Here",
				MajorHash = "AAA",
				MinorHash = "BBB",
				TimeStamp = now + TimeSpan.FromHours(3),
				Files = new List<FaultFile>(),
				Source = "WindowsDebugger",
				Exploitability = "UNKNOWN",
				Description = "Fault Description Goes Here",
				Reproducible = true,
			});

			// NORMAL
			cache.IterationStarting(JobMode.Fuzzing);
			cache.StateStarting("S3", "", 1);
			cache.ActionStarting("A3", "");
			cache.DataMutating(NameKind.Machine, "P3", "E3", "M3", "D3");
			cache.DataMutating(NameKind.Human, "", "", "M3", "");
			cache.IterationFinished();

			// NORMAL
			cache.IterationStarting(JobMode.Fuzzing);
			cache.StateStarting("S3", "", 1);
			cache.ActionStarting("A3", "");
			cache.DataMutating(NameKind.Machine, "P3", "E3", "M3", "D3");
			cache.DataMutating(NameKind.Human, "", "", "M3", "");
			cache.IterationFinished();

			cache.IterationStarting(JobMode.Reproducing);
			cache.StateStarting("S3", "", 1);
			cache.ActionStarting("A3", "");
			cache.DataMutating(NameKind.Machine, "P3", "E3", "M3", "D3");
			cache.DataMutating(NameKind.Human, "", "", "M3", "");
			cache.OnFault(new FaultDetail
			{
				Iteration = 8,
				Title = "Fault 你好 Goes Here",
				MajorHash = "XXX",
				MinorHash = "YYY",
				TimeStamp = now + TimeSpan.FromHours(4),
				Files = new List<FaultFile>(),
				Source = "WindowsDebugger",
				Exploitability = "UNKNOWN",
				Description = "Fault 機除拍禁響地章手棚国歳違不 Goes Here",
				Reproducible = true,
			});

			cache.TestFinished();
		}

		[SetUp]
		public void SetUp()
		{
			_tmp = new TempDirectory();

			// The database doesn't store milliseconds/microseconds, so don't include them in the test
			_now = DateTime.Parse(
				"2015-06-18 00:00:00",
				CultureInfo.InvariantCulture,
				DateTimeStyles.AssumeLocal
			);

			_job = new Job { LogPath = _tmp.Path };

			MakeSampleCache(_now, _job);
		}

		[TearDown]
		public void TearDown()
		{
			_tmp.Dispose();
		}

		[Test]
		public void TestQueryStates()
		{
			using (var db = new JobDatabase(_job.DatabasePath))
			{
				DatabaseTests.AssertResult(db.LoadTableKind<StateMetric>(NameKind.Machine), new[]
				{
					new StateMetric("S3_1", 5),
					new StateMetric("S1_1", 2),
					new StateMetric("S2_1", 2),
					new StateMetric("S4_1", 1),
					new StateMetric("S5_1", 1),
					new StateMetric("S5_2", 1),
				});

				// If fieldIds are being used, don't show states w/o fieldId attribute
				DatabaseTests.AssertResult(db.LoadTableKind<StateMetric>(NameKind.Human), new StateMetric[0]);
			}
		}

		[Test]
		public void TestQueryIterations()
		{
			using (var db = new JobDatabase(_job.DatabasePath))
			{
				DatabaseTests.AssertResult(db.LoadTableKind<IterationMetric>(NameKind.Machine), new[]
				{
					new IterationMetric("S2_1", "A2", "P1", "E1", "M1", "", 1),
					new IterationMetric("S3_1", "A3", "P1", "E1", "M1", "", 1),
					new IterationMetric("S3_1", "A3", "P2", "E2", "M2", "D2", 1),
					new IterationMetric("S2_1", "A2", "P1", "E1", "M3", "D1", 1),
					new IterationMetric("S4_1", "A4", "P4", "E4", "M4", "D4", 1),
					new IterationMetric("S5_1", "A5", "P5", "E5", "M5", "D5", 1),
					new IterationMetric("S5_2", "A5", "P5", "E5", "M5", "D5", 1),
					new IterationMetric("S3_1", "A3", "P3", "E3", "M3", "D8", 1),
					new IterationMetric("S3_1", "A3", "P3", "E3", "M3", "D3", 3),
				});

				DatabaseTests.AssertResult(db.LoadTableKind<IterationMetric>(NameKind.Human), new[]
				{
					new IterationMetric("", "", "", "", "M1", "", 2) { Kind = NameKind.Human },
					new IterationMetric("", "", "", "Field1", "M2", "D2", 1) { Kind = NameKind.Human },
					new IterationMetric("", "", "", "", "M3", "D1", 1) { Kind = NameKind.Human },
					new IterationMetric("", "", "", "Field2", "M4", "", 1) { Kind = NameKind.Human },
					new IterationMetric("", "", "", "Field2", "M5", "", 2) { Kind = NameKind.Human },
					new IterationMetric("", "", "", "", "M3", "", 4) { Kind = NameKind.Human },
				});
			}
		}

		[Test]
		public void TestQueryBuckets()
		{
			using (var db = new JobDatabase(_job.DatabasePath))
			{
				DatabaseTests.AssertResult(db.LoadTableKind<BucketMetric>(NameKind.Machine), new[]
				{
					new BucketMetric("AAA_BBB", "M3", "S3_1.A3.P3.E3", 4, 2),
					new BucketMetric("XXX_YYY", "M3", "S3_1.A3.P3.E3", 4, 1),
					new BucketMetric("AAA_BBB", "M1", "S2_1.A2.P1.E1", 1, 1),
					new BucketMetric("XXX_YYY", "M4", "S4_1.A4.P4.E4", 1, 1),
					new BucketMetric("AAA_YYY", "M5", "S5_1.A5.P5.E5", 1, 1),
					new BucketMetric("AAA_YYY", "M5", "S5_2.A5.P5.E5", 1, 1),
				});

				DatabaseTests.AssertResult(db.LoadTableKind<BucketMetric>(NameKind.Human), new[]
				{
					new BucketMetric("AAA_BBB", "M3", "", 5, 2) { Kind = NameKind.Human },
					new BucketMetric("XXX_YYY", "M3", "", 5, 1) { Kind = NameKind.Human },
					new BucketMetric("AAA_BBB", "M1", "", 2, 1) { Kind = NameKind.Human },
					new BucketMetric("AAA_YYY", "M5", "Field2", 2, 1) { Kind = NameKind.Human },
					new BucketMetric("XXX_YYY", "M4", "Field2", 1, 1) { Kind = NameKind.Human },
				});
			}
		}

		[Test]
		public void TestQueryBucketTimeline()
		{
			using (var db = new JobDatabase(_job.DatabasePath))
			{
				DatabaseTests.AssertResult(db.LoadTable<BucketTimelineMetric>(), new[]
				{
					new BucketTimelineMetric("AAA_BBB", 1, _now, 3),
					new BucketTimelineMetric("AAA_YYY", 5, _now + TimeSpan.FromHours(2), 1),
					new BucketTimelineMetric("XXX_YYY", 4, _now + TimeSpan.FromHours(1), 2),
				});
			}
		}

		[Test]
		public void TestQueryMutator()
		{
			using (var db = new JobDatabase(_job.DatabasePath))
			{
				// Mutator,ElementCount,IterationCount,BucketCount,FaultCount
				DatabaseTests.AssertResult(db.LoadTable<MutatorMetric>(), new[]
				{
					new MutatorMetric("M3", 2, 5, 2, 3),
					new MutatorMetric("M1", 2, 2, 1, 1),
					new MutatorMetric("M5", 2, 2, 1, 1),
					new MutatorMetric("M4", 1, 1, 1, 1),
					new MutatorMetric("M2", 1, 1, 0, 0),
				});
			}
		}

		[Test]
		public void TestQueryElement()
		{
			using (var db = new JobDatabase(_job.DatabasePath))
			{
				DatabaseTests.AssertResult(db.LoadTableKind<ElementMetric>(NameKind.Machine), new[]
				{
					new ElementMetric("S3_1", "A3", "P3.E3", 4, 2, 3),
					new ElementMetric("S2_1", "A2", "P1.E1", 2, 1, 1),
					new ElementMetric("S4_1", "A4", "P4.E4", 1, 1, 1),
					new ElementMetric("S5_1", "A5", "P5.E5", 1, 1, 1),
					new ElementMetric("S5_2", "A5", "P5.E5", 1, 1, 1),
					new ElementMetric("S3_1", "A3", "P1.E1", 1, 0, 0),
					new ElementMetric("S3_1", "A3", "P2.E2", 1, 0, 0),
				});

				DatabaseTests.AssertResult(db.LoadTableKind<ElementMetric>(NameKind.Human), new[]
				{
					new ElementMetric("", "", "", 7, 2, 5) { Kind = NameKind.Human },
					new ElementMetric("", "", "Field2", 3, 2, 2) { Kind = NameKind.Human },
					new ElementMetric("", "", "Field1", 1, 0, 0) { Kind = NameKind.Human },
				});
			}
		}

		[Test]
		public void TestQueryDataset()
		{
			using (var db = new JobDatabase(_job.DatabasePath))
			{
				DatabaseTests.AssertResult(db.LoadTableKind<DatasetMetric>(NameKind.Machine), new[]
				{
					new DatasetMetric("S3.A3.P3/D3", 3, 2, 2),
					new DatasetMetric("S5.A5.P5/D5", 2, 2, 2),
					new DatasetMetric("S3.A3.P3/D8", 1, 1, 1),
					new DatasetMetric("S4.A4.P4/D4", 1, 1, 1),
					new DatasetMetric("S2.A2.P1/D1", 1, 0, 0),
					new DatasetMetric("S3.A3.P2/D2", 1, 0, 0),
				});

				DatabaseTests.AssertResult(db.LoadTableKind<DatasetMetric>(NameKind.Human), new[]
				{
					// If fieldIds are being used, don't show data sets w/o fieldId attribute
					new DatasetMetric("D1", 1, 0, 0) { Kind = NameKind.Human },
					new DatasetMetric("D2", 1, 0, 0) { Kind = NameKind.Human },
				});
			}
		}

		[Test]
		public void TestQueryFaultTimeline()
		{
			using (var db = new JobDatabase(_job.DatabasePath))
			{
				DatabaseTests.AssertResult(db.LoadTable<FaultTimelineMetric>(), new[]
				{
					new FaultTimelineMetric(_now, 2),
					new FaultTimelineMetric(_now + TimeSpan.FromHours(1), 1),
					new FaultTimelineMetric(_now + TimeSpan.FromHours(2), 1),
					new FaultTimelineMetric(_now + TimeSpan.FromHours(3), 1),
					new FaultTimelineMetric(_now + TimeSpan.FromHours(4), 1),
				});
			}
		}

		[Test]
		public void TestQueryFaults()
		{
			using (var db = new JobDatabase(_job.DatabasePath))
			{
				DatabaseTests.AssertResult(db.LoadTableKind<FaultMutation>(NameKind.Machine), new[]
				{
					new FaultMutation(1, "S2_1", "A2", "P1.E1", "M1", ""),
					new FaultMutation(3, "S3_1", "A3", "P3.E3", "M3", "D3"),
					new FaultMutation(4, "S4_1", "A4", "P4.E4", "M4", "D4"),
					new FaultMutation(4, "S4_1", "A5", "P4.E5", "M9", "D9"),
					new FaultMutation(5, "S5_1", "A5", "P5.E5", "M5", "D5"),
					new FaultMutation(5, "S5_2", "A5", "P5.E5", "M5", "D5"),
					new FaultMutation(6, "S3_1", "A3", "P3.E3", "M3", "D8"),
					new FaultMutation(8, "S3_1", "A3", "P3.E3", "M3", "D3"),
				});

				DatabaseTests.AssertResult(db.LoadTableKind<FaultMutation>(NameKind.Human), new[]
				{
					new FaultMutation(1, "", "", "", "M1", "") { Kind = NameKind.Human },
					new FaultMutation(3, "", "", "", "M3", "") { Kind = NameKind.Human },
					new FaultMutation(4, "", "", "Field2", "M4", "") { Kind = NameKind.Human },
					new FaultMutation(4, "", "", "", "M9", "") { Kind = NameKind.Human },
					new FaultMutation(5, "", "", "Field2", "M5", "") { Kind = NameKind.Human },
					new FaultMutation(5, "", "", "Field2", "M5", "") { Kind = NameKind.Human },
					new FaultMutation(6, "", "", "", "M3", "") { Kind = NameKind.Human },
					new FaultMutation(8, "", "", "", "M3", "") { Kind = NameKind.Human },
				});

				DatabaseTests.AssertResult(db.GetFaultMutations(5, NameKind.Machine), new[]
				{
					new FaultMutation(5, "S5_1", "A5", "P5.E5", "M5", "D5"),
					new FaultMutation(5, "S5_2", "A5", "P5.E5", "M5", "D5"),
				});
			}
		}
	}
}
