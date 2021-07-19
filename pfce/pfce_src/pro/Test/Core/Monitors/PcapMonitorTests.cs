

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Test;
using Peach.Pro.Core.OS;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Peach.Pro.Test.Core.Monitors
{
	[TestFixture]
	[Quick]
	[Peach]
	class PcapMonitorTests
	{
		private AutoResetEvent _evt;
		private string _iface;
		private Socket _socket;
		private IPEndPoint _localEp;
		private IPEndPoint _remoteEp;

		[SetUp]
		public void SetUp()
		{
			_localEp = new IPEndPoint(IPAddress.None, 0);
			_remoteEp = new IPEndPoint(IPAddress.Parse("1.1.1.1"), 22222);

			using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			{
				try
				{
					s.Connect(_remoteEp);
					_localEp.Address = ((IPEndPoint)s.LocalEndPoint).Address;
				}
				catch (SocketException)
				{
					Assert.Ignore("Couldn't find primary local IP address.");
				}
			}

			var macAddr = NetworkInterface.GetAllNetworkInterfaces()
				.Where(n => n.GetIPProperties().UnicastAddresses.Any(a => a.Address.Equals(_localEp.Address)))
				.Select(n => n.GetPhysicalAddress())
				.First();

			_iface = CaptureDeviceList.Instance
				.OfType<LibPcapLiveDevice>()
				.Select(p => p.Interface)
				.Where(i => i.MacAddress.Equals(macAddr))
				.Select(i => i.FriendlyName)
				.FirstOrDefault();

			if (_iface == null)
				Assert.Ignore("Could not find a valid adapter to use for testing.");

			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			_socket.Bind(_localEp);
			_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);

			_localEp = (IPEndPoint) _socket.LocalEndPoint;

			_evt = new AutoResetEvent(false);
		}

		[TearDown]
		public void TearDown()
		{
			if (_evt != null)
				_evt.Dispose();

			if (_socket != null)
				_socket.Dispose();

			_evt = null;
			_socket = null;
			_iface = null;
			_localEp = null;
			_remoteEp = null;
		}

		[Test]
		public void BasicTest()
		{
			var runner = new MonitorRunner("NetworkCapture", new Dictionary<string, string>
			{
				{ "Device", _iface },
			});

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void AliasTest()
		{
			// Ensure we can still run the monitor via its aliased name

			var runner = new MonitorRunner("Pcap", new Dictionary<string, string>
			{
				{ "Device", _iface },
			});

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void DataCollection()
		{
			const int max = 10;

			var runner = new MonitorRunner("NetworkCapture", new Dictionary<string, string>
			{
				{ "Device", _iface },
			})
			{
				IterationFinished = m =>
				{
					// Capture starts in IterationStarting, and stops in IterationFinished
					for (var i = 0; i < max; ++i)
						_socket.SendTo("Hello World", _remoteEp);

					m.IterationFinished();
				},
				DetectedFault = m =>
				{
					Assert.False(m.DetectedFault(), "Monitor should not detect fault");

					// Trigger data collection
					return true;
				},
			};

			MonitorData[] faults;

			using (var si = Pal.SingleInstance("Peach.Pro.Test.Core.Monitors.PcapMonitorTests.DataCollection"))
			{
				si.Lock();

				faults = runner.Run();
			}

			Assert.AreEqual(1, faults.Length);
			Assert.Null(faults[0].Fault);
			Assert.AreEqual("NetworkCapture", faults[0].DetectionSource);
			Assert.AreEqual(1, faults[0].Data.Count);
			Assert.True(faults[0].Data.ContainsKey("pcap"));

			const string begin = "Collected ";
			StringAssert.StartsWith(begin, faults[0].Title);

			const string end = " packets.";
			StringAssert.EndsWith(end, faults[0].Title);

			var str = faults[0].Title.Substring(begin.Length, faults[0].Title.Length - begin.Length - end.Length);
			var cnt = int.Parse(str);

			Assert.GreaterOrEqual(cnt, max, "Captured {0} packets, expected at least 10".Fmt(cnt));
		}

		[Test]
		public void MultipleIterationsTest()
		{
			var runner = new MonitorRunner("NetworkCapture", new Dictionary<string, string>
			{
				{ "Device", _iface },
			});

			var faults = runner.Run(10);

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void BadDeviceTest()
		{
			var runner = new MonitorRunner("NetworkCapture", new Dictionary<string, string>
			{
				{ "Device", "Some Unknown Device" },
			});

			var ex = Assert.Throws<PeachException>(() => runner.Run());

			Assert.AreEqual("Error, PcapMonitor was unable to locate device 'Some Unknown Device'.", ex.Message);
		}

		[Test]
		public void NoDeviceTest()
		{
			var runner = new MonitorRunner("NetworkCapture", new Dictionary<string, string>());

			var ex = Assert.Throws<PeachException>(() => runner.Run());

			Assert.AreEqual("Could not start monitor \"NetworkCapture\".  Monitor 'NetworkCapture' is missing required parameter 'Device'.", ex.Message);
		}

		[Test]
		public void BadFilterTest()
		{
			var runner = new MonitorRunner("NetworkCapture", new Dictionary<string, string>
			{
				{ "Device", _iface },
				{ "Filter", "Bad filter string" },
			});

			var ex = Assert.Throws<PeachException>(() => runner.Run());

			Assert.AreEqual("Error, PcapMonitor was unable to set the filter 'Bad filter string'.", ex.Message);
		}

		[Test]
		public void MultipleMonitorsTest()
		{
			const int count = 5;
			var num = 0;
			bool first = true;

			var runner = new MonitorRunner
			{
				SessionStarting = m =>
				{
					m.InternalEvent += (s, e) =>
					{
						if (++num == count)
							_evt.Set();
					};

					m.SessionStarting();
				},
				IterationFinished = m =>
				{
					// Send test packets on IterationFinished to 1st monitor
					if (first)
					{
						// Capture starts in IterationStarting, and stops in IterationFinished
						for (var i = 0; i < count; ++i)
						{
							var ep = new IPEndPoint(_remoteEp.Address, _remoteEp.Port + 1 + i);
							_socket.SendTo("Hello World", ep);
						}

						// Ensure packets are captured
						if (!_evt.WaitOne(5000))
							Assert.Fail("Didn't receive packets within 5 second.");
					}

					first = false;

					m.IterationFinished();
				},
				DetectedFault = m =>
				{
					Assert.False(m.DetectedFault(), "Monitor should not detect fault");

					// Trigger data collection
					return true;
				},
			};

			// Add one extra that is not sent to
			// Skew by one so we don't conflict with DataCollection test port
			for (var i = 0; i <= count; ++i)
			{
				runner.Add("NetworkCapture", new Dictionary<string, string>
				{
					{ "Device", _iface },
					{ "Filter", "udp and dst port " + (_remoteEp.Port + 1 + i) },
				});
			}

			MonitorData[] faults;

			using (var si = Pal.SingleInstance("Peach.Pro.Test.Core.Monitors.PcapMonitorTests.MultipleMonitorsTest"))
			{
				si.Lock();

				faults = runner.Run();
			}

			// Expect a fault for each pcap monitor
			Assert.AreEqual(count + 1, faults.Length);

			for (var i = 0; i <= count; ++i)
			{
				var f = faults[i];

				Assert.AreEqual("Mon_{0}".Fmt(i), f.MonitorName);
				Assert.AreEqual("NetworkCapture", f.DetectionSource);
				Assert.Null(f.Fault);
				Assert.AreEqual(1, f.Data.Count);
				Assert.True(f.Data.ContainsKey("pcap"));

				Assert.AreEqual(i == count ? "Collected 0 packets." : "Collected 1 packet.", f.Title);
			}
		}
	}
}
