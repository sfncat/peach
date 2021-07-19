using System.Diagnostics;
using System.Security.Principal;
using Peach.Core;
using SysProcess = System.Diagnostics.Process;

namespace Peach.Pro.WebApi2.Utility
{
	internal static class UacHelpers
	{
		private static readonly string user;

		static UacHelpers()
		{
			var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
			var account = sid.Translate(typeof(NTAccount)) as NTAccount;

			user = account != null ? account.Value : "Everyone";
			
		}

		public static bool AddUrl(string url)
		{
			try
			{
				using (var p = new SysProcess())
				{
					p.StartInfo = new ProcessStartInfo
					{
						Verb = "runas",
						FileName = Command,
						Arguments = GetArguments(url)
					};

					p.Start();
					p.WaitForExit();

					// 0 == success, 1 == already registered
					return p.ExitCode == 0 || p.ExitCode == 1;
				}
			}
			catch
			{
				return false;
			}
		}

		public static readonly string Command = "netsh";

		public static string GetArguments(string url)
		{
			return "http add urlacl url=\"{0}\" user=\"{1}\"".Fmt(url, user);
		}
	}

}
