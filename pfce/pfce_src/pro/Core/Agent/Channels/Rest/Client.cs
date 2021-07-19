//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.IO;
using Logger = NLog.Logger;

namespace Peach.Pro.Core.Agent.Channels.Rest
{

	[Agent("tcp")]
	public class Client : AgentClient
	{
		#region Publisher Proxy

		class PublisherProxy : IPublisher
		{
			readonly Client _client;
			readonly PublisherRequest _createReq;

			Uri _publisherUri;

			public PublisherProxy(Client client, string name, string cls, Dictionary<string, string> args)
			{
				_client = client;
				_createReq = new PublisherRequest
				{
					Name = name,
					Class = cls,
					Args = args,
				};

				InputStream = new MemoryStream();

				Connect();

				_client._publishers.Add(this);
			}

			public void Dispose()
			{
				Send("DELETE", "", null);

				InputStream.Dispose();
				InputStream = null;

				_client._publishers.Remove(this);
			}

			internal void SimulateDisconnect()
			{
				Send("DELETE", "", null);
			}

			public Stream InputStream
			{
				get;
				private set;
			}

			public void Connect()
			{
				_publisherUri = new Uri(_client._baseUrl, Server.PublisherPath);
				var resp = Send<PublisherResponse>("POST", "", _createReq);
				_publisherUri = new Uri(_client._baseUrl, resp.Url);
			}

			public void Open(uint iteration, bool isControlIteration, bool isControlRecordingIteration, bool isIterationAfterFault)
			{
				var req = new PublisherOpenRequest
				{
					Iteration = iteration,
					IsControlIteration = isControlIteration,
					IsControlRecordingIteration = isControlRecordingIteration,
					IsIterationAfterFault = isIterationAfterFault
				};

				Guard("Open", () => Send("PUT", "/open", req));

				InputStream.Position = 0;
				InputStream.SetLength(0);
			}

			public void Close()
			{
				Guard("Close", () => Send("PUT", "/close", null));
			}

			public void Accept()
			{
				Guard("Accept", () => Send("PUT", "/accept", null));
			}

			public Variant Call(string method, List<BitwiseStream> args)
			{
				var req = new CallRequest
				{
					Method = method,
					Args = new List<CallRequest.Param>()
				};

				foreach (var arg in args)
				{
					var param = new CallRequest.Param
					{
						Name = arg.Name,
						Value = new byte[arg.Length]
					};

					arg.Seek(0, SeekOrigin.Begin);
					arg.Read(param.Value, 0, param.Value.Length);

					req.Args.Add(param);
				}

				return Guard("Call", () =>
				{
					var resp = Send<CallResponse>("PUT", "/call", req);
					var ret = resp.ToVariant();
					return ret;
				});
			}

			public void SetProperty(string name, Variant value)
			{
				Guard("SetProperty", () =>
				{
					var req = value.ToModel<SetPropertyRequest>();
					req.Name = name;
					Send("PUT", "/property", req);
				});
			}

			public Variant GetProperty(string name)
			{
				return Guard("GetProperty", () =>
				{
					var resp = Send<GetPropertyResponse>("GET", "/property?name=" + name, null);
					return resp.ToVariant();
				});
			}

			public void Output(BitwiseStream data)
			{
				Guard("Output", () =>
				{
					var uri = new Uri(_publisherUri, _publisherUri.PathAndQuery + "/output");
					var request = RouteResponse.AsStream(data);
					_client.Execute("PUT", uri, request, SendStream, resp => resp.Consume());
				});
			}

			public void Input()
			{
				Guard("Input", () =>
				{
					var resp = Send<BoolResponse>("PUT", "/input", null);
					if (resp.Value)
					{
						InputStream.Position = 0;
						InputStream.SetLength(0);
					}

					// Read all input bytes starting at offset 'Length'
					// so we don't re-download bytes we have already gotten.

					ReadInputData("?offset={0}".Fmt(InputStream.Length));
				});
			}

			public void WantBytes(long count)
			{
				var needed = count - InputStream.Length + InputStream.Position;

				if (needed > 0)
				{
					var query = "?offset={0}&count={1}".Fmt(InputStream.Length, needed);
					Guard("WantBytes", () => ReadInputData(query));
				}
			}

			private void Send(string method, string path, object request)
			{
				var uri = new Uri(_publisherUri, _publisherUri.PathAndQuery + path);
				_client.Execute(method, uri, request, SendJson, resp => resp.Consume());
			}

