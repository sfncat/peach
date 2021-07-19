using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Monitors
{
	[TestFixture]
	[Quick]
	[Peach]
	class PingMonitorTests
	{
		// TEST-NET-3 from RFC5737
		const string InvalidAddress = "203.0.113.0";

		private static void Verify(MonitorData[] faults, string title, bool isFault)
		{
			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("Ping", faults[0].DetectionSource);
			StringAssert.IsMatch(title, faults[0].Title);

			if (!isFault)
			{
				Assert.Null(faults[0].Fault, "Should not be marked as a fault");
				Assert.NotNull(faults[0].Data);
				Assert.AreEqual(0, faults[0].Data.Count);
			}
			else
			{
				Assert.NotNull(faults[0].Fault);
				Assert.AreNotEqual("", faults[0].Fault.MajorHash);
				Assert.AreNotEqual("", faults[0].Fault.MinorHash);
				Assert.AreEqual(null, faults[0].Fault.Risk);
				Assert.NotNull(faults[0].Data);
				Assert.AreEqual(0, faults[0].Data.Count);
			}

			Assert.NotNull(faults[0].Data);
			Assert.AreEqual(0, faults[0].Data.Count);
		}

		[Test]
		public void TestSuccess()
		{
			var runner = new MonitorRunner("Ping", new Dictionary<string, string>
			{
				{ "Host", "127.0.0.1" },
			});

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void TestSuccessV6()
		{
			var runner = new MonitorRunner("Ping", new Dictionary<string, string>
			{
				{ "Host", "::1" },
			});

			if (Platform.GetOS() == Platform.OS.Windows)
			{
				var faults = runner.Run();

				Assert.AreEqual(0, faults.Length);
			}
			else
			{
				var ex = Assert.Throws<PeachException>(() => runner.Run());
				Assert.AreEqual("Could not start monitor \"Ping\".  Error, the Ping monitor only supports IPv6 addresses on Windows.", ex.Message);
			}
		}

		[Test]
		public void TestFailure()
		{
			var runner = new MonitorRunner("Ping", new Dictionary<string, string>
			{
				{ "Host", InvalidAddress },
			});

			var faults = runner.Run();

			Verify(faults, "The ICMP echo reply was not received within the allotted time.", true);
		}

		[Test]
		public void TestFaultSuccess()
		{
			var runner = new MonitorRunner("Ping", new Dictionary<string, string>
			{
				{ "Host", InvalidAddress },
				{ "FaultOnSuccess", "true" },
			});

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void TestFaultFailure()
		{
			var runner = new MonitorRunner("Ping", new Dictionary<string, string>
			{
				{ "Host", "127.0.0.1" },
				{ "FaultOnSuccess", "true" },
			});

			var faults = runner.Run();

			Verify(faults, "Reply from 127.0.0.1: bytes=\\d+ time=\\d+ms TTL=\\d+", true);
		}

		[Test]
		public void TestSuccessData()
		{
			var runner = new MonitorRunner("Ping", new Dictionary<string, string>
			{
				{ "Host", "127.0.0.1" },
			})
			{
				DetectedFault = m =>
				{
					Assert.False(m.DetectedFault(), "Monitor should not detect fault");

					// Trigger data collection
					return true;
				}
			};

			var faults = runner.Run();

			Verify(faults, "Reply from 127.0.0.1: bytes=\\d+ time=\\d+ms TTL=\\d+", false);
		}

		[Test]
		public void TestFaultSuccessData()
		{
			var runner = new MonitorRunner("Ping", new Dictionary<string, string>
			{
				{ "Host", InvalidAddress },
				{ "FaultOnSuccess", "true" },
			})
			{
				DetectedFault = m =>
				{
					Assert.False(m.DetectedFault(), "Monitor should not detect fault");

					// Trigger data collection
					return true;
				}
			};

			var faults = runner.Run();

			Verify(faults, "The ICMP echo reply was not received within the allotted time.", false);
		}

		[Test]
		public void TestBadHost()
		{
			// RFC6761 says .invalid is guranteed to be an invalid TLD
			var ex = Assert.Throws<PeachException>(() => 
				new MonitorRunner("Ping", new Dictionary<string, string>
				{
					{ "Host", "some.host.invalid" },
				}).Run());

			StringAssert.IsMatch("(Could not resolve host)|(No such host is known)", ex.Message);
		}

		[Test]
		public void TestBadHostSuccess()
		{
			// RFC6761 says .invalid is guranteed to be an invalid TLD
			var ex = Assert.Throws<PeachException>(() =>
				new MonitorRunner("Ping", new Dictionary<string, string>
				{
					{ "Host", "some.host.invalid" },
					{ "FaultOnSuccess", "true" },
				}).Run());

			StringAssert.IsMatch("(Could not resolve host)|(No such host is known)", ex.Message);
		}

		[TestCase(1000)]
		[TestCase(2000)]
		[TestCase(3000)]
		public void TestTimeout(long timeout)
		{
			var runner = new MonitorRunner("Ping", new Dictionary<string, string>
			{
				{ "Host", InvalidAddress },
				{ "Timeout", timeout.ToString(CultureInfo.InvariantCulture) },
			})
			{
				DetectedFault = m =>
				{
					var sw = new Stopwatch();

					sw.Start();
					Assert.True(m.DetectedFault(), "Monitor should have detected fault");
					sw.Stop();

					var elapsed = sw.ElapsedMilliseconds;

					Assert.Greater(elapsed, timeout - 250);
					Assert.Less(elapsed, timeout + 250);

					return true;
				}
			};

			var faults = runner.Run();

			Verify(faults, "The ICMP echo reply was not received within the allotted time.", true);
		}

		[Test]
		public void TestData()
		{
			var runner = new MonitorRunner("Ping", new Dictionary<string, string>
			{
				{ "Host", "127.0.0.1" },
				{ "FaultOnSuccess", "true" },
				{ "Data", new string('a', 70) },
			});

			var faults = runner.Run();

			Verify(faults, "Reply from 127.0.0.1: bytes=70 time=\\d+ms TTL=\\d+", true);
		}
	}
}
