using System;
using System.Configuration;
using System.IO;
using Peach.Core;

namespace Peach.Pro.Core.License
{
    public class NullLicense : ILicense
    {
        public LicenseStatus Status => LicenseStatus.Valid;
        public string ErrorText => string.Empty;
        public DateTime Expiration => DateTime.MaxValue;

        public bool EulaAccepted { get { return true; } set {} }
        public string EulaText => "";
        public EulaType Eula => EulaType.Developer;

        private JobLicense _jobLicense;

        public NullLicense()
        {
            _jobLicense = new JobLicense();
        }

        public PitFeature CanUsePit(string path)
        {
                var pitName = string.Join("/",
				Path.GetFileName(Path.GetDirectoryName(path)),
				Path.GetFileName(path)
			);

			return new PitFeature
			{
				Name = pitName,
				Path = path,
				IsValid = true
			};
        }
        public bool CanUseMonitor(string name)
        {
            return true;
        }

        public IJobLicense NewJob(string pit, string config, string job)
        {
            return _jobLicense;
        }

        public void Dispose()
	    {
	        // nothing to do
	    }

        class JobLicense : IJobLicense
		{
			public bool CanExecuteTestCase()
			{
				return true;
			}

			public void Dispose()
			{
			}
		}

    }
}
