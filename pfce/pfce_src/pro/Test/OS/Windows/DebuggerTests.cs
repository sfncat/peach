using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core.OS.Windows.Debugger;
using Peach.Pro.OS.Windows.Agent.Monitors;

namespace Peach.Pro.Test.OS.Windows
{
	[TestFixture]
	[Quick]
	[Platform("Win")]
	internal class DebuggerTests
	{
		IDebuggerInstance _dbg;

		[SetUp]
		public void SetUp()
		{
			_dbg = new DebugEngineInstance
			{
				WinDbgPath = WindowsKernelDebugger.FindWinDbg(null),
				SymbolsPath = DebugEngineInstance.DefaultSymbolPath
			};
		}

		[TearDown]
		public void TearDown()
		{
			_dbg.Dispose();
			_dbg = null;
		}

		[Test]
		public void TestBadCommand()
		{
			Assert.Throws<PeachException>(() => _dbg.StartProcess("foo.exe"));
		}
	}
}