			private T Send<T>(string method, string path, object request)
			{
				var uri = new Uri(_publisherUri, _publisherUri.PathAndQuery + path);
				return _client.Execute(method, uri, request, SendJson, req => req.FromJson<T>());
			}

			private void ReadInputData(string query)
			{
				var uri = new Uri(_publisherUri, _publisherUri.PathAndQuery + "/data" + query);

				_client.Execute("GET", uri, (object)null, null, resp =>
				{
					var pos = InputStream.Position;

					try
					{
						InputStream.Seek(0, SeekOrigin.End);

						using (var strm = resp.GetResponseStream())
						{
							if (strm != null)
								strm.CopyTo(InputStream);
						}
					}
					finally
					{
						InputStream.Seek(pos, SeekOrigin.Begin);
					}

					return (object)null;
				});
			}

			private void Guard(string what, Action action)
			{
				Guard<object>(what, () => { action(); return null; });
			}

			private T Guard<T>(string what, Func<T> action)
			{
				try
				{
					return action();
				}
				catch (PeachException)
				{
					throw;
				}
				catch (SoftException)
				{
					throw;
				}
				catch (WebException ex)
				{
					Logger.Debug("Ignoring {0} calling '{1}' on remote {2} publisher '{3}'.",
						ex.Status,
						what,
						_createReq.Class,
						_createReq.Name);

					Logger.Debug(ex.Message);
				}
				catch (Exception ex)
				{
					Logger.Warn("Ignoring {0} calling '{1}' on remote {2} publisher '{3}'.",
						ex.GetType().Name,
						what,
						_createReq.Class,
						_createReq.Name);

					Logger.Debug(ex.Message);
				}

				return default(T);
			}
		}

		#endregion

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private readonly List<PublisherProxy> _publishers;
		private readonly CookieContainer _cookies;
		private readonly LogSink _sink;

		private Uri _baseUrl;
		private ConnectRequest _connectReq;
		private ConnectResponse _connectResp;
		private Uri _agentUri;
		private bool _offline;

		public Client(string name, string uri, string password)
			: base(name, uri, password)
		{
			_publishers = new List<PublisherProxy>();
			_cookies = new CookieContainer();
			_sink = new LogSink(Name);
		}

		public override void AgentConnect()
		{
			try
			{
				_baseUrl = new Uri(Url);

				if (_baseUrl.IsDefaultPort)
				{
					_baseUrl = new Uri("{0}://{1}:{2}".Fmt(
						_baseUrl.Scheme,
						_baseUrl.Host,
						Server.DefaultPort));
				}

				_baseUrl = new Uri("http://{0}:{1}".Fmt(_baseUrl.Host, _baseUrl.Port));
			}
			catch (SystemException ex)
			{
				throw new PeachException(ex.Message, ex);
			}

			// Initialize our monitors to be empty and populate
			// them with calls to StartMonitor()
			_connectReq = new ConnectRequest
			{
				Monitors = new List<MonitorRequest>(),
			};

			ReconnectAgent(null);
		}

		public override void StartMonitor(string monName, string cls, Dictionary<string, string> args)
		{
			var mon = new MonitorRequest
			{
				Name = monName,
				Class = cls,
				Args = args
			};

			// Save off this monitor for future reconnects
			_connectReq.Monitors.Add(mon);

			// Tell the agent to start this single monitor and
			// update our connect response with the supported mesages
			_connectResp = Send<ConnectResponse>("POST", "", mon);
		}

		public override void SessionStarting()
		{
			if (_connectResp.Messages.Contains("SessionStarting"))
				Send("PUT", "/SessionStarting", null);
		}

		public override void SessionFinished()
		{
			if (_connectResp.Messages.Contains("SessionFinished"))
				Send("PUT", "/SessionFinished", null);
		}

		public override void StopAllMonitors()
		{
			// AgentDisconnect calls DELETE which does StopAllMonitors
		}

		public override void AgentDisconnect()
		{
			// If reconnect failed, we won't have an agent uri
			if (_agentUri != null)
				Send("DELETE", "", null);

			_connectResp = null;
			_sink.Stop();
		}

