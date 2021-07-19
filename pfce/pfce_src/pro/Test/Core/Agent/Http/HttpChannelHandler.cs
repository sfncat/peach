
//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NLog;
using NLog.Targets.Wrappers;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Agent.Channels;
using Peach.Pro.Core.Agent.Channels.Rest;
using Logger = NLog.Logger;
using HttpListenerRequest = SocketHttpListener.Net.HttpListenerRequest;
using SocketHttpListener.Net;

namespace Peach.Pro.Test.Core.Agent.Http
{
	internal class HttpChannelHandler : IDisposable
	{
		[Serializable]
		class WantBytesRequest
		{
			public long count { get; set; }
		}

		private readonly Func<long, byte[]> _wantBytes;
		private readonly RouteHandler _routes;
		private readonly List<string> _restCalls; 

		public HttpChannelHandler(RouteHandler routes, List<string> restCalls, Func<long, byte[]> wantBytes)
		{
			_wantBytes = wantBytes;
			_restCalls = restCalls;

			_routes = routes;
			_routes.Add(HttpServer.MonitorPath + "/AgentConnect", "GET", OnHandler);
			_routes.Add(HttpServer.MonitorPath + "/AgentDisconnect", "GET", OnHandler);
			_routes.Add(HttpServer.MonitorPath + "/StartMonitor", "POST", OnHandler);
			_routes.Add(HttpServer.MonitorPath + "/StopAllMonitors", "GET", OnHandler);
			_routes.Add(HttpServer.MonitorPath + "/SessionStarting", "GET", OnHandler);
			_routes.Add(HttpServer.MonitorPath + "/SessionFinished", "GET", OnHandler);
			_routes.Add(HttpServer.MonitorPath + "/IterationStarting", "GET", OnHandler);
			_routes.Add(HttpServer.MonitorPath + "/IterationFinished", "GET", OnHandler);
			_routes.Add(HttpServer.MonitorPath + "/DetectedFault", "GET", OnHandlerFalse);
			_routes.Add(HttpServer.MonitorPath + "/GetMonitorData", "GET", OnHandler);

			_routes.Add(HttpServer.PublisherPath + "/CreatePublisher", "POST", OnHandler);
			_routes.Add(HttpServer.PublisherPath + "/start", "GET", OnHandler);
			_routes.Add(HttpServer.PublisherPath + "/stop", "GET", OnHandler);
			_routes.Add(HttpServer.PublisherPath + "/Set_Iteration", "POST", OnHandler);
			_routes.Add(HttpServer.PublisherPath + "/Set_IsControlIteration", "POST", OnHandler);
			_routes.Add(HttpServer.PublisherPath + "/open", "GET", OnHandler);
			_routes.Add(HttpServer.PublisherPath + "/close", "GET", OnHandler);
			_routes.Add(HttpServer.PublisherPath + "/accept", "GET", OnHandler);
			_routes.Add(HttpServer.PublisherPath + "/call", "POST", OnHandler);
			_routes.Add(HttpServer.PublisherPath + "/setProperty", "POST", OnHandler);
			_routes.Add(HttpServer.PublisherPath + "/getProperty", "POST", OnHandler);
			_routes.Add(HttpServer.PublisherPath + "/output", "POST", OnHandler);
			_routes.Add(HttpServer.PublisherPath + "/input", "GET", OnHandler);
			_routes.Add(HttpServer.PublisherPath + "/WantBytes", "POST", OnWantBytesHandler);
		}

		public void Dispose()
		{
		}

		private RouteResponse OnHandler(HttpListenerRequest req)
		{
			_restCalls.Add(req.Url.PathAndQuery);

			var resp = new JsonResponse
			{
				Status = "true",
				Data = null,
				Results = null
			};

			return RouteResponse.AsJson(resp, HttpStatusCode.OK);
		}

		private RouteResponse OnWantBytesHandler(HttpListenerRequest req)
		{
			string data;

			using (var sr = new StreamReader(req.InputStream))
				data = sr.ReadToEnd();

			_restCalls.Add(req.Url.PathAndQuery + " " + data);

			var obj = JsonConvert.DeserializeObject<WantBytesRequest>(data);

			var resp = new JsonResponse
			{
				Status = "true",
				Data = null,
				Results = null
			};

			if (_wantBytes != null)
				resp.Data = _wantBytes(obj.count);

			return RouteResponse.AsJson(resp, HttpStatusCode.OK);
		}
		private RouteResponse OnHandlerFalse(HttpListenerRequest req)
		{
			_restCalls.Add(req.Url.PathAndQuery);

			var resp = new JsonResponse
			{
				Status = "false",
				Data = null,
				Results = null
			};

			return RouteResponse.AsJson(resp, HttpStatusCode.OK);
		}

		[Serializable]
		class JsonResponse
		{
			public string Status { get; set; }
			public byte[] Data { get; set; }
			public Dictionary<string, object> Results { get; set; }
		}

		[Serializable]
		class JsonFaultResponse
		{
			public string Status { get; set; }
			public string Data { get; set; }
			public Fault[] Results { get; set; }
		}

	}
}
