using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Ionic.Zip;

namespace Peach.Pro.WebApi2.Utility
{
	internal class ZipResult : IHttpActionResult
	{
		private readonly DirectoryInfo _info;
		private readonly string _fileName;

		public ZipResult(string fileName, DirectoryInfo di)
		{
			_fileName = fileName;
			_info = di;
		}

		public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
		{
			var resp = new HttpResponseMessage
			{
				Content = new PushStreamContent((a,b,c) => WriteToStream(a))
			};

			var contentType = MimeMapping.GetMimeMapping(_fileName);
			var contentDisposition = new ContentDispositionHeaderValue("attachment")
			{
				FileNameStar = _fileName
			};

			resp.Content.Headers.ContentDisposition = contentDisposition;
			resp.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

			return Task.FromResult(resp);
		}

		private void WriteToStream(Stream stream)
		{
			try
			{
				using (var zip = new ZipFile())
				{
					zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestSpeed;
					zip.AddDirectory(_info.FullName);
					zip.Save(stream);
				}
			}
			catch (HttpException)
			{
			}
			finally
			{
				stream.Close();
			}
		}
	}
}
