//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.IO;
using System.Linq;
using Peach.Core;
using Peach.Core.Agent.Channels;
using Peach.Core.IO;
using HttpListenerRequest = SocketHttpListener.Net.HttpListenerRequest;
using SocketHttpListener.Net;

namespace Peach.Pro.Core.Agent.Channels.Rest
{
	internal class PublisherHandler : IDisposable
	{
		class Context : INamed, IDisposable
		{
			#region Obsolete Functions

			[Obsolete("This property is obsolete and has been replaced by the Name property.")]
			public string name { get { return Name; } }

			#endregion

			private readonly PublisherHandler _handler;
			private readonly Publisher _publisher;

			public string Name { get { return _publisher.Name; } }
			public string Url { get; private set; }

			public Context(PublisherHandler handler, PublisherRequest req)
			{
				_handler = handler;

				Url = Server.PublisherPath + "/" + Guid.NewGuid();

				_publisher = AgentLocal.ActivatePublisher(req.Name, req.Class, req.Args);
				_publisher.start();

				_handler._routes.Add(Url, "DELETE", OnDelete);
				_handler._routes.Add(Url + "/open", "PUT", OnOpen);
				_handler._routes.Add(Url + "/close", "PUT", OnClose);
				_handler._routes.Add(Url + "/accept", "PUT", OnAccept);
				_handler._routes.Add(Url + "/output", "PUT", OnOutput);
				_handler._routes.Add(Url + "/input", "PUT", OnInput);
				_handler._routes.Add(Url + "/call", "PUT", OnCall);
				_handler._routes.Add(Url + "/property", "PUT", OnSetProperty);
				_handler._routes.Add(Url + "/property", "GET", OnGetProperty);
				_handler._routes.Add(Url + "/data", "GET", OnWantBytes);
			}

			public void Dispose()
			{
				_publisher.close();
				_publisher.stop();

				_handler._routes.Remove(Url);
				_handler._routes.Remove(Url + "/open");
				_handler._routes.Remove(Url + "/close");
				_handler._routes.Remove(Url + "/accept");
				_handler._routes.Remove(Url + "/output");
				_handler._routes.Remove(Url + "/input");
				_handler._routes.Remove(Url + "/call");
				_handler._routes.Remove(Url + "/property");
				_handler._routes.Remove(Url + "/data");

				_handler._contexts.Remove(this);
			}

			private RouteResponse OnDelete(HttpListenerRequest req)
			{
				Dispose();

				return RouteResponse.Success();
			}

			private RouteResponse OnOpen(HttpListenerRequest req)
			{
				var args = req.FromJson<PublisherOpenRequest>();

				_publisher.Iteration = args.Iteration;
				_publisher.IsControlIteration = args.IsControlIteration;
				_publisher.IsControlRecordingIteration = args.IsControlRecordingIteration;
				_publisher.IsIterationAfterFault = args.IsIterationAfterFault;
				_publisher.open();

				return RouteResponse.Success();
			}

			private RouteResponse OnClose(HttpListenerRequest req)
			{
				_publisher.close();

				return RouteResponse.Success();
			}

			private RouteResponse OnAccept(HttpListenerRequest req)
			{
				_publisher.accept();

				return RouteResponse.Success();
			}

			private RouteResponse OnOutput(HttpListenerRequest req)
			{
				using (var strm = req.InputStream)
				{
					var bs = new BitStream();
					strm.CopyTo(bs);
					bs.Seek(0, SeekOrigin.Begin);

					try
					{
						_publisher.output(bs);
					}
					catch (NotSupportedException ex)
					{
						RaiseNotSupported(ex, "output");
					}
				}

				return RouteResponse.Success();
			}

			private RouteResponse OnInput(HttpListenerRequest req)
			{
				_publisher.input();

				var resp = new BoolResponse { Value = _publisher.Position == 0 };

				return RouteResponse.AsJson(resp);
			}

			private RouteResponse OnCall(HttpListenerRequest req)
			{
				var json = req.FromJson<CallRequest>();

				var args = json.Args
					.Select(i => new BitStream(i.Value) { Name = i.Name })
					.ToList<BitwiseStream>();

				Variant ret = null;

				try
				{
					ret = _publisher.call(json.Method, args);
				}
				catch (NotSupportedException ex)
				{
					RaiseNotSupported(ex, "call");
				}

				var resp = ret.ToModel<CallResponse>();

				return RouteResponse.AsJson(resp);
			}

			private RouteResponse OnSetProperty(HttpListenerRequest req)
			{
				var args = req.FromJson<SetPropertyRequest>();
				var value = args.ToVariant();

				_publisher.setProperty(args.Name, value);

				return RouteResponse.Success();
			}

			private RouteResponse OnGetProperty(HttpListenerRequest req)
			{
				var property = req.QueryString["name"];
				if (string.IsNullOrEmpty(property))
					return RouteResponse.BadRequest();

				var value = _publisher.getProperty(property);
				var resp = value.ToModel<GetPropertyResponse>();

				return RouteResponse.AsJson(resp);
			}

			private RouteResponse OnWantBytes(HttpListenerRequest req)
			{
				// These can acquire a lock, so cache the length
				var len = _publisher.Length;

				long offset;
				if (!req.QueryString.TryGetValue("offset", out offset, 0))
					return RouteResponse.BadRequest();

				long count;
				if (!req.QueryString.TryGetValue("count", out count, len - offset))
					return RouteResponse.BadRequest();

				var needed = count - len + offset;

				if (needed > 0)
				{
					// WantBytes is based off publisher Position
					_publisher.WantBytes(count - _publisher.Position + offset);

					// Recheck Length since WantBytes can change it
					len = _publisher.Length;
				}

				if (offset >= len)
					return RouteResponse.Success();

				// Set position so the stream returned starts at the proper offset
				_publisher.Position = offset;

				return RouteResponse.AsStream(_publisher);
			}

			private void RaiseNotSupported(Exception ex, string action)
			{
				var type = _publisher.GetType();
				var cls = type
					.GetAttributes<PublisherAttribute>()
					.Select(a => a.Name)
					.FirstOrDefault() ?? type.Name;

				throw new PeachException("The {0} publisher does not support {1} actions when run on remote agents.".Fmt(cls, action), ex);
			}
		}

		private readonly NamedCollection<Context> _contexts;
		private readonly RouteHandler _routes;

		public PublisherHandler(RouteHandler routes)
		{

			_contexts = new NamedCollection<Context>();
			_routes = routes;
			_routes.Add(Server.PublisherPath, "POST", OnCreatePublisher);
		}

		public void Dispose()
		{
			while (_contexts.Count > 0)
			{
				_contexts[0].Dispose();
			}
		}

		private RouteResponse OnCreatePublisher(HttpListenerRequest req)
		{
			var ctx = new Context(this, req.FromJson<PublisherRequest>());

			_contexts.Add(ctx);

			var resp = new PublisherResponse
			{
				Url = ctx.Url,
			};

			return RouteResponse.AsJson(resp, HttpStatusCode.Created);
		}
	}	
}
