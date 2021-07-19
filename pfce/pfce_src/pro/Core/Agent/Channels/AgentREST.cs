

// Authors:
//   Adam Cecchetti (adam@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Publishers;

namespace Peach.Pro.Core.Agent.Channels
{
	[Agent("http")]
	public class AgentClientRest : AgentClient
	{
		#region Models

		[Serializable]
		class JsonResponse
		{
			public string Status { get; set; }
			public string Data { get; set; }
			public Dictionary<string, object> Results { get; set; }
		}

		[Serializable]
		class JsonFaultResponse
		{
			public string Status { get; set; }
			public string Data { get; set; }
			public Fault[] Results { get; set; }
		}

		[Serializable]
		class JsonArgsRequest
		{
			public Dictionary<string, string> args { get; set; }
		}

		[Serializable]
		class CreatePublisherRequest
		{
			public uint iteration { get; set; }
			public bool isControlIteration { get; set; }
			public bool isControlRecordingIteration { get; set; }
			public bool isIterationAfterFault { get; set; }
			public string Cls { get; set; }
			public Dictionary<string, string> args { get; set; }
		}

		[Serializable]
		class RestProxyPublisherResponse
		{
			public bool error { get; set; }
			public string errorString { get; set; }
		}

		[Serializable]
		class IterationRequest
		{
			public uint iteration { get; set; }
		}

		[Serializable]
		class IsControlIterationRequest
		{
			public bool isControlIteration { get; set; }
		}

		[Serializable]
		class IsControlRecordingIteration
		{
			public bool isControlRecordingIteration { get; set; }
		}

		[Serializable]
		class IsIterationAfterFault
		{
			public bool isIterationAfterFault { get; set; }
		}

		[Serializable]
		class OnCallArgument
		{
			public string name { get; set; }
			public byte[] data { get; set; }
			public ActionParameter.Type type { get; set; }
		}

		[Serializable]
		class OnCallRequest
		{
			public string method { get; set; }
			public OnCallArgument[] args { get; set; }
		}

		[Serializable]
		class OnCallResponse : RestProxyPublisherResponse
		{
			public byte[] value { get; set; }
		}

		[Serializable]
		class OnSetPropertyRequest
		{
			public string property { get; set; }
			public byte[] data { get; set; }
		}

		[Serializable]
		class OnGetPropertyResponse : RestProxyPublisherResponse
		{
			public byte[] value { get; set; }
		}

		[Serializable]
		class OnOutputRequest
		{
			public byte[] data { get; set; }
		}

		[Serializable]
		class WantBytesRequest
		{
			public long count { get; set; }
		}

		[Serializable]
		class ReadBytesRequest
		{
			public int count { get; set; }
		}

		[Serializable]
		class ReadBytesResponse : RestProxyPublisherResponse
		{
			public byte[] data { get; set; }
		}

		[Serializable]
		class ReadRequest
		{
			public int offset { get; set; }
			public int count { get; set; }
		}
		[Serializable]
		class ReadResponse : RestProxyPublisherResponse
		{
			public int count { get; set; }
			public byte[] data { get; set; }
		}

		[Serializable]
		class ReadByteResponse : RestProxyPublisherResponse
		{
			public int data { get; set; }
		}


		#endregion

		#region IPublisher

		class PublisherProxy : IPublisher
		{
			RestProxyPublisher publisher;

			public PublisherProxy(string serviceUrl, string cls, Dictionary<string, string> args)
			{
				publisher = new RestProxyPublisher(args)
				{
					Url = serviceUrl,
					Class = cls,
				};

				publisher.start();
			}

			#region IPublisher

			public Stream InputStream
			{
				get
				{
					return publisher;
				}
			}

			public void Dispose()
			{
				publisher.stop();
				publisher = null;
			}

			public void Open(uint iteration, bool isControlIteration, bool isControlRecordingIteration, bool isIterationAfterFault)
			{
				publisher.Iteration = iteration;
				publisher.IsControlIteration = isControlIteration;
				publisher.IsControlRecordingIteration = isControlRecordingIteration;
				publisher.IsIterationAfterFault = isIterationAfterFault;
				publisher.open();
			}

			public void Close()
			{
				publisher.close();
			}

			public void Accept()
			{
				publisher.accept();
			}

			public Variant Call(string method, List<BitwiseStream> args)
			{
				return publisher.call(method, args);
			}

