using System.Collections.Generic;
using Peach.Core.Runtime;
using Peach.Pro.Core.License;
using Peach.Pro.Core.WebServices;

namespace Peach.Pro.Core.Runtime
{
	public class Service : BaseProgram
	{
		string _pitLibraryPath;

		protected override void AddCustomOptions(OptionSet options)
		{
			options.Add(
				"pits=",
				"Pit Library Path",
				v => _pitLibraryPath = v
			);
			options.Add(
				"webport=",
				"Specifies port web interface runs on.",
				(int v) => _webPort = v
			);
		}

		protected override int OnRun(List<string> args)
		{
			PrepareLicensing(_pitLibraryPath, false);

			if (_license.Status != LicenseStatus.Valid)
				return -1;

			// Ensure pit library exists
			var pits = FindPitLibrary(_pitLibraryPath);

			if (CreateWeb != null)
				RunWeb(pits, false, new ExternalJobMonitor());

			return 0;
		}
	}
}
