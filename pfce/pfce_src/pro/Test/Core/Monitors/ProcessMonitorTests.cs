using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Test;
using Peach.Pro.Core.Agent.Monitors;
using Peach.Pro.Core.Agent.Monitors.Utilities;

namespace Peach.Pro.Test.Core.Monitors
{
	[TestFixture]
	[Quick]
	[Peach]
	class ProcessMonitorTests
	{
		[Test]
		public void TestBadProcss()
		{
			var runner = new MonitorRunner("Process", new Dictionary<string, string>
			{
				{ "Executable", "some_invalid_process" }
			});

			var ex = Assert.Throws<PeachException>(() => runner.Run());
			StringAssert.StartsWith("Could not start process 'some_invalid_process'.", ex.Message);
		}

		[Test]
		public void TestStartOnCall()
		{
			var sw = new Stopwatch();

			var runner = new MonitorRunner("Process", new Dictionary<string, string> {
				{ "Executable", Utilities.GetAppResourcePath("CrashableServer") },
				{ "Arguments", "127.0.0.1" },
				{ "StartOnCall", "foo" },
				{ "WaitForExitTimeout", "2000" },
				{ "NoCpuKill", "true" },
			}) {
				Message = m =>
				{
					m.Message("foo");
					Thread.Sleep(500);
				},
				IterationFinished = m =>
				{
					sw.Start();
					m.IterationFinished();
					sw.Stop();
				}
			};

			var faults = runner.Run();
			Assert.AreEqual(0, faults.Length);

			Assert.GreaterOrEqual(sw.Elapsed.TotalSeconds, 1.9);
			Assert.LessOrEqual(sw.Elapsed.TotalSeconds, 2.1);
		}

		[Test]
		public void TestCpuKill()
		{
			var sw = new Stopwatch();

			var runner = new MonitorRunner("Process", new Dictionary<string, string>
			{
				{ "Executable", Utilities.GetAppResourcePath("CrashableServer") },
				{ "Arguments", "127.0.0.1 0" },
				{ "StartOnCall", "foo" },
			})
			{
				Message = m =>
				{
					m.Message("foo");
					Thread.Sleep(500);
				},
				IterationFinished = m =>
				{
					sw.Start();
					m.IterationFinished();
					sw.Stop();
				}
			};

			var faults = runner.Run();
			Assert.AreEqual(0, faults.Length);

			Assert.GreaterOrEqual(sw.Elapsed.TotalSeconds, 0.0);
			Assert.LessOrEqual(sw.Elapsed.TotalSeconds, 0.5);
		}