			public void SetProperty(string property, Variant value)
			{
				publisher.setProperty(property, value);
			}

			public Variant GetProperty(string property)
			{
				return publisher.getProperty(property);
			}

			public void Output(BitwiseStream data)
			{
				publisher.output(data);
			}

			public void Input()
			{
				publisher.input();
			}

			public void WantBytes(long count)
			{
				publisher.WantBytes(count);
			}

			#endregion
		}

		#endregion

		#region Publisher Proxy

		class RestProxyPublisher : StreamPublisher
		{
			private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
			protected override NLog.Logger Logger { get { return logger; } }

			public string Url = null;
			public string Agent { get; set; }
			public string Class { get; set; }
			public Dictionary<string, string> Args { get; set; }

			/// <summary>
			/// Has the create publisher rest call been made?
			/// </summary>
			protected bool isCreated = false;

			protected bool isOpen = false;

			public RestProxyPublisher(Dictionary<string, string> args)
				: base(new Dictionary<string, Variant>())
			{
				Args = args;
				stream = new MemoryStream();
			}

			public string Send(string query)
			{
				return Send(query, "");
			}

			public string Send(string query, Dictionary<string, Variant> args)
			{
				var newArg = new Dictionary<string, string>();

				foreach (var kv in args)
				{
					// NOTE: cast to string, rather than .ToString() since
					// .ToString() can include debugging information.
					newArg.Add(kv.Key, (string)kv.Value);
				}

				JsonArgsRequest request = new JsonArgsRequest();
				request.args = newArg;

				return Send(query, JsonConvert.SerializeObject(request));
			}

			public string Send(string query, string json, bool restart = true)
			{
				try
				{
					var httpWebRequest = (HttpWebRequest)WebRequest.Create(Url + "/Publisher/" + query);
					httpWebRequest.ContentType = "text/json";

					if (string.IsNullOrEmpty(json))
					{
						httpWebRequest.Method = "GET";
					}
					else
					{
						httpWebRequest.Method = "POST";
						using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
						{
							streamWriter.Write(json);
						}
					}

					var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

					if (httpResponse.GetResponseStream() != null)
					{
						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
						{
							var jsonResponse = streamReader.ReadToEnd();
							var response = JsonConvert.DeserializeObject<RestProxyPublisherResponse>(jsonResponse);

							if (response.error && restart)
							{
								logger.Warn("Query \"" + query + "\" error, attempting to restart remote publisher: " + response.errorString);
								RestartRemotePublisher();

								jsonResponse = Send(query, json, false);
								response = JsonConvert.DeserializeObject<RestProxyPublisherResponse>(jsonResponse);

								if (response.error)
								{
									logger.Warn("Unable to restart connection");
									throw new SoftException("Query \"" + query + "\" error: " + response.errorString);
								}
							}
							else if (response.error)
							{
								logger.Error("Query \"" + query + "\" error: " + response.errorString);
								throw new SoftException("Query \"" + query + "\" error: " + response.errorString);
							}

							return jsonResponse;
						}
					}

					return "";
				}
				catch (SoftException)
				{
					throw;
				}
				catch (Exception e)
				{
					throw new SoftException("Failure communicating with REST Agent", e);
				}
			}

			protected void RestartRemotePublisher()
			{
				if (isCreated)
					logger.Debug("Restarting remote publisher");
				else
					logger.Debug("Starting remote publisher");

				var request = new CreatePublisherRequest();
				request.iteration = Iteration;
				request.isControlIteration = IsControlIteration;
				request.isControlRecordingIteration = IsControlRecordingIteration;
				request.isIterationAfterFault = IsIterationAfterFault;
				request.Cls = Class;
				request.args = Args;

				Send("CreatePublisher", JsonConvert.SerializeObject(request), false);

				isCreated = true;
			}

			protected override void OnStart()
			{
				if(!isCreated)
					RestartRemotePublisher();

				Send("start");
			}

			protected override void OnStop()
			{
				isOpen = false;
				Send("stop");
			}

