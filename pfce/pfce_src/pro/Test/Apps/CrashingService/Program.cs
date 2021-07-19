using System;
using System.ServiceProcess;

namespace CrashingService
{
	internal static class Program
	{
		private static int Main(string[] args)
		{
			if (args.Length == 1)
			{
				if (args[0] == "install")
					return Installer.Install();

				if (args[0] == "uninstall")
					return Installer.Uninstall();

				Console.WriteLine("{0} <install|uninstall>", AppDomain.CurrentDomain.FriendlyName);
				return -1;
			}

			var ServicesToRun = new ServiceBase[] 
			{ 
				new CrashingService() 
			};

			ServiceBase.Run(ServicesToRun);

			return 0;
		}
	}
}
