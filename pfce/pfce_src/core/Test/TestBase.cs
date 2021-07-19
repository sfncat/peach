using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Sockets;
using Peach.Core;

// This assembly contains Peach plugins
[assembly: PluginAssembly]

namespace Peach.Core.Test
{
	public class PeachAttribute : CategoryAttribute { }
	public class QuickAttribute : CategoryAttribute { }
	public class SlowAttribute : CategoryAttribute { }

	public class SetUpFixture
	{
		public static ushort MakePort(ushort min, ushort max)
		{
			var pid = System.Diagnostics.Process.GetCurrentProcess().Id;
			var seed = Environment.TickCount * pid;
			var rng = new Random((uint)seed);

			while (true)
			{
				var ret = (ushort)rng.Next(min, max);

				using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
				{
					try
					{
						s.Bind(new IPEndPoint(IPAddress.Any, ret));
					}
					catch
					{
						continue;
					}
				}

				using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
				{
					try
					{
						s.Bind(new IPEndPoint(IPAddress.Any, ret));
					}
					catch
					{
						continue;
					}
				}

				return ret;
			}
		}

		class AssertTestFail : TraceListener
		{
			public override void Write(string message)
			{
				Assert.Fail(message);
			}

			public override void WriteLine(string message)
			{
				var sb = new StringBuilder();

				sb.AppendLine("Assertion " + message);
				sb.AppendLine(new StackTrace(2, true).ToString());

				Assert.Fail(sb.ToString());
			}
		}

		protected void DoSetUp()
		{
			Debug.Listeners.Insert(0, new AssertTestFail());

			var logLevel = 0;

			var peachDebug = Environment.GetEnvironmentVariable("PEACH_DEBUG");
			if (peachDebug == "1")
				logLevel = 1;

			var peachTrace = Environment.GetEnvironmentVariable("PEACH_TRACE");
			if (peachTrace == "1")
				logLevel = 2;

			Utilities.ConfigureLogging(logLevel);
		}

		public static void EnableDebug()
		{
			var config = LogManager.Configuration ?? new LoggingConfiguration();
			var target = new ColoredConsoleTarget 
			{ 
				Layout = "${time} ${logger} ${message} ${exception:format=tostring}" 
			};
			var rule = new LoggingRule("*", LogLevel.Debug, target);
			
			config.AddTarget("debugConsole", target);
			config.LoggingRules.Add(rule);

			LogManager.Configuration = config;
		}

		public static void EnableTrace()
		{
			var config = LogManager.Configuration ?? new LoggingConfiguration();
			var target = new ConsoleTarget { Layout = "${time} ${logger} ${message} ${exception:format=tostring}" };
			var rule = new LoggingRule("*", LogLevel.Trace, target);

			config.AddTarget("debugConsole", target);
			config.LoggingRules.Add(rule);

			LogManager.Configuration = config;
		}

		protected void DoTearDown()
		{
			LogManager.Flush();
			LogManager.Configuration = null;
		}
	}

	public abstract class TestFixture
	{
		readonly Assembly _asm;

		protected TestFixture(Assembly asm) { _asm = asm; }

		protected void DoAssertWorks()
		{
#if DEBUG
			Assert.Throws<AssertionException>(() => Debug.Assert(false));
#else
			Debug.Assert(false);
#endif
		}

		protected void DoNoMissingAttributes()
		{
			var missing = new List<string>();

			foreach (var type in _asm.GetTypes())
			{
				if (!type.GetAttributes<TestFixtureAttribute>().Any())
					continue;

				foreach (var attr in type.GetCustomAttributes(true))
				{
					if (attr is QuickAttribute || attr is SlowAttribute)
						goto Found;
				}

				missing.Add(type.FullName);

			Found:
				{ }
			}

			Assert.That(missing, Is.Empty);
		}
	}

	[SetUpFixture]
	internal class TestBase : SetUpFixture
	{
		[OneTimeSetUp]
		public void SetUp()
		{
			DoSetUp();

			ClassLoader.Initialize();
		}

		[OneTimeTearDown]
		public void TearDown()
		{
			DoTearDown();
		}
	}

	[TestFixture]
	[Quick]
	internal class CommonTests : TestFixture
	{
		public CommonTests()
			: base(Assembly.GetExecutingAssembly())
		{
		}

		[Test]
		public void AssertWorks()
		{
			DoAssertWorks();
		}

		[Test]
		public void NoMissingAttributes()
		{
			DoNoMissingAttributes();
		}
	}
}