		[Test]
		public void TestExitOnCallNoFault()
		{
			var runner = new MonitorRunner("Process", new Dictionary<string, string>
			{
				{ "Executable", Utilities.GetAppResourcePath("CrashingFileConsumer") },
				{ "StartOnCall", "foo" },
				{ "WaitForExitOnCall", "bar" },
				{ "NoCpuKill", "true" },
			})
			{
				Message = m =>
				{
					m.Message("foo");
					m.Message("bar");
				},
			};

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void TestExitOnCallFault()
		{
			var exe = Utilities.GetAppResourcePath("CrashableServer");
			
			var runner = new MonitorRunner("Process", new Dictionary<string, string> {
				{ "Executable", exe },
				{ "Arguments", "127.0.0.1 0" },
				{ "StartOnCall", "foo" },
				{ "WaitForExitOnCall", "bar" },
				{ "WaitForExitTimeout", "2000" },
				{ "NoCpuKill", "true" },
			}) {
				Message = m =>
				{
					m.Message("foo");
					m.Message("bar");
				},
			};

			var faults = runner.Run();

			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("Process '{0}' did not exit in 2000ms.".Fmt(exe), faults[0].Title);
			Assert.NotNull(faults[0].Fault);
			Assert.AreEqual(Monitor2.Hash("Process{0}".Fmt(exe)), faults[0].Fault.MajorHash);
			Assert.AreEqual(Monitor2.Hash("FailedToExit"), faults[0].Fault.MinorHash);
		}

		[Test]
		public void TestExitTime()
		{
			var sw = new Stopwatch();

			var runner = new MonitorRunner("Process", new Dictionary<string, string>
			{
				{ "Executable", Utilities.GetAppResourcePath("CrashableServer") },
				{ "Arguments", "127.0.0.1 0" },
				{ "RestartOnEachTest", "true" },
			})
			{
				IterationFinished = m =>
				{
					sw.Start();
					m.IterationFinished();
					sw.Stop();
				}
			};

			var faults = runner.Run();
			Assert.AreEqual(0, faults.Length);

			Assert.GreaterOrEqual(sw.Elapsed.TotalSeconds, 0.0);
			Assert.LessOrEqual(sw.Elapsed.TotalSeconds, 0.1);
		}

		[Test]
		public void TestExitEarlyFault()
		{
			var exe = Utilities.GetAppResourcePath("CrashingFileConsumer");
			var runner = new MonitorRunner("Process", new Dictionary<string, string> {
				{ "Executable",  exe },
				{ "FaultOnEarlyExit", "true" },
			}) {
				Message = m => Thread.Sleep(1000),
			};

			var faults = runner.Run();

			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("Process '{0}' exited early.".Fmt(exe), faults[0].Title);
			Assert.NotNull(faults[0].Fault);
			Assert.AreEqual(Monitor2.Hash("Process{0}".Fmt(exe)), faults[0].Fault.MajorHash);
			Assert.AreEqual(Monitor2.Hash("ExitedEarly"), faults[0].Fault.MinorHash);
		}

		[Test]
		public void TestExitEarlyFault1()
		{
			// FaultOnEarlyExit doesn't fault when stop message is sent

			var runner = new MonitorRunner("Process", new Dictionary<string, string>
			{
				{ "Executable", Utilities.GetAppResourcePath("CrashingFileConsumer") },
				{ "StartOnCall", "foo" },
				{ "WaitForExitOnCall", "bar" },
				{ "FaultOnEarlyExit", "true" },
			})
			{
				Message = m =>
				{
					m.Message("foo");
					m.Message("bar");
				},
			};

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void TestExitEarlyFault2()
		{
			// FaultOnEarlyExit faults when WaitForExitOnCall is used and stop message is not sent

			var exe = Utilities.GetAppResourcePath("CrashingFileConsumer");

			var runner = new MonitorRunner("Process", new Dictionary<string, string> {
				{ "Executable", exe },
				{ "StartOnCall", "foo" },
				{ "WaitForExitOnCall", "bar" },
				{ "FaultOnEarlyExit", "true" },
			}) {
				Message = m =>
				{
					m.Message("foo");
					Thread.Sleep(1000);
				},
			};

			var faults = runner.Run();

			Assert.AreEqual(1, faults.Length);
			Assert.AreEqual("Process '{0}' exited early.".Fmt(exe), faults[0].Title);
			Assert.NotNull(faults[0].Fault);
			Assert.AreEqual(Monitor2.Hash("Process{0}".Fmt(exe)), faults[0].Fault.MajorHash);
			Assert.AreEqual(Monitor2.Hash("ExitedEarly"), faults[0].Fault.MinorHash);
		}

		[Test]
		public void TestExitEarlyFault3()
		{
			// FaultOnEarlyExit doesn't fault when StartOnCall is used

			var runner = new MonitorRunner("Process", new Dictionary<string, string>
			{
				{ "Executable", Utilities.GetAppResourcePath("CrashableServer") },
				{ "Arguments", "127.0.0.1 0" },
				{ "StartOnCall", "foo" },
				{ "FaultOnEarlyExit", "true" },
			})
			{
				Message = m => m.Message("foo"),
			};

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void TestExitEarlyFault4()
		{
			// FaultOnEarlyExit doesn't fault when restart every iteration is true

			var runner = new MonitorRunner("Process", new Dictionary<string, string>
			{
				{ "Executable", Utilities.GetAppResourcePath("CrashableServer") },
				{ "Arguments", "127.0.0.1 0" },
				{ "RestartOnEachTest", "true" },
				{ "FaultOnEarlyExit", "true" },
			});

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void TestRestartAfterFault()
		{
			var startCount = 0;
			var iteration = 0;

			var runner = new MonitorRunner("Process", new Dictionary<string, string>
			{
				{ "Executable", Utilities.GetAppResourcePath("CrashableServer") },
				{ "Arguments", "127.0.0.1 0" },
				{ "RestartAfterFault", "true" },
			})
			{
				StartMonitor = (m, args) =>
				{
					m.InternalEvent += (s, e) => ++startCount;
					m.StartMonitor(args);
				},
				DetectedFault = m =>
				{
					Assert.False(m.DetectedFault(), "Should not have detected a fault");

					return ++iteration == 2;
				}
			}
			;

			var faults = runner.Run(5);

			Assert.AreEqual(0, faults.Length);
			Assert.AreEqual(2, startCount);
		}

		[Test]
		[Repeat(30)]
		public void TestAddressSanitizer()
		{
			if (Platform.GetOS() == Platform.OS.Windows)
				Assert.Ignore("ASAN is not supported on Windows");

			var runner = new MonitorRunner("Process", new Dictionary<string, string>
			{
				{ "Executable", Utilities.GetAppResourcePath("UseAfterFree") },
			})
			{
				Message = m =>
				{
					Thread.Sleep(10);
				}
			};

			var faults = runner.Run(10);

			Assert.NotNull(faults);

			Assert.Greater(faults.Length, 0);

			foreach (var data in faults)
			{
				Assert.AreEqual("Process", data.DetectionSource);
				Assert.AreEqual("heap-use-after-free", data.Fault.Risk);
				Assert.IsFalse(data.Fault.MustStop);
				StringAssert.Contains("Shadow bytes", data.Fault.Description);

				Console.WriteLine(data.Fault.Description);

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
		}

		[Test]
		public void TestAsanRegex()
		{
			const string example = @"==13983==ERROR: AddressSanitizer: SEGV on unknown address 0x00002f10b7d6 (pc 0x0000004ee255 bp 0x7ffc0abb72d0 sp 0x7ffc0abb72d0 T0)
    #0 0x4ee254 in decode_tag_number /home/peach/bacnet-stack-0.8.3/lib/../src/bacdcode.c:313:9
    #1 0x4ee4e2 in decode_tag_number_and_value /home/peach/bacnet-stack-0.8.3/lib/../src/bacdcode.c:379:11
    #2 0x519191 in awf_decode_service_request /home/peach/bacnet-stack-0.8.3/lib/../src/awf.c:130:17
    #3 0x50fd31 in handler_atomic_write_file /home/peach/bacnet-stack-0.8.3/lib/../demo/handler/h_awf.c:114:11
    #4 0x4ed71f in apdu_handler /home/peach/bacnet-stack-0.8.3/lib/../src/apdu.c:477:21
    #5 0x50a2e3 in npdu_handler /home/peach/bacnet-stack-0.8.3/lib/../demo/handler/h_npdu.c:88:17
    #6 0x4c4662 in main /home/peach/bacnet-stack-0.8.3/demo/server/main.c:188:13
    #7 0x7f1926df3ec4 in __libc_start_main /build/eglibc-3GlaMS/eglibc-2.19/csu/libc-start.c:287
    #8 0x4c442c in _start (/home/peach/bacnet-stack-0.8.3/bin/bacserv+0x4c442c)

AddressSanitizer can not provide additional info.
SUMMARY: AddressSanitizer: SEGV /home/peach/bacnet-stack-0.8.3/lib/../src/bacdcode.c:313 decode_tag_number
==13983==ABORTING";


			Assert.IsTrue(Asan.CheckForAsanFault(example));
			var data = Asan.AsanToMonitorData(null, example);

			Assert.AreEqual("SEGV on unknown address 0x00002f10b7d6 (pc 0x0000004ee255 bp 0x7ffc0abb72d0 sp 0x7ffc0abb72d0 T0)", data.Title);
			Assert.AreEqual("SEGV", data.Fault.Risk);
			Assert.AreEqual(example, data.Fault.Description);
			Assert.AreEqual("EB7CE44C", data.Fault.MajorHash);
			Assert.AreEqual("2E409A3D", data.Fault.MinorHash);

		}

		[Test]
		public void TestAsanOOMRegex()
		{
			const string example = @"==3960==ERROR: AddressSanitizer failed to allocate 0x41417000 (1094807552) bytes of LargeMmapAllocator: 12
==3960==Process memory map follows:
	0x000000400000-0x0000004e6000	/home/user/Desktop/lab/firefox/firefox
	0x0000006e5000-0x0000006ea000	/home/user/Desktop/lab/firefox/firefox
	0x0000006ea000-0x000001323000	
	0x00007fff7000-0x00008fff7000	
	0x00008fff7000-0x02008fff7000	
	0x02008fff7000-0x10007fff8000	
	0x600000000000-0x602000000000	
	0x602000000000-0x602000380000	
	0x602000380000-0x603000000000	
	0x603000000000-0x603000700000	
	0x603000700000-0x604000000000	
	0x604000000000-0x604000400000	
	0x604000400000-0x606000000000	
	0x606000000000-0x606000680000	
	0x606000680000-0x607000000000	
	0x607000000000-0x607000480000	
	0x607000480000-0x608000000000	
	0x608000000000-0x608000300000	
	0x608000300000-0x60b000000000	
	0x60b000000000-0x60b000280000	
	0x60b000280000-0x60c000000000	
	0x60c000000000-0x60c000580000	
	0x60c000580000-0x60d000000000	
	0x60d000000000-0x60d000280000	
	0x60d000280000-0x60e000000000	
	0x60e000000000-0x60e000200000	
	0x60e000200000-0x60f000000000	
	0x60f000000000-0x60f000280000	
	0x60f000280000-0x610000000000	
	0x610000000000-0x6100002c0000	
	0x6100002c0000-0x611000000000	
	0x611000000000-0x611001040000	
	0x611001040000-0x612000000000	
	0x612000000000-0x6120002c0000	
	0x6120002c0000-0x613000000000	
	0x613000000000-0x6130002c0000	
	0x6130002c0000-0x614000000000	
	0x614000000000-0x6140002c0000	
	0x6140002c0000-0x615000000000	
	0x615000000000-0x6150005c0000	
	0x6150005c0000-0x616000000000	
	0x616000000000-0x616000440000	
	0x616000440000-0x617000000000	
	0x617000000000-0x617000340000	
	0x617000340000-0x618000000000	
	0x618000000000-0x6180001c0000	
	0x6180001c0000-0x619000000000	
	0x619000000000-0x619001300000	
	0x619001300000-0x61a000000000	
	0x61a000000000-0x61a000200000	
	0x61a000200000-0x61b000000000	
	0x61b000000000-0x61b000300000	
	0x61b000300000-0x61c000000000	
	0x61c000000000-0x61c000100000	
	0x61c000100000-0x61d000000000	
	0x61d000000000-0x61d000a80000	
	0x61d000a80000-0x61e000000000	
	0x61e000000000-0x61e000180000	
	0x61e000180000-0x61f000000000	
	0x61f000000000-0x61f000340000	
	0x61f000340000-0x620000000000	
	0x620000000000-0x620000100000	
	0x620000100000-0x621000000000	
	0x621000000000-0x621000cc0000	
	0x621000cc0000-0x622000000000	
	0x622000000000-0x6220001c0000	
	0x6220001c0000-0x623000000000	
	0x623000000000-0x623000200000	
	0x623000200000-0x624000000000	
	0x624000000000-0x624000a80000	
	0x624000a80000-0x625000000000	
	0x625000000000-0x625001500000	
	0x625001500000-0x626000000000	
	0x626000000000-0x626000240000	
	0x626000240000-0x627000000000	
	0x627000000000-0x6270001c0000	
	0x6270001c0000-0x628000000000	
	0x628000000000-0x628000180000	
	0x628000180000-0x629000000000	
	0x629000000000-0x629000900000	
	0x629000900000-0x62a000000000	
	0x62a000000000-0x62a0001c0000	
	0x62a0001c0000-0x62b000000000	
	0x62b000000000-0x62b000200000	
	0x62b000200000-0x62c000000000	
	0x62c000000000-0x62c000140000	
	0x62c000140000-0x62d000000000	
	0x62d000000000-0x62d002580000	
	0x62d002580000-0x62e000000000	
	0x62e000000000-0x62e0001c0000	
	0x62e0001c0000-0x62f000000000	
	0x62f000000000-0x62f000240000	
	0x62f000240000-0x630000000000	
	0x630000000000-0x630000240000	
	0x630000240000-0x631000000000	
	0x631000000000-0x631001400000	
	0x631001400000-0x632000000000	
	0x632000000000-0x632000180000	
	0x632000180000-0x633000000000	
	0x633000000000-0x633000640000	
	0x633000640000-0x634000000000	
	0x634000000000-0x634000140000	
	0x634000140000-0x640000000000	
	0x640000000000-0x640000003000	
	0x7fd174725000-0x7fd1749e0000	
	0x7fd1749e0000-0x7fd174a17000	/usr/lib/x86_64-linux-gnu/libtxc_dxtn_s2tc.so.0.0.0
	0x7fd174a17000-0x7fd174c16000	/usr/lib/x86_64-linux-gnu/libtxc_dxtn_s2tc.so.0.0.0
	0x7fd174c16000-0x7fd174c17000	/usr/lib/x86_64-linux-gnu/libtxc_dxtn_s2tc.so.0.0.0
	0x7fd174c17000-0x7fd174c18000	/usr/lib/x86_64-linux-gnu/libtxc_dxtn_s2tc.so.0.0.0
	0x7fd174c18000-0x7fd174c3d000	/lib/x86_64-linux-gnu/libtinfo.so.5.9
	0x7fd174c3d000-0x7fd174e3c000	/lib/x86_64-linux-gnu/libtinfo.so.5.9
	0x7fd174e3c000-0x7fd174e40000	/lib/x86_64-linux-gnu/libtinfo.so.5.9
	0x7fd174e40000-0x7fd174e41000	/lib/x86_64-linux-gnu/libtinfo.so.5.9
	0x7fd174e41000-0x7fd174e6a000	/usr/lib/x86_64-linux-gnu/libedit.so.2.0.47
	0x7fd174e6a000-0x7fd17506a000	/usr/lib/x86_64-linux-gnu/libedit.so.2.0.47
	0x7fd17506a000-0x7fd17506c000	/usr/lib/x86_64-linux-gnu/libedit.so.2.0.47
	0x7fd17506c000-0x7fd17506d000	/usr/lib/x86_64-linux-gnu/libedit.so.2.0.47
	0x7fd17506d000-0x7fd175071000	
	0x7fd175071000-0x7fd176b79000	/usr/lib/x86_64-linux-gnu/libLLVM-3.5.so.1
	0x7fd176b79000-0x7fd176b7a000	/usr/lib/x86_64-linux-gnu/libLLVM-3.5.so.1
	0x7fd176b7a000-0x7fd176d42000	/usr/lib/x86_64-linux-gnu/libLLVM-3.5.so.1
	0x7fd176d42000-0x7fd176d48000	/usr/lib/x86_64-linux-gnu/libLLVM-3.5.so.1
	0x7fd176d48000-0x7fd176d5d000	
	0x7fd176d5d000-0x7fd176d69000	/usr/lib/x86_64-linux-gnu/libdrm_radeon.so.1.0.1
	0x7fd176d69000-0x7fd176f68000	/usr/lib/x86_64-linux-gnu/libdrm_radeon.so.1.0.1
	0x7fd176f68000-0x7fd176f69000	/usr/lib/x86_64-linux-gnu/libdrm_radeon.so.1.0.1
	0x7fd176f69000-0x7fd176f6a000	/usr/lib/x86_64-linux-gnu/libdrm_radeon.so.1.0.1
	0x7fd176f6a000-0x7fd176f7f000	/usr/lib/x86_64-linux-gnu/libelf-0.158.so
	0x7fd176f7f000-0x7fd17717e000	/usr/lib/x86_64-linux-gnu/libelf-0.158.so
	0x7fd17717e000-0x7fd17717f000	/usr/lib/x86_64-linux-gnu/libelf-0.158.so
	0x7fd17717f000-0x7fd177180000	/usr/lib/x86_64-linux-gnu/libelf-0.158.so
	0x7fd177180000-0x7fd177186000	/usr/lib/x86_64-linux-gnu/libdrm_nouveau.so.2.0.0
	0x7fd177186000-0x7fd177385000	/usr/lib/x86_64-linux-gnu/libdrm_nouveau.so.2.0.0
	0x7fd177385000-0x7fd177386000	/usr/lib/x86_64-linux-gnu/libdrm_nouveau.so.2.0.0
	0x7fd177386000-0x7fd177387000	/usr/lib/x86_64-linux-gnu/libdrm_nouveau.so.2.0.0
	0x7fd177387000-0x7fd177acd000	/usr/lib/x86_64-linux-gnu/dri/swrast_dri.so
	0x7fd177acd000-0x7fd177ccc000	/usr/lib/x86_64-linux-gnu/dri/swrast_dri.so
	0x7fd177ccc000-0x7fd177d1f000	/usr/lib/x86_64-linux-gnu/dri/swrast_dri.so
	0x7fd177d1f000-0x7fd177d2b000	/usr/lib/x86_64-linux-gnu/dri/swrast_dri.so
	0x7fd177d2b000-0x7fd177f13000	
	0x7fd177f13000-0x7fd177f1e000	/usr/lib/x86_64-linux-gnu/libdrm.so.2.4.0
	0x7fd177f1e000-0x7fd17811d000	/usr/lib/x86_64-linux-gnu/libdrm.so.2.4.0
	0x7fd17811d000-0x7fd17811e000	/usr/lib/x86_64-linux-gnu/libdrm.so.2.4.0
	0x7fd17811e000-0x7fd17811f000	/usr/lib/x86_64-linux-gnu/libdrm.so.2.4.0
	0x7fd17811f000-0x7fd178123000	/usr/lib/x86_64-linux-gnu/libXxf86vm.so.1.0.0
	0x7fd178123000-0x7fd178323000	/usr/lib/x86_64-linux-gnu/libXxf86vm.so.1.0.0
	0x7fd178323000-0x7fd178324000	/usr/lib/x86_64-linux-gnu/libXxf86vm.so.1.0.0
	0x7fd178324000-0x7fd178325000	/usr/lib/x86_64-linux-gnu/libXxf86vm.so.1.0.0
	0x7fd178325000-0x7fd178326000	/usr/lib/x86_64-linux-gnu/libxshmfence.so.1.0.0
	0x7fd178326000-0x7fd178525000	/usr/lib/x86_64-linux-gnu/libxshmfence.so.1.0.0
	0x7fd178525000-0x7fd178526000	/usr/lib/x86_64-linux-gnu/libxshmfence.so.1.0.0
	0x7fd178526000-0x7fd178527000	/usr/lib/x86_64-linux-gnu/libxshmfence.so.1.0.0
	0x7fd178527000-0x7fd17852c000	/usr/lib/x86_64-linux-gnu/libxcb-sync.so.1.0.0
	0x7fd17852c000-0x7fd17872b000	/usr/lib/x86_64-linux-gnu/libxcb-sync.so.1.0.0
	0x7fd17872b000-0x7fd17872c000	/usr/lib/x86_64-linux-gnu/libxcb-sync.so.1.0.0
	0x7fd17872c000-0x7fd17872d000	/usr/lib/x86_64-linux-gnu/libxcb-sync.so.1.0.0
	0x7fd17872d000-0x7fd17872f000	/usr/lib/x86_64-linux-gnu/libxcb-present.so.0.0.0
	0x7fd17872f000-0x7fd17892e000	/usr/lib/x86_64-linux-gnu/libxcb-present.so.0.0.0
	0x7fd17892e000-0x7fd17892f000	/usr/lib/x86_64-linux-gnu/libxcb-present.so.0.0.0
	0x7fd17892f000-0x7fd178930000	/usr/lib/x86_64-linux-gnu/libxcb-present.so.0.0.0
	0x7fd178930000-0x7fd178932000	/usr/lib/x86_64-linux-gnu/libxcb-dri3.so.0.0.0
	0x7fd178932000-0x7fd178b31000	/usr/lib/x86_64-linux-gnu/libxcb-dri3.so.0.0.0
	0x7fd178b31000-0x7fd178b32000	/usr/lib/x86_64-linux-gnu/libxcb-dri3.so.0.0.0
	0x7fd178b32000-0x7fd178b33000	/usr/lib/x86_64-linux-gnu/libxcb-dri3.so.0.0.0
	0x7fd178b33000-0x7fd178b36000	/usr/lib/x86_64-linux-gnu/libxcb-dri2.so.0.0.0
	0x7fd178b36000-0x7fd178d36000	/usr/lib/x86_64-linux-gnu/libxcb-dri2.so.0.0.0
	0x7fd178d36000-0x7fd178d37000	/usr/lib/x86_64-linux-gnu/libxcb-dri2.so.0.0.0
	0x7fd178d37000-0x7fd178d38000	/usr/lib/x86_64-linux-gnu/libxcb-dri2.so.0.0.0
	0x7fd178d38000-0x7fd178d4d000	/usr/lib/x86_64-linux-gnu/libxcb-glx.so.0.0.0
	0x7fd178d4d000-0x7fd178f4c000	/usr/lib/x86_64-linux-gnu/libxcb-glx.so.0.0.0
	0x7fd178f4c000-0x7fd178f4e000	/usr/lib/x86_64-linux-gnu/libxcb-glx.so.0.0.0
	0x7fd178f4e000-0x7fd178f4f000	/usr/lib/x86_64-linux-gnu/libxcb-glx.so.0.0.0
	0x7fd178f4f000-0x7fd178f50000	/usr/lib/x86_64-linux-gnu/libX11-xcb.so.1.0.0
	0x7fd178f50000-0x7fd17914f000	/usr/lib/x86_64-linux-gnu/libX11-xcb.so.1.0.0
	0x7fd17914f000-0x7fd179150000	/usr/lib/x86_64-linux-gnu/libX11-xcb.so.1.0.0
	0x7fd179150000-0x7fd179151000	/usr/lib/x86_64-linux-gnu/libX11-xcb.so.1.0.0
	0x7fd179151000-0x7fd179175000	/usr/lib/x86_64-linux-gnu/libglapi.so.0.0.0
	0x7fd179175000-0x7fd179375000	/usr/lib/x86_64-linux-gnu/libglapi.so.0.0.0
	0x7fd179375000-0x7fd179378000	/usr/lib/x86_64-linux-gnu/libglapi.so.0.0.0
	0x7fd179378000-0x7fd179379000	/usr/lib/x86_64-linux-gnu/libglapi.so.0.0.0
	0x7fd179379000-0x7fd17937a000	
	0x7fd17937a000-0x7fd179406000	/usr/lib/x86_64-linux-gnu/mesa/libGL.so.1.2.0
	0x7fd179406000-0x7fd179605000	/usr/lib/x86_64-linux-gnu/mesa/libGL.so.1.2.0
	0x7fd179605000-0x7fd179608000	/usr/lib/x86_64-linux-gnu/mesa/libGL.so.1.2.0
	0x7fd179608000-0x7fd179609000	/usr/lib/x86_64-linux-gnu/mesa/libGL.so.1.2.0
	0x7fd179609000-0x7fd17960a000	
	0x7fd17960f000-0x7fd179d00000	
	0x7fd179d0a000-0x7fd17a8f9000	
	0x7fd17a8f9000-0x7fd17a8fa000	
	0x7fd17a8fa000-0x7fd17b500000	[stack:4024]
	0x7fd17b508000-0x7fd17b5f8000	
	0x7fd17b5f8000-0x7fd17b65e000	/usr/share/fonts/truetype/ubuntu-font-family/Ubuntu-L.ttf
	0x7fd17b65e000-0x7fd17b6c4000	/usr/share/fonts/truetype/ubuntu-font-family/Ubuntu-L.ttf
	0x7fd17b6c4000-0x7fd17b744000	
	0x7fd17b744000-0x7fd17b745000	
	0x7fd17b745000-0x7fd17c25b000	[stack:4023]
	0x7fd17c25b000-0x7fd17c25c000	
	0x7fd17c25c000-0x7fd17ce22000	[stack:4022]
	0x7fd17ce22000-0x7fd17ce23000	
	0x7fd17ce23000-0x7fd17d1f9000	[stack:4021]
	0x7fd17d1f9000-0x7fd17d1fa000	
	0x7fd17d1fa000-0x7fd17deb9000	[stack:4020]
	0x7fd17deb9000-0x7fd17deba000	
	0x7fd17deba000-0x7fd17ea20000	[stack:4019]
	0x7fd17ea20000-0x7fd17ea4e000	/usr/lib/x86_64-linux-gnu/libgconf-2.so.4.1.5
	0x7fd17ea4e000-0x7fd17ec4d000	/usr/lib/x86_64-linux-gnu/libgconf-2.so.4.1.5
	0x7fd17ec4d000-0x7fd17ec4e000	/usr/lib/x86_64-linux-gnu/libgconf-2.so.4.1.5
	0x7fd17ec4e000-0x7fd17ec4f000	/usr/lib/x86_64-linux-gnu/libgconf-2.so.4.1.5
	0x7fd17ec54000-0x7fd17ecd4000	
	0x7fd17ecd4000-0x7fd17ecd5000	
	0x7fd17ecd5000-0x7fd17f7db000	[stack:4018]
	0x7fd17f7db000-0x7fd17f7e0000	/lib/x86_64-linux-gnu/libnss_dns-2.19.so
	0x7fd17f7e0000-0x7fd17f9df000	/lib/x86_64-linux-gnu/libnss_dns-2.19.so
	0x7fd17f9df000-0x7fd17f9e0000	/lib/x86_64-linux-gnu/libnss_dns-2.19.so
	0x7fd17f9e0000-0x7fd17f9e1000	/lib/x86_64-linux-gnu/libnss_dns-2.19.so
	0x7fd17f9e1000-0x7fd17f9e2000	
	0x7fd17f9e2000-0x7fd1804e8000	[stack:4017]
	0x7fd1804e8000-0x7fd1804ea000	/lib/x86_64-linux-gnu/libnss_mdns4_minimal.so.2
	0x7fd1804ea000-0x7fd1806e9000	/lib/x86_64-linux-gnu/libnss_mdns4_minimal.so.2
	0x7fd1806e9000-0x7fd1806ea000	/lib/x86_64-linux-gnu/libnss_mdns4_minimal.so.2
	0x7fd1806ea000-0x7fd1806eb000	/lib/x86_64-linux-gnu/libnss_mdns4_minimal.so.2
	0x7fd1806eb000-0x7fd1806ec000	
	0x7fd1806ec000-0x7fd1811f2000	[stack:4016]
	0x7fd1811f2000-0x7fd1811f3000	
	0x7fd1811f3000-0x7fd181cf9000	[stack:4015]
	0x7fd181cf9000-0x7fd181cfa000	
	0x7fd181cfa000-0x7fd182900000	[stack:4014]
	0x7fd182909000-0x7fd182939000	
	0x7fd18293e000-0x7fd182c00000	
	0x7fd182c0e000-0x7fd182e00000	
	0x7fd182e05000-0x7fd183400000	
	0x7fd18340e000-0x7fd183a00000	
	0x7fd183a0e000-0x7fd184100000	
	0x7fd18410b000-0x7fd184800000	
	0x7fd184802000-0x7fd1848b5000	
	0x7fd1848b5000-0x7fd1848b6000	
	0x7fd1848b6000-0x7fd1856d2000	[stack:4013]
	0x7fd1856d2000-0x7fd1857bc000	/home/user/Desktop/lab/firefox/libnssckbi.so
	0x7fd1857bc000-0x7fd1859bc000	/home/user/Desktop/lab/firefox/libnssckbi.so
	0x7fd1859bc000-0x7fd1859fe000	/home/user/Desktop/lab/firefox/libnssckbi.so
	0x7fd1859fe000-0x7fd185b3a000	/home/user/Desktop/lab/firefox/libfreebl3.so
	0x7fd185b3a000-0x7fd185d39000	/home/user/Desktop/lab/firefox/libfreebl3.so
	0x7fd185d39000-0x7fd185d3e000	/home/user/Desktop/lab/firefox/libfreebl3.so
	0x7fd185d3e000-0x7fd185d43000	
	0x7fd185d43000-0x7fd185db5000	/home/user/Desktop/lab/firefox/libnssdbm3.so
	0x7fd185db5000-0x7fd185fb5000	/home/user/Desktop/lab/firefox/libnssdbm3.so
	0x7fd185fb5000-0x7fd185fb8000	/home/user/Desktop/lab/firefox/libnssdbm3.so
	0x7fd185fb8000-0x7fd186057000	/home/user/Desktop/lab/firefox/libsoftokn3.so
	0x7fd186057000-0x7fd186257000	/home/user/Desktop/lab/firefox/libsoftokn3.so
	0x7fd186257000-0x7fd18625e000	/home/user/Desktop/lab/firefox/libsoftokn3.so
	0x7fd18625e000-0x7fd186400000	
	0x7fd186400000-0x7fd186408000	/home/user/.mozilla/firefox/hl23m4kw.default/healthreport.sqlite-shm
	0x7fd186408000-0x7fd186b00000	
	0x7fd186b01000-0x7fd186b09000	/home/user/.mozilla/firefox/hl23m4kw.default/webappsstore.sqlite-shm
	0x7fd186b09000-0x7fd186e00000	
	0x7fd186e05000-0x7fd186e0d000	/home/user/.mozilla/firefox/hl23m4kw.default/cookies.sqlite-shm
	0x7fd186e0d000-0x7fd187000000	
	0x7fd187005000-0x7fd1870f9000	
	0x7fd1870f9000-0x7fd1870fa000	
	0x7fd1870fa000-0x7fd1878fa000	[stack:4012]
	0x7fd1878fb000-0x7fd1879c7000	
	0x7fd1879c7000-0x7fd1879c9000	/usr/lib/x86_64-linux-gnu/libXss.so.1.0.0
	0x7fd1879c9000-0x7fd187bc9000	/usr/lib/x86_64-linux-gnu/libXss.so.1.0.0
	0x7fd187bc9000-0x7fd187bca000	/usr/lib/x86_64-linux-gnu/libXss.so.1.0.0
	0x7fd187bca000-0x7fd187bcb000	/usr/lib/x86_64-linux-gnu/libXss.so.1.0.0
	0x7fd187bd0000-0x7fd187bf0000	
	0x7fd187bf0000-0x7fd187c00000	
	0x7fd187c00000-0x7fd187e00000	
	0x7fd187e01000-0x7fd187e46000	
	0x7fd187e46000-0x7fd187e56000	
	0x7fd187e56000-0x7fd187e66000	
	0x7fd187e66000-0x7fd187e67000	
	0x7fd187e67000-0x7fd18839d000	[stack:4009]
	0x7fd18839d000-0x7fd1883ad000	
	0x7fd1883ad000-0x7fd188452000	
	0x7fd188452000-0x7fd188462000	
	0x7fd188462000-0x7fd1884a2000	
	0x7fd1884a2000-0x7fd188522000	/SYSV00000000 (deleted)
	0x7fd188522000-0x7fd188542000	
	0x7fd188542000-0x7fd188599000	/usr/share/fonts/truetype/ubuntu-font-family/Ubuntu-R.ttf
	0x7fd188599000-0x7fd1885a9000	
	0x7fd1885a9000-0x7fd18865f000	/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf
	0x7fd18865f000-0x7fd1886b6000	/usr/share/fonts/truetype/ubuntu-font-family/Ubuntu-R.ttf
	0x7fd1886b6000-0x7fd1886c6000	
	0x7fd1886c6000-0x7fd188720000	/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf
	0x7fd188720000-0x7fd188730000	
	0x7fd188730000-0x7fd188731000	
	0x7fd188731000-0x7fd189237000	[stack:4008]
	0x7fd189237000-0x7fd189238000	
	0x7fd189238000-0x7fd189d3e000	[stack:4007]
	0x7fd189d3e000-0x7fd189d5f000	/lib/x86_64-linux-gnu/liblzma.so.5.0.0
	0x7fd189d5f000-0x7fd189f5e000	/lib/x86_64-linux-gnu/liblzma.so.5.0.0
	0x7fd189f5e000-0x7fd189f5f000	/lib/x86_64-linux-gnu/liblzma.so.5.0.0
	0x7fd189f5f000-0x7fd189f60000	/lib/x86_64-linux-gnu/liblzma.so.5.0.0
	0x7fd189f60000-0x7fd18a0bc000	/usr/lib/x86_64-linux-gnu/libxml2.so.2.9.1
	0x7fd18a0bc000-0x7fd18a2bb000	/usr/lib/x86_64-linux-gnu/libxml2.so.2.9.1
	0x7fd18a2bb000-0x7fd18a2c3000	/usr/lib/x86_64-linux-gnu/libxml2.so.2.9.1
	0x7fd18a2c3000-0x7fd18a2c5000	/usr/lib/x86_64-linux-gnu/libxml2.so.2.9.1
	0x7fd18a2c5000-0x7fd18a2c6000	
	0x7fd18a2c6000-0x7fd18a2fe000	/usr/lib/x86_64-linux-gnu/libcroco-0.6.so.3.0.1
	0x7fd18a2fe000-0x7fd18a4fd000	/usr/lib/x86_64-linux-gnu/libcroco-0.6.so.3.0.1
	0x7fd18a4fd000-0x7fd18a500000	/usr/lib/x86_64-linux-gnu/libcroco-0.6.so.3.0.1
	0x7fd18a500000-0x7fd18a501000	/usr/lib/x86_64-linux-gnu/libcroco-0.6.so.3.0.1
	0x7fd18a501000-0x7fd18a535000	/usr/lib/x86_64-linux-gnu/librsvg-2.so.2.40.2
	0x7fd18a535000-0x7fd18a734000	/usr/lib/x86_64-linux-gnu/librsvg-2.so.2.40.2
	0x7fd18a734000-0x7fd18a735000	/usr/lib/x86_64-linux-gnu/librsvg-2.so.2.40.2
	0x7fd18a735000-0x7fd18a736000	/usr/lib/x86_64-linux-gnu/librsvg-2.so.2.40.2
	0x7fd18a73b000-0x7fd18a74b000	
	0x7fd18a74b000-0x7fd18a74d000	/usr/lib/x86_64-linux-gnu/gdk-pixbuf-2.0/2.10.0/loaders/libpixbufloader-svg.so
	0x7fd18a74d000-0x7fd18a94c000	/usr/lib/x86_64-linux-gnu/gdk-pixbuf-2.0/2.10.0/loaders/libpixbufloader-svg.so
	0x7fd18a94c000-0x7fd18a94d000	/usr/lib/x86_64-linux-gnu/gdk-pixbuf-2.0/2.10.0/loaders/libpixbufloader-svg.so
	0x7fd18a94d000-0x7fd18a94e000	/usr/lib/x86_64-linux-gnu/gdk-pixbuf-2.0/2.10.0/loaders/libpixbufloader-svg.so
	0x7fd18a94e000-0x7fd18a99e000	
	0x7fd18a99e000-0x7fd18a99f000	
	0x7fd18a99f000-0x7fd18b7db000	[stack:4006]
	0x7fd18b7db000-0x7fd18b7dc000	
	0x7fd18b7dc000-0x7fd18bd32000	[stack:4004]
	0x7fd18bd32000-0x7fd18bd33000	
	0x7fd18bd33000-0x7fd18c839000	[stack:4003]
	0x7fd18c839000-0x7fd18c83a000	
	0x7fd18c83a000-0x7fd18d500000	[stack:4002]
	0x7fd18d504000-0x7fd18d5c4000	
	0x7fd18d5c4000-0x7fd18d624000	/SYSV00000000 (deleted)
	0x7fd18d624000-0x7fd18d628000	/usr/lib/x86_64-linux-gnu/gdk-pixbuf-2.0/2.10.0/loaders/libpixbufloader-png.so
	0x7fd18d628000-0x7fd18d828000	/usr/lib/x86_64-linux-gnu/gdk-pixbuf-2.0/2.10.0/loaders/libpixbufloader-png.so
	0x7fd18d828000-0x7fd18d829000	/usr/lib/x86_64-linux-gnu/gdk-pixbuf-2.0/2.10.0/loaders/libpixbufloader-png.so
	0x7fd18d829000-0x7fd18d82a000	/usr/lib/x86_64-linux-gnu/gdk-pixbuf-2.0/2.10.0/loaders/libpixbufloader-png.so
	0x7fd18d82a000-0x7fd18d848000	/usr/share/mime/mime.cache
	0x7fd18d848000-0x7fd18fc53000	/usr/share/icons/hicolor/icon-theme.cache
	0x7fd18fc53000-0x7fd191297000	/usr/share/icons/gnome/icon-theme.cache
	0x7fd191297000-0x7fd191380000	/usr/share/icons/Humanity/icon-theme.cache
	0x7fd191380000-0x7fd191392000	/usr/share/icons/ubuntu-mono-dark/icon-theme.cache
	0x7fd191392000-0x7fd1913e2000	
	0x7fd1913e2000-0x7fd1913e3000	
	0x7fd1913e3000-0x7fd191ef9000	[stack:4001]
	0x7fd191ef9000-0x7fd191efa000	
	0x7fd191efa000-0x7fd192b00000	[stack:4000]
	0x7fd192b05000-0x7fd192ef9000	
	0x7fd192ef9000-0x7fd192efa000	
	0x7fd192efa000-0x7fd193b00000	[stack:3998]
	0x7fd193b00000-0x7fd193b08000	/home/user/.mozilla/firefox/hl23m4kw.default/places.sqlite-shm
	0x7fd193b08000-0x7fd193b48000	
	0x7fd193b48000-0x7fd193b49000	
	0x7fd193b49000-0x7fd193d00000	[stack:3999]
	0x7fd193d01000-0x7fd193d02000	
	0x7fd193d02000-0x7fd193d0d000	/usr/share/icons/Humanity-Dark/icon-theme.cache
	0x7fd193d0d000-0x7fd193dcf000	
	0x7fd193dcf000-0x7fd193dd0000	
	0x7fd193dd0000-0x7fd1948d6000	[stack:4005]
	0x7fd1948d6000-0x7fd1948d7000	
	0x7fd1948d7000-0x7fd1953dd000	[stack:3996]
	0x7fd1953dd000-0x7fd1953de000	
	0x7fd1953de000-0x7fd195ee4000	[stack:3995]
	0x7fd195ee4000-0x7fd195f3d000	/usr/lib/x86_64-linux-gnu/libibus-1.0.so.5.0.505
	0x7fd195f3d000-0x7fd19613d000	/usr/lib/x86_64-linux-gnu/libibus-1.0.so.5.0.505
	0x7fd19613d000-0x7fd19613f000	/usr/lib/x86_64-linux-gnu/libibus-1.0.so.5.0.505
	0x7fd19613f000-0x7fd196140000	/usr/lib/x86_64-linux-gnu/libibus-1.0.so.5.0.505
	0x7fd196140000-0x7fd196141000	
	0x7fd196141000-0x7fd196147000	/usr/lib/x86_64-linux-gnu/gtk-2.0/2.10.0/immodules/im-ibus.so
	0x7fd196147000-0x7fd196347000	/usr/lib/x86_64-linux-gnu/gtk-2.0/2.10.0/immodules/im-ibus.so
	0x7fd196347000-0x7fd196348000	/usr/lib/x86_64-linux-gnu/gtk-2.0/2.10.0/immodules/im-ibus.so
	0x7fd196348000-0x7fd196349000	/usr/lib/x86_64-linux-gnu/gtk-2.0/2.10.0/immodules/im-ibus.so
	0x7fd196349000-0x7fd19639b000	/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf
	0x7fd19639b000-0x7fd1963f2000	/usr/share/fonts/truetype/ubuntu-font-family/Ubuntu-R.ttf
	0x7fd1963f2000-0x7fd1963f3000	
	0x7fd1963f3000-0x7fd196ef9000	[stack:3994]
	0x7fd196ef9000-0x7fd196efa000	
	0x7fd196efa000-0x7fd197b01000	[stack:3993]
	0x7fd197b01000-0x7fd197b04000	
	0x7fd197b04000-0x7fd197b05000	/home/user/.local/share/mime/mime.cache
	0x7fd197b05000-0x7fd197b36000	
	0x7fd197b36000-0x7fd197b37000	/var/cache/fontconfig/c05880de57d1f5e948fdfacc138775d9-le64.cache-4
	0x7fd197b37000-0x7fd197b42000	/var/cache/fontconfig/945677eb7aeaf62f1d50efc3fb3ec7d8-le64.cache-4
	0x7fd197b42000-0x7fd197b48000	/var/cache/fontconfig/2cd17615ca594fa2959ae173292e504c-le64.cache-4
	0x7fd197b48000-0x7fd197b49000	/var/cache/fontconfig/e7071f4a29fa870f4323321c154eba04-le64.cache-4
	0x7fd197b49000-0x7fd197b4a000	/var/cache/fontconfig/0d8c3b2ac0904cb8a57a757ad11a4a08-le64.cache-4
	0x7fd197b4a000-0x7fd197b51000	/var/cache/fontconfig/a755afe4a08bf5b97852ceb7400b47bc-le64.cache-4
	0x7fd197b51000-0x7fd197b65000	/var/cache/fontconfig/04aabc0a78ac019cf9454389977116d2-le64.cache-4
	0x7fd197b65000-0x7fd197b66000	/var/cache/fontconfig/1ac9eb803944fde146138c791f5cc56a-le64.cache-4
	0x7fd197b66000-0x7fd197b6a000	/var/cache/fontconfig/385c0604a188198f04d133e54aba7fe7-le64.cache-4
	0x7fd197b6a000-0x7fd197b6b000	/var/cache/fontconfig/dc05db6664285cc2f12bf69c139ae4c3-le64.cache-4
	0x7fd197b6b000-0x7fd197b70000	/var/cache/fontconfig/8801497958630a81b71ace7c5f9b32a8-le64.cache-4
	0x7fd197b70000-0x7fd197b77000	/var/cache/fontconfig/3047814df9a2f067bd2d96a2b9c36e5a-le64.cache-4
	0x7fd197b77000-0x7fd197b7d000	/var/cache/fontconfig/b47c4e1ecd0709278f4910c18777a504-le64.cache-4
	0x7fd197b7d000-0x7fd197b90000	/var/cache/fontconfig/d52a8644073d54c13679302ca1180695-le64.cache-4
	0x7fd197b90000-0x7fd197b99000	/var/cache/fontconfig/3f7329c5293ffd510edef78f73874cfd-le64.cache-4
	0x7fd197b99000-0x7fd197bf9000	
	0x7fd197bf9000-0x7fd197bfa000	
	0x7fd197bfa000-0x7fd199700000	[stack:3992]
	0x7fd199700000-0x7fd19970b000	/var/cache/fontconfig/d589a48862398ed80a3d6066f4f56f4c-le64.cache-4
	0x7fd19970b000-0x7fd1997b7000	
	0x7fd1997b7000-0x7fd1997b8000	
	0x7fd1997b8000-0x7fd199b3e000	[stack:3991]
	0x7fd199b3e000-0x7fd199b3f000	
	0x7fd199b3f000-0x7fd199ec5000	[stack:3990]
	0x7fd199ec5000-0x7fd199ec6000	
	0x7fd199ec6000-0x7fd19a24c000	[stack:3989]
	0x7fd19a24c000-0x7fd19a24d000	
	0x7fd19a24d000-0x7fd19a5d3000	[stack:3988]
	0x7fd19a5d3000-0x7fd19a5d4000	
	0x7fd19a5d4000-0x7fd19a95a000	[stack:3987]
	0x7fd19a95a000-0x7fd19a98a000	/home/user/Desktop/lab/firefox/browser/components/libbrowsercomps.so
	0x7fd19a98a000-0x7fd19ab8a000	/home/user/Desktop/lab/firefox/browser/components/libbrowsercomps.so
	0x7fd19ab8a000-0x7fd19ab90000	/home/user/Desktop/lab/firefox/browser/components/libbrowsercomps.so
	0x7fd19ab90000-0x7fd19abf7000	
	0x7fd19abf7000-0x7fd19abf8000	
	0x7fd19abf8000-0x7fd19b70e000	[stack:3986]
	0x7fd19b70e000-0x7fd19b70f000	
	0x7fd19b70f000-0x7fd19c277000	[stack:3985]
	0x7fd19c277000-0x7fd19c2a3000	/home/user/Desktop/lab/firefox/components/libmozgnome.so
	0x7fd19c2a3000-0x7fd19c4a2000	/home/user/Desktop/lab/firefox/components/libmozgnome.so
	0x7fd19c4a2000-0x7fd19c4a9000	/home/user/Desktop/lab/firefox/components/libmozgnome.so
	0x7fd19c4a9000-0x7fd19c4c3000	/home/user/Desktop/lab/firefox/components/libdbusservice.so
	0x7fd19c4c3000-0x7fd19c6c3000	/home/user/Desktop/lab/firefox/components/libdbusservice.so
	0x7fd19c6c3000-0x7fd19c6c6000	/home/user/Desktop/lab/firefox/components/libdbusservice.so
	0x7fd19c6c6000-0x7fd19d1b3000	/home/user/Desktop/lab/firefox/browser/omni.ja
	0x7fd19d1b3000-0x7fd19dc63000	/home/user/Desktop/lab/firefox/omni.ja
	0x7fd19dc63000-0x7fd19dc64000	
	0x7fd19dc64000-0x7fd19e76a000	[stack:3984]
	0x7fd19e76a000-0x7fd19e76b000	
	0x7fd19e76b000-0x7fd19f271000	[stack:3983]
	0x7fd19f271000-0x7fd19f27c000	/lib/x86_64-linux-gnu/libnss_files-2.19.so
	0x7fd19f27c000-0x7fd19f47b000	/lib/x86_64-linux-gnu/libnss_files-2.19.so
	0x7fd19f47b000-0x7fd19f47c000	/lib/x86_64-linux-gnu/libnss_files-2.19.so
	0x7fd19f47c000-0x7fd19f47d000	/lib/x86_64-linux-gnu/libnss_files-2.19.so
	0x7fd19f47d000-0x7fd19f47e000	
	0x7fd19f47e000-0x7fd19ff84000	[stack:3982]
	0x7fd19ff84000-0x7fd19ff8c000	/lib/x86_64-linux-gnu/libnih-dbus.so.1.0.0
	0x7fd19ff8c000-0x7fd1a018c000	/lib/x86_64-linux-gnu/libnih-dbus.so.1.0.0
	0x7fd1a018c000-0x7fd1a018d000	/lib/x86_64-linux-gnu/libnih-dbus.so.1.0.0
	0x7fd1a018d000-0x7fd1a018e000	/lib/x86_64-linux-gnu/libnih-dbus.so.1.0.0
	0x7fd1a018e000-0x7fd1a01a5000	/lib/x86_64-linux-gnu/libnih.so.1.0.0
	0x7fd1a01a5000-0x7fd1a03a4000	/lib/x86_64-linux-gnu/libnih.so.1.0.0
	0x7fd1a03a4000-0x7fd1a03a5000	/lib/x86_64-linux-gnu/libnih.so.1.0.0
	0x7fd1a03a5000-0x7fd1a03a6000	/lib/x86_64-linux-gnu/libnih.so.1.0.0
	0x7fd1a03a6000-0x7fd1a03bf000	/lib/x86_64-linux-gnu/libcgmanager.so.0.0.0
	0x7fd1a03bf000-0x7fd1a05be000	/lib/x86_64-linux-gnu/libcgmanager.so.0.0.0
	0x7fd1a05be000-0x7fd1a05c0000	/lib/x86_64-linux-gnu/libcgmanager.so.0.0.0
	0x7fd1a05c0000-0x7fd1a05c1000	/lib/x86_64-linux-gnu/libcgmanager.so.0.0.0
	0x7fd1a05c1000-0x7fd1a05d1000	/lib/x86_64-linux-gnu/libudev.so.1.3.5
	0x7fd1a05d1000-0x7fd1a07d0000	/lib/x86_64-linux-gnu/libudev.so.1.3.5
	0x7fd1a07d0000-0x7fd1a07d1000	/lib/x86_64-linux-gnu/libudev.so.1.3.5
	0x7fd1a07d1000-0x7fd1a07d2000	/lib/x86_64-linux-gnu/libudev.so.1.3.5
	0x7fd1a07d2000-0x7fd1a0806000	/usr/lib/x86_64-linux-gnu/gvfs/libgvfscommon.so
	0x7fd1a0806000-0x7fd1a0a06000	/usr/lib/x86_64-linux-gnu/gvfs/libgvfscommon.so
	0x7fd1a0a06000-0x7fd1a0a0b000	/usr/lib/x86_64-linux-gnu/gvfs/libgvfscommon.so
	0x7fd1a0a0b000-0x7fd1a0a0c000	/usr/lib/x86_64-linux-gnu/gvfs/libgvfscommon.so
	0x7fd1a0a0c000-0x7fd1a0a3b000	/usr/lib/x86_64-linux-gnu/gio/modules/libgvfsdbus.so
	0x7fd1a0a3b000-0x7fd1a0c3b000	/usr/lib/x86_64-linux-gnu/gio/modules/libgvfsdbus.so
	0x7fd1a0c3b000-0x7fd1a0c3c000	/usr/lib/x86_64-linux-gnu/gio/modules/libgvfsdbus.so
	0x7fd1a0c3c000-0x7fd1a0c3d000	/usr/lib/x86_64-linux-gnu/gio/modules/libgvfsdbus.so
	0x7fd1a0c3d000-0x7fd1a0c3e000	
	0x7fd1a0c3e000-0x7fd1a0c57000	/usr/lib/x86_64-linux-gnu/gio/modules/libgioremote-volume-monitor.so
	0x7fd1a0c57000-0x7fd1a0e57000	/usr/lib/x86_64-linux-gnu/gio/modules/libgioremote-volume-monitor.so
	0x7fd1a0e57000-0x7fd1a0e5a000	/usr/lib/x86_64-linux-gnu/gio/modules/libgioremote-volume-monitor.so
	0x7fd1a0e5a000-0x7fd1a0e5b000	/usr/lib/x86_64-linux-gnu/gio/modules/libgioremote-volume-monitor.so
	0x7fd1a0e5b000-0x7fd1a0e66000	/usr/lib/x86_64-linux-gnu/gio/modules/libdconfsettings.so
	0x7fd1a0e66000-0x7fd1a1066000	/usr/lib/x86_64-linux-gnu/gio/modules/libdconfsettings.so
	0x7fd1a1066000-0x7fd1a1067000	/usr/lib/x86_64-linux-gnu/gio/modules/libdconfsettings.so
	0x7fd1a1067000-0x7fd1a1068000	/usr/lib/x86_64-linux-gnu/gio/modules/libdconfsettings.so
	0x7fd1a1068000-0x7fd1a10aa000	/usr/share/glib-2.0/schemas/gschemas.compiled
	0x7fd1a10aa000-0x7fd1a10b1000	/usr/lib/x86_64-linux-gnu/libogg.so.0.8.1
	0x7fd1a10b1000-0x7fd1a12b1000	/usr/lib/x86_64-linux-gnu/libogg.so.0.8.1
	0x7fd1a12b1000-0x7fd1a12b2000	/usr/lib/x86_64-linux-gnu/libogg.so.0.8.1
	0x7fd1a12b2000-0x7fd1a12b3000	/usr/lib/x86_64-linux-gnu/libogg.so.0.8.1
	0x7fd1a12b3000-0x7fd1a12df000	/usr/lib/x86_64-linux-gnu/libvorbis.so.0.4.5
	0x7fd1a12df000-0x7fd1a14de000	/usr/lib/x86_64-linux-gnu/libvorbis.so.0.4.5
	0x7fd1a14de000-0x7fd1a14df000	/usr/lib/x86_64-linux-gnu/libvorbis.so.0.4.5
	0x7fd1a14df000-0x7fd1a14e0000	/usr/lib/x86_64-linux-gnu/libvorbis.so.0.4.5
	0x7fd1a14e0000-0x7fd1a14e9000	/usr/lib/x86_64-linux-gnu/libltdl.so.7.3.0
	0x7fd1a14e9000-0x7fd1a16e8000	/usr/lib/x86_64-linux-gnu/libltdl.so.7.3.0
	0x7fd1a16e8000-0x7fd1a16e9000	/usr/lib/x86_64-linux-gnu/libltdl.so.7.3.0
	0x7fd1a16e9000-0x7fd1a16ea000	/usr/lib/x86_64-linux-gnu/libltdl.so.7.3.0
	0x7fd1a16ea000-0x7fd1a16fb000	/usr/lib/x86_64-linux-gnu/libtdb.so.1.2.12
	0x7fd1a16fb000-0x7fd1a18fa000	/usr/lib/x86_64-linux-gnu/libtdb.so.1.2.12
	0x7fd1a18fa000-0x7fd1a18fb000	/usr/lib/x86_64-linux-gnu/libtdb.so.1.2.12
	0x7fd1a18fb000-0x7fd1a18fc000	/usr/lib/x86_64-linux-gnu/libtdb.so.1.2.12
	0x7fd1a18fc000-0x7fd1a1903000	/usr/lib/x86_64-linux-gnu/libvorbisfile.so.3.3.4
	0x7fd1a1903000-0x7fd1a1b02000	/usr/lib/x86_64-linux-gnu/libvorbisfile.so.3.3.4
	0x7fd1a1b02000-0x7fd1a1b03000	/usr/lib/x86_64-linux-gnu/libvorbisfile.so.3.3.4
	0x7fd1a1b03000-0x7fd1a1b04000	/usr/lib/x86_64-linux-gnu/libvorbisfile.so.3.3.4
	0x7fd1a1b04000-0x7fd1a1b13000	/usr/lib/x86_64-linux-gnu/libcanberra.so.0.2.5
	0x7fd1a1b13000-0x7fd1a1d12000	/usr/lib/x86_64-linux-gnu/libcanberra.so.0.2.5
	0x7fd1a1d12000-0x7fd1a1d13000	/usr/lib/x86_64-linux-gnu/libcanberra.so.0.2.5
	0x7fd1a1d13000-0x7fd1a1d14000	/usr/lib/x86_64-linux-gnu/libcanberra.so.0.2.5
	0x7fd1a1d14000-0x7fd1a1d18000	/usr/lib/x86_64-linux-gnu/libcanberra-gtk.so.0.1.9
	0x7fd1a1d18000-0x7fd1a1f17000	/usr/lib/x86_64-linux-gnu/libcanberra-gtk.so.0.1.9
	0x7fd1a1f17000-0x7fd1a1f18000	/usr/lib/x86_64-linux-gnu/libcanberra-gtk.so.0.1.9
	0x7fd1a1f18000-0x7fd1a1f19000	/usr/lib/x86_64-linux-gnu/libcanberra-gtk.so.0.1.9
	0x7fd1a1f19000-0x7fd1a1f1e000	/usr/lib/x86_64-linux-gnu/gtk-2.0/modules/libcanberra-gtk-module.so
	0x7fd1a1f1e000-0x7fd1a211d000	/usr/lib/x86_64-linux-gnu/gtk-2.0/modules/libcanberra-gtk-module.so
	0x7fd1a211d000-0x7fd1a211e000	/usr/lib/x86_64-linux-gnu/gtk-2.0/modules/libcanberra-gtk-module.so
	0x7fd1a211e000-0x7fd1a211f000	/usr/lib/x86_64-linux-gnu/gtk-2.0/modules/libcanberra-gtk-module.so
	0x7fd1a211f000-0x7fd1a214e000	/usr/lib/x86_64-linux-gnu/gtk-2.0/2.10.0/engines/libmurrine.so
	0x7fd1a214e000-0x7fd1a234e000	/usr/lib/x86_64-linux-gnu/gtk-2.0/2.10.0/engines/libmurrine.so
	0x7fd1a234e000-0x7fd1a234f000	/usr/lib/x86_64-linux-gnu/gtk-2.0/2.10.0/engines/libmurrine.so
	0x7fd1a234f000-0x7fd1a2350000	/usr/lib/x86_64-linux-gnu/gtk-2.0/2.10.0/engines/libmurrine.so
	0x7fd1a2350000-0x7fd1a2360000	/usr/lib/x86_64-linux-gnu/libunity-gtk2-parser.so.0.0.0
	0x7fd1a2360000-0x7fd1a255f000	/usr/lib/x86_64-linux-gnu/libunity-gtk2-parser.so.0.0.0
	0x7fd1a255f000-0x7fd1a2560000	/usr/lib/x86_64-linux-gnu/libunity-gtk2-parser.so.0.0.0
	0x7fd1a2560000-0x7fd1a2561000	/usr/lib/x86_64-linux-gnu/libunity-gtk2-parser.so.0.0.0
	0x7fd1a2561000-0x7fd1a2563000	/var/cache/fontconfig/767a8244fc0220cfb567a839d0392e0b-le64.cache-4
	0x7fd1a2563000-0x7fd1a2564000	/var/cache/fontconfig/4794a0821666d79190d59a36cb4f44b5-le64.cache-4
	0x7fd1a2564000-0x7fd1a2565000	/var/cache/fontconfig/56cf4f4769d0f4abc89a4895d7bd3ae1-le64.cache-4
	0x7fd1a2565000-0x7fd1a2566000	/var/cache/fontconfig/b9d506c9ac06c20b433354fa67a72993-le64.cache-4
	0x7fd1a2566000-0x7fd1a2576000	
	0x7fd1a2576000-0x7fd1a257b000	/usr/lib/x86_64-linux-gnu/gtk-2.0/modules/libunity-gtk-module.so
	0x7fd1a257b000-0x7fd1a277a000	/usr/lib/x86_64-linux-gnu/gtk-2.0/modules/libunity-gtk-module.so
	0x7fd1a277a000-0x7fd1a277b000	/usr/lib/x86_64-linux-gnu/gtk-2.0/modules/libunity-gtk-module.so
	0x7fd1a277b000-0x7fd1a277c000	/usr/lib/x86_64-linux-gnu/gtk-2.0/modules/libunity-gtk-module.so
	0x7fd1a277c000-0x7fd1a278d000	/usr/lib/x86_64-linux-gnu/gtk-2.0/modules/liboverlay-scrollbar.so
	0x7fd1a278d000-0x7fd1a298c000	/usr/lib/x86_64-linux-gnu/gtk-2.0/modules/liboverlay-scrollbar.so
	0x7fd1a298c000-0x7fd1a298d000	/usr/lib/x86_64-linux-gnu/gtk-2.0/modules/liboverlay-scrollbar.so
	0x7fd1a298d000-0x7fd1a298e000	/usr/lib/x86_64-linux-gnu/gtk-2.0/modules/liboverlay-scrollbar.so
	0x7fd1a298e000-0x7fd1a2991000	/usr/lib/x86_64-linux-gnu/gconv/UTF-16.so
	0x7fd1a2991000-0x7fd1a2b90000	/usr/lib/x86_64-linux-gnu/gconv/UTF-16.so
	0x7fd1a2b90000-0x7fd1a2b91000	/usr/lib/x86_64-linux-gnu/gconv/UTF-16.so
	0x7fd1a2b91000-0x7fd1a2b92000	/usr/lib/x86_64-linux-gnu/gconv/UTF-16.so
	0x7fd1a2b92000-0x7fd1a3274000	/usr/lib/locale/locale-archive
	0x7fd1a3274000-0x7fd1a32ec000	
	0x7fd1a32ec000-0x7fd1a32f0000	/lib/x86_64-linux-gnu/libuuid.so.1.3.0
	0x7fd1a32f0000-0x7fd1a34ef000	/lib/x86_64-linux-gnu/libuuid.so.1.3.0
	0x7fd1a34ef000-0x7fd1a34f0000	/lib/x86_64-linux-gnu/libuuid.so.1.3.0
	0x7fd1a34f0000-0x7fd1a34f1000	/lib/x86_64-linux-gnu/libuuid.so.1.3.0
	0x7fd1a34f1000-0x7fd1a34f6000	/usr/lib/x86_64-linux-gnu/libXdmcp.so.6.0.0
	0x7fd1a34f6000-0x7fd1a36f5000	/usr/lib/x86_64-linux-gnu/libXdmcp.so.6.0.0
	0x7fd1a36f5000-0x7fd1a36f6000	/usr/lib/x86_64-linux-gnu/libXdmcp.so.6.0.0
	0x7fd1a36f6000-0x7fd1a36f7000	/usr/lib/x86_64-linux-gnu/libXdmcp.so.6.0.0
	0x7fd1a36f7000-0x7fd1a36f9000	/usr/lib/x86_64-linux-gnu/libXau.so.6.0.0
	0x7fd1a36f9000-0x7fd1a38f9000	/usr/lib/x86_64-linux-gnu/libXau.so.6.0.0
	0x7fd1a38f9000-0x7fd1a38fa000	/usr/lib/x86_64-linux-gnu/libXau.so.6.0.0
	0x7fd1a38fa000-0x7fd1a38fb000	/usr/lib/x86_64-linux-gnu/libXau.so.6.0.0
	0x7fd1a38fb000-0x7fd1a3901000	/usr/lib/x86_64-linux-gnu/libdatrie.so.1.3.1
	0x7fd1a3901000-0x7fd1a3b00000	/usr/lib/x86_64-linux-gnu/libdatrie.so.1.3.1
	0x7fd1a3b00000-0x7fd1a3b01000	/usr/lib/x86_64-linux-gnu/libdatrie.so.1.3.1
	0x7fd1a3b01000-0x7fd1a3b02000	/usr/lib/x86_64-linux-gnu/libdatrie.so.1.3.1
	0x7fd1a3b02000-0x7fd1a3b1c000	/usr/lib/x86_64-linux-gnu/libgraphite2.so.3.0.1
	0x7fd1a3b1c000-0x7fd1a3d1b000	/usr/lib/x86_64-linux-gnu/libgraphite2.so.3.0.1
	0x7fd1a3d1b000-0x7fd1a3d1d000	/usr/lib/x86_64-linux-gnu/libgraphite2.so.3.0.1
	0x7fd1a3d1d000-0x7fd1a3d1e000	/usr/lib/x86_64-linux-gnu/libgraphite2.so.3.0.1
	0x7fd1a3d1e000-0x7fd1a3d35000	/usr/lib/x86_64-linux-gnu/libICE.so.6.3.0
	0x7fd1a3d35000-0x7fd1a3f34000	/usr/lib/x86_64-linux-gnu/libICE.so.6.3.0
	0x7fd1a3f34000-0x7fd1a3f35000	/usr/lib/x86_64-linux-gnu/libICE.so.6.3.0
	0x7fd1a3f35000-0x7fd1a3f36000	/usr/lib/x86_64-linux-gnu/libICE.so.6.3.0
	0x7fd1a3f36000-0x7fd1a3f3a000	
	0x7fd1a3f3a000-0x7fd1a3f41000	/usr/lib/x86_64-linux-gnu/libSM.so.6.0.1
	0x7fd1a3f41000-0x7fd1a4140000	/usr/lib/x86_64-linux-gnu/libSM.so.6.0.1
	0x7fd1a4140000-0x7fd1a4141000	/usr/lib/x86_64-linux-gnu/libSM.so.6.0.1
	0x7fd1a4141000-0x7fd1a4142000	/usr/lib/x86_64-linux-gnu/libSM.so.6.0.1
	0x7fd1a4142000-0x7fd1a415f000	/usr/lib/x86_64-linux-gnu/libxcb.so.1.1.0
	0x7fd1a415f000-0x7fd1a435f000	/usr/lib/x86_64-linux-gnu/libxcb.so.1.1.0
	0x7fd1a435f000-0x7fd1a4360000	/usr/lib/x86_64-linux-gnu/libxcb.so.1.1.0
	0x7fd1a4360000-0x7fd1a4361000	/usr/lib/x86_64-linux-gnu/libxcb.so.1.1.0
	0x7fd1a4361000-0x7fd1a4369000	/usr/lib/x86_64-linux-gnu/libxcb-render.so.0.0.0
	0x7fd1a4369000-0x7fd1a4568000	/usr/lib/x86_64-linux-gnu/libxcb-render.so.0.0.0
	0x7fd1a4568000-0x7fd1a4569000	/usr/lib/x86_64-linux-gnu/libxcb-render.so.0.0.0
	0x7fd1a4569000-0x7fd1a456a000	/usr/lib/x86_64-linux-gnu/libxcb-render.so.0.0.0
	0x7fd1a456a000-0x7fd1a456c000	/usr/lib/x86_64-linux-gnu/libxcb-shm.so.0.0.0
	0x7fd1a456c000-0x7fd1a476b000	/usr/lib/x86_64-linux-gnu/libxcb-shm.so.0.0.0
	0x7fd1a476b000-0x7fd1a476c000	/usr/lib/x86_64-linux-gnu/libxcb-shm.so.0.0.0
	0x7fd1a476c000-0x7fd1a476d000	/usr/lib/x86_64-linux-gnu/libxcb-shm.so.0.0.0
	0x7fd1a476d000-0x7fd1a480e000	/usr/lib/x86_64-linux-gnu/libpixman-1.so.0.30.2
	0x7fd1a480e000-0x7fd1a4a0e000	/usr/lib/x86_64-linux-gnu/libpixman-1.so.0.30.2
	0x7fd1a4a0e000-0x7fd1a4a15000	/usr/lib/x86_64-linux-gnu/libpixman-1.so.0.30.2
	0x7fd1a4a15000-0x7fd1a4a16000	/usr/lib/x86_64-linux-gnu/libpixman-1.so.0.30.2
	0x7fd1a4a16000-0x7fd1a4a1e000	/usr/lib/x86_64-linux-gnu/libthai.so.0.2.0
	0x7fd1a4a1e000-0x7fd1a4c1d000	/usr/lib/x86_64-linux-gnu/libthai.so.0.2.0
	0x7fd1a4c1d000-0x7fd1a4c1e000	/usr/lib/x86_64-linux-gnu/libthai.so.0.2.0
	0x7fd1a4c1e000-0x7fd1a4c1f000	/usr/lib/x86_64-linux-gnu/libthai.so.0.2.0
	0x7fd1a4c1f000-0x7fd1a4c28000	/usr/lib/x86_64-linux-gnu/libXcursor.so.1.0.2
	0x7fd1a4c28000-0x7fd1a4e27000	/usr/lib/x86_64-linux-gnu/libXcursor.so.1.0.2
	0x7fd1a4e27000-0x7fd1a4e28000	/usr/lib/x86_64-linux-gnu/libXcursor.so.1.0.2
	0x7fd1a4e28000-0x7fd1a4e29000	/usr/lib/x86_64-linux-gnu/libXcursor.so.1.0.2
	0x7fd1a4e29000-0x7fd1a4e32000	/usr/lib/x86_64-linux-gnu/libXrandr.so.2.2.0
	0x7fd1a4e32000-0x7fd1a5031000	/usr/lib/x86_64-linux-gnu/libXrandr.so.2.2.0
	0x7fd1a5031000-0x7fd1a5032000	/usr/lib/x86_64-linux-gnu/libXrandr.so.2.2.0
	0x7fd1a5032000-0x7fd1a5033000	/usr/lib/x86_64-linux-gnu/libXrandr.so.2.2.0
	0x7fd1a5033000-0x7fd1a5042000	/usr/lib/x86_64-linux-gnu/libXi.so.6.1.0
	0x7fd1a5042000-0x7fd1a5241000	/usr/lib/x86_64-linux-gnu/libXi.so.6.1.0
	0x7fd1a5241000-0x7fd1a5242000	/usr/lib/x86_64-linux-gnu/libXi.so.6.1.0
	0x7fd1a5242000-0x7fd1a5243000	/usr/lib/x86_64-linux-gnu/libXi.so.6.1.0
	0x7fd1a5243000-0x7fd1a5245000	/usr/lib/x86_64-linux-gnu/libXinerama.so.1.0.0
	0x7fd1a5245000-0x7fd1a5444000	/usr/lib/x86_64-linux-gnu/libXinerama.so.1.0.0
	0x7fd1a5444000-0x7fd1a5445000	/usr/lib/x86_64-linux-gnu/libXinerama.so.1.0.0
	0x7fd1a5445000-0x7fd1a5446000	/usr/lib/x86_64-linux-gnu/libXinerama.so.1.0.0
	0x7fd1a5446000-0x7fd1a5499000	/usr/lib/x86_64-linux-gnu/libharfbuzz.so.0.927.0
	0x7fd1a5499000-0x7fd1a5699000	/usr/lib/x86_64-linux-gnu/libharfbuzz.so.0.927.0
	0x7fd1a5699000-0x7fd1a569a000	/usr/lib/x86_64-linux-gnu/libharfbuzz.so.0.927.0
	0x7fd1a569a000-0x7fd1a569b000	/usr/lib/x86_64-linux-gnu/libharfbuzz.so.0.927.0
	0x7fd1a569b000-0x7fd1a56b2000	/lib/x86_64-linux-gnu/libresolv-2.19.so
	0x7fd1a56b2000-0x7fd1a58b2000	/lib/x86_64-linux-gnu/libresolv-2.19.so
	0x7fd1a58b2000-0x7fd1a58b3000	/lib/x86_64-linux-gnu/libresolv-2.19.so
	0x7fd1a58b3000-0x7fd1a58b4000	/lib/x86_64-linux-gnu/libresolv-2.19.so
	0x7fd1a58b4000-0x7fd1a58b6000	
	0x7fd1a58b6000-0x7fd1a58d6000	/lib/x86_64-linux-gnu/libselinux.so.1
	0x7fd1a58d6000-0x7fd1a5ad5000	/lib/x86_64-linux-gnu/libselinux.so.1
	0x7fd1a5ad5000-0x7fd1a5ad6000	/lib/x86_64-linux-gnu/libselinux.so.1
	0x7fd1a5ad6000-0x7fd1a5ad7000	/lib/x86_64-linux-gnu/libselinux.so.1
	0x7fd1a5ad7000-0x7fd1a5ad9000	
	0x7fd1a5ad9000-0x7fd1a5b16000	/lib/x86_64-linux-gnu/libpcre.so.3.13.1
	0x7fd1a5b16000-0x7fd1a5d15000	/lib/x86_64-linux-gnu/libpcre.so.3.13.1
	0x7fd1a5d15000-0x7fd1a5d16000	/lib/x86_64-linux-gnu/libpcre.so.3.13.1
	0x7fd1a5d16000-0x7fd1a5d17000	/lib/x86_64-linux-gnu/libpcre.so.3.13.1
	0x7fd1a5d17000-0x7fd1a5d1e000	/usr/lib/x86_64-linux-gnu/libffi.so.6.0.1
	0x7fd1a5d1e000-0x7fd1a5f1d000	/usr/lib/x86_64-linux-gnu/libffi.so.6.0.1
	0x7fd1a5f1d000-0x7fd1a5f1e000	/usr/lib/x86_64-linux-gnu/libffi.so.6.0.1
	0x7fd1a5f1e000-0x7fd1a5f1f000	/usr/lib/x86_64-linux-gnu/libffi.so.6.0.1
	0x7fd1a5f1f000-0x7fd1a5f46000	/lib/x86_64-linux-gnu/libexpat.so.1.6.0
	0x7fd1a5f46000-0x7fd1a6146000	/lib/x86_64-linux-gnu/libexpat.so.1.6.0
	0x7fd1a6146000-0x7fd1a6148000	/lib/x86_64-linux-gnu/libexpat.so.1.6.0
	0x7fd1a6148000-0x7fd1a6149000	/lib/x86_64-linux-gnu/libexpat.so.1.6.0
	0x7fd1a6149000-0x7fd1a616e000	/lib/x86_64-linux-gnu/libpng12.so.0.50.0
	0x7fd1a616e000-0x7fd1a636d000	/lib/x86_64-linux-gnu/libpng12.so.0.50.0
	0x7fd1a636d000-0x7fd1a636e000	/lib/x86_64-linux-gnu/libpng12.so.0.50.0
	0x7fd1a636e000-0x7fd1a636f000	/lib/x86_64-linux-gnu/libpng12.so.0.50.0
	0x7fd1a636f000-0x7fd1a6387000	/lib/x86_64-linux-gnu/libz.so.1.2.8
	0x7fd1a6387000-0x7fd1a6586000	/lib/x86_64-linux-gnu/libz.so.1.2.8
	0x7fd1a6586000-0x7fd1a6587000	/lib/x86_64-linux-gnu/libz.so.1.2.8
	0x7fd1a6587000-0x7fd1a6588000	/lib/x86_64-linux-gnu/libz.so.1.2.8
	0x7fd1a6588000-0x7fd1a6589000	/usr/lib/x86_64-linux-gnu/libgthread-2.0.so.0.4002.0
	0x7fd1a6589000-0x7fd1a6788000	/usr/lib/x86_64-linux-gnu/libgthread-2.0.so.0.4002.0
	0x7fd1a6788000-0x7fd1a6789000	/usr/lib/x86_64-linux-gnu/libgthread-2.0.so.0.4002.0
	0x7fd1a6789000-0x7fd1a678a000	/usr/lib/x86_64-linux-gnu/libgthread-2.0.so.0.4002.0
	0x7fd1a678a000-0x7fd1a67e9000	/usr/lib/x86_64-linux-gnu/libXt.so.6.0.0
	0x7fd1a67e9000-0x7fd1a69e9000	/usr/lib/x86_64-linux-gnu/libXt.so.6.0.0
	0x7fd1a69e9000-0x7fd1a69ea000	/usr/lib/x86_64-linux-gnu/libXt.so.6.0.0
	0x7fd1a69ea000-0x7fd1a69ef000	/usr/lib/x86_64-linux-gnu/libXt.so.6.0.0
	0x7fd1a69ef000-0x7fd1a69f0000	
	0x7fd1a69f0000-0x7fd1a6b20000	/usr/lib/x86_64-linux-gnu/libX11.so.6.3.0
	0x7fd1a6b20000-0x7fd1a6d20000	/usr/lib/x86_64-linux-gnu/libX11.so.6.3.0
	0x7fd1a6d20000-0x7fd1a6d21000	/usr/lib/x86_64-linux-gnu/libX11.so.6.3.0
	0x7fd1a6d21000-0x7fd1a6d25000	/usr/lib/x86_64-linux-gnu/libX11.so.6.3.0
	0x7fd1a6d25000-0x7fd1a6d28000	/usr/lib/x86_64-linux-gnu/libgmodule-2.0.so.0.4002.0
	0x7fd1a6d28000-0x7fd1a6f27000	/usr/lib/x86_64-linux-gnu/libgmodule-2.0.so.0.4002.0
	0x7fd1a6f27000-0x7fd1a6f28000	/usr/lib/x86_64-linux-gnu/libgmodule-2.0.so.0.4002.0
	0x7fd1a6f28000-0x7fd1a6f29000	/usr/lib/x86_64-linux-gnu/libgmodule-2.0.so.0.4002.0
	0x7fd1a6f29000-0x7fd1a702f000	/usr/lib/x86_64-linux-gnu/libcairo.so.2.11301.0
	0x7fd1a702f000-0x7fd1a722e000	/usr/lib/x86_64-linux-gnu/libcairo.so.2.11301.0
	0x7fd1a722e000-0x7fd1a7231000	/usr/lib/x86_64-linux-gnu/libcairo.so.2.11301.0
	0x7fd1a7231000-0x7fd1a7232000	/usr/lib/x86_64-linux-gnu/libcairo.so.2.11301.0
	0x7fd1a7232000-0x7fd1a7234000	
	0x7fd1a7234000-0x7fd1a727e000	/usr/lib/x86_64-linux-gnu/libpango-1.0.so.0.3600.3
	0x7fd1a727e000-0x7fd1a747e000	/usr/lib/x86_64-linux-gnu/libpango-1.0.so.0.3600.3
	0x7fd1a747e000-0x7fd1a7480000	/usr/lib/x86_64-linux-gnu/libpango-1.0.so.0.3600.3
	0x7fd1a7480000-0x7fd1a7481000	/usr/lib/x86_64-linux-gnu/libpango-1.0.so.0.3600.3
	0x7fd1a7481000-0x7fd1a748c000	/usr/lib/x86_64-linux-gnu/libpangocairo-1.0.so.0.3600.3
	0x7fd1a748c000-0x7fd1a768c000	/usr/lib/x86_64-linux-gnu/libpangocairo-1.0.so.0.3600.3
	0x7fd1a768c000-0x7fd1a768d000	/usr/lib/x86_64-linux-gnu/libpangocairo-1.0.so.0.3600.3
	0x7fd1a768d000-0x7fd1a768e000	/usr/lib/x86_64-linux-gnu/libpangocairo-1.0.so.0.3600.3
	0x7fd1a768e000-0x7fd1a76ad000	/usr/lib/x86_64-linux-gnu/libgdk_pixbuf-2.0.so.0.3000.7
	0x7fd1a76ad000-0x7fd1a78ad000	/usr/lib/x86_64-linux-gnu/libgdk_pixbuf-2.0.so.0.3000.7
	0x7fd1a78ad000-0x7fd1a78ae000	/usr/lib/x86_64-linux-gnu/libgdk_pixbuf-2.0.so.0.3000.7
	0x7fd1a78ae000-0x7fd1a78af000	/usr/lib/x86_64-linux-gnu/libgdk_pixbuf-2.0.so.0.3000.7
	0x7fd1a78af000-0x7fd1a795c000	/usr/lib/x86_64-linux-gnu/libgdk-x11-2.0.so.0.2400.23
	0x7fd1a795c000-0x7fd1a7b5b000	/usr/lib/x86_64-linux-gnu/libgdk-x11-2.0.so.0.2400.23
	0x7fd1a7b5b000-0x7fd1a7b5f000	/usr/lib/x86_64-linux-gnu/libgdk-x11-2.0.so.0.2400.23
	0x7fd1a7b5f000-0x7fd1a7b61000	/usr/lib/x86_64-linux-gnu/libgdk-x11-2.0.so.0.2400.23
	0x7fd1a7b61000-0x7fd1a7b75000	/usr/lib/x86_64-linux-gnu/libpangoft2-1.0.so.0.3600.3
	0x7fd1a7b75000-0x7fd1a7d74000	/usr/lib/x86_64-linux-gnu/libpangoft2-1.0.so.0.3600.3
	0x7fd1a7d74000-0x7fd1a7d75000	/usr/lib/x86_64-linux-gnu/libpangoft2-1.0.so.0.3600.3
	0x7fd1a7d75000-0x7fd1a7d76000	/usr/lib/x86_64-linux-gnu/libpangoft2-1.0.so.0.3600.3
	0x7fd1a7d76000-0x7fd1a7ee2000	/usr/lib/x86_64-linux-gnu/libgio-2.0.so.0.4002.0
	0x7fd1a7ee2000-0x7fd1a80e1000	/usr/lib/x86_64-linux-gnu/libgio-2.0.so.0.4002.0
	0x7fd1a80e1000-0x7fd1a80e5000	/usr/lib/x86_64-linux-gnu/libgio-2.0.so.0.4002.0
	0x7fd1a80e5000-0x7fd1a80e7000	/usr/lib/x86_64-linux-gnu/libgio-2.0.so.0.4002.0
	0x7fd1a80e7000-0x7fd1a80e9000	
	0x7fd1a80e9000-0x7fd1a8108000	/usr/lib/x86_64-linux-gnu/libatk-1.0.so.0.21009.1
	0x7fd1a8108000-0x7fd1a8308000	/usr/lib/x86_64-linux-gnu/libatk-1.0.so.0.21009.1
	0x7fd1a8308000-0x7fd1a830a000	/usr/lib/x86_64-linux-gnu/libatk-1.0.so.0.21009.1
	0x7fd1a830a000-0x7fd1a830b000	/usr/lib/x86_64-linux-gnu/libatk-1.0.so.0.21009.1
	0x7fd1a830b000-0x7fd1a873a000	/usr/lib/x86_64-linux-gnu/libgtk-x11-2.0.so.0.2400.23
	0x7fd1a873a000-0x7fd1a8939000	/usr/lib/x86_64-linux-gnu/libgtk-x11-2.0.so.0.2400.23
	0x7fd1a8939000-0x7fd1a8940000	/usr/lib/x86_64-linux-gnu/libgtk-x11-2.0.so.0.2400.23
	0x7fd1a8940000-0x7fd1a8944000	/usr/lib/x86_64-linux-gnu/libgtk-x11-2.0.so.0.2400.23
	0x7fd1a8944000-0x7fd1a8947000	
	0x7fd1a8947000-0x7fd1a8a4d000	/lib/x86_64-linux-gnu/libglib-2.0.so.0.4002.0
	0x7fd1a8a4d000-0x7fd1a8c4c000	/lib/x86_64-linux-gnu/libglib-2.0.so.0.4002.0
	0x7fd1a8c4c000-0x7fd1a8c4d000	/lib/x86_64-linux-gnu/libglib-2.0.so.0.4002.0
	0x7fd1a8c4d000-0x7fd1a8c4e000	/lib/x86_64-linux-gnu/libglib-2.0.so.0.4002.0
	0x7fd1a8c4e000-0x7fd1a8c4f000	
	0x7fd1a8c4f000-0x7fd1a8c9e000	/usr/lib/x86_64-linux-gnu/libgobject-2.0.so.0.4002.0
	0x7fd1a8c9e000-0x7fd1a8e9e000	/usr/lib/x86_64-linux-gnu/libgobject-2.0.so.0.4002.0
	0x7fd1a8e9e000-0x7fd1a8e9f000	/usr/lib/x86_64-linux-gnu/libgobject-2.0.so.0.4002.0
	0x7fd1a8e9f000-0x7fd1a8ea0000	/usr/lib/x86_64-linux-gnu/libgobject-2.0.so.0.4002.0
	0x7fd1a8ea0000-0x7fd1a8ee4000	/lib/x86_64-linux-gnu/libdbus-1.so.3.7.6
	0x7fd1a8ee4000-0x7fd1a90e3000	/lib/x86_64-linux-gnu/libdbus-1.so.3.7.6
	0x7fd1a90e3000-0x7fd1a90e4000	/lib/x86_64-linux-gnu/libdbus-1.so.3.7.6
	0x7fd1a90e4000-0x7fd1a90e5000	/lib/x86_64-linux-gnu/libdbus-1.so.3.7.6
	0x7fd1a90e5000-0x7fd1a910a000	/usr/lib/x86_64-linux-gnu/libdbus-glib-1.so.2.2.2
	0x7fd1a910a000-0x7fd1a930a000	/usr/lib/x86_64-linux-gnu/libdbus-glib-1.so.2.2.2
	0x7fd1a930a000-0x7fd1a930b000	/usr/lib/x86_64-linux-gnu/libdbus-glib-1.so.2.2.2
	0x7fd1a930b000-0x7fd1a930c000	/usr/lib/x86_64-linux-gnu/libdbus-glib-1.so.2.2.2
	0x7fd1a930c000-0x7fd1a93f5000	/usr/lib/x86_64-linux-gnu/libasound.so.2.0.0
	0x7fd1a93f5000-0x7fd1a95f4000	/usr/lib/x86_64-linux-gnu/libasound.so.2.0.0
	0x7fd1a95f4000-0x7fd1a95fb000	/usr/lib/x86_64-linux-gnu/libasound.so.2.0.0
	0x7fd1a95fb000-0x7fd1a95fc000	/usr/lib/x86_64-linux-gnu/libasound.so.2.0.0
	0x7fd1a95fc000-0x7fd1a95fe000	/usr/lib/x86_64-linux-gnu/libXcomposite.so.1.0.0
	0x7fd1a95fe000-0x7fd1a97fd000	/usr/lib/x86_64-linux-gnu/libXcomposite.so.1.0.0
	0x7fd1a97fd000-0x7fd1a97fe000	/usr/lib/x86_64-linux-gnu/libXcomposite.so.1.0.0
	0x7fd1a97fe000-0x7fd1a97ff000	/usr/lib/x86_64-linux-gnu/libXcomposite.so.1.0.0
	0x7fd1a97ff000-0x7fd1a9804000	/usr/lib/x86_64-linux-gnu/libXfixes.so.3.1.0
	0x7fd1a9804000-0x7fd1a9a03000	/usr/lib/x86_64-linux-gnu/libXfixes.so.3.1.0
	0x7fd1a9a03000-0x7fd1a9a04000	/usr/lib/x86_64-linux-gnu/libXfixes.so.3.1.0
	0x7fd1a9a04000-0x7fd1a9a05000	/usr/lib/x86_64-linux-gnu/libXfixes.so.3.1.0
	0x7fd1a9a05000-0x7fd1a9a07000	/usr/lib/x86_64-linux-gnu/libXdamage.so.1.1.0
	0x7fd1a9a07000-0x7fd1a9c06000	/usr/lib/x86_64-linux-gnu/libXdamage.so.1.1.0
	0x7fd1a9c06000-0x7fd1a9c07000	/usr/lib/x86_64-linux-gnu/libXdamage.so.1.1.0
	0x7fd1a9c07000-0x7fd1a9c08000	/usr/lib/x86_64-linux-gnu/libXdamage.so.1.1.0
	0x7fd1a9c08000-0x7fd1a9c19000	/usr/lib/x86_64-linux-gnu/libXext.so.6.4.0
	0x7fd1a9c19000-0x7fd1a9e18000	/usr/lib/x86_64-linux-gnu/libXext.so.6.4.0
	0x7fd1a9e18000-0x7fd1a9e19000	/usr/lib/x86_64-linux-gnu/libXext.so.6.4.0
	0x7fd1a9e19000-0x7fd1a9e1a000	/usr/lib/x86_64-linux-gnu/libXext.so.6.4.0
	0x7fd1a9e1a000-0x7fd1a9e23000	/usr/lib/x86_64-linux-gnu/libXrender.so.1.3.0
	0x7fd1a9e23000-0x7fd1aa022000	/usr/lib/x86_64-linux-gnu/libXrender.so.1.3.0
	0x7fd1aa022000-0x7fd1aa023000	/usr/lib/x86_64-linux-gnu/libXrender.so.1.3.0
	0x7fd1aa023000-0x7fd1aa024000	/usr/lib/x86_64-linux-gnu/libXrender.so.1.3.0
	0x7fd1aa024000-0x7fd1aa05e000	/usr/lib/x86_64-linux-gnu/libfontconfig.so.1.8.0
	0x7fd1aa05e000-0x7fd1aa25d000	/usr/lib/x86_64-linux-gnu/libfontconfig.so.1.8.0
	0x7fd1aa25d000-0x7fd1aa25f000	/usr/lib/x86_64-linux-gnu/libfontconfig.so.1.8.0
	0x7fd1aa25f000-0x7fd1aa260000	/usr/lib/x86_64-linux-gnu/libfontconfig.so.1.8.0
	0x7fd1aa260000-0x7fd1aa2fc000	/usr/lib/x86_64-linux-gnu/libfreetype.so.6.11.1
	0x7fd1aa2fc000-0x7fd1aa4fb000	/usr/lib/x86_64-linux-gnu/libfreetype.so.6.11.1
	0x7fd1aa4fb000-0x7fd1aa501000	/usr/lib/x86_64-linux-gnu/libfreetype.so.6.11.1
	0x7fd1aa501000-0x7fd1aa502000	/usr/lib/x86_64-linux-gnu/libfreetype.so.6.11.1
	0x7fd1aa502000-0x7fd1b682e000	/home/user/Desktop/lab/firefox/libxul.so
	0x7fd1b682e000-0x7fd1b6a2d000	/home/user/Desktop/lab/firefox/libxul.so
	0x7fd1b6a2d000-0x7fd1b771f000	/home/user/Desktop/lab/firefox/libxul.so
	0x7fd1b771f000-0x7fd1b78b7000	
	0x7fd1b78b7000-0x7fd1b78c0000	/home/user/Desktop/lab/firefox/libmozalloc.so
	0x7fd1b78c0000-0x7fd1b7abf000	/home/user/Desktop/lab/firefox/libmozalloc.so
	0x7fd1b7abf000-0x7fd1b7ac0000	/home/user/Desktop/lab/firefox/libmozalloc.so
	0x7fd1b7ac0000-0x7fd1b7df2000	/home/user/Desktop/lab/firefox/libmozsqlite3.so
	0x7fd1b7df2000-0x7fd1b7ff2000	/home/user/Desktop/lab/firefox/libmozsqlite3.so
	0x7fd1b7ff2000-0x7fd1b8007000	/home/user/Desktop/lab/firefox/libmozsqlite3.so
	0x7fd1b8007000-0x7fd1b80aa000	/home/user/Desktop/lab/firefox/libssl3.so
	0x7fd1b80aa000-0x7fd1b82aa000	/home/user/Desktop/lab/firefox/libssl3.so
	0x7fd1b82aa000-0x7fd1b82b4000	/home/user/Desktop/lab/firefox/libssl3.so
	0x7fd1b82b4000-0x7fd1b82b5000	
	0x7fd1b82b5000-0x7fd1b8308000	/home/user/Desktop/lab/firefox/libsmime3.so
	0x7fd1b8308000-0x7fd1b8508000	/home/user/Desktop/lab/firefox/libsmime3.so
	0x7fd1b8508000-0x7fd1b8510000	/home/user/Desktop/lab/firefox/libsmime3.so
	0x7fd1b8510000-0x7fd1b890f000	/home/user/Desktop/lab/firefox/libnss3.so
	0x7fd1b890f000-0x7fd1b8b0f000	/home/user/Desktop/lab/firefox/libnss3.so
	0x7fd1b8b0f000-0x7fd1b8b37000	/home/user/Desktop/lab/firefox/libnss3.so
	0x7fd1b8b37000-0x7fd1b8b3a000	
	0x7fd1b8b3a000-0x7fd1b8ba2000	/home/user/Desktop/lab/firefox/libnssutil3.so
	0x7fd1b8ba2000-0x7fd1b8da1000	/home/user/Desktop/lab/firefox/libnssutil3.so
	0x7fd1b8da1000-0x7fd1b8db6000	/home/user/Desktop/lab/firefox/libnssutil3.so
	0x7fd1b8db6000-0x7fd1b8db7000	
	0x7fd1b8db7000-0x7fd1b8dbc000	/home/user/Desktop/lab/firefox/libplds4.so
	0x7fd1b8dbc000-0x7fd1b8fbc000	/home/user/Desktop/lab/firefox/libplds4.so
	0x7fd1b8fbc000-0x7fd1b8fbd000	/home/user/Desktop/lab/firefox/libplds4.so
	0x7fd1b8fbd000-0x7fd1b8fc5000	/home/user/Desktop/lab/firefox/libplc4.so
	0x7fd1b8fc5000-0x7fd1b91c4000	/home/user/Desktop/lab/firefox/libplc4.so
	0x7fd1b91c4000-0x7fd1b91c5000	/home/user/Desktop/lab/firefox/libplc4.so
	0x7fd1b91c5000-0x7fd1b9247000	/home/user/Desktop/lab/firefox/libnspr4.so
	0x7fd1b9247000-0x7fd1b9447000	/home/user/Desktop/lab/firefox/libnspr4.so
	0x7fd1b9447000-0x7fd1b9451000	/home/user/Desktop/lab/firefox/libnspr4.so
	0x7fd1b9451000-0x7fd1bb75b000	
	0x7fd1bb75b000-0x7fd1bb916000	/lib/x86_64-linux-gnu/libc-2.19.so
	0x7fd1bb916000-0x7fd1bbb16000	/lib/x86_64-linux-gnu/libc-2.19.so
	0x7fd1bbb16000-0x7fd1bbb1a000	/lib/x86_64-linux-gnu/libc-2.19.so
	0x7fd1bbb1a000-0x7fd1bbb1c000	/lib/x86_64-linux-gnu/libc-2.19.so
	0x7fd1bbb1c000-0x7fd1bbb21000	
	0x7fd1bbb21000-0x7fd1bbb37000	/lib/x86_64-linux-gnu/libgcc_s.so.1
	0x7fd1bbb37000-0x7fd1bbd36000	/lib/x86_64-linux-gnu/libgcc_s.so.1
	0x7fd1bbd36000-0x7fd1bbd37000	/lib/x86_64-linux-gnu/libgcc_s.so.1
	0x7fd1bbd37000-0x7fd1bbe1d000	/usr/lib/x86_64-linux-gnu/libstdc++.so.6.0.19
	0x7fd1bbe1d000-0x7fd1bc01c000	/usr/lib/x86_64-linux-gnu/libstdc++.so.6.0.19
	0x7fd1bc01c000-0x7fd1bc024000	/usr/lib/x86_64-linux-gnu/libstdc++.so.6.0.19
	0x7fd1bc024000-0x7fd1bc026000	/usr/lib/x86_64-linux-gnu/libstdc++.so.6.0.19
	0x7fd1bc026000-0x7fd1bc03b000	
	0x7fd1bc03b000-0x7fd1bc140000	/lib/x86_64-linux-gnu/libm-2.19.so
	0x7fd1bc140000-0x7fd1bc33f000	/lib/x86_64-linux-gnu/libm-2.19.so
	0x7fd1bc33f000-0x7fd1bc340000	/lib/x86_64-linux-gnu/libm-2.19.so
	0x7fd1bc340000-0x7fd1bc341000	/lib/x86_64-linux-gnu/libm-2.19.so
	0x7fd1bc341000-0x7fd1bc344000	/lib/x86_64-linux-gnu/libdl-2.19.so
	0x7fd1bc344000-0x7fd1bc543000	/lib/x86_64-linux-gnu/libdl-2.19.so
	0x7fd1bc543000-0x7fd1bc544000	/lib/x86_64-linux-gnu/libdl-2.19.so
	0x7fd1bc544000-0x7fd1bc545000	/lib/x86_64-linux-gnu/libdl-2.19.so
	0x7fd1bc545000-0x7fd1bc54c000	/lib/x86_64-linux-gnu/librt-2.19.so
	0x7fd1bc54c000-0x7fd1bc74b000	/lib/x86_64-linux-gnu/librt-2.19.so
	0x7fd1bc74b000-0x7fd1bc74c000	/lib/x86_64-linux-gnu/librt-2.19.so
	0x7fd1bc74c000-0x7fd1bc74d000	/lib/x86_64-linux-gnu/librt-2.19.so
	0x7fd1bc74d000-0x7fd1bc766000	/lib/x86_64-linux-gnu/libpthread-2.19.so
	0x7fd1bc766000-0x7fd1bc965000	/lib/x86_64-linux-gnu/libpthread-2.19.so
	0x7fd1bc965000-0x7fd1bc966000	/lib/x86_64-linux-gnu/libpthread-2.19.so
	0x7fd1bc966000-0x7fd1bc967000	/lib/x86_64-linux-gnu/libpthread-2.19.so
	0x7fd1bc967000-0x7fd1bc96b000	
	0x7fd1bc96b000-0x7fd1bc98e000	/lib/x86_64-linux-gnu/ld-2.19.so
	0x7fd1bc98e000-0x7fd1bcb36000	
	0x7fd1bcb36000-0x7fd1bcb37000	/var/cache/fontconfig/0c9eb80ebd1c36541ebe2852d3bb0c49-le64.cache-4
	0x7fd1bcb37000-0x7fd1bcb3a000	/var/cache/fontconfig/e13b20fdb08344e0e664864cc2ede53d-le64.cache-4
	0x7fd1bcb3a000-0x7fd1bcb3e000	/var/cache/fontconfig/7ef2298fde41cc6eeb7af42e48b7d293-le64.cache-4
	0x7fd1bcb3e000-0x7fd1bcb40000	
	0x7fd1bcb40000-0x7fd1bcb44000	/home/user/.config/dconf/user
	0x7fd1bcb44000-0x7fd1bcb4b000	/usr/lib/x86_64-linux-gnu/gconv/gconv-modules.cache
	0x7fd1bcb4b000-0x7fd1bcb77000	
	0x7fd1bcb77000-0x7fd1bcb7d000	
	0x7fd1bcb7d000-0x7fd1bcb7e000	/run/user/1000/dconf/user
	0x7fd1bcb7e000-0x7fd1bcb8d000	
	0x7fd1bcb8d000-0x7fd1bcb8e000	/lib/x86_64-linux-gnu/ld-2.19.so
	0x7fd1bcb8e000-0x7fd1bcb8f000	/lib/x86_64-linux-gnu/ld-2.19.so
	0x7fd1bcb8f000-0x7fd1bcb90000	
	0x7fff6f6c7000-0x7fff6f72e000	[stack]
	0x7fff6f7fc000-0x7fff6f7fe000	[vdso]
	0x7fff6f7fe000-0x7fff6f800000	[vvar]
	0xffffffffff600000-0xffffffffff601000	[vsyscall]
==3960==End of process memory map.
==3960==AddressSanitizer CHECK failed: /builds/slave/moz-toolchain/src/llvm/projects/compiler-rt/lib/sanitizer_common/sanitizer_posix.cc:68 ""((""unable to mmap"" && 0)) != (0)"" (0x0, 0x0)
    #0 0x4783ab (/home/user/Desktop/lab/firefox/firefox+0x4783ab)
    #1 0x47e961 (/home/user/Desktop/lab/firefox/firefox+0x47e961)
    #2 0x48293e (/home/user/Desktop/lab/firefox/firefox+0x48293e)
    #3 0x43c0d8 (/home/user/Desktop/lab/firefox/firefox+0x43c0d8)
    #4 0x437f91 (/home/user/Desktop/lab/firefox/firefox+0x437f91)
    #5 0x43894e (/home/user/Desktop/lab/firefox/firefox+0x43894e)
    #6 0x471f33 (/home/user/Desktop/lab/firefox/firefox+0x471f33)
    #7 0x7fd1b44e0725 (/home/user/Desktop/lab/firefox/libxul.so+0x9fde725)
    #8 0x7fd1b3a7062c (/home/user/Desktop/lab/firefox/libxul.so+0x956e62c)
    #9 0x7fd1b3809899 (/home/user/Desktop/lab/firefox/libxul.so+0x9307899)
    #10 0x7fd1b37eeb4e (/home/user/Desktop/lab/firefox/libxul.so+0x92ecb4e)
    #11 0x7fd1b37ccd64 (/home/user/Desktop/lab/firefox/libxul.so+0x92cad64)
    #12 0x7fd1b380a93f (/home/user/Desktop/lab/firefox/libxul.so+0x930893f)
    #13 0x7fd1b380aef7 (/home/user/Desktop/lab/firefox/libxul.so+0x9308ef7)
    #14 0x7fd1b423a6d3 (/home/user/Desktop/lab/firefox/libxul.so+0x9d386d3)
    #15 0x7fd1b423adee (/home/user/Desktop/lab/firefox/libxul.so+0x9d38dee)
    #16 0x7fd1adb0b049 (/home/user/Desktop/lab/firefox/libxul.so+0x3609049)
    #17 0x7fd1adb0bffb (/home/user/Desktop/lab/firefox/libxul.so+0x3609ffb)
    #18 0x7fd1adb84204 (/home/user/Desktop/lab/firefox/libxul.so+0x3682204)
    #19 0x7fd1adb819ce (/home/user/Desktop/lab/firefox/libxul.so+0x367f9ce)
    #20 0x7fd1adb7bc68 (/home/user/Desktop/lab/firefox/libxul.so+0x3679c68)
    #21 0x7fd1adb77946 (/home/user/Desktop/lab/firefox/libxul.so+0x3675946)
    #22 0x7fd1ad036654 (/home/user/Desktop/lab/firefox/libxul.so+0x2b34654)
    #23 0x7fd1ad034ac0 (/home/user/Desktop/lab/firefox/libxul.so+0x2b32ac0)
    #24 0x7fd1ad03bc6b (/home/user/Desktop/lab/firefox/libxul.so+0x2b39c6b)
    #25 0x7fd1ab9442a4 (/home/user/Desktop/lab/firefox/libxul.so+0x14422a4)
    #26 0x7fd1ab9a372b (/home/user/Desktop/lab/firefox/libxul.so+0x14a172b)
    #27 0x7fd1ac1cb3c9 (/home/user/Desktop/lab/firefox/libxul.so+0x1cc93c9)
    #28 0x7fd1ac176b9c (/home/user/Desktop/lab/firefox/libxul.so+0x1c74b9c)
    #29 0x7fd1b0819a57 (/home/user/Desktop/lab/firefox/libxul.so+0x6317a57)
    #30 0x7fd1b2234878 (/home/user/Desktop/lab/firefox/libxul.so+0x7d32878)
    #31 0x7fd1b2324e76 (/home/user/Desktop/lab/firefox/libxul.so+0x7e22e76)
    #32 0x7fd1b2325eb1 (/home/user/Desktop/lab/firefox/libxul.so+0x7e23eb1)
    #33 0x7fd1b2326dc5 (/home/user/Desktop/lab/firefox/libxul.so+0x7e24dc5)
    #34 0x48a2fa (/home/user/Desktop/lab/firefox/firefox+0x48a2fa)
    #35 0x7fd1bb77cec4 (/lib/x86_64-linux-gnu/libc.so.6+0x21ec4)
    #36 0x48975c (/home/user/Desktop/lab/firefox/firefox+0x48975c)

";

			Assert.IsTrue(Asan.CheckForAsanFault(example));
			var data = Asan.AsanToMonitorData(null, example);

			Assert.AreEqual("Failed to allocate 0x41417000 (1094807552) bytes of LargeMmapAllocator: 12", data.Title);
			Assert.AreEqual("Out of Memory", data.Fault.Risk);
			Assert.AreEqual(example, data.Fault.Description);
			Assert.AreEqual("F0C8A045", data.Fault.MajorHash);
			Assert.AreEqual("75A7437C", data.Fault.MinorHash);
		}
		[Test]
		public void TestAsanOOMGccRegex()
		{
			const string example = @"==36536== ERROR: AddressSanitizer failed to allocate 0x100002000 (4294975488) bytes of LargeMmapAllocator: Cannot allocate memory
==36536== Process memory map follows:
        0x000000400000-0x000000401000   /home/mike/asan/oom
        0x000000600000-0x000000601000   /home/mike/asan/oom
        0x000000601000-0x000000602000   /home/mike/asan/oom
        0x00007fff7000-0x00008fff7000
        0x00008fff7000-0x02008fff7000
        0x02008fff7000-0x10007fff8000
        0x600000000000-0x610000000000
        0x610000000000-0x610000005000
        0x7efd5761e000-0x7eff57622000
        0x7eff57622000-0x7eff57638000   /lib/x86_64-linux-gnu/libgcc_s.so.1
        0x7eff57638000-0x7eff57837000   /lib/x86_64-linux-gnu/libgcc_s.so.1
        0x7eff57837000-0x7eff57838000   /lib/x86_64-linux-gnu/libgcc_s.so.1
        0x7eff57838000-0x7eff5783b000   /lib/x86_64-linux-gnu/libdl-2.19.so
        0x7eff5783b000-0x7eff57a3a000   /lib/x86_64-linux-gnu/libdl-2.19.so
        0x7eff57a3a000-0x7eff57a3b000   /lib/x86_64-linux-gnu/libdl-2.19.so
        0x7eff57a3b000-0x7eff57a3c000   /lib/x86_64-linux-gnu/libdl-2.19.so
        0x7eff57a3c000-0x7eff57a55000   /lib/x86_64-linux-gnu/libpthread-2.19.so
        0x7eff57a55000-0x7eff57c54000   /lib/x86_64-linux-gnu/libpthread-2.19.so
        0x7eff57c54000-0x7eff57c55000   /lib/x86_64-linux-gnu/libpthread-2.19.so
        0x7eff57c55000-0x7eff57c56000   /lib/x86_64-linux-gnu/libpthread-2.19.so
        0x7eff57c56000-0x7eff57c5a000
        0x7eff57c5a000-0x7eff57e15000   /lib/x86_64-linux-gnu/libc-2.19.so
        0x7eff57e15000-0x7eff58015000   /lib/x86_64-linux-gnu/libc-2.19.so
        0x7eff58015000-0x7eff58019000   /lib/x86_64-linux-gnu/libc-2.19.so
        0x7eff58019000-0x7eff5801b000   /lib/x86_64-linux-gnu/libc-2.19.so
        0x7eff5801b000-0x7eff58020000
        0x7eff58020000-0x7eff58048000   /usr/lib/x86_64-linux-gnu/libasan.so.0.0.0
        0x7eff58048000-0x7eff58248000   /usr/lib/x86_64-linux-gnu/libasan.so.0.0.0
        0x7eff58248000-0x7eff58249000   /usr/lib/x86_64-linux-gnu/libasan.so.0.0.0
        0x7eff58249000-0x7eff5824a000   /usr/lib/x86_64-linux-gnu/libasan.so.0.0.0
        0x7eff5824a000-0x7eff5afaf000
        0x7eff5afaf000-0x7eff5afd2000   /lib/x86_64-linux-gnu/ld-2.19.so
        0x7eff5b1aa000-0x7eff5b1be000
        0x7eff5b1c7000-0x7eff5b1c9000
        0x7eff5b1cb000-0x7eff5b1d1000
        0x7eff5b1d1000-0x7eff5b1d2000   /lib/x86_64-linux-gnu/ld-2.19.so
        0x7eff5b1d2000-0x7eff5b1d3000   /lib/x86_64-linux-gnu/ld-2.19.so
        0x7eff5b1d3000-0x7eff5b1d4000
        0x7fffa8133000-0x7fffa8154000   [stack]
        0x7fffa81fe000-0x7fffa8200000   [vdso]
        0xffffffffff600000-0xffffffffff601000   [vsyscall]
==36536== End of process memory map.
==36536== AddressSanitizer CHECK failed: ../../../../src/libsanitizer/sanitizer_common/sanitizer_posix.cc:70 ""((""unable to mmap"" && 0)) != (0)"" (0x0, 0x0)
    #0 0x7eff5803231d (/usr/lib/x86_64-linux-gnu/libasan.so.0.0.0+0x1231d)
    #1 0x7eff58039133 (/usr/lib/x86_64-linux-gnu/libasan.so.0.0.0+0x19133)
    #2 0x7eff5803b6d3 (/usr/lib/x86_64-linux-gnu/libasan.so.0.0.0+0x1b6d3)
    #3 0x7eff58029078 (/usr/lib/x86_64-linux-gnu/libasan.so.0.0.0+0x9078)
    #4 0x7eff58035442 (/usr/lib/x86_64-linux-gnu/libasan.so.0.0.0+0x15442)
    #5 0x4006fe (/home/mike/asan/oom+0x4006fe)
    #6 0x7eff57c7bec4 (/lib/x86_64-linux-gnu/libc-2.19.so+0x21ec4)
    #7 0x400628 (/home/mike/asan/oom+0x400628)
";

			Assert.IsTrue(Asan.CheckForAsanFault(example));
			var data = Asan.AsanToMonitorData(null, example);

			Assert.AreEqual("Failed to allocate 0x100002000 (4294975488) bytes of LargeMmapAllocator: Cannot allocate memory", data.Title);
			Assert.AreEqual("Out of Memory", data.Fault.Risk);
			Assert.AreEqual(example, data.Fault.Description);
			Assert.AreEqual("F0C8A045", data.Fault.MajorHash);
			Assert.AreEqual("9FBE9B75", data.Fault.MinorHash);
		}
	}
}