using System.Linq;
using System.Web.Http.Description;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.WebApi2.Controllers;
using Swashbuckle.Swagger;

namespace Peach.Pro.WebApi2.Utility
{
	internal class CommonResponseFilter : IOperationFilter
	{
		public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
		{
			operation.responses.Add("500", new Response
			{
				description = "Internal Server Error",
				schema = schemaRegistry.GetOrRegister(typeof(Error))
			});

			if (apiDescription.RelativePath.StartsWith(LicenseController.Prefix))
				return;

			operation.responses.Add("401", new Response
			{
				description = "EULA has not been accepted"
			});

			operation.responses.Add("402", new Response
			{
				description = "Invalid or expired license"
			});

			var attr = apiDescription.GetControllerAndActionAttributes<ResultFileAttribute>().FirstOrDefault();

			if (attr != null)
			{
				operation.produces = new[] { attr.ContentType };
				operation.responses["200"].schema = new Schema { type = "file" };
			}
		}
	}
}