		public override IPublisher CreatePublisher(string pubName, string cls, Dictionary<string, string> args)
		{
			return new PublisherProxy(this, pubName, cls, args);
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			if (_offline)
			{
				// If any previous call failed, we need to reconnect
				// This will send IterationStarting as well as
				// restart any remote publishers.
				ReconnectAgent(args);
				return;
			}

			if (!_connectResp.Messages.Contains("IterationStarting"))
				return;

			var req = new IterationStartingRequest
			{
				IsReproduction = args.IsReproduction,
				LastWasFault = args.LastWasFault,
			};

			try
			{
				Send("PUT", "/IterationStarting", req);
			}
			catch (WebException)
			{
				// If we are not offline now or we were previously offline
				// don't try and reconnect since we just did!
				if (!_offline)
					throw;

				// Being offline now means this is the first message we sent
				// to a newly restarted agent, so we need to reconnect.
				ReconnectAgent(args);
			}
		}

		public override void IterationFinished()
		{
			if (_connectResp.Messages.Contains("IterationFinished"))
				Send("PUT", "/IterationFinished", null);
		}

		public override bool DetectedFault()
		{
			if (!_connectResp.Messages.Contains("DetectedFault"))
				return false;

			var resp = Send<BoolResponse>("GET", "/DetectedFault", null);

			return resp != null && resp.Value;
		}

		public override IEnumerable<MonitorData> GetMonitorData()
		{
			if (!_connectResp.Messages.Contains("GetMonitorData"))
				return new MonitorData[0];

			var resp = Send<FaultResponse>("GET", "/GetMonitorData", null);

			if (resp == null || resp.Faults == null)
				return new MonitorData[0];

			var ret = resp.Faults.Select(MakeFault);

			return ret;
		}

		public override void Message(string msg)
		{
			var path = "Message/{0}".Fmt(msg);

			if (_connectResp.Messages.Contains(path))
				Send("PUT", "/" + path, null);
		}

		internal void SimulateDisconnect()
		{
			Send("DELETE", "", null);

			foreach (var pub in _publishers)
				pub.SimulateDisconnect();
		}

		private MonitorData MakeFault(FaultResponse.Record f)
		{
			var ret = new MonitorData
			{
				AgentName = Name,
				DetectionSource = f.DetectionSource,
				MonitorName = f.MonitorName,
				Title = f.Title,
				Data = f.Data.ToDictionary(i => i.Key, DownloadFile),
			};

			if (f.Fault != null)
			{
				ret.Fault = new MonitorData.Info
				{
					Description = f.Fault.Description,
					MajorHash = f.Fault.MajorHash,
					MinorHash = f.Fault.MinorHash,
					Risk = f.Fault.Risk,
					MustStop = f.Fault.MustStop,
				};
			}

			return ret;
		}

		private Stream DownloadFile(FaultResponse.Record.FaultData data)
		{
			Logger.Trace("Downloading {0} byte file '{1}'.", data.Size, data.Key);

			var uri = new Uri(_baseUrl, data.Url);

			return Execute("GET", uri, (object)null, null, resp =>
			{
				using (var strm = resp.GetResponseStream())
				{
					var ms = new MemoryStream();

					if (strm != null)
						strm.CopyTo(ms);

					return ms;
				}
			});
		}

		private void ReconnectAgent(IterationStartingArgs args)
		{
			_sink.Start(_baseUrl);

			if (args != null)
				Logger.Debug("Restarting all monitors on remote agent {0}", _baseUrl);

			_offline = false;

			try
			{
				// Send the initial POST to the base url
				_agentUri = new Uri(_baseUrl, Server.MonitorPath);
				_connectResp = Send<ConnectResponse>("POST", "", _connectReq);
				_agentUri = new Uri(_baseUrl, _connectResp.Url);
			}
			catch
			{
				_agentUri = null;

				throw;
			}

			if (args == null)
				return;

			// If we are reconnecting, we need to send iteration starting
			// prior to restarting the publishers to allow any automation to run
			if (_connectResp.Messages.Contains("IterationStarting"))
				Send("PUT", "/IterationStarting", args);

			if (_publishers.Count > 0)
				Logger.Debug("Restarting all publishers on remote agent {0}", _baseUrl);

			// If we are reconnecting, ensure all the publishers are recreated
			foreach (var pub in _publishers)
				pub.Connect();

			Logger.Debug("Successfully restored connection to remote agent {0}", _baseUrl);
		}

		private void Send(string method, string path, object request)
		{
			var uri = new Uri(_agentUri, _agentUri.PathAndQuery + path);
			Execute(method, uri, request, SendJson, resp => resp.Consume());
		}

		private T Send<T>(string method, string path, object request)
		{
			var uri = new Uri(_agentUri, _agentUri.PathAndQuery + path);
			return Execute(method, uri, request, SendJson, req => req.FromJson<T>());
		}

