using NLog;
using System;
using WebSocketSharp;
using Newtonsoft.Json;
using Peach.Core;
using System.Threading;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using LogLevel = NLog.LogLevel;

namespace Peach.Pro.Core.Agent.Channels.Rest
{
	class NLogLevelConverter : JsonConverter
	{
		static NLogLevelConverter()
		{
			Instance = new NLogLevelConverter();
		}

		public static NLogLevelConverter Instance { get; private set; }

		private NLogLevelConverter()
		{
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(LogLevel);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.None)
				return null;
			// { "foo":12345 }
			var name = (string)serializer.Deserialize(reader, typeof(string));

			return LogLevel.FromString(name);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value == null)
				return;

			var level = (LogLevel)value;

			serializer.Serialize(writer, level.Name);
		}
	}

	class LogSink : IDisposable
	{
		private readonly string _name;
		private readonly NLog.Logger _logger;
		private readonly NLog.Logger _chain;
		private AutoResetEvent _evtReady;
		private AutoResetEvent _evtClosed;
		private AutoResetEvent _evtFlushed;
		private readonly SortedDictionary<long, LogEventInfo> _pending;
		private long _expectId;
		private WebSocket _ws;
		private bool _isClosed;

		public LogSink(string name)
		{
			_name = name;
			_logger = LogManager.GetCurrentClassLogger();
			_chain = LogManager.GetLogger("Agent." + name);
			_pending = new SortedDictionary<long, LogEventInfo>();
		}

		public void Start(Uri baseUri)
		{
			_logger.Trace("Start>");

			Stop();

			_expectId = 0;
			_evtReady = new AutoResetEvent(false);
			_evtClosed = new AutoResetEvent(false);

			var url = "ws://{0}:{1}{2}?level={3}".Fmt(
				baseUri.Host,
				baseUri.Port,
				Server.LogPath,
				Configuration.LogLevel);

			_ws = new WebSocket(url, "log")
			{
				Log = { Output = OnWebSocketLog }
			};
			//_ws.Log.Level = WebSocketSharp.LogLevel.Debug;

			_ws.OnOpen += OnOpen;
			_ws.OnMessage += OnMessage;
			_ws.OnError += OnError;
			_ws.OnClose += OnClose;

			//_ws.Compression = CompressionMethod.DEFLATE;

			Retry.Backoff(TimeSpan.FromSeconds(1), 30, () =>
			{
				// This will prevent requiring users to add a TcpPortMonitor
				// to wait for remote agents to become available.
				// It will also prevent the log websocket from hanging
				// when it tries to connect too soon.

				var urlGet = "http://{0}:{1}{2}".Fmt(
					baseUri.Host,
					baseUri.Port,
					Server.LogPath);

				_logger.Trace("Attempting to GET '{0}'", urlGet);

				var req = (HttpWebRequest)WebRequest.Create(urlGet);
				req.Method = "GET";
				req.ServicePoint.Expect100Continue = false;
				req.Timeout = 1000;

				using (var resp = (HttpWebResponse)req.GetResponse())
				{
					if (resp.StatusCode != HttpStatusCode.OK)
						throw new PeachException("Logging service not available");
				}
			});

			_ws.Connect();

			if (!_evtReady.WaitOne(TimeSpan.FromSeconds(10)))
				throw new SoftException("Timeout waiting for remote logging service to start");
		}

		private void OnWebSocketLog(LogData logData, string msg)
		{
			var level = LogLevel.Info;
			switch (logData.Level)
			{
				case WebSocketSharp.LogLevel.Debug:
				case WebSocketSharp.LogLevel.Fatal:
				case WebSocketSharp.LogLevel.Error:
					level = LogLevel.Debug;
					break;
				case WebSocketSharp.LogLevel.Info:
					level = LogLevel.Info;
					break;
				case WebSocketSharp.LogLevel.Trace:
					level = LogLevel.Trace;
					break;
				case WebSocketSharp.LogLevel.Warn:
					level = LogLevel.Warn;
					break;
			}
			_logger.Log(level, msg);
		}

		public void Stop()
		{
			_logger.Trace("Stop>");

			if (_ws != null)
			{
				_evtFlushed = new AutoResetEvent(false);

				try
				{
					_ws.Send("Flush");
					if (!_evtFlushed.WaitOne(TimeSpan.FromSeconds(10)))
						_logger.Warn("Timeout waiting for remote logging service to flush");
				}
				finally
				{
					_evtFlushed.Dispose();
					_evtFlushed = null;
				}

				if (!_isClosed)
				{
					_ws.Close();

					if (!_evtClosed.WaitOne(TimeSpan.FromSeconds(10)))
						_logger.Warn("Timeout waiting for remote logging service to stop");

					_isClosed = true;
				}

				Dispose();
			}
		}

		void OnOpen(object sender, EventArgs e)
		{
			_logger.Trace("OnOpen>");
			_evtReady.Set();
		}

		void OnClose(object sender, CloseEventArgs e)
		{
			_logger.Trace("OnClose> WasClean: {0}, Code: {1}, Reason: {2}",
				e.WasClean,
				e.Code,
				e.Reason);
			_evtClosed.Set();
			if (_evtFlushed != null)
				_evtFlushed.Set();
		}

		void OnError(object sender, ErrorEventArgs e)
		{
			_logger.Debug("OnError> {0}", e.Message);
			_evtReady.Set();
			if (_evtFlushed != null)
				_evtFlushed.Set();
		}

		void OnMessage(object sender, MessageEventArgs e)
		{
			_logger.Trace("OnMessage>");
			try
			{
				var logEvent = JsonConvert.DeserializeObject<LogEventInfo>(e.Data, NLogLevelConverter.Instance);
				lock (this)
				{
					ProcessMessage(logEvent);
				}
			}
			catch (Exception ex)
			{
				_logger.Error("OnMessage> Error: Could not deserialize logEvent: {0}", ex.Message);
			}
		}

		void ProcessMessage(LogEventInfo logEvent)
		{
			var id = Convert.ToInt64(logEvent.Properties["ID"]);
			if (id == -1)
			{
				foreach (var kv in _pending)
				{
					ProcessEvent(kv.Value);
					_expectId = kv.Key + 1;
				}
				_pending.Clear();
				if (_evtFlushed != null)
					_evtFlushed.Set();
			}
			else
			{
				if (!_pending.ContainsKey(id))
					_pending.Add(id, logEvent);

				ProcessPending();
			}
		}

		void ProcessPending()
		{
			while (true)
			{
				LogEventInfo logEvent;
				if (!_pending.TryGetValue(_expectId, out logEvent))
					break;

				_pending.Remove(_expectId);
				ProcessEvent(logEvent);
				_expectId++;
			}
		}

		void ProcessEvent(LogEventInfo logEvent)
		{
			logEvent.LoggerName = "[{0}] {1}".Fmt(_name, logEvent.LoggerName);
			logEvent.Properties.Add("PreventLoop", true);
			_chain.Log(logEvent);
		}

		public void Dispose()
		{
			if (_ws != null)
			{
				if (!_isClosed)
				{
					_ws.Close();
					_isClosed = true;
				}

				_ws.OnOpen -= OnOpen;
				_ws.OnMessage -= OnMessage;
				_ws.OnError -= OnError;
				_ws.OnClose -= OnClose;

				_ws = null;
			}

			if (_evtReady != null)
			{
				_evtReady.Dispose();
				_evtReady = null;
			}

			if (_evtClosed != null)
			{
				_evtClosed.Dispose();
				_evtClosed = null;
			}

			_pending.Clear();
		}
	}
}
