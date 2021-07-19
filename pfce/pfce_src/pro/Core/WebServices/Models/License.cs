using System;
using Peach.Pro.Core.License;

namespace Peach.Pro.Core.WebServices.Models
{
	public class License
	{
		public LicenseStatus Status { get; set; }

		/// <summary>
		/// Human readable error for why license is not valid.
		/// </summary>
		public string ErrorText { get; set; }

		/// <summary>
		/// When the license expires.
		/// </summary>
		public DateTime Expiration { get; set; }

		/// <summary>
		/// Has the eula been accepted.
		/// </summary>
		public bool EulaAccepted { get; set; }

		/// <summary>
		/// Gets or sets the EULA.
		/// </summary>
		public EulaType Eula { get; set; }

		public string Version { get; set; }
	}
}
