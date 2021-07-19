

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices.Models;
using Encoding = Peach.Core.Encoding;
using Logger = Peach.Core.Logger;
using System.Diagnostics;
using Action = Peach.Core.Dom.Action;
using State = Peach.Core.Dom.State;
using Peach.Pro.Core.License;
using System.Reflection;

namespace Peach.Pro.Core.Loggers
{
	public enum CompleteTestEvents
	{
		None,
		Last,
		All
	}

	/// <summary>
	/// Standard file system Logger.
	/// </summary>
	[Logger("File")]
	[Logger("Filesystem", true)]
	[Logger("Logger.Filesystem")]
	[Parameter("Path", typeof(string), "Log folder", "")]
	public class JobLogger : Logger
	{
		private class MergedFault : Fault
		{
			public class Asset : INamed
			{
				public string FileName { get; set; }

				public string AgentName { get; set; }
				public string MonitorName { get; set; }
				public string MonitorClass { get; set; }
				public string DisplayName { get; set; }
				public Stream Value { get; set; }
				public bool Initial { get; set; }
				public FaultFileType Type { get; set; }

				[Obsolete]
				string INamed.name
				{
					get { throw new NotImplementedException(); }
				}

				[Obsolete]
				string INamed.Name
				{
					get { return FileName; }
				}
			}

			public readonly List<Asset> Assets = new List<Asset>();
		}

		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		const string DebugLogLayout = "${longdate} ${logger} ${message} ${exception:format=tostring}";

		// Filter these loggers to the info level since they are spammy at debug
		static readonly string[] FilteredLoggers =
		{
			"Peach.Core.Dom.Array",
			"Peach.Core.Dom.Choice",
			"Peach.Core.Dom.DataElement",
			"Peach.Core.Cracker.DataCracker"
		};

		readonly List<Fault.State> _states = new List<Fault.State>();
		readonly List<TestEvent> _events = new List<TestEvent>();
		readonly List<LoggingRule> _tempRules = new List<LoggingRule>();
		MergedFault _reproFault;
		TextWriter _log;
		AsyncDbCache _cache;
		int _agentConnect;
		Exception _caught;
		Target _tempTarget;
		ILicense _license;
		IJobLicense _jobLicense;
		ulong _counter;

		enum Category { Faults, Reproducing, NonReproducible }

		/// <summary>
		/// The user configured base path for all the logs
		/// </summary>
		public string BasePath { get; set; }

		public JobLogger()
		{
			BasePath = Configuration.LogRoot;
		}

		public JobLogger(Dictionary<string, Variant> args)
		{
			Variant path;
			if (!args.TryGetValue("Path", out path))
				path = new Variant(Configuration.LogRoot);
			BasePath = Path.GetFullPath((string)path);
		}

		public void Initialize(RunConfiguration config, ILicense license, IJobLicense jobLicense)
		{
			_license = license;
			_jobLicense = jobLicense;
			_tempTarget = new DatabaseTarget(config.id) { Layout = DebugLogLayout };
			ConfigureLogging(null, _tempTarget);
		}

		protected override void Agent_AgentConnect(RunContext context, AgentClient agent)
		{
			if (context.controlRecordingIteration)
			{
				using (var db = new NodeDatabase())
				{
					AddEvent(db,
						context.config.id,
						"Connecting to agent",
						"Connecting to agent '{0}'".Fmt(agent.Url),
						_agentConnect++ > 0  ? CompleteTestEvents.Last : CompleteTestEvents.None);
				}
			}
		}

