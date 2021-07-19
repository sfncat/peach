using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace Peach.Pro.WebApi2.Utility
{
	internal class FileResult : IHttpActionResult
	{
		private readonly FileInfo _info;

		public FileResult(FileInfo info)
		{
			_info = info;
		}

		public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
		{
			var resp = new HttpResponseMessage
			{
				Content = new StreamContent(_info.OpenRead())
			};

			var contentType = MimeMapping.GetMimeMapping(_info.Name);
			var contentDisposition = new ContentDispositionHeaderValue("attachment")
			{
				FileNameStar = _info.Name
			};

			resp.Content.Headers.LastModified = _info.LastWriteTime;
			resp.Content.Headers.ContentLength = _info.Length;
			resp.Content.Headers.ContentDisposition = contentDisposition;
			resp.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

			return Task.FromResult(resp);
		}
	}
}
