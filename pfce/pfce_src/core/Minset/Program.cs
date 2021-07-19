


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Peach.Core;
using Peach.Core.Analysis;
using Peach.Core.Runtime;

namespace PeachMinset
{
	internal class Program
	{
		private static int Main(string[] args)
		{
			try
			{
				return new Program().Run(args);
			}
			catch (OptionException ex)
			{
				Console.WriteLine(ex.Message + "\n");
			}
			catch (SyntaxException ex)
			{
				if (!string.IsNullOrEmpty(ex.Message))
					Console.WriteLine(ex.Message + "\n");
				else
					Syntax();
			}
			catch (PeachException ex)
			{
				Console.WriteLine("{0}\n", ex.Message);

				if (ex.InnerException != null && ex.InnerException.Message != ex.Message)
					Console.WriteLine("{0}\n", ex.InnerException.Message);
			}

			return -1;
		}

		private readonly Stopwatch sw = new Stopwatch();

		public int Run(string[] args)
		{
			Console.WriteLine();
			Console.WriteLine("] Peach Minset v" + Assembly.GetExecutingAssembly().GetName().Version);
			Console.WriteLine("] {0}\n", Assembly.GetExecutingAssembly().GetCopyright());

			var kill = false;
			var verbose = 0;
			string samples = null;
			string traces = null;
			string minset = null;

			var p = new OptionSet()
				{
					{ "h|?|help", v => Syntax() },
					{ "k", v => kill = true },
					{ "v", v => verbose = 1 },
					{ "s|samples=", v => samples = v },
					{ "t|traces=", v => traces = v},
					{ "m|minset=", v => minset = v }
				};

			var extra = p.Parse(args);
			var executable = extra.FirstOrDefault();
			var arguments = string.Join(" ", extra.Skip(1));

			if (args.Length == 0)
				throw new SyntaxException("Error, missing arguments.");

			if (samples == null)
				throw new SyntaxException("Error, 'samples' argument is required.");

			if (traces == null)
				throw new SyntaxException("Error, 'traces' argument is required.");

			if (minset == null && executable == null)
				throw new SyntaxException("Error, 'minset' or command argument is required.");

			if (executable != null && !arguments.Contains("%s"))
				throw new SyntaxException("Error, command argument missing '%s'.");

			Utilities.ConfigureLogging(verbose);

			var sampleFiles = GetFiles(samples, "sample");

			// If we are generating traces, ensure we can write to the traces folder
			if (executable != null)
				VerifyDirectory(traces);

			// If we are generating minset, ensure we can write to the minset folder
			if (minset != null)
				VerifyDirectory(minset);

			var ms = new Minset();

			sw.Reset();
			sw.Start();

			if (verbose == 0)
			{
				ms.TraceCompleted += ms_TraceCompleted;
				ms.TraceStarting += ms_TraceStarting;
				ms.TraceFailed += ms_TraceFailed;
				ms.TraceMessage += ms_TraceMessage;
			}

			if (minset != null && executable != null)
				Console.WriteLine("[*] Running both trace and coverage analysis\n");

			if (executable != null)
			{
				Console.WriteLine("[*] Running trace analysis on " + sampleFiles.Length + " samples...");

				ms.RunTraces(executable, arguments, traces, sampleFiles, kill);

				Console.WriteLine("\n[{0}] Finished\n", sw.Elapsed);
			}

			if (minset == null)
				return 0;

			var traceFiles = GetFiles(traces, "trace");

			Console.WriteLine("[*] Running coverage analysis...");

			var minsetFiles = ms.RunCoverage(sampleFiles, traceFiles);

			Console.WriteLine("[-]   {0} files were selected from a total of {1}.", minsetFiles.Length, sampleFiles.Length);

			if (minsetFiles.Length > 0)
				Console.WriteLine("[*] Copying over selected files...");

			foreach (var src in minsetFiles)
			{
				var file = Path.GetFileName(src);

				Debug.Assert(file != null);

				var dst = Path.Combine(minset, file);

				Console.Write("[-]   {0} -> {1}", src, dst);

				try
				{
					File.Copy(src, dst, true);
					Console.WriteLine();
				}
				catch (Exception ex)
				{
					Console.WriteLine(" failed: {0}", ex.Message);
				}
			}

			Console.WriteLine("\n[{0}] Finished\n", sw.Elapsed);

			return 0;
		}

