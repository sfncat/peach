//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SocketHttpListener.Net;

namespace Peach.Pro.Core.Agent.Channels.Rest
{
	internal class RouteHandler
	{
		private class Route
		{
			public string Prefix { get; set; }

			public Dictionary<string, RequestHandler> Handlers { get; set; }
		}

		private readonly Dictionary<string, Route> _routes = new Dictionary<string, Route>();

		public delegate RouteResponse RequestHandler(HttpListenerRequest req);

		public void Add(string prefix, string method, RequestHandler handler)
		{
			Route route;
			if (!_routes.TryGetValue(prefix, out route))
			{
				route = new Route
				{
					Prefix = prefix,
					Handlers = new Dictionary<string, RequestHandler>()
				};

				_routes.Add(prefix, route);
			}

			route.Handlers.Add(method, handler);
		}

		public void Remove(string prefix)
		{
			if (!_routes.Remove(prefix))
				throw new KeyNotFoundException();
		}

		public RouteResponse Dispatch(HttpListenerRequest req)
		{
			Route route;

			if (!_routes.TryGetValue(req.Url.AbsolutePath, out route))
				return RouteResponse.NotFound();

			Debug.Assert(req.Url.AbsolutePath == route.Prefix);

			RequestHandler handler;
			if (!route.Handlers.TryGetValue(req.HttpMethod, out handler))
				return RouteResponse.NotAllowed();

			try
			{
				return handler(req);
			}
			catch (Exception ex)
			{
				return RouteResponse.Error(ex);
			}
		}
	}	
}
