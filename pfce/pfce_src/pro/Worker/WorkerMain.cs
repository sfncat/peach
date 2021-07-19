using Peach.Pro.Core.Runtime;

namespace PeachWorker
{
	public class WorkerMain
	{
		static int Main(string[] args)
		{
			//System.Diagnostics.Debugger.Launch();

			using (var worker = new Worker())
			{
				return worker.Run(args);
			}
		}
	}
}