			protected override void OnOpen()
			{
				isOpen = true;

				var req1 = new IterationRequest { iteration = Iteration };
				Send("Set_Iteration", JsonConvert.SerializeObject(req1));

				var req2 = new IsControlIterationRequest { isControlIteration = IsControlIteration };
				Send("Set_IsControlIteration", JsonConvert.SerializeObject(req2));

				var req3 = new IsControlRecordingIteration { isControlRecordingIteration = IsControlRecordingIteration };
				Send("Set_IsControlRecordingIteration", JsonConvert.SerializeObject(req3));

				var req4 = new IsIterationAfterFault { isIterationAfterFault = IsIterationAfterFault };
				Send("Set_IsIterationAfterFault", JsonConvert.SerializeObject(req4));

				Send("open");
			}

			protected override void OnClose()
			{
				isOpen = false;

				Send("close");
			}

			protected override void OnAccept()
			{
				Send("accept");
			}

			protected override Variant OnCall(string method, List<BitwiseStream> args)
			{
				if (!isOpen)
				{
					var req1 = new IterationRequest { iteration = Iteration };
					Send("Set_Iteration", JsonConvert.SerializeObject(req1));

					var req2 = new IsControlIterationRequest { isControlIteration = IsControlIteration };
					Send("Set_IsControlIteration", JsonConvert.SerializeObject(req2));

					var req3 = new IsControlRecordingIteration { isControlRecordingIteration = IsControlRecordingIteration };
					Send("Set_IsControlRecordingIteration", JsonConvert.SerializeObject(req3));

					var req4 = new IsIterationAfterFault { isIterationAfterFault = IsIterationAfterFault };
					Send("Set_IsIterationAfterFault", JsonConvert.SerializeObject(req4));
				}

				var request = new OnCallRequest();

				request.method = method;
				request.args = new OnCallArgument[args.Count];

				for (int cnt = 0; cnt < args.Count; cnt++)
				{
					request.args[cnt] = new OnCallArgument();
					request.args[cnt].name = args[cnt].Name;
					request.args[cnt].data = new byte[args[cnt].Length];
					args[cnt].Read(request.args[cnt].data, 0, (int)args[cnt].Length);
				}

				var json = Send("call", JsonConvert.SerializeObject(request));
				var response = JsonConvert.DeserializeObject<OnCallResponse>(json);

				return new Variant(response.value ?? new byte[0]);
			}

			protected override void OnSetProperty(string property, Variant value)
			{
				// The Engine always gives us a BitStream but we can't remote that

				var request = new OnSetPropertyRequest();

				request.property = property;
				request.data = (byte[])value;

				Send("setProperty", JsonConvert.SerializeObject(request));
			}

			protected override Variant OnGetProperty(string property)
			{
				var json = Send("getProperty",
					JsonConvert.SerializeObject(property));
				var response = JsonConvert.DeserializeObject<OnGetPropertyResponse>(json);
				return new Variant(response.value);
			}

			protected override void OnOutput(BitwiseStream data)
			{
				var request = new OnOutputRequest();
				request.data = new byte[data.Length];
				data.Read(request.data, 0, (int)data.Length);

				data.Position = 0;

				Send("output", JsonConvert.SerializeObject(request));
			}

			protected override void OnInput()
			{
				Send("input");

				stream.Position = 0;
				stream.SetLength(0);

				// No need to call WantBytes(1) since the cracker will do this for us!
			}

			public override void WantBytes(long count)
			{
				var needed = count - stream.Length + stream.Position;

				if (needed <= 0)
					return;

				var request = new WantBytesRequest();
				request.count = needed;

				var json = Send("WantBytes", JsonConvert.SerializeObject(request));
				var response = JsonConvert.DeserializeObject<ReadBytesResponse>(json);

				if (response.data == null)
					return;

				var pos = stream.Position;
				stream.Seek(0, SeekOrigin.End);
				stream.Write(response.data, 0, response.data.Length);
				stream.Seek(pos, SeekOrigin.Begin);
			}
		}

		#endregion

		#region Private Members

		static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		readonly string _serviceUrl;
		bool _monitors;

		#endregion

		#region Constructor

		public AgentClientRest(string name, string uri, string password)
			: base(name, uri, password)
		{
			_serviceUrl = new Uri(new Uri(uri), "/Agent").ToString();
		}

		#endregion

		#region AgentClient Overrides

		public override void AgentConnect()
		{
			Send("AgentConnect");
		}

		public override void AgentDisconnect()
		{
			Send("AgentDisconnect");
		}

		public override IPublisher CreatePublisher(string pubName, string cls, Dictionary<string, string> args)
		{
			return new PublisherProxy(_serviceUrl, cls, args);
		}

