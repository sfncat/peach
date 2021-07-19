using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Test;
using Peach.Pro.OS.OSX.Agent.Monitors;

namespace Peach.Pro.Test.OS.OSX.Agent.Monitors
{
	[TestFixture]
	[Quick]
	[Peach]
	[Platform("MacOSX")]
	public class CrashWranglerTest
	{
		const string ExecHandler = "/usr/local/bin/exc_handler";
		
		[Test]
		public void BadHandler()
		{
			var args = new Dictionary<string, string>
			{
				{ "Executable", "foo" },
				{ "ExecHandler", "foo" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			const string expected = "CrashWrangler could not start handler \"foo\" - No such file or directory.";
			Assert.Throws<PeachException>(w.SessionStarting, expected);
		}

		[Test]
		public void BadCommand()
		{
			var args = new Dictionary<string, string>
			{
				{ "ExecHandler", ExecHandler },
				{ "Executable", "foo" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			const string expected = "CrashWrangler handler could not find command \"foo\".";
			Assert.Throws<PeachException>(w.SessionStarting, expected);
		}
		
		[Test]
		public void TestNoFault()
		{
			var args = new Dictionary<string, string>
			{
				{ "ExecHandler", ExecHandler },
				{ "Executable", "echo" },
				{ "Arguments", "hello" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			w.SessionStarting();
			w.IterationStarting(null);
			Thread.Sleep(1000);
			w.IterationFinished();
			Assert.IsFalse(w.DetectedFault());
			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestStopping()
		{
			var args = new Dictionary<string, string>
			{
				{ "ExecHandler", ExecHandler },
				{ "Executable", "nc" },
				{ "Arguments", "-l 12345" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			w.SessionStarting();
			w.IterationStarting(null);
			Thread.Sleep(1000);
			w.IterationFinished();
			Assert.IsFalse(w.DetectedFault());
			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestStartOnCall()
		{
			var args = new Dictionary<string, string>
			{
				{ "ExecHandler", ExecHandler },
				{ "Executable", "nc" },
				{ "Arguments", "-l 12345" },
				{ "StartOnCall", "foo" },
				{ "WaitForExitTimeout", "2000" },
				{ "NoCpuKill", "true" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);

			w.Message("foo");
			Thread.Sleep(1000);

			var before = DateTime.Now;
			w.IterationFinished();
			var after = DateTime.Now;

			var span = (after - before);

			Assert.IsFalse(w.DetectedFault());

			w.SessionFinished();
			w.StopMonitor();

			Assert.GreaterOrEqual(span.TotalSeconds, 1.8);
			Assert.LessOrEqual(span.TotalSeconds, 2.2);
		}

		[Test]
		public void TestCpuKill()
		{
			var args = new Dictionary<string, string>
			{
				{ "ExecHandler", ExecHandler },
				{ "Executable", "nc" },
				{ "Arguments", "-l 12345" },
				{ "StartOnCall", "foo" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);

			w.Message("foo");
			Thread.Sleep(1000);

			var before = DateTime.Now;
			w.IterationFinished();
			var after = DateTime.Now;

			var span = (after - before);

			Assert.IsFalse(w.DetectedFault());

			w.SessionFinished();
			w.StopMonitor();

			Assert.GreaterOrEqual(span.TotalSeconds, 0.0);
			Assert.LessOrEqual(span.TotalSeconds, 0.5);
		}

		[Test]
		public void TestExitOnCallNoFault()
		{
			var args = new Dictionary<string, string>
			{
				{ "ExecHandler", ExecHandler },
				{ "Executable", "echo" },
				{ "Arguments", "hello" },
				{ "StartOnCall", "foo" },
				{ "WaitForExitOnCall", "bar" },
				{ "NoCpuKill", "true" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);

			w.Message("foo");
			w.Message("bar");

			w.IterationFinished();

			Assert.IsFalse(w.DetectedFault());

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestExitOnCallFault()
		{
			var args = new Dictionary<string, string>
			{
				{ "ExecHandler", ExecHandler },
				{ "Executable", "nc" },
				{ "Arguments", "-l 12345" },
				{ "StartOnCall", "foo" },
				{ "WaitForExitOnCall", "bar" },
				{ "WaitForExitTimeout", "2000" },
				{ "NoCpuKill", "true" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);

			w.Message("foo");
			w.Message("bar");

			w.IterationFinished();

			Assert.IsTrue(w.DetectedFault());
			var f = w.GetMonitorData();
			Assert.NotNull(f);
			Assert.NotNull(f.Fault);
			Assert.AreEqual(Monitor2.Hash("CrashWranglernc"), f.Fault.MajorHash);
			Assert.AreEqual(Monitor2.Hash("FailedToExit"), f.Fault.MinorHash);

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestExitTime()
		{
			var args = new Dictionary<string, string>
			{
				{ "ExecHandler", ExecHandler },
				{ "Executable", "nc" },
				{ "Arguments", "-l 12345" },
				{ "RestartOnEachTest", "true" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			w.SessionStarting();
			w.IterationStarting(null);

			var before = DateTime.Now;
			w.IterationFinished();
			var after = DateTime.Now;

			var span = (after - before);

			Assert.IsFalse(w.DetectedFault());

			w.SessionFinished();
			w.StopMonitor();

			Assert.GreaterOrEqual(span.TotalSeconds, 0.0);
			Assert.LessOrEqual(span.TotalSeconds, 0.25);
		}

		[Test]
		public void TestExitEarlyFault()
		{
			var args = new Dictionary<string, string>
			{
				{ "ExecHandler", ExecHandler },
				{ "Executable", "echo" },
				{ "Arguments", "hello" },
				{ "FaultOnEarlyExit", "true" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			w.IterationStarting(null);

			Thread.Sleep(1000);

			w.IterationFinished();

			Assert.IsTrue(w.DetectedFault());
			var f = w.GetMonitorData();
			Assert.NotNull(f);
			Assert.NotNull(f.Fault);
			Assert.AreEqual(Monitor2.Hash("CrashWranglerecho"), f.Fault.MajorHash);
			Assert.AreEqual(Monitor2.Hash("ExitedEarly"), f.Fault.MinorHash);

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestExitEarlyFault1()
		{
			// FaultOnEarlyExit doesn't fault when stop message is sent

			var args = new Dictionary<string, string>
			{
				{ "ExecHandler", ExecHandler },
				{ "Executable", "echo" },
				{ "Arguments", "hello" },
				{ "StartOnCall", "foo" },
				{ "WaitForExitOnCall", "bar" },
				{ "FaultOnEarlyExit", "true" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			w.SessionStarting();
			w.IterationStarting(null);

			w.Message("foo");
			w.Message("bar");

			w.IterationFinished();

			Assert.IsFalse(w.DetectedFault());

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestExitEarlyFault2()
		{
			// FaultOnEarlyExit faults when StartOnCall is used and stop message is not sent

			var args = new Dictionary<string, string>
			{
				{ "ExecHandler", ExecHandler },
				{ "Executable", "echo" },
				{ "Arguments", "hello" },
				{ "StartOnCall", "foo" },
				{ "FaultOnEarlyExit", "true" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			w.SessionStarting();
			w.IterationStarting(null);

			w.Message("foo");

			Thread.Sleep(1000);

			w.IterationFinished();

			Assert.IsTrue(w.DetectedFault());
			var f = w.GetMonitorData();
			Assert.NotNull(f);
			Assert.NotNull(f.Fault);
			Assert.AreEqual(Monitor2.Hash("CrashWranglerecho"), f.Fault.MajorHash);
			Assert.AreEqual(Monitor2.Hash("ExitedEarly"), f.Fault.MinorHash);

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestExitEarlyFault3()
		{
			// FaultOnEarlyExit doesn't fault when StartOnCall is used

			var args = new Dictionary<string, string>
			{
				{ "ExecHandler", ExecHandler },
				{ "Executable", "nc" },
				{ "Arguments", "-l 12345" },
				{ "StartOnCall", "foo" },
				{ "FaultOnEarlyExit", "true" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			w.SessionStarting();
			w.IterationStarting(null);

			w.Message("foo");

			w.IterationFinished();

			Assert.IsFalse(w.DetectedFault());

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestExitEarlyFault4()
		{
			// FaultOnEarlyExit doesn't fault when restart every iteration is true

			var args = new Dictionary<string, string>
			{
				{ "ExecHandler", ExecHandler },
				{ "Executable", "nc" },
				{ "Arguments", "-l 12345" },
				{ "RestartOnEachTest", "true" },
				{ "FaultOnEarlyExit", "true" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			w.SessionStarting();
			w.IterationStarting(null);

			w.IterationFinished();

			Assert.IsFalse(w.DetectedFault());

			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestGetData()
		{
			var args = new Dictionary<string, string>
			{
				{ "ExecHandler", ExecHandler },
				{ "Executable", Utilities.GetAppResourcePath("CrashingProgram") },
			};

			Environment.SetEnvironmentVariable("PEACH", "qwertyuiopasdfghjklzxcvbnmqwertyuio");

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			w.SessionStarting();
			w.IterationStarting(null);
			Thread.Sleep(1000);
			w.IterationFinished();
			Assert.IsTrue(w.DetectedFault());
			var fault = w.GetMonitorData();
			Assert.NotNull(fault);
			Assert.NotNull(fault.Fault);
			Assert.False(string.IsNullOrEmpty(fault.Fault.Description));
			Assert.AreEqual(0, fault.Data.Count);
			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestCommandQuoting()
		{
			var args = new Dictionary<string, string>
			{
				{ "ExecHandler", ExecHandler },
				{ "Executable", "/Applications/QuickTime Player.app/Contents/MacOS/QuickTime Player" },
				{ "Arguments", "" },
				{ "RestartOnEachTest", "true" },
				{ "FaultOnEarlyExit", "true" },
			};

			var w = new CrashWrangler(null);
			w.StartMonitor(args);
			w.SessionStarting();
			Thread.Sleep(1000);

			Assert.IsFalse(w.DetectedFault());
			w.SessionFinished();
			w.StopMonitor();
		}

		[Test]
		public void TestRestartAfterFault()
		{
			var startCount = 0;
			var iteration = 0;

			var runner = new MonitorRunner("CrashWrangler", new Dictionary<string, string>
			{
				{ "ExecHandler", ExecHandler },
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

		const string cw_log = @"exception=EXC_CRASH:signal=6:is_exploitable=yes:instruction_disassembly=jae     CONSTANT:instruction_address=0x00007fff87e13d46:access_type=:access_address=0x0000000000000000:
The crash is suspected to be an exploitable issue due to the suspicious function in the stack trace of the crashing thread: ' __stack_chk_fail '
Test case was (null)



Process:         CrashingProgram [77800]
Path:            /Volumes/Data/path/to/CrashingProgram
Identifier:      CrashingProgram
Version:         ???
Code Type:       X86-64 (Native)
Parent Process:  exc_handler [77799]
User ID:         502

Date/Time:       2015-08-11 14:01:15.768 -0700
OS Version:      Mac OS X 10.8.5 (12F2542)
Report Version:  10

Crashed Thread:  0  Dispatch queue: com.apple.main-thread

Exception Type:  EXC_CRASH (SIGABRT)
Exception Codes: 0x0000000000000000, 0x0000000000000000

Application Specific Information:
[77800] stack overflow

Thread 0 Crashed:: Dispatch queue: com.apple.main-thread
0   libsystem_kernel.dylib              0x00007fff87e13d46 __kill + 10
1   libsystem_c.dylib                   0x00007fff89e28053 __abort + 193
2   libsystem_c.dylib                   0x00007fff89e28f97 __stack_chk_fail + 195
3   CrashingProgram                     0x0000000100000d32 Foo() + 306
4   ???                                 0x6975797472657771 0 + 7599113487299999601

Thread 0 crashed with X86 Thread State (64-bit):
  rax: 0x0000000000000000  rbx: 0x00007fff5fbff990  rcx: 0x00007fff5fbff978  rdx: 0x0000000000000000
  rdi: 0x0000000000012fe8  rsi: 0x0000000000000006  rbp: 0x00007fff5fbff9a0  rsp: 0x00007fff5fbff978
   r8: 0x0000000000000000   r9: 0x0000000000000000  r10: 0x00007fff87e15342  r11: 0x0000000000000206
  r12: 0x0000000000000000  r13: 0x0000000000000000  r14: 0x0000000000000000  r15: 0x0000000000000000
  rip: 0x00007fff87e13d46  rfl: 0x0000000000000206  cr2: 0x00007fff72024ff0
Logical CPU: 0

Binary Images:
       0x100000000 -        0x100000ff7 +CrashingProgram (???) <488A922A-A230-36E6-AE18-E8C4FD0E4BB6> /Volumes/Data/path/to/CrashingProgram
    0x7fff6724a000 -     0x7fff6727e93f  dyld (210.2.3) <6900F2BA-DB48-3B78-B668-58FC0CF6BCB8> /usr/lib/dyld
    0x7fff80d21000 -     0x7fff80d29ff7  libsystem_dnssd.dylib (379.38.1) <BDCB8566-0189-34C0-9634-35ABD3EFE25B> /usr/lib/system/libsystem_dnssd.dylib
    0x7fff80d32000 -     0x7fff80d37fff  libcompiler_rt.dylib (30) <08F8731D-5961-39F1-AD00-4590321D24A9> /usr/lib/system/libcompiler_rt.dylib
    0x7fff80fba000 -     0x7fff810d292f  libobjc.A.dylib (532.2) <90D31928-F48D-3E37-874F-220A51FD9E37> /usr/lib/libobjc.A.dylib
    0x7fff81196000 -     0x7fff81197ff7  libdnsinfo.dylib (453.19) <14202FFB-C3CA-3FCC-94B0-14611BF8692D> /usr/lib/system/libdnsinfo.dylib
    0x7fff816f0000 -     0x7fff816f6fff  libmacho.dylib (829) <BF332AD9-E89F-387E-92A4-6E1AB74BD4D9> /usr/lib/system/libmacho.dylib
    0x7fff817f2000 -     0x7fff81814ff7  libxpc.dylib (140.43) <70BC645B-6952-3264-930C-C835010CCEF9> /usr/lib/system/libxpc.dylib
    0x7fff81879000 -     0x7fff818affff  libsystem_info.dylib (406.17) <4FFCA242-7F04-365F-87A6-D4EFB89503C1> /usr/lib/system/libsystem_info.dylib
    0x7fff81a3c000 -     0x7fff81a3dff7  libsystem_sandbox.dylib (220.4) <E2A3D8A9-80A3-3666-8D8B-D22829C2B0EC> /usr/lib/system/libsystem_sandbox.dylib
    0x7fff82967000 -     0x7fff8298cff7  libc++abi.dylib (26) <D86169F3-9F31-377A-9AF3-DB17142052E4> /usr/lib/libc++abi.dylib
    0x7fff847dc000 -     0x7fff847e3ff7  libcopyfile.dylib (89.0.70) <30824A67-6743-3D99-8DC3-92578FA9D7CB> /usr/lib/system/libcopyfile.dylib
    0x7fff84ed9000 -     0x7fff84edfff7  libunwind.dylib (35.1) <21703D36-2DAB-3D8B-8442-EAAB23C060D3> /usr/lib/system/libunwind.dylib
    0x7fff86851000 -     0x7fff8689dff7  libauto.dylib (185.4) <AD5A4CE7-CB53-313C-9FAE-673303CC2D35> /usr/lib/libauto.dylib
    0x7fff869cf000 -     0x7fff869ddfff  libcommonCrypto.dylib (60027) <BAAFE0C9-BB86-3CA7-88C0-E3CBA98DA06F> /usr/lib/system/libcommonCrypto.dylib
    0x7fff873f4000 -     0x7fff873fcfff  liblaunch.dylib (442.26.2) <2F71CAF8-6524-329E-AC56-C506658B4C0C> /usr/lib/system/liblaunch.dylib
    0x7fff87a4e000 -     0x7fff87a51ff7  libdyld.dylib (210.2.3) <F59367C9-C110-382B-A695-9035A6DD387E> /usr/lib/system/libdyld.dylib
    0x7fff87a65000 -     0x7fff87a67ff7  libunc.dylib (25) <92805328-CD36-34FF-9436-571AB0485072> /usr/lib/system/libunc.dylib
    0x7fff87e02000 -     0x7fff87e1dff7  libsystem_kernel.dylib (2050.48.19) <81945B94-D6CB-3B77-9E95-0429540B0DF0> /usr/lib/system/libsystem_kernel.dylib
    0x7fff88281000 -     0x7fff882afff7  libsystem_m.dylib (3022.6) <B434BE5C-25AB-3EBD-BAA7-5304B34E3441> /usr/lib/system/libsystem_m.dylib
    0x7fff8924a000 -     0x7fff8924afff  libkeymgr.dylib (25) <CC9E3394-BE16-397F-926B-E579B60EE429> /usr/lib/system/libkeymgr.dylib
    0x7fff89289000 -     0x7fff892f1ff7  libc++.1.dylib (65.1) <20E31B90-19B9-3C2A-A9EB-474E08F9FE05> /usr/lib/libc++.1.dylib
    0x7fff89dce000 -     0x7fff89e9aff7  libsystem_c.dylib (825.40.1) <543B05AE-CFA5-3EFE-8E58-77225411BA6B> /usr/lib/system/libsystem_c.dylib
    0x7fff8a03b000 -     0x7fff8a046fff  libsystem_notify.dylib (98.6) <1E490CB2-9311-3B36-8372-37D3FB0FD818> /usr/lib/system/libsystem_notify.dylib
    0x7fff8a252000 -     0x7fff8a254fff  libquarantine.dylib (52.1) <143B726E-DF47-37A8-90AA-F059CFD1A2E4> /usr/lib/system/libquarantine.dylib
    0x7fff8a2b7000 -     0x7fff8a2c5ff7  libsystem_network.dylib (77.10) <0D99F24E-56FE-380F-B81B-4A4C630EE587> /usr/lib/system/libsystem_network.dylib
    0x7fff8a65f000 -     0x7fff8a660fff  libDiagnosticMessagesClient.dylib (8) <8548E0DC-0D2F-30B6-B045-FE8A038E76D8> /usr/lib/libDiagnosticMessagesClient.dylib
    0x7fff8b8cd000 -     0x7fff8b8cefff  libsystem_blocks.dylib (59) <D92DCBC3-541C-37BD-AADE-ACC75A0C59C8> /usr/lib/system/libsystem_blocks.dylib
    0x7fff8b9d4000 -     0x7fff8b9d5ff7  libSystem.B.dylib (169.3) <FF25248A-574C-32DB-952F-B948C389B2A4> /usr/lib/libSystem.B.dylib
    0x7fff8bf71000 -     0x7fff8bfdafff  libstdc++.6.dylib (56) <EAA2B53E-EADE-39CF-A0EF-FB9D4940672A> /usr/lib/libstdc++.6.dylib
    0x7fff8bfed000 -     0x7fff8c03cff7  libcorecrypto.dylib (106.2) <CE0C29A3-C420-339B-ADAA-52F4683233CC> /usr/lib/system/libcorecrypto.dylib
    0x7fff8c45c000 -     0x7fff8c45dff7  libremovefile.dylib (23.2) <6763BC8E-18B8-3AD9-8FFA-B43713A7264F> /usr/lib/system/libremovefile.dylib
    0x7fff8c469000 -     0x7fff8c47eff7  libdispatch.dylib (228.23) <D26996BF-FC57-39EB-8829-F63585561E09> /usr/lib/system/libdispatch.dylib
    0x7fff8c4eb000 -     0x7fff8c4f0fff  libcache.dylib (57) <65187C6E-3FBF-3EB8-A1AA-389445E2984D> /usr/lib/system/libcache.dylib
";

		[Test]
		public void TestBucketing()
		{
			var s = new CrashWrangler.Summary(cw_log);

			Assert.NotNull(s);

		}
	}
}