		protected override void Agent_StartMonitor(RunContext context, AgentClient agent, string name, string cls)
		{
			if (context.controlRecordingIteration)
			{
				using (var db = new NodeDatabase())
				{
					AddEvent(db,
						context.config.id,
						"Starting monitor",
						"Starting monitor '{0}' named '{1}'".Fmt(cls, name),
						CompleteTestEvents.Last
					);

					var url = new Uri(agent.Url);
					if (url.Scheme == "local" || url.Scheme == "tcp")
					{
						// Note: don't use user supplied monitor name as it could
						//   reference an older name for the monitor.
						//   Instead lookup the attribute marked as IsDefault and use
						//   that name.

						var type = ClassLoader.FindPluginByName<MonitorAttribute>(cls);
						var monitorName = type.GetAttributes<MonitorAttribute>()
							.First(x => x.IsDefault)
							.Name;

						if (type.Assembly == Assembly.GetExecutingAssembly() &&
						   !_license.CanUseMonitor(monitorName))
						{
							throw new PeachException(
								"The {0} monitor is not supported with your current license. ".Fmt(cls) +
								"Contact Peach Fuzzer sales for more information."
							);
						}
					}
				}
			}
		}

		protected override void Agent_SessionStarting(RunContext context, AgentClient agent)
		{
			if (context.controlRecordingIteration)
			{
				using (var db = new NodeDatabase())
				{
					AddEvent(db,
						context.config.id,
						"Starting fuzzing session",
						"Notifying agent '{0}' that the fuzzing session is starting".Fmt(agent.Url),
						CompleteTestEvents.Last);
				}
			}
		}

		protected override void Engine_TestStarting(RunContext context)
		{
			Logger.Trace(">>> Engine_TestStarting");

			Debug.Assert(_log == null);

			Job job;
			using (var db = new NodeDatabase())
			{
				job = db.GetJob(context.config.id) ?? new Job(context.config);
				job.Mode = JobMode.Fuzzing;
				job.Status = JobStatus.Running;
				job.HeartBeat = DateTime.Now;
				job.Seed = context.config.randomSeed;
				job.MetricKind = context.test.stateModel.HasFieldIds ? NameKind.Human : NameKind.Machine;
			}

			if (job.DatabasePath == null)
				job.LogPath = GetLogPath(context, Path.GetFullPath(BasePath));

			if (!Directory.Exists(job.LogPath))
				Directory.CreateDirectory(job.LogPath);

			ConfigureDebugLogging(job.DebugLogPath, context.config);

			_cache = new AsyncDbCache(job);

			using (var db = new NodeDatabase())
			{
				AddEvent(db,
					context.config.id,
					"Starting fuzzing engine",
					"Starting fuzzing engine",
					CompleteTestEvents.Last);

				// Before we get iteration start, we will get AgentConnect & SessionStart
				_agentConnect = 0;

				db.UpdateJob(_cache.Job);
			}

			_log = File.CreateText(Path.Combine(_cache.Job.LogPath, "status.txt"));

			_log.WriteLine("Peach Fuzzing Run");
			_log.WriteLine("=================");
			_log.WriteLine("");
			_log.WriteLine("Date of run: " + context.config.runDateTime);
			_log.WriteLine("Peach Version: " + context.config.version);

			_log.WriteLine("Seed: " + context.config.randomSeed);

			_log.WriteLine("Command line: " + string.Join(" ", context.config.commandLine));
			_log.WriteLine("Pit File: " + context.config.pitFile);
			_log.WriteLine("Strategy: " + context.test.strategy.GetType().GetPluginName());
			_log.WriteLine(". Test starting: " + context.test.Name);
			_log.WriteLine("");

			_log.Flush();

			Logger.Trace("<<< Engine_TestStarting");
		}

