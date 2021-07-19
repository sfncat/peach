using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using Peach.Core;
using Peach.Core.Runtime;
using Peach.Pro.Core.License;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Core.Runtime
{
	public class Worker : BaseProgram
	{
		string _pitLibraryPath;
		string _query;
		Guid? _guid;
		uint? _start;
		uint? _stop;
		TimeSpan? _duration;
		uint? _seed;
		bool _init;
		bool? _test;
		bool syncLogging;

		protected override void AddCustomOptions(OptionSet options)
		{
			options.Add(
				"pits=",
				"The path to the pit library",
				v => _pitLibraryPath = v
			);
			options.Add(
				"guid=",
				"The guid that identifies a job",
				(Guid v) => _guid = v
			);
			options.Add(
				"seed=",
				"The seed used by the random number generator",
				(uint v) => _seed = v
			);
			options.Add(
				"start=",
				"The iteration to start fuzzing",
				(uint v) => _start = v
			);
			options.Add(
				"stop=",
				"The iteration to stop fuzzing",
				(uint v) => _stop = v
			);
			options.Add(
				"duration=",
				"How long to run the fuzzer for",
				(TimeSpan v) => _duration = v
			);
			options.Add(
				"init",
				"Initialize a new job",
				v => _init = true
			);
			options.Add(
				"query=",
				v => _query = v
			);
			options.Add(
				"logRoot=",
				"The root directory for output files",
				v => Configuration.LogRoot = v
			);
			options.Add(
				"test",
				"Run a single dry iteration to test a pit",
				v => _test = true
			);
			options.Add(
				"syncLogging",
				"Use synchronous logging, useful for testing",
				v => syncLogging = true
			);
		}

		protected override void ConfigureLogging()
		{
			// Override logging so that we force messages to stderr instead of stdout

			Target target = new ColoredConsoleTarget
			{
				Layout = "${longdate} ${logger} ${message} ${exception:format=tostring}",
				ErrorStream = true,
			};

			if (!syncLogging)
			{
				target = new AsyncTargetWrapper(target)
				{
					OverflowAction = AsyncTargetWrapperOverflowAction.Block
				};
			}

			var rule = new LoggingRule("*", LogLevel, target);

			var nconfig = new LoggingConfiguration();
			nconfig.AddTarget("console", target);
			nconfig.LoggingRules.Add(rule);
			LogManager.Configuration = nconfig;

			Configuration.LogLevel = LogLevel;
		}

		protected override int OnRun(List<string> args)
		{
			if (!string.IsNullOrEmpty(_query))
			{
				RunQuery();
				return 0;
			}

			if (args.Count == 0)
				throw new SyntaxException("Missing <pit> argument.");

			PrepareLicensing(_pitLibraryPath, false);
			if (_license.Status != LicenseStatus.Valid)
				return -1;

			if (_init)
				InitJob(args.First());
			else
				RunJob(args.First());

			return 0;
		}

		Job InitJob(string pitFile)
		{
			if (!_guid.HasValue)
				_guid = Guid.NewGuid();

			using (var db = new NodeDatabase())
			{
				var job = db.GetJob(_guid.Value);
				if (job != null)
					throw new Exception("Job has already been initialized.");

				// this code should be identical to JobRunner.Start()
				job = new Job
				{
					Guid = _guid.Value,
					PitFile = Path.GetFileName(pitFile),
					StartDate = DateTime.Now,
					Status = JobStatus.Starting,
					Mode = JobMode.Preparing,
					PeachVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),

					RangeStart = _start ?? 0,
					RangeStop = _stop,
					Duration = _duration,
					Seed = _seed,
					DryRun = _test.HasValue && _test.Value,
				};
				db.InsertJob(job);
				return job;
			}
		}

		void RunJob(string pitFile)
		{
			if (!_guid.HasValue)
				_guid = Guid.NewGuid();

			Job job;
			using (var db = new NodeDatabase())
			{
				job = db.GetJob(_guid.Value) ?? InitJob(pitFile);
			}

			var runner = new JobRunner(_license, job, _pitLibraryPath, pitFile);
			var evtReady = new AutoResetEvent(false);
			var engineTask = Task.Factory.StartNew(() => runner.Run(evtReady), TaskCreationOptions.LongRunning);
			if (!evtReady.WaitOne(1000))
				throw new PeachException("Timeout waiting for job to start");
			Loop(runner, engineTask);
		}

		private void Loop(JobRunner runner, Task engineTask)
		{
			Console.WriteLine("OK");

			while (true)
			{
				Console.Write("> ");
				var readerTask = Task.Factory.StartNew<string>(Console.ReadLine, TaskCreationOptions.LongRunning);
				var index = Task.WaitAny(engineTask, readerTask);
				if (index == 0)
				{
					// this causes any unhandled exceptions to be thrown
					engineTask.Wait(TimeSpan.FromSeconds(10));
					return;
				}

				switch (readerTask.Result)
				{
					case "help":
						ShowHelp();
						break;
					case "stop":
						Console.WriteLine("OK");
						runner.Stop();
						engineTask.Wait(TimeSpan.FromSeconds(10));
						return;
					case "pause":
						Console.WriteLine("OK");
						runner.Pause();
						break;
					case "continue":
						Console.WriteLine("OK");
						runner.Continue();
						break;
					default:
						Console.WriteLine("Invalid command");
						break;
				}
			}
		}

		private void ShowHelp()
		{
			Console.WriteLine("Available commands:");
			Console.WriteLine("    help");
			Console.WriteLine("    stop");
			Console.WriteLine("    pause");
			Console.WriteLine("    continue");
		}

		protected override string UsageLine
		{
			get
			{
				var name = Assembly.GetEntryAssembly().GetName();
				return "Usage: {0} [OPTION]... <pit> [<name>]".Fmt(name.Name);
			}
		}

		private void RunQuery()
		{
			if (!_guid.HasValue)
				throw new SyntaxException("The '--guid' argument is required.");

			Job job;
			using (var db = new NodeDatabase())
			{
				job = db.GetJob(_guid.Value);
			}

			if (job == null || !File.Exists(job.DatabasePath))
				throw new Exception("Job not found");

			using (var db = new JobDatabase(job.DatabasePath))
			{
				switch (_query.ToLower())
				{
					case "states":
						Database.Dump(db.LoadTable<StateMetric>());
						break;
					case "iterations":
						Database.Dump(db.LoadTable<IterationMetric>());
						break;
					case "buckets":
						Database.Dump(db.LoadTable<BucketMetric>());
						break;
					case "buckettimeline":
						Database.Dump(db.LoadTable<BucketTimelineMetric>());
						break;
					case "mutators":
						Database.Dump(db.LoadTable<MutatorMetric>());
						break;
					case "elements":
						Database.Dump(db.LoadTable<ElementMetric>());
						break;
					case "datasets":
						Database.Dump(db.LoadTable<DatasetMetric>());
						break;
				}
			}
		}
	}
}
