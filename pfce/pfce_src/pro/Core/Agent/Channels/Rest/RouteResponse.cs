//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.IO;
using Newtonsoft.Json;
using Peach.Core;
using SocketHttpListener.Net;

namespace Peach.Pro.Core.Agent.Channels.Rest
{
	/// <summary>
	/// Object returned by route handlers.
	/// Includes the content, content type and status code.
	/// </summary>
	/// <remarks>
	/// Each time the response is sent to the client, the
	/// content stream is repositioned to the beginning.
	/// </remarks>
	internal class RouteResponse
	{
		public string ContentType { get; set; }

		public Stream Content { get; set; }

		public HttpStatusCode StatusCode { get; set; }

		public virtual void Complete(HttpListenerContext ctx)
		{
			var resp = ctx.Response;

			resp.StatusCode = (int)StatusCode;

			if (Content == null)
			{
				resp.ContentLength64 = 0;
				resp.OutputStream.Close();
			}
			else
			{
				// Leave the stream at the position it was given to us at
				// so we can return data starting at an offset.

				resp.ContentType = ContentType;
				resp.ContentLength64 = Content.Length - Content.Position;

				using (var stream = resp.OutputStream)
				{
					Content.CopyTo(stream);
				}
			}
		}

		/// <summary>
		/// The generic success response.
		/// </summary>
		/// <returns></returns>
		public static RouteResponse Success()
		{
			return new RouteResponse
			{
				Content = null,
				StatusCode = HttpStatusCode.OK,
			};
		}

		/// <summary>
		/// Sent when the requested uri is not found.
		/// </summary>
		/// <returns></returns>
		public static RouteResponse NotFound()
		{
			return new RouteResponse
			{
				Content = null,
				StatusCode = HttpStatusCode.NotFound,
			};
		}

		/// <summary>
		/// Sent when the request is invalid.
		/// For example, a query parameter is missing or a
		/// posted json value is out of range.
		/// </summary>
		/// <returns></returns>
		public static RouteResponse BadRequest()
		{
			return new RouteResponse
			{
				Content = null,
				StatusCode = HttpStatusCode.BadRequest,
			};
		}

		/// <summary>
		/// Sent when the method is not allowed.
		/// For example, a GET when only POST is allowed.
		/// </summary>
		/// <returns></returns>
		public static RouteResponse NotAllowed()
		{
			return new RouteResponse
			{
				Content = null,
				StatusCode = HttpStatusCode.MethodNotAllowed,
			};
		}

		/// <summary>
		/// Serialize the object as JSON and send it as the response to the client.
		/// </summary>
		/// <param name="obj">The object to serialize to JSON.</param>
		/// <param name="code">The HTTP status code to return.</param>
		/// <returns></returns>
		public static RouteResponse AsJson(object obj, HttpStatusCode code = HttpStatusCode.OK)
		{
			var json = JsonConvert.SerializeObject(obj);
			var stream = new MemoryStream();
			var writer = new StreamWriter(stream, System.Text.Encoding.UTF8);

			writer.Write(json);
			writer.Flush();

			stream.Seek(0, SeekOrigin.Begin);

			return new RouteResponse
			{
				ContentType = "application/json;charset=utf-8",
				Content = stream,
				StatusCode = code,
			};
		}

		/// <summary>
		/// Return a raw stream of bytes to the client.
		/// </summary>
		/// <param name="stream">The bytes to return.</param>
		/// <returns></returns>
		public static RouteResponse AsStream(Stream stream)
		{
			return new RouteResponse
			{
				ContentType = "application/octet-stream",
				Content = stream,
				StatusCode = HttpStatusCode.OK,
			};
		}

		/// <summary>
		/// Return an error response to the client when an exception is thrown
		/// by a route handler.
		/// </summary>
		/// <remarks>
		/// If the exception is a SoftException, HTTP status 503 is returned.
		/// This implies it is a recoverable error (try again later).
		/// If the exception is not a SoftException, HTTP status 500 is returned.
		/// This implies it is a non-recoverable error.
		/// </remarks>
		/// <param name="ex">The exception to return to the client.</param>
		/// <returns></returns>
		public static RouteResponse Error(Exception ex)
		{
			var code = HttpStatusCode.InternalServerError;

			var resp = new ExceptionResponse
			{
				Message = ex.Message,
				StackTrace = ex.ToString(),
			};

			var fe = ex as FaultException;
			if (fe != null)
			{
				resp.Fault = fe.Fault;
				code = HttpStatusCode.ServiceUnavailable;
			}
			else if (ex is SoftException)
			{
				code =  HttpStatusCode.ServiceUnavailable;
			}

			return AsJson(resp, code);
		}
	}
}