		protected override void Engine_TestError(RunContext context, Exception ex)
		{
			Logger.Trace("Engine_TestError");

			_caught = ex;

			// Happens if we can't open the log during TestStarting()
			// because of permission issues
			if (_log != null)
			{
				_log.WriteLine("! Test error:");
				_log.WriteLine(ex);
				_log.Flush();
			}
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			Logger.Trace(">>> Engine_TestFinished");

			if (_jobLicense != null)
			{
				_jobLicense.Dispose();
				_jobLicense = null;
			}

			Job job;
			if (_cache != null)
			{
				Logger.Trace("Engine_TestFinished> Update JobDatabase");

				_cache.TestFinished();

				job = _cache.Job;
			}
			else
			{
				// Job was killed before TestStarting got called
				using (var db = new NodeDatabase())
				{
					job = db.GetJob(context.config.id) ?? new Job(context.config);
					job.StopDate = DateTime.Now;
					job.HeartBeat = job.StopDate;
					job.Mode = JobMode.Fuzzing;
					job.Status = JobStatus.Stopping;
				}
			}

			Logger.Trace("Engine_TestFinished> Update NodeDatabase");
			using (var db = new NodeDatabase())
			{
				if (_caught == null)
				{
					AddEvent(db, job.Guid, "Flushing logs.", "Flushing logs.", CompleteTestEvents.All);
				}
				db.UpdateJob(job);
			}

			// it's possible we reach here before Engine_TestStarting has had a chance to finish
			if (_log != null)
			{
				Logger.Trace("Engine_TestFinished> Close status.txt");
				_log.WriteLine(". Test finished: " + context.test.Name);
				_log.Flush();
				_log.Close();
				_log.Dispose();
				_log = null;
			}

			Logger.Trace("<<< Engine_TestFinished");
		}

		protected override void Engine_IterationStarting(
			RunContext context,
			uint currentIteration,
			uint? totalIterations)
		{
			if (!context.controlIteration && !context.controlRecordingIteration)
			{
				if (_counter >= 100)
				{
					Debug.Assert(_jobLicense != null);
					if (!_jobLicense.CanExecuteTestCase())
					{
						throw new PeachException(
							"Total number of FuzzFlex test cases reached. " +
							"Contact sales@peachfuzzer.com for more test cases."
						);
					}
				}
				_counter++;
			}
			
			_states.Clear();

			var mode = JobMode.Fuzzing;

			if (context.reproducingFault)
			{
				if (context.reproducingIterationJumpCount == 0)
				{
					Debug.Assert(context.reproducingInitialIteration == currentIteration);
					mode = JobMode.Reproducing;
				}
				else
				{
					mode = JobMode.Searching;
				}
			}

			_cache.IterationStarting(mode);

			if (context.controlRecordingIteration)
			{
				using (var db = new NodeDatabase())
				{
					var desc = context.reproducingFault
						? "Reproducing fault found on the initial control record iteration"
						: "Running the initial control record iteration";

					// Add event for the iteration running
					AddEvent(db, context.config.id, "Running iteration", desc, CompleteTestEvents.All);
				}

				_log.WriteLine(". Record Iteration {0}", currentIteration);
				_log.Flush();
			}
			else if (!context.reproducingFault && (currentIteration == 1 || currentIteration % 100 == 0))
			{
				if (totalIterations.HasValue && totalIterations.Value < uint.MaxValue)
				{
					_log.WriteLine(". {0}Iteration {1} of {2} : {3}",
						context.controlIteration ? "Control " : "",
						currentIteration, (uint)totalIterations, DateTime.Now);
					_log.Flush();
				}
				else
				{
					_log.WriteLine(". {0} Iteration {1} : {2}",
						context.controlIteration ? "Control " : "",
						currentIteration, DateTime.Now);
					_log.Flush();
				}
			}
		}

		protected override void Engine_IterationFinished(RunContext context, uint currentIteration)
		{
			if (!context.reproducingFault &&
				!context.controlIteration &&
				!context.controlRecordingIteration)
			{
				_cache.IterationFinished();
			}
		}

		protected override void StateStarting(RunContext context, State state)
		{
			_states.Add(new Fault.State
			{
				name = state.Name,
				actions = new List<Fault.Action>()
			});

			_cache.StateStarting(state.Name, state.FieldId ?? "", state.runCount);
		}

		protected override void ActionStarting(RunContext context, Action action)
		{
			_cache.ActionStarting(action.Name, action.FieldId ?? "");

			var rec = new Fault.Action
			{
				name = action.Name,
				type = action.type,
				models = new List<Fault.Model>()
			};

			foreach (var data in action.allData)
			{
				rec.models.Add(new Fault.Model
				{
					name = data.dataModel.Name,
					parameter = data.Name ?? "",
					dataSet = data.selectedData != null ? data.selectedData.Name : "",
					mutations = new List<Fault.Mutation>(),
				});
			}

			if (rec.models.Count == 0)
				rec.models = null;

			_states.Last().actions.Add(rec);
		}