		private static void SendJson(HttpWebRequest req, object obj)
		{
			if (req.Method == "GET")
			{
				Debug.Assert(obj == null);
				return;
			}

			var json = RouteResponse.AsJson(obj);

			SendStream(req, json);
		}

		private static void SendStream(HttpWebRequest req, RouteResponse obj)
		{
			req.ContentType = obj.ContentType;
			req.ContentLength = obj.Content.Length;

			// Ensure we are at the begining so any retries
			// will send all the data
			obj.Content.Seek(0, SeekOrigin.Begin);

			using (var strm = req.GetRequestStream())
				obj.Content.CopyTo(strm);
		}

		private TOut Execute<TOut,TIn>(string method,
			Uri uri,
			TIn request,
			Action<HttpWebRequest, TIn> encode,
			Func<HttpWebResponse, TOut> decode)
		{
			if (_offline)
			{
				Logger.Debug("Agent server '{0}' is offline", uri);
				Logger.Debug("Ignoring command '{0} {1}'", method, uri.PathAndQuery);
				return default(TOut);
			}

			try
			{
				return ExecuteInner(method, uri, request, encode, decode);
			}
			catch (WebException ex)
			{
				if (ex.Status != WebExceptionStatus.ProtocolError)
				{
					Logger.Debug(ex.Message);

					// Mark offline to trigger a future reconnect
					_offline = true;

					throw;
				}

				using (var resp = (HttpWebResponse)ex.Response)
				{
					Logger.Trace("<<< {0} {1}", (int)resp.StatusCode, resp.StatusDescription);

					// If we get a 500 or 503, this means the command we ran
					// failed to complete, but our agentUrl is still valid
					// so we don't want to clear the agent url

					if (resp.StatusCode != HttpStatusCode.InternalServerError &&
						resp.StatusCode != HttpStatusCode.ServiceUnavailable)
					{
						Logger.Debug(ex.Message);

						// Mark offline to trigger a future reconnect
						_offline = true;

						// Consume all bytes sent to us in the response
						resp.Consume();

						throw;
					}

					var error = resp.FromJson<ExceptionResponse>();

					// 503 means try again later
					if (resp.StatusCode == HttpStatusCode.ServiceUnavailable)
					{
						if (error.Fault != null)
						{
							error.Fault.AgentName = Name;
							throw new FaultException(error.Fault, ex);
						}

						Logger.Trace(error.Message);
						Logger.Trace("Server Stack Trace:\n{0}", error.StackTrace);
						throw new SoftException(error.Message, ex);
					}

					// 500 is hard fail
					Logger.Trace(error.Message);
					Logger.Trace("Server Stack Trace:\n{0}", error.StackTrace);
					throw new PeachException(error.Message, ex);
				}
			}
			catch (Exception ex)
			{
				Logger.Debug(ex.Message);
				Logger.Trace(ex);

				throw;
			}
		}

		private TOut ExecuteInner<TOut, TIn>(string method,
			Uri uri,
			TIn request,
			Action<HttpWebRequest, TIn> encode,
			Func<HttpWebResponse, TOut> decode)
		{
			var retryCount = 10;

			while (true)
			{
				Logger.Trace(">>> {0} {1}", method, uri);

				try
				{
					var req = (HttpWebRequest)WebRequest.Create(uri);

					// This should enable connection reuse.
					// The container doesn't need to actually have any cookies in it
					req.CookieContainer = _cookies;

					// For POST we don't need to expect 100 CONTINUE responses
					req.ServicePoint.Expect100Continue = false;
					req.Timeout = 60000;
					req.KeepAlive = true;

					req.Method = method;

					if (Equals(request, default(TIn)))
						req.ContentLength = 0;
					else
						encode(req, request);

					using (var resp = (HttpWebResponse)req.GetResponse())
					{
						Logger.Trace("<<< {0} {1}", (int)resp.StatusCode, resp.StatusDescription);

						return decode(resp);
					}
				}
				catch (WebException ex)
				{
					if (ex.Status == WebExceptionStatus.ReceiveFailure && retryCount --> 0)
					{
						Logger.Trace("Receive failure, trying again!");
						continue;
					}

					throw;
				}
				catch (SocketException ex)
				{
					if (ex.SocketErrorCode == SocketError.Shutdown && retryCount --> 0)
					{
						Logger.Trace("Socket shutdown, trying again!");
						continue;
					}

					throw;
				}
				catch (ObjectDisposedException)
				{
					if (retryCount --> 0)
					{
						Logger.Trace("Socket disposed, trying again!");
						continue;
					}

					throw;
				}
			}
		}
	}
}