		private void ms_TraceStarting(object sender, string fileName, int count, int totalCount)
		{
			Console.Write("[{0}] ({1}:{2}) Coverage trace of {3}...", 
				sw.Elapsed, count, totalCount, fileName);
		}

		private static void ms_TraceCompleted(object sender, string fileName, int count, int totalCount)
		{
			Console.WriteLine(" Completed");
		}

		private static void ms_TraceFailed(object sender, string fileName, int count, int totalCount)
		{
			Console.WriteLine(" Failed");
		}

		private static void ms_TraceMessage(object sender, string message)
		{
			Console.WriteLine("[-] {0}", message);
		}

		private static string[] GetFiles(string path, string what)
		{
			string[] fileNames;

			try
			{
				fileNames = path.Contains("*")
					? Directory.GetFiles(Path.GetDirectoryName(path) ?? Environment.CurrentDirectory, Path.GetFileName(path))
					: Directory.GetFiles(path);
			}
			catch (IOException ex)
			{
				var err = "Error, unable to get the list of {0} files.".Fmt(what);
				throw new PeachException(err, ex);
			}
			catch (UnauthorizedAccessException ex)
			{
				var err = "Error, unable to get the list of {0} files.".Fmt(what);
				throw new PeachException(err, ex);
			}

			Array.Sort(fileNames);

			return fileNames;
		}

		private static void VerifyDirectory(string path)
		{
			try
			{
				if (!Directory.Exists(path))
					Directory.CreateDirectory(path);
			}
			catch (IOException ex)
			{
				throw new PeachException(ex.Message, ex);
			}
			catch (UnauthorizedAccessException ex)
			{
				throw new PeachException(ex.Message, ex);
			}
		}

		private static void Syntax()
		{
			Console.WriteLine(@"

Peach Minset is used to locate the minimum set of sample data with 
the best code coverage metrics to use while fuzzing.  This process 
can be distributed out across multiple machines to decrease the run 
time.

There are two steps to the process:

  1. Collect traces       [long process]
  2. Compute minimum set  [short process]

The first step, collecting traces, can be distributed and the results
collected for analysis by step #2.

Collect Traces
--------------

Perform code coverage using all files in the 'samples' folder.  Collect
the .trace files in the 'traces' folder for later analysis.

Syntax:
  PeachMinset [-k -v] -s samples -t traces command.exe args %s
  PeachMinset [-k -v] -s samples -t traces -- command.exe --flags args %s

Note:
  %s will be replaced by sample filename.
  -k will terminate command.exe when CPU becomes idle.
  -v will enable debug log messages.


Compute Minimum Set
-------------------

Analyzes all .trace files in the 'traces' folder to determin the minimum
set of samples to use during fuzzing. The minimum set of samples will
be copied from the 'samples' folder to the 'minset' folder.

Syntax:
  PeachMinset -s samples -t traces -m minset


All-In-One
----------

Both tracing and computing can be performed in a single step.

Syntax:
  PeachMinset [-k -v] -s samples -t traces -m minset command.exe %s
  PeachMinset [-k -v] -s samples -t traces -m minset -- command.exe --flags args %s

Note:
  %s will be replaced by sample filename.
  -k will terminate command.exe when CPU becomes idle.
  -v will enable debug log messages.


Distributing Minset
-------------------

Minset can be distributed by splitting up the sample files and 
distributing the collecting of traces to multiple machines.  The
final compute minimum set cannot be distributed.

");
		}
	}
}