		protected override void ActionFinished(RunContext context, Action action)
		{
			var rec = _states.Last().actions.Last();
			if (rec.models == null)
				return;

			foreach (var model in rec.models)
			{
				Debug.Assert(model.mutations != null);

				if (model.mutations.Count == 0)
					model.mutations = null;
			}
		}

		protected override void DataMutating(
			RunContext context,
			ActionData data,
			DataElement element,
			Mutator mutator)
		{
			var rec = _states.Last().actions.Last();

			var tgtName = data.dataModel.Name;
			var tgtParam = data.Name ?? "";
			var tgtDataSet = data.selectedData != null ? data.selectedData.Name : "";
			var model = rec.models.FirstOrDefault(m =>
				m.name == tgtName &&
				m.parameter == tgtParam &&
				m.dataSet == tgtDataSet);
			Debug.Assert(model != null);

			model.mutations.Add(new Fault.Mutation
			{
				element = element.fullName,
				mutator = mutator.Name
			});

			_cache.DataMutating(NameKind.Machine, tgtParam, element.fullName, mutator.Name, tgtDataSet);

			var fieldId = data.selectedData != null ? data.selectedData.FieldId ?? "" : "";

			_cache.DataMutating(NameKind.Human, "", element.FullFieldId ?? "", mutator.Name, fieldId);
		}

		protected override void Engine_ReproFault(
			RunContext context,
			uint currentIteration,
			StateModel stateModel,
			Fault[] faults)
		{
			Debug.Assert(_reproFault == null);

			_reproFault = CombineFaults(currentIteration, stateModel, faults);
			SaveFault(context, Category.Reproducing, _reproFault);
		}

		protected override void Engine_ReproFailed(RunContext context, uint currentIteration)
		{
			Debug.Assert(_reproFault != null);

			// Update the searching ranges for the fault
			_reproFault.iterationStart = context.reproducingInitialIteration - context.reproducingIterationJumpCount;
			_reproFault.iterationStop = _reproFault.iteration;

			SaveFault(context, Category.NonReproducible, _reproFault);
			_reproFault = null;
		}

		protected override void Engine_Fault(
			RunContext context,
			uint currentIteration,
			StateModel stateModel,
			Fault[] faults)
		{
			var fault = CombineFaults(currentIteration, stateModel, faults);

			if (_reproFault != null)
			{
				// Save reproFault assets in fault
				foreach (var item in _reproFault.Assets)
				{
					var fileName = Path.Combine(
						"Initial",
						_reproFault.iteration.ToString(),
						item.FileName);

					fault.Assets.Add(new MergedFault.Asset
					{
						FileName = fileName,
						DisplayName = item.DisplayName,
						AgentName = item.AgentName,
						Initial = true,
						MonitorClass = item.MonitorClass,
						MonitorName = item.MonitorName,
						Value = item.Value,
						Type = item.Type
					});
				}

				_reproFault = null;
			}

			SaveFault(context, Category.Faults, fault);
		}

		long SaveFile(string fullPath, Stream contents)
		{
			try
			{
				var dir = Path.GetDirectoryName(fullPath);

				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
					Directory.CreateDirectory(dir);

				contents.Seek(0, SeekOrigin.Begin);

				using (var fs = new FileStream(fullPath, FileMode.CreateNew))
				{
					contents.CopyTo(fs, BitwiseStream.BlockCopySize);
					return fs.Position;
				}
			}
			catch (Exception e)
			{
				throw new PeachException(e.Message, e);
			}
		}

