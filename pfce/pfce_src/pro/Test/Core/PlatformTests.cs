using System;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
	public class PlatformTests
	{
		[Test]
		public void TestDetectOS()
		{
			var expected = Environment.GetEnvironmentVariable("PEACH_OS");
			if (string.IsNullOrEmpty(expected))
				Assert.Ignore("Environment variable missing: PEACH_OS");
			Assert.AreEqual(expected, Platform.GetOS().ToString());
		}

		[Test]
		public void TestDetectArch()
		{
			var expected = Environment.GetEnvironmentVariable("PEACH_ARCH");
			if (string.IsNullOrEmpty(expected))
				Assert.Ignore("Environment variable missing: PEACH_ARCH");
			Assert.AreEqual(expected, Platform.GetArch().ToString());
		}
	}
}
