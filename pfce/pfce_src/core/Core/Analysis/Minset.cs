


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;
using System.Collections;

namespace Peach.Core.Analysis
{
	public delegate void TraceEventHandler(object sender, string fileName, int count, int totalCount);
	public delegate void TraceEventMessage(object sender, string message);
	public delegate void TraceLoadedHandler(object sender, int trace);

	/// <summary>
	/// Perform analysis on sample sets to identify the smallest sample set
	/// that provides the largest code coverage.
	/// </summary>
	public class Minset
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public event TraceEventHandler TraceStarting;
		public event TraceEventHandler TraceCompleted;
		public event TraceEventHandler TraceFailed;
		public event TraceEventMessage TraceMessage;
		public event TraceLoadedHandler TraceLoaded;

		protected void OnTraceStarting(string fileName, int count, int totalCount)
		{
			if (TraceStarting != null)
				TraceStarting(this, fileName, count, totalCount);
		}

		public void OnTraceCompleted(string fileName, int count, int totalCount)
		{
			if (TraceCompleted != null)
				TraceCompleted(this, fileName, count, totalCount);
		}

		public void OnTraceFaled(string fileName, int count, int totalCount)
		{
			if (TraceFailed != null)
				TraceFailed(this, fileName, count, totalCount);
		}

		private void ValidateTraces(List<string> samples, List<string> traces)
		{
			samples.Sort(string.CompareOrdinal);
			traces.Sort(string.CompareOrdinal);

			var i = 0;

			while (i < samples.Count || i < traces.Count)
			{
				int cmp;

				if (i == samples.Count)
					cmp = 1;
				else if (i == traces.Count)
					cmp = -1;
				else
					cmp = string.CompareOrdinal(Path.GetFileName(samples[i] + ".trace"), Path.GetFileName(traces[i]));

				if (cmp < 0)
				{
					if (TraceMessage != null)
						TraceMessage(this, "Ignoring sample '{0}' becaues of mising trace file.".Fmt(samples[i]));

					Logger.Debug("Ignoring sample '{0}' becaues of mising trace file.".Fmt(samples[i]));

					samples.RemoveAt(i);
				}
				else if (cmp > 0)
				{
					if (TraceMessage != null)
						TraceMessage(this, "Ignoring trace '{0}' becaues of mising sample file.".Fmt(traces[i]));

					Logger.Debug("Ignoring trace '{0}' becaues of mising sample file.".Fmt(traces[i]));

					traces.RemoveAt(i);
				}
				else
				{
					++i;
				}
			}
		}

		class BasicBlock
		{
			public ushort Module { get; set; }
			public ulong Address { get; set; }

			public override bool Equals(object obj)
			{
				var rhs = obj as BasicBlock;
				if (rhs == null)
					return false;
				return Module == rhs.Module && Address == rhs.Address;
			}

			public override int GetHashCode()
			{
				return Module.GetHashCode() ^ Address.GetHashCode();
			}
		}

