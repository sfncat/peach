using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Peach.Core.Analysis;

namespace Peach.Core.Test
{
	[TestFixture]
	[Peach]
	[Quick]
	public class MinsetTests
	{
		[Test]
		public void TestCoverage()
		{
			using (var tmpDir = new TempDirectory())
			{
				var sampleFiles = new[] {
					Path.Combine(tmpDir.Path, "sample1.bin"),
					Path.Combine(tmpDir.Path, "sample2.bin"),
					Path.Combine(tmpDir.Path, "sample3.bin"),
				};

				var traceFiles = new[] {
					MakeTraceFile(tmpDir.Path, "sample1.bin.trace", new[] {
						"0x0000000000000001 /usr/lib/system/libsystem_kernel.dylib",
						"0x0000000000000002 /usr/lib/system/libsystem_c.dylib",
						"0x0000000000000003 /usr/lib/system/libsystem_c.dylib",
					}),
					MakeTraceFile(tmpDir.Path, "sample2.bin.trace", new[] {
						"0x0000000000000001 /usr/lib/system/libsystem_kernel.dylib",
						"0x0000000000000002 /usr/lib/system/libsystem_c.dylib",
						"0x0000000000000003 /usr/lib/system/libsystem_c.dylib",
					}),
					MakeTraceFile(tmpDir.Path, "sample3.bin.trace", new[] {
						"0x0000000000000002 /usr/lib/system/libsystem_kernel.dylib",
					}),
				};

				var minset = new Minset();
				var actual = minset.RunCoverage(sampleFiles, traceFiles);
				var expected = new[] {
					Path.Combine(tmpDir.Path, "sample1.bin"),
					Path.Combine(tmpDir.Path, "sample3.bin"),
				};
				CollectionAssert.AreEquivalent(expected, actual);
			}
		}

		[Test]
		public void TestEfficiency()
		{
			const int MAX_SAMPLES = 2500;
			const int MAX_LINES = 1000;
			const int MAX_UNIQUE = 10;
			using (var tmpDir = new TempDirectory())
			{
				var samples = new List<string>();
				var traces = new List<string>();
				for (var i = 0; i < MAX_SAMPLES; i++)
				{
					var name = "sample-{0}.bin".Fmt(i);
					samples.Add(Path.Combine(tmpDir.Path, name));
					traces.Add(MakeTraceFile(tmpDir.Path, name + ".trace", MakeTraceLines(i, MAX_LINES, MAX_UNIQUE)));
				}

				var proc = System.Diagnostics.Process.GetCurrentProcess();
				var minset = new Minset();
				minset.TraceLoaded += (s, i) =>
				{
					if (i % 100 == 0)
					{
						var keySize = sizeof(ushort) + sizeof(ulong);
						var theory = (MAX_LINES * keySize) + (MAX_LINES * (MAX_SAMPLES / 8));
						Console.WriteLine("Trace: {0}, Memory Usage: WorkingSet: {1}M, GC Total: {2}M, Theory: {3}K",
							i,
							proc.PrivateMemorySize64 / 1024 / 1024,
							GC.GetTotalMemory(false) / 1024 / 1024,
							theory / 1024
						);
					}
				};

				var actual = minset.RunCoverage(samples.ToArray(), traces.ToArray());
				CollectionAssert.AreEquivalent(samples.Take(MAX_UNIQUE), actual);
			}
		}

		string[] MakeTraceLines(int index, int max, int unique)
		{
			var lines = new List<string>();

			for (var i = 0; i < max; i++)
			{
				lines.Add("0x{0:X8} /usr/lib/system/libsystem_kernel.dylib".Fmt(i));
				if (index < unique)
					lines.Add("0x{0:X8} /usr/lib/system/libsystem_kernel.dylib-{1}".Fmt(0, index));
			}

			return lines.ToArray();
		}

		string MakeTraceFile(string dir, string name, string[] lines)
		{
			var path = Path.Combine(dir, name);
			File.WriteAllLines(path, lines);
			return path;
		}
	}
}
