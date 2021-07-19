using System.Linq;
using System.Reflection;
using System.Web.Http;
using Peach.Pro.Core.License;
using Peach.Pro.WebApi2.Utility;
using LicenseModel = Peach.Pro.Core.WebServices.Models.License;

namespace Peach.Pro.WebApi2.Controllers
{
	[NoCache]
	[RoutePrefix(Prefix)]
	public class LicenseController : ApiController
	{
		public const string Prefix = "p/license";

		ILicense _license;

		public LicenseController(ILicense license)
		{
			_license = license;
		}

		/// <summary>
		/// Gets information about the current peach license.
		/// </summary>
		/// <returns></returns>
		[Route("")]
		public LicenseModel Get()
		{
			return new LicenseModel
			{
				Status = _license.Status,
				ErrorText = _license.ErrorText,
				Expiration = _license.Expiration,
				Eula = _license.Eula,
				EulaAccepted = _license.EulaAccepted,
				Version = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
			};
		}

		/// <summary>
		/// Accepts the end user licensing agreement.
		/// </summary>
		/// <returns></returns>
		[Route("")]
		public LicenseModel Post()
		{
			_license.EulaAccepted = true;

			return Get();
		}
	}
}
