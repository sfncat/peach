using System;
using Peach.Core;

namespace Peach.Pro.Core.License
{
	public enum EulaType
	{
		Academic,
		Developer,
		Enterprise,
		Flex,
		Professional,
		Trial,
	}

	public enum LicenseStatus
	{
		Missing,
		Expired,
		Invalid,
		Valid
	}

	public class PitFeature
	{
		public string Path { get; set; }
		public string Name { get; set; }
		public byte[] Key { get; set; }
		public bool IsCustom { get; set; }
		public bool IsValid { get; set; }
	}

	public interface ILicense : IDisposable
	{
		LicenseStatus Status { get; }
		string ErrorText { get; }
		DateTime Expiration { get; }

		bool EulaAccepted { get; set; }
		string EulaText { get; }
		EulaType Eula { get; }

		PitFeature CanUsePit(string path);
		bool CanUseMonitor(string name);

		IJobLicense NewJob(string pit, string config, string job);
	}

	public interface IJobLicense : IDisposable
	{
		bool CanExecuteTestCase();
	}

	public static class LicenseExtensions
	{
		public static string ExpirationWarning(this ILicense license)
		{
			return "Warning: Peach expires in {0} days".Fmt(license.ExpirationInDays());
		}

		public static int ExpirationInDays(this ILicense license)
		{
			return (license.Expiration - DateTime.Now).Days;
		}

		public static bool IsNearingExpiration(this ILicense license)
		{
			return license.IsValid() && license.Expiration < DateTime.Now.AddDays(30);
		}

		public static bool IsValid(this ILicense license)
		{
			return license.Status == LicenseStatus.Valid;
		}
	}
}
