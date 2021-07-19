namespace Peach.Pro.Core.OS.Windows.Debugger
{
	public class DebugEngineInstance : SystemDebuggerInstance
	{
		public const string DefaultSymbolPath = "SRV*http://msdl.microsoft.com/download/symbols";

		public override string Name
		{
			get { return "WindowsDebugEngine"; }
		}

		protected override IDebugger OnStartProcess(string commandLine)
		{
			return DebugEngine.CreateProcess(WinDbgPath, SymbolsPath, commandLine);
		}

		protected override IDebugger OnAttachProcess(int pid)
		{
			return DebugEngine.AttachToProcess(WinDbgPath, SymbolsPath, pid);
		}
	}
}
