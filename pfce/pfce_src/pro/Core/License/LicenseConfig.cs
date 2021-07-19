using System;
using System.Configuration;
using System.IO;
using Peach.Core;

namespace Peach.Pro.Core.License
{
	public interface ILicenseConfig
	{
		string ActivationId { get; set; }
		string LicenseUrl { get; set; }
		string LicensePath { get; set; }
		PitManifest Manifest { get; set; }

		/// <summary>
		/// Throws an exception if the license config is invalid.
		/// </summary>
		void Validate();
	}

	class LicenseConfig : ILicenseConfig
	{
		const string ConfigFilename = "Peach.license.config";

		static class Keys
		{
			public const string LicenseUrl = "licenseUrl";
			public const string LicensePath = "licensePath";
			public const string ActivationId = "activationId";
		}

		public string ActivationId
		{
			get { return GetConfig(Keys.ActivationId); }
			set { SetConfig(Keys.ActivationId, value); }
		}

		public PitManifest Manifest { get; set; }

		public void Validate()
		{
			if (LicenseUrl == "LOCAL_LICENSE_SERVER_URL")
			{
				var msg = @"
Your product license requires a local license server to operate.
You can find more information about setting up a local license server in the
Peach User Guide under section 4.3.2: Local License Server (Offline Synchronization).
Once your organization has a local license server
installed and running (this may require the assistance of an IT administrator)
the Peach license configuration must be updated.

Follow these steps to update your local configuration. 
To update the local configuration you will need the local license server URL.
An example of a local license server URL is ""http://192.168.1.2:7070/request"".

1. Edit the file ""Peach.license.config"" located in your Peach install folder.
2. Replace LOCAL_LICENSE_SERVER_URL with the local license server URL.
3. Restart Peach.

Example of an updated file is:

<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <appSettings>
    <add key=""licenseUrl"" value=""http://192.168.1.2:7070/request"" />
    <add key=""activationId"" value=""0000-0000-0000-0000-0000-0000-0000-0000"" />
  </appSettings>
</configuration> 
";
				throw new ApplicationException(msg);
			}
		}

		public string LicenseUrl
		{
			get { return GetConfig(Keys.LicenseUrl); }
			set { SetConfig(Keys.LicenseUrl, value); }
		}

		public string LicensePath
		{
			get
			{
				var path = GetConfig(Keys.LicensePath);
				if (path != null)
					return path;

			    // Unix: $HOME/.config/Peach
				path = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
					"Peach"
				);

				if (!Directory.Exists(path))
					Directory.CreateDirectory(path);

				return path;
			}
			set
			{
				SetConfig(Keys.LicensePath, value);
			}
		}

		public byte[] IdentityData
		{
			get { return IdentityClient.IdentityData; }
		}

		public bool DetectConfig
		{
			get { return Utilities.DetectConfig(ConfigFilename); }
		}

		string GetConfig(string name)
		{
			return Utilities.OpenConfig(ConfigFilename).AppSettings.Settings.Get(name);
		}

		void SetConfig(string name, string value)
		{
			var config = Utilities.OpenConfig(ConfigFilename);
			var settings = config.AppSettings.Settings;
			settings.Set(name, value);
			config.Save(ConfigurationSaveMode.Modified);
		}
	}
}