		public override void StartMonitor(string monName, string cls, Dictionary<string, string> args)
		{
			_monitors = true;
			Send(string.Format("StartMonitor?name={0}&cls={1}",
				 System.Web.HttpUtility.UrlEncode(monName),
				 System.Web.HttpUtility.UrlEncode(cls)), args);
		}

		public override void StopAllMonitors()
		{
			if (_monitors)
				Send("StopAllMonitors");
		}

		public override void SessionStarting()
		{
			if (_monitors)
				Send("SessionStarting");
		}

		public override void SessionFinished()
		{
			if (_monitors)
				Send("SessionFinished");
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			if (_monitors)
				Send(string.Format("IterationStarting?iterationCount=0&isReproduction={0}&lastWasFault={1}",
					args.IsReproduction,
					args.LastWasFault));
		}

		public override void IterationFinished()
		{
			if (_monitors)
				Send("IterationFinished");
		}

		public override bool DetectedFault()
		{
			if (!_monitors)
				return false;

			var json = Send("DetectedFault");
			return ParseResponse(json);
		}

		public override IEnumerable<MonitorData> GetMonitorData()
		{
			if (!_monitors)
				return new MonitorData[0];

			try
			{
				var json = Send("GetMonitorData");
				var response = JsonConvert.DeserializeObject<JsonFaultResponse>(json);

				var ret = response.Results.Select(AsMonitorData).ToList();
				foreach (var item in ret.Where(item => item.Fault != null))
				{
					item.Fault.MustStop = ParseResponse(Send("MustStop"));
				}
				return ret;
			}
			catch (Exception e)
			{
				Logger.Debug(e.ToString());
				throw new PeachException("Failed to get Monitor Data", e);
			}
		}

		public override void Message(string msg)
		{
			if (!_monitors)
				Send(string.Format("Message?msg={0}",
					System.Web.HttpUtility.UrlEncode(msg)));
		}

		#endregion

		#region Private Helpers

		private MonitorData AsMonitorData(Fault f)
		{
			var ret = new MonitorData
			{
				AgentName = Name,
				DetectionSource = f.detectionSource,
				MonitorName = f.monitorName,
				Title = f.title,
				Data = f.collectedData.ToDictionary(i => i.Key, i => (Stream)new MemoryStream(i.Value)),
			};

			if (f.type == FaultType.Fault)
			{
				ret.Fault = new MonitorData.Info
				{
					Description = f.description,
					MajorHash = f.majorHash,
					MinorHash = f.minorHash,
					Risk = f.exploitability,
					MustStop = f.mustStop,
				};
			}

			return ret;
		}

		bool ParseResponse(string json)
		{
			if (string.IsNullOrEmpty(json))
				throw new PeachException("Agent Response Empty");

			JsonResponse resp;

			try
			{
				resp = JsonConvert.DeserializeObject<JsonResponse>(json);
			}
			catch (Exception e)
			{
				throw new PeachException("Failed to deserialize JSON response from Agent", e);
			}

			return Convert.ToBoolean(resp.Status);
		}

		string Send(string query)
		{
			return Send(query, "");
		}

		string Send(string query, Dictionary<string, string> args)
		{
			var request = new JsonArgsRequest { args = args };

			return Send(query, JsonConvert.SerializeObject(request));
		}

		string Send(string query, string json)
		{
			try
			{

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(_serviceUrl + "/" + query);
				httpWebRequest.ContentType = "text/json";
				if (string.IsNullOrEmpty(json))
				{
					Logger.Debug("Send: GET {0}", _serviceUrl + "/" + query);
					httpWebRequest.Method = "GET";
				}
				else
				{
					Logger.Debug("Send: POST {0}", _serviceUrl + "/" + query);
					httpWebRequest.Method = "POST";
					using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
					{
						streamWriter.Write(json);
					}
				}
				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				Logger.Debug("Send: Got response");

				if (httpResponse.GetResponseStream() != null)
				{
					Logger.Debug("Send: Readoing to end of stream");
					using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
					{
						return streamReader.ReadToEnd();
					}
				}
				else
				{
					Logger.Debug("Send: No stream data");
					return "";
				}
			}
			catch (Exception e)
			{
				Logger.Debug("Send Failed: {0}", _serviceUrl + "/" + query);

				throw new PeachException("Failure communicating with REST Agent", e);
			}
		}

		#endregion
	}
}
