using System;
using NLog;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core.Runtime;

namespace Peach.Pro.PitTester
{
	[SetUpFixture]
	public class TestBase
	{
		[OneTimeSetUp]
		public void SetUp()
		{
			var logLevel = 0;

			var peachDebug = Environment.GetEnvironmentVariable("PEACH_DEBUG");
			if (peachDebug == "1")
				logLevel = 1;

			var peachTrace = Environment.GetEnvironmentVariable("PEACH_TRACE");
			if (peachTrace == "1")
				logLevel = 2;

			Utilities.ConfigureLogging(logLevel);

			BaseProgram.Initialize();
		}

		[OneTimeTearDown]
		public void TearDown()
		{
			LogManager.Flush();
		}
	}
}
