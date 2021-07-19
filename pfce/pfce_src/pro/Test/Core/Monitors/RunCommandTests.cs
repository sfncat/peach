using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Monitors
{
	[TestFixture]
	[Quick]
	[Peach]
	class RunCommandTests
	{
		private readonly string _scriptFile = Utilities.GetAppResourcePath("CrashTest");
		private string _outputFile;

		[SetUp]
		public void SetUp()
		{
			_outputFile = Path.GetTempFileName();
		}

		[TearDown]
		public void TearDown()
		{
			File.Delete(_outputFile);
		}

		private MonitorRunner MakeWhen(string when)
		{
			return new MonitorRunner("RunCommand", new Dictionary<string, string>
			{
				{ "Command", _scriptFile },
				{ "Arguments", string.Join(" ", "when", _outputFile, when) },
				{ "When", when },
			});
		}

		private void AddCall(MonitorRunner runner, string startOnCall)
		{
			runner.Add("RunCommand", new Dictionary<string, string>
			{
				{ "Command", _scriptFile },
				{ "Arguments", "OnCall" },
				{ "When", "OnCall" },
				{ "StartOnCall", startOnCall },
			});
		}

		private void Verify(string expected)
		{
			var lines = File.ReadAllText(_outputFile);
			Assert.AreEqual(expected, lines.Trim());
		}

		private void VerifyLines(IEnumerable<string> expected)
		{
			var lines = File.ReadAllLines(_outputFile).Select(s => s.Trim());
			Assert.That(expected, Is.EqualTo(lines));
		}

		private void VerifyCall(int index)
		{
			switch (index)
			{
				case 0:
					VerifyLines(new string[0]);
					break;
				case 1:
					VerifyLines(new[] { "CallOne" });
					break;
				case 2:
					VerifyLines(new[] { "CallOne", "CallTwo" });
					break;
				case 3:
					VerifyLines(new[] { "CallOne", "CallTwo", "CallThree" });
					break;
				default:
					Assert.Fail("Unexpected number of lines to verify");
					break;
			}
		}

		[Test]
		public void TestNoArgs()
		{
			var runner = new MonitorRunner("RunCommand", new Dictionary<string, string>());
			var ex = Assert.Throws<PeachException>(() => runner.Run());

			const string msg = "Could not start monitor \"RunCommand\".  Monitor 'RunCommand' is missing required parameter 'Command'.";
			Assert.AreEqual(msg, ex.Message);
		}

		[Test]
		public void TestNoWhen()
		{
			var runner = MakeWhen("");
			var ex = Assert.Throws<PeachException>(() => runner.Run());

			const string msg = "Could not start monitor \"RunCommand\".  Monitor 'RunCommand' could not set value type parameter 'When' to 'null'.";
			Assert.AreEqual(msg, ex.Message);
		}

		[Test]
		public void TestOnStart()
		{
			var runner = MakeWhen("OnStart");

			runner.SessionStarting = m =>
			{
				Verify("");

				m.SessionStarting();

				Verify("OnStart");
			};

			runner.Run();

			Verify("OnStart");
		}

		[Test]
		public void TestOnEnd()
		{
			var runner = MakeWhen("OnEnd");

			runner.SessionFinished = m =>
			{
				Verify("");

				m.SessionFinished();

				Verify("OnEnd");
			};

			runner.Run();

			Verify("OnEnd");
		}

		[Test]
		public void TestOnIterationStart()
		{
			var runner = MakeWhen("OnIterationStart");

			runner.IterationStarting = (m, args) =>
			{
				Verify("");

				m.IterationStarting(args);

				Verify("OnIterationStart");
			};

			runner.Run();

			Verify("OnIterationStart");
		}

		[Test]
		public void TestOnIterationEnd()
		{
			var runner = MakeWhen("OnIterationEnd");

			runner.IterationFinished = m =>
			{
				Verify("");

				m.IterationFinished();

				Verify("OnIterationEnd");
			};

			runner.Run();

			Verify("OnIterationEnd");
		}

		[Test]
		public void TestOnFault()
		{
			var runner = MakeWhen("OnFault");

			runner.DetectedFault = m =>
			{
				Assert.False(m.DetectedFault(), "Monitor should not have detected fault.");

				// Trigger fault to runner
				return true;
			};

			runner.GetMonitorData = m =>
			{
				Verify("");

				var ret = m.GetMonitorData();

				Verify("OnFault");

				return ret;
			};

			runner.Run();

			Verify("OnFault");
		}

		[Test]
		public void TestIterAfterFault()
		{
			var runner = MakeWhen("OnIterationStartAfterFault");
			var it = 0;

			runner.DetectedFault = m =>
			{
				Assert.False(m.DetectedFault(), "Monitor should not have detected fault.");

				// Trigger fault to runner
				return true;
			};

			runner.IterationStarting = (m, args) =>
			{
				Verify("");

				m.IterationStarting(args);

				// We fault on every iteration, so iteration two is the first iteration after fault
				var exp = ++it == 2 ? "OnIterationStartAfterFault" : "";

				Verify(exp);
			};

			runner.Run(2);

			Verify("OnIterationStartAfterFault");
		}

		[Test]
		public void TestMissingStartOnCall()
		{
			var runner = MakeWhen("OnCall");

			runner.Run();

			Verify("");
		}

		[Test]
		public void TestOnCall()
		{
			var runner = new MonitorRunner();

			AddCall(runner, "CallOne");
			AddCall(runner, "CallTwo");
			AddCall(runner, "CallThree");

			var idx = 0;

			runner.Message = m =>
			{
				VerifyCall(idx++);

				// Called for each monitor, but each monitor just listens to
				// the specific message, so send every message to every monitor
				m.Message("CallOne");
				m.Message("CallTwo");
				m.Message("CallThree");

				VerifyCall(idx);
			};
		}

		[Test]
		[Repeat(30)]
		public void TestAddressSanitizer()
		{
			if (Platform.GetOS() == Platform.OS.Windows)
				Assert.Ignore("ASAN is not supported on Windows");

			MonitorData data = null;

			var runner = new MonitorRunner("RunCommand", new Dictionary<string, string>
			{
				{ "Command", Utilities.GetAppResourcePath("UseAfterFree") },
				{ "When", "OnStart" },
			})
			{
				DetectedFault = m =>
				{
					var ret = m.DetectedFault();
					Assert.True(ret, "Monitor should have detected fault.");
					return ret;
				},
				GetMonitorData = m =>
				{
					data = m.GetMonitorData();
					return data;
				},
			};

			runner.Run();

			Assert.NotNull(data);

			Console.WriteLine(data.Fault.Description);

			Assert.AreEqual("RunCommand", data.DetectionSource);
			Assert.AreEqual("heap-use-after-free", data.Fault.Risk);
			Assert.IsFalse(data.Fault.MustStop);
			StringAssert.Contains("Shadow bytes", data.Fault.Description);

			if (Platform.GetOS() == Platform.OS.OSX)
			{
				const string pattern = "heap-use-after-free on address 0x61400000fe44 at pc 0x000100001b8f";
				StringAssert.StartsWith(pattern, data.Title);
				StringAssert.Contains(pattern, data.Fault.Description);
				Assert.AreEqual("02133A7E", data.Fault.MajorHash);
				Assert.AreEqual("9DD19897", data.Fault.MinorHash);
			}
			else if (Platform.GetOS() == Platform.OS.Linux)
			{
				const string pattern = "heap-use-after-free on address ";
				if (Platform.GetArch() == Platform.Architecture.x64)
				{
					StringAssert.StartsWith(pattern, data.Title);
					StringAssert.Contains(pattern, data.Fault.Description);
					CollectionAssert.Contains(new[] { Monitor2.Hash("0x0000004008b2"), Monitor2.Hash("0x4008b9") }, data.Fault.MajorHash);
					CollectionAssert.Contains(new[] { Monitor2.Hash("0x61400000fe44"), Monitor2.Hash("0x602e0001fc64") }, data.Fault.MinorHash);
				}
				else
				{
					StringAssert.StartsWith(pattern, data.Title);
					StringAssert.Contains(pattern, data.Fault.Description);
					CollectionAssert.Contains(new[] { Monitor2.Hash("0x80486de") }, data.Fault.MajorHash);
					CollectionAssert.Contains(new[] { Monitor2.Hash("0xb5e03e24") }, data.Fault.MinorHash);
				}
			}
		}

		class Result
		{
			public bool DetectedFault { get; set; }
			public MonitorData Data { get; set; }
		}

		Result DoExitTest(bool faultOnExitCode, bool faultOnNonZeroExit, int faultExitCode, int exitCode)
		{
			var result = new Result();

			var runner = new MonitorRunner("RunCommand", new Dictionary<string, string>
			{
				{ "Command", _scriptFile },
				{ "Arguments", string.Join(" ", "exit", exitCode.ToString()) },
				{ "When", "OnStart" },
				{ "FaultOnExitCode", faultOnExitCode.ToString() },
				{ "FaultExitCode", faultExitCode.ToString() },
				{ "FaultOnNonZeroExit", faultOnNonZeroExit.ToString() },
			})
			{
				DetectedFault = m =>
				{
					result.DetectedFault = m.DetectedFault();
					return result.DetectedFault;
				},
				GetMonitorData = m =>
				{
					result.Data = m.GetMonitorData();
					return result.Data;
				},
			};

			runner.Run();

			return result;
		}

		void ExitShouldFault(bool faultOnExitCode, bool faultOnNonZeroExit, int faultExitCode, int exitCode)
		{
			var result = DoExitTest(faultOnExitCode, faultOnNonZeroExit, faultExitCode, exitCode);

			Assert.IsTrue(result.DetectedFault);
			Assert.IsNotNull(result.Data);

			Console.WriteLine("Title: {0}", result.Data.Title);
			Console.WriteLine("Fault.MajorHash: {0}", result.Data.Fault.MajorHash);
			Console.WriteLine("Fault.MinorHash: {0}", result.Data.Fault.MinorHash);

			var majorHash = Monitor2.Hash("RunCommand" + _scriptFile);
			var minorHash = Monitor2.Hash(exitCode.ToString(CultureInfo.InvariantCulture));

			Assert.AreEqual("Process exited with code {0}.".Fmt(exitCode), result.Data.Title);
			Assert.AreEqual(majorHash, result.Data.Fault.MajorHash);
			Assert.AreEqual(minorHash, result.Data.Fault.MinorHash);
			Assert.IsFalse(result.Data.Fault.MustStop);
			Assert.IsNull(result.Data.Fault.Description);
		}

		void ExitNoFault(bool faultOnExitCode, bool faultOnNonZeroExit, int faultExitCode, int exitCode)
		{
			var result = DoExitTest(faultOnExitCode, faultOnNonZeroExit, faultExitCode, exitCode);

			Assert.IsFalse(result.DetectedFault);
			Assert.IsNull(result.Data);
		}

		[Test]
		public void TestFaultDefault()
		{
			ExitNoFault(false, false, 0, 0);
			ExitNoFault(false, false, 0, 1);
			ExitNoFault(false, false, 1, 0);
			ExitNoFault(false, false, 1, 1);
		}

		[Test]
		public void TestFaultOnExitCode()
		{
			ExitNoFault(true, false, 1, 0);
			ExitNoFault(true, false, 0, 1);
			ExitNoFault(true, false, 1, 2);
			ExitShouldFault(true, false, 0, 0);
			ExitShouldFault(true, false, 1, 1);
			ExitShouldFault(true, false, 2, 2);
		}

		[Test]
		public void TestFaultOnNonZeroExit()
		{
			ExitNoFault(false, true, 0, 0);
			ExitShouldFault(false, true, 0, 1);
			ExitShouldFault(false, true, 1, 1);
		}

		[Test]
		public void TestFaultOnBoth()
		{
			ExitNoFault(true, true, 1, 0);
			ExitShouldFault(true, true, 0, 0);
			ExitShouldFault(true, true, 0, 1);
			ExitShouldFault(true, true, 1, 1);
			ExitShouldFault(true, true, 1, 2);
		}

		Result DoTimeoutTest(int timeout, int sleep)
		{
			var result = new Result();

			var runner = new MonitorRunner("RunCommand", new Dictionary<string, string>
			{
				{ "Command", _scriptFile },
				{ "Arguments", string.Join(" ", "timeout", sleep.ToString()) },
				{ "When", "OnStart" },
				{ "Timeout", timeout.ToString() },
			})
			{
				DetectedFault = m =>
				{
					result.DetectedFault = m.DetectedFault();
					return result.DetectedFault;
				},
				GetMonitorData = m =>
				{
					result.Data = m.GetMonitorData();
					return result.Data;
				},
			};

			runner.Run();

			return result;
		}

		void TimeoutShouldFault(int timeout, int sleep)
		{
			var result = DoTimeoutTest(timeout, sleep);

			Assert.IsTrue(result.DetectedFault);
			Assert.IsNotNull(result.Data);

			Console.WriteLine("Title: {0}", result.Data.Title);
			Console.WriteLine("Fault.MajorHash: {0}", result.Data.Fault.MajorHash);
			Console.WriteLine("Fault.MinorHash: {0}", result.Data.Fault.MinorHash);

			var majorHash = Monitor2.Hash("RunCommand" + _scriptFile);
			var minorHash = Monitor2.Hash("FailedToExit");

			Assert.AreEqual("Process failed to exit in allotted time.", result.Data.Title);
			Assert.AreEqual(majorHash, result.Data.Fault.MajorHash);
			Assert.AreEqual(minorHash, result.Data.Fault.MinorHash);
			Assert.IsFalse(result.Data.Fault.MustStop);
			Assert.IsNull(result.Data.Fault.Description);
		}

		void TimeoutNoFault(int timeout, int sleep)
		{
			var result = DoTimeoutTest(timeout, sleep);

			Assert.IsFalse(result.DetectedFault);
			Assert.IsNull(result.Data);
		}

		[Test]
		public void TestTimeoutNoFault()
		{
			TimeoutNoFault(-1, 0);
			TimeoutNoFault(-1, 3);
			TimeoutNoFault(10000, 0);
		}

		[Test]
		public void TestTimeout()
		{
			TimeoutShouldFault(0, 10);
			TimeoutShouldFault(1000, 10);
		}

		Result DoRegexTest(string regex, string stdout, string stderr)
		{
			var result = new Result();

			var runner = new MonitorRunner("RunCommand", new Dictionary<string, string>
			{
				{ "Command", _scriptFile },
				{ "Arguments", string.Join(" ", "regex", stdout, stderr) },
				{ "When", "OnStart" },
				{ "FaultOnRegex", regex },
			})
			{
				DetectedFault = m =>
				{
					result.DetectedFault = m.DetectedFault();
					return result.DetectedFault;
				},
				GetMonitorData = m =>
				{
					result.Data = m.GetMonitorData();
					return result.Data;
				},
			};

			runner.Run();

			return result;
		}

		void RegexShouldFault(string regex, string stdout, string stderr)
		{
			var useStdout = regex == stdout;
			var result = DoRegexTest(regex, stdout, stderr);

			Assert.IsTrue(result.DetectedFault, "DetectedFault should be true");
			Assert.IsNotNull(result.Data, "MonitorData should not be null");

			Console.WriteLine("Title: {0}", result.Data.Title);
			Console.WriteLine("Fault.MajorHash: {0}", result.Data.Fault.MajorHash);
			Console.WriteLine("Fault.MinorHash: {0}", result.Data.Fault.MinorHash);

			var majorHash = Monitor2.Hash("RunCommand" + _scriptFile);
			var minorHash = Monitor2.Hash(regex);
			var pipe = useStdout ? "stdout" : "stderr";

			Assert.AreEqual("Process {0} matched FaultOnRegex \"{1}\".".Fmt(pipe, regex), result.Data.Title);
			Assert.AreEqual(majorHash, result.Data.Fault.MajorHash);
			Assert.AreEqual(minorHash, result.Data.Fault.MinorHash);
			Assert.IsFalse(result.Data.Fault.MustStop, "MustStop should be false");
			StringAssert.Contains(useStdout ? stdout : stderr, result.Data.Fault.Description);
		}

		void RegexNoFault(string regex, string stdout, string stderr)
		{
			var result = DoRegexTest(regex, stdout, stderr);

			Assert.IsFalse(result.DetectedFault, "DetectedFault should be false");
			Assert.IsNull(result.Data, "Should not have data");
		}

		[Test]
		public void TestRegexNoFault()
		{
			RegexNoFault("", "foo", "foo");
			RegexNoFault("foo", "bar", "bar");
		}

		[Test]
		public void TestRegexStdout()
		{
			RegexShouldFault("foo", "foo", "bar");
		}

		[Test]
		public void TestRegexStderr()
		{
			RegexShouldFault("foo", "bar", "foo");
		}

		[Test]
		public void TestRegexBoth()
		{
			RegexShouldFault("foo", "foo", "foo");
		}

		[Test]
		public void TestInvalidWorkingDirectory()
		{
			var args = string.Join(" ", "when", _outputFile, "OnStart");
			var dir = "/this/path/does/not/exist";
			var runner = new MonitorRunner("RunCommand", new Dictionary<string, string> {
				{ "Command", _scriptFile },
				{ "Arguments", args },
				{ "When", "OnStart" },
				{ "WorkingDirectory", dir },
			});

			var ex = Assert.Throws<PeachException>(() => runner.Run());

			var expected = "RunCommand (Mon_0) failed to run command: '{0} {1}'. Specified WorkingDirectory does not exist: '{2}'.".Fmt(
				_scriptFile,
				args,
				dir
			);
			Assert.AreEqual(expected, ex.Message);
		}
	}
}