		private MergedFault CombineFaults(uint currentIteration, StateModel stateModel, Fault[] faults)
		{
			// The combined fault will use Assets and not collectedData
			var ret = new MergedFault { collectedData = null };

			var coreFault = faults.FirstOrDefault(f => f.type == FaultType.Fault);

			if (coreFault == null)
				throw new PeachException("Error, we should always have a fault with type = Fault!");

			Logger.Debug("Found core fault [" + coreFault.title + "]");

			// Gather up data from the state model
			foreach (var item in stateModel.dataActions)
			{
				Logger.Debug("Saving data from action: " + item.Key);

				// Genrating an aligned Stream here to make sure all
				// data is saved correctly.  For BitwiseStream, if
				// .LengthBits % 8 != 0, then .Length will not include
				// the trailing bits.  Padding will be required in this case.

				ret.Assets.Add(new MergedFault.Asset
				{
					DisplayName = item.Name,
					FileName = item.Key,
					Value = item.Value.PadBits(),
					Type = item.IsInput ? FaultFileType.Input : FaultFileType.Ouput
				});
			}

			// Write out all collected data information
			foreach (var fault in faults)
			{
				Logger.Debug("Saving fault: " + fault.title);

				// Put description at beginning of list
				if (!string.IsNullOrEmpty(fault.description))
				{
					// Use System.Text.Encoding so invalid chars get substituted with '?'
					var desc = System.Text.Encoding.UTF8.GetBytes(fault.description);
					ret.Assets.Add(MakeFileAsset(fault, "description.txt", desc));
				}

				// Put collected data second
				foreach (var kv in fault.collectedData)
				{
					ret.Assets.Add(MakeFileAsset(fault, kv.Key, kv.Value));
				}
			}

			// Save all states, actions, data sets, mutations
			var json = JsonConvert.SerializeObject(
				new { States = _states },
				Formatting.Indented,
				new JsonSerializerSettings
				{
					DefaultValueHandling = DefaultValueHandling.Ignore
				}
			);

			ret.Assets.Add(new MergedFault.Asset
			{
				DisplayName = "fault.json",
				FileName = "fault.json",
				Value = new MemoryStream(Encoding.UTF8.GetBytes(json)),
				Type = FaultFileType.Asset
			});

			// Copy over information from the core fault
			ret.folderName = coreFault.folderName;
			ret.controlIteration = coreFault.controlIteration;
			ret.controlRecordingIteration = coreFault.controlRecordingIteration;
			ret.description = coreFault.description;
			ret.detectionSource = coreFault.detectionSource;
			ret.monitorName = coreFault.monitorName;
			ret.agentName = coreFault.agentName;
			ret.iteration = currentIteration;
			ret.iterationStart = coreFault.iterationStart;
			ret.iterationStop = coreFault.iterationStop;
			ret.exploitability = coreFault.exploitability;
			ret.majorHash = coreFault.majorHash;
			ret.minorHash = coreFault.minorHash;
			ret.title = coreFault.title;
			ret.type = coreFault.type;
			ret.states = _states;

			// If a folder name was not specified, try using hashes for bucketing
			if (string.IsNullOrEmpty(ret.folderName))
				ret.folderName = string.Join("_", new[]
				{
					ret.exploitability,
					ret.majorHash,
					ret.minorHash
				}.Where(s => !string.IsNullOrEmpty(s)));

			// If no hashes were specified, use sensible default
			if (string.IsNullOrEmpty(ret.folderName))
				ret.folderName = "UNKNOWN";

			// DetectionSource needs to be set
			if (string.IsNullOrEmpty(ret.detectionSource))
				ret.detectionSource = "Unknown";

			// Default the major hash to be the hash of the detection source
			if (string.IsNullOrEmpty(ret.majorHash))
				ret.majorHash = Monitor2.Hash(ret.detectionSource);

			// Default the minor hash to be the major hash
			if (string.IsNullOrEmpty(ret.minorHash))
				ret.minorHash = ret.majorHash;

			// Default the risk to "UNKNOWN"
			if (string.IsNullOrEmpty(ret.exploitability))
				ret.exploitability = "UNKNOWN";

			return ret;
		}

		private static MergedFault.Asset MakeFileAsset(Fault fault, string name, byte[] value)
		{
			var parts = new[]
			{
				fault.agentName,
				fault.monitorName,
				fault.detectionSource,
				name
			};


			if (parts.Any(string.IsNullOrEmpty))
			{
				// If agentName, detectionSource or monitorName is missing treat asset as "Other"
				var fileName = string.Join(".", parts.Where(a => !string.IsNullOrEmpty(a)));

				return new MergedFault.Asset
				{
					DisplayName = fileName,
					FileName = fileName,
					Value = new MemoryStream(value),
					Type = FaultFileType.Asset
				};
			}

			return new MergedFault.Asset
			{
				AgentName = fault.agentName,
				MonitorClass = fault.detectionSource,
				MonitorName = fault.monitorName,
				DisplayName = name,
				FileName = string.Join(".", parts),
				Value = new MemoryStream(value),
				Type = FaultFileType.Asset
			};
		}

		void SaveFault(RunContext context, Category category, MergedFault fault)
		{
			var now = DateTime.Now;

			var desc = "at {0}{1}iteration {2}".Fmt(
				fault.controlIteration ? "control " : "",
				fault.controlRecordingIteration ? "record " : "",
				fault.iteration);

			switch (category)
			{
				case Category.Faults:
					_log.WriteLine("! Reproduced fault {0} : {1}",
						desc, now);
					break;
				case Category.NonReproducible:
					_log.WriteLine("! Non-reproducible fault detected {0} : {1}",
						desc, now);
					break;
				case Category.Reproducing:
					_log.WriteLine("! Fault detected {0}, trying to reproduce : {1}",
						desc, now);
					break;
			}

			// Fault should have already been sanitized by CombineFaults
			Debug.Assert(!string.IsNullOrEmpty(fault.folderName));
			Debug.Assert(!string.IsNullOrEmpty(fault.majorHash));
			Debug.Assert(!string.IsNullOrEmpty(fault.minorHash));
			Debug.Assert(!string.IsNullOrEmpty(fault.exploitability));

			// root/category/bucket/iteration
			var initialFaultPath = Path.Combine(
				category.ToString(),
				fault.folderName,
				fault.iteration.ToString(CultureInfo.InvariantCulture)
			);

			if (context.controlRecordingIteration)
				initialFaultPath += "R";
			else if (context.controlIteration)
				initialFaultPath += "C";

			var faultPath = initialFaultPath;

			for (var i = 1; Directory.Exists(Path.Combine(_cache.Job.LogPath, faultPath)); ++i)
				faultPath = initialFaultPath + "_" + i;

			var faultDetail = new FaultDetail
			{
				Files = new List<FaultFile>(),
				Reproducible = category == Category.Faults,
				Iteration = fault.iteration,
				TimeStamp = now,
				Source = fault.detectionSource,
				Exploitability = fault.exploitability,
				MajorHash = fault.majorHash,
				MinorHash = fault.minorHash,

				Title = fault.title,
				Description = fault.description,
				Seed = context.config.randomSeed,
				IterationStart = fault.iterationStart,
				IterationStop = fault.iterationStop,

				FaultPath = faultPath,
			};

			if (context.controlIteration)
				faultDetail.Flags |= IterationFlags.Control;

			if (context.controlRecordingIteration)
				faultDetail.Flags |= IterationFlags.Record;

			var subDir = Path.Combine(_cache.Job.LogPath, faultPath);

			foreach (var kv in fault.Assets)
			{
				var fileName = Path.Combine(subDir, kv.FileName);
				var size = SaveFile(fileName, kv.Value);
				faultDetail.Files.Add(new FaultFile
				{
					Name = kv.DisplayName,
					FullName = kv.FileName,
					Size = size,
					Initial = kv.Initial,
					AgentName = kv.AgentName,
					MonitorClass = kv.MonitorClass,
					MonitorName = kv.MonitorName,
					Type = kv.Type
				});
			}

			if (category != Category.Reproducing)
			{
				// Ensure any past saving of this fault as Reproducing has been cleaned up
				var reproDir = Path.Combine(_cache.Job.LogPath, Category.Reproducing.ToString());

				if (Directory.Exists(reproDir))
				{
					try
					{
						Directory.Delete(reproDir, true);
					}
					catch (IOException)
					{
						// Can happen if a process has a file/subdirectory open...
					}
				}

				_cache.OnFault(faultDetail);
			}

			_log.Flush();
		}

