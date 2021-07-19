using System;
using System.Linq;
using Peach.Pro.Core.WebServices.Models;
using Swashbuckle.Swagger;

namespace Peach.Pro.WebApi2.Utility
{
	internal class RequiredParameterFilter : ISchemaFilter
	{
		public void Apply(Schema schema, SchemaRegistry schemaRegistry, Type type)
		{
			// For now, mark all properties as required
			schema.required = schema.properties.Keys.ToList();

			if (type != typeof(Error))
				return;

			schema.@default = new Error
			{
				ErrorMessage = "Error message goes here.",
				FullException = "Full exception output goes here."
			};
		}
	}
}
