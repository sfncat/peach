using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Reflection;
using System.ServiceProcess;

namespace CrashingService
{
	// NOTE: Tell msvs this is not a 'Component'
	[DesignerCategory("Code")]
	[RunInstaller(true)]
	public class Installer : System.Configuration.Install.Installer
	{
		public static string FullFileName
		{
			get
			{
				return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);
			}
		}

		public static string FileName
		{
			get
			{
				return Path.GetFileName(Assembly.GetExecutingAssembly().Location);
			}
		}

		public static int Uninstall()
		{
			return Run(new[] { "/uninstall", FullFileName });
		}

		public static int Install()
		{
			return Run(new[] { FullFileName });
		}

		private static int Run(string[] args)
		{
			try
			{
				ManagedInstallerClass.InstallHelper(args);
				return 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return -1;
			}
		}

		public Installer()
		{
			Installers.Add(new ServiceProcessInstaller
			{
				Account = ServiceAccount.NetworkService
			});

			Installers.Add(new ServiceInstaller
			{
				StartType = ServiceStartMode.Manual,
				ServiceName = "CrashingService"
			});
		}
	}
}
