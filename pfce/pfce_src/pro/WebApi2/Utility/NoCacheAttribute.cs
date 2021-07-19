using System.Net.Http.Headers;
using System.Web.Http.Filters;

namespace Peach.Pro.WebApi2.Utility
{
	internal class NoCacheAttribute : ActionFilterAttribute
	{
		public override void OnActionExecuted(HttpActionExecutedContext context)
		{
			if (context.Response != null)
			{
				context.Response.Headers.CacheControl = new CacheControlHeaderValue
				{
					NoCache = true
				};

				context.Response.Headers.Pragma.Add(new NameValueHeaderValue("no-cache"));
			}

			base.OnActionExecuted(context);
		}
	}
}
