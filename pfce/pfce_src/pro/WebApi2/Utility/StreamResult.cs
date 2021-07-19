using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Peach.Pro.WebApi2.Utility
{
	internal class StreamResult : IHttpActionResult
	{
		private readonly Stream _strm;

		public StreamResult(Stream strm)
		{
			_strm = strm;
		}

		public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
		{
			var resp = new HttpResponseMessage
			{
				Content = new StreamContent(_strm)
			};

			resp.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

			return Task.FromResult(resp);
		}
	}
}
