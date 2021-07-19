using System.Collections.Generic;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.WebApi2.Utility;
using Swashbuckle.Swagger.Annotations;

namespace Peach.Pro.WebApi2.Controllers
{
	[NoCache]
	[RestrictedApi]
	[RoutePrefix(Prefix)]
	public class LibrariesController : ApiController
	{
		public const string Prefix = "p/libraries";

		private IPitDatabase _pitDatabase;

		public LibrariesController(IPitDatabase pitDatabase)
		{
			_pitDatabase = pitDatabase;
		}

		/// <summary>
		/// Gets the list of all libraries
		/// </summary>
		/// <example>
		/// GET /p/libraries
		/// </example>
		/// <remarks>
		/// Returns a list of all libraries
		/// </remarks>
		/// <returns>List of all libraries</returns>
		[Route("")]
		public IEnumerable<Library> Get()
		{
			return _pitDatabase.Libraries;
		}

		/// <summary>
		/// Gets the details for specified library
		/// </summary>
		/// <example>
		/// GET /p/libraries/id
		/// </example>
		/// <remarks>
		/// The library details contains the list of all pits contained in the library
		/// </remarks>
		/// <param name="id">Library identifier</param>
		/// <returns>Library details</returns>
		[Route("{id}")]
		[ResponseType(typeof(Library))]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified library does not exist")]
		public IHttpActionResult Get(string id)
		{
			var lib = _pitDatabase.GetLibraryById(id);
			if (lib == null)
				return NotFound();

			return Ok(lib);
		}
	}
}
