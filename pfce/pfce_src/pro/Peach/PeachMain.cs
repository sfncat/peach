using System;
using Peach.Pro.Core.Runtime;
using Peach.Pro.WebApi2;

namespace Peach
{
	/// <summary>
	/// Command line interface for Peach 3.
	/// Mostly backwards compatable with Peach 2.3.
	/// </summary>
	public class PeachMain
	{
		static int Main(string[] args)
		{
			try
			{
				//System.Diagnostics.Debugger.Launch();

				using (var program = new ConsoleProgram
				{
					CreateWeb = (license, pitLibraryPath, jobMonitor) =>
						new WebServer(license, pitLibraryPath, jobMonitor)
				})
				{
					return program.Run(args);
				}
			}
			catch (Exception)
			{
				if (System.Diagnostics.Debugger.IsAttached)
					System.Diagnostics.Debugger.Break();
				throw;
			}
			finally
			{
				if (System.Diagnostics.Debugger.IsAttached)
					System.Diagnostics.Debugger.Break();
			}
		}
	}
}