		/// <summary>
		/// Perform coverage analysis of trace files.
		/// </summary>
		/// <remarks>
		/// Note: The sample and trace collections must have matching indexes.
		/// </remarks>
		/// <param name="sampleFiles">Collection of sample files</param>
		/// <param name="traceFiles">Collection of trace files for sample files</param>
		/// <returns>Returns the minimum set of smaple files.</returns>
		public string[] RunCoverage(string[] sampleFiles, string[] traceFiles)
		{
			var samples = sampleFiles.ToList();
			var traces = traceFiles.ToList();

			// Expect samples and traces to correlate 1 <-> 1
			ValidateTraces(samples, traces);

			Debug.Assert(samples.Count == traces.Count);

			var modules = new List<string>();
			var coverage = new Dictionary<int, long>(traces.Count);
			var db = new Dictionary<BasicBlock, BitArray>();

			if (TraceMessage != null)
				TraceMessage(this, "Loading {0} trace files...".Fmt(traces.Count));

			for (var i = 0; i < traces.Count; ++i)
			{
				var trace = traces[i];

				Logger.Debug("Loading '{0}'", trace);

				using (var rdr = new StreamReader(trace))
				{
					long count = 0;

					string line;
					while ((line = rdr.ReadLine()) != null)
					{
						var delimiter = line.IndexOf(' ');
						var strAddress = line.Substring(0, delimiter);
						var strModule = line.Substring(delimiter + 1);

						var module = modules.IndexOf(strModule);
						if (module == -1)
						{
							module = modules.Count;
							modules.Add(strModule);
						}

						var block = new BasicBlock
						{
							Module = (ushort)module,
							Address = Convert.ToUInt64(strAddress, 16)
						};

						BitArray bits;
						if (!db.TryGetValue(block, out bits))
						{
							bits = new BitArray(traces.Count);
							db.Add(block, bits);
						}
						bits.Set(i, true);
						count++;
					}
				
					coverage[i] = count;
				}

				GC.Collect();
				if (TraceLoaded != null)
					TraceLoaded(this, i);
			}

			if (TraceMessage != null)
				TraceMessage(this, "Computing minimum set coverage...".Fmt(traces.Count));

			Logger.Debug("Loaded {0} files, starting minset computation", traces.Count);

			var total = db.Count;
			var ret = new List<string>();
			var isFirst = true;

			while (coverage.Count > 0)
			{
				// Find trace with greatest coverage
				if (isFirst)
				{
					isFirst = false;
				}
				else
				{
					foreach (var kv in db)
					{
						for (var i = 0; i < kv.Value.Length; i++)
						{
							if (kv.Value[i])
								coverage[i] += 1;
						}
					}
				}

				var max = coverage.Max(kv => kv.Value);
				var keep = coverage.First(kv => kv.Value == max).Key;

				if (max == 0)
					break;

				Logger.Debug("Keeping '{0}' with coverage {1}/{2}", keep, max, total);

				if (max < 10)
				{
					foreach (var x in db.Where(kv => kv.Value.Get(keep)))
						Logger.Debug(x.Key);
				}

				ret.Add(samples[keep]);

				// Don't track selected trace anymore
				coverage.Remove(keep);

				// Reset coverage counts to 0
				foreach (var k in coverage.Keys.ToList())
					coverage[k] = 0;

				// Select all rows that are now covered
				var prune = db
					.Where(kv => kv.Value.Get(keep))
					.Select(kv => kv.Key)
					.ToList();

				// Remove all covered rows
				foreach (var p in prune)
					db.Remove(p);
			}

			Logger.Debug("Removing {0} sample files", coverage.Count);

			foreach (var kv in coverage)
				Logger.Debug(" - {0}", kv.Key);

			Logger.Debug("Done");

			return ret.ToArray();
		}

		/// <summary>
		/// Collect traces for a collection of sample files.
		/// </summary>
		/// <remarks>
		/// This method will use the TraceStarting and TraceCompleted events
		/// to report progress.
		/// </remarks>
		/// <param name="executable">Executable to run.</param>
		/// <param name="arguments">Executable arguments.  Must contain a "%s" placeholder for the sampe filename.</param>
		/// <param name="tracesFolder">Where to write trace files</param>
		/// <param name="sampleFiles">Collection of sample files</param>
		/// <param name="needsKilling">Does this command requiring forcefull killing to exit?</param>
		/// <returns>Returns a collection of trace files</returns>
		public string[] RunTraces(string executable, string arguments, string tracesFolder, string[] sampleFiles, bool needsKilling = false)
		{
			try
			{
				var cov = new Coverage(executable, arguments, needsKilling);
				var ret = new List<string>();

				for (var i = 0; i < sampleFiles.Length; ++i)
				{
					var sampleFile = sampleFiles[i];
					var traceFile = Path.Combine(tracesFolder, Path.GetFileName(sampleFile) + ".trace");

					Logger.Debug("Starting trace [{0}:{1}] {2}", i + 1, sampleFiles.Length, sampleFile);

					OnTraceStarting(sampleFile, i + 1, sampleFiles.Length);

					try
					{
						cov.Run(sampleFile, traceFile);
						ret.Add(traceFile);
						Logger.Debug("Successfully created trace {0}", traceFile);
						OnTraceCompleted(sampleFile, i + 1, sampleFiles.Length);
					}
					catch (Exception ex)
					{
						Logger.Debug("Failed to generate trace.\n{0}", ex.Message);
						OnTraceFaled(sampleFile, i + 1, sampleFiles.Length);
					}
				}

				return ret.ToArray();
			}
			catch (Exception ex)
			{
				Logger.Debug(ex, "Failed to create coverage.");

				throw new PeachException(ex.Message, ex);
			}
		}
	}
}