		public void AddEvent(
			NodeDatabase db, 
			Guid jobId, 
			string name, 
			string description,
			CompleteTestEvents complete)
		{
			IEnumerable<TestEvent> completed;
			switch (complete)
			{
				case CompleteTestEvents.All:
					completed = _events;
					break;
				case CompleteTestEvents.Last:
					completed = _events.Count > 0 
						? _events.LastEnumerable() 
						: new TestEvent[0];
					break;
				default:
					completed = new TestEvent[0];
					break;
			}

			db.Transaction(() =>
			{
				db.UpdateTestEvents(completed.Select(x =>
				{
					x.Status = TestStatus.Pass;
					return x;
				}));

				var testEvent = new TestEvent
				{
					JobId = jobId.ToString(),
					Status = TestStatus.Active,
					Short = name,
					Description = description,
				};
				db.InsertTestEvent(testEvent);
				_events.Add(testEvent);
			});
		}

		public void EventFail(NodeDatabase db, string resolve)
		{
			db.UpdateTestEvents(_events.LastEnumerable().Select(x =>
			{
				x.Status = TestStatus.Fail;
				x.Resolve = resolve;
				return x;
			}));
		}

		public void JobFail(Guid id, string message)
		{
			JobHelper.Fail(id, _ => _events, message);
		}

		public void Pause()
		{
			_cache.Pause();
		}

		public void Continue()
		{
			_cache.Continue();
		}

		public void Stop()
		{
			// Will be null if stop gets called prior to TestStarting event
			if (_cache != null)
				_cache.Stop();
		}

		public void RestoreLogging(Guid id)
		{
			Logger.Trace("RestoreLogging>");

			ConfigureLogging(_tempTarget, null);
			_tempTarget = null;

			using (var db = new NodeDatabase())
			{
				var job = db.GetJob(id);
				job.StopDate = DateTime.Now;
				job.HeartBeat = job.StopDate;
				job.Status = JobStatus.Stopped;
				db.UpdateJob(job);
				db.PassPendingTestEvents(id);
			}
		}

		void ConfigureDebugLogging(string logPath, RunConfiguration config)
		{
			var target = new FileTarget
			{
				Name = "FileTarget",
				Layout = DebugLogLayout,
				FileName = logPath,
				ConcurrentWrites = false,
				KeepFileOpen = !config.singleIteration,
				ArchiveAboveSize = 10 * 1024 * 1024,
				MaxArchiveFiles = 10,
				ArchiveNumbering = ArchiveNumberingMode.Sequence,
				Encoding = System.Text.Encoding.UTF8,
			};

			var oldTarget = _tempTarget;
			_tempTarget = new AsyncTargetWrapper(target)
			{
				Name = target.Name,
				OverflowAction = AsyncTargetWrapperOverflowAction.Block
			};

			ConfigureLogging(oldTarget, _tempTarget);

			Logger.Info("Writing debug.log to: {0}", logPath);
		}

		void ConfigureLogging(Target oldTarget, Target newTarget)
		{
			var nconfig = LogManager.Configuration;
			
			if (oldTarget != null)
			{
				nconfig.RemoveTarget(oldTarget.Name);
				foreach (var rule in _tempRules)
					nconfig.LoggingRules.Remove(rule);
				_tempRules.Clear();
				oldTarget.Dispose();
			}

			if (newTarget != null)
			{
				nconfig.AddTarget(newTarget.Name, newTarget);

				foreach (var logger in FilteredLoggers)
				{
					var rule = new LoggingRule(logger, LogLevel.Info, newTarget) { Final = true };
					_tempRules.Add(rule);
					nconfig.LoggingRules.Add(rule);
				}

				var defaultRule = new LoggingRule(
					"*", 
					Logger.IsTraceEnabled ? LogLevel.Trace : LogLevel.Debug, 
					newTarget);

				_tempRules.Add(defaultRule);
				nconfig.LoggingRules.Add(defaultRule);
			}

			LogManager.Configuration = nconfig;
		}
	}
}
