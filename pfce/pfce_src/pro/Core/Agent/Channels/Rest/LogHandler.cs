using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using SocketHttpListener;
using SocketHttpListener.Net;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Peach.Pro.Core.Agent.Channels.Rest
{
	class LogHandler : IDisposable
	{
		private readonly List<LogResponse> _responses = new List<LogResponse>();
		private readonly LogTarget _target;
		private readonly LoggingRule _rule;
		private readonly RouteHandler _routes;

		public LogHandler(RouteHandler routes)
		{
			_target = new LogTarget { Name = "RestLogTarget" };
			_rule = new LoggingRule("*", LogLevel.Trace, _target);
			_routes = routes;

			var config = LogManager.Configuration;
			config.AddTarget(_target.Name, _target);
			config.LoggingRules.Add(_rule);
			LogManager.Configuration = config;

			_routes.Add(Server.LogPath, "GET", OnSubscribe);
		}

		public void Dispose()
		{
			var config = LogManager.Configuration;
			config.LoggingRules.Remove(_rule);
			config.RemoveTarget(_target.Name);
			LogManager.Configuration = config;

			_target.Dispose();

			foreach (var resp in _responses)
			{
				resp.Dispose();
			}

			_routes.Remove(Server.LogPath);
		}

		private RouteResponse OnSubscribe(HttpListenerRequest req)
		{
			// a normal HTTP GET will be used to probe that this service is available
			if (!req.IsWebSocketRequest)
				return RouteResponse.Success();

			var levelName = req.QueryString.Get("level");
			if (string.IsNullOrEmpty(levelName))
				levelName = "Info";
			var level = LogLevel.FromString(levelName);

			var response = new LogResponse(_target, level);
			_responses.Add(response);
			return response;
		}
	}

	class LogResponse : RouteResponse, IDisposable
	{
		private WebSocket _ws;
		private readonly LogTarget _target;
		private readonly LogLevel _level;
		private int _counter = 0;
		private volatile int _pending = 0;
		private AutoResetEvent _evt;

		public LogResponse(LogTarget target, LogLevel level)
		{
			_target = target;
			_level = level;

			_evt = new AutoResetEvent(false);
		}

		public override void Complete(HttpListenerContext ctx)
		{
			_ws = ctx.AcceptWebSocket("log").WebSocket;
			_ws.OnMessage += OnMessage;
			_ws.OnClose += OnClose;
			_ws.ConnectAsServer();
			_target.Add(this);
		}

		void OnMessage(object sender, MessageEventArgs e)
		{
			Flush();
		}

		private void OnClose(object sender, CloseEventArgs e)
		{
			_target.Remove(this);
			_ws = null;
		}

		public void Dispose()
		{
			if (_ws != null)
			{
				_target.Remove(this);
				_ws.Close();
				_ws = null;
			}
		}

		private void Flush()
		{
			while (_pending > 0)
			{
				_evt.WaitOne(TimeSpan.FromSeconds(1));
			}

			var logEvent = new LogEventInfo(LogLevel.Off, "$LogResponse", "Flushed");
			logEvent.Properties["ID"] = -1;

			var json = JsonConvert.SerializeObject(logEvent, NLogLevelConverter.Instance);
			_ws.SendAsync(json, null);
		}

		public void Log(LogEventInfo logEvent)
		{
			try
			{
				if (logEvent.Level >= _level)
				{
					var id = _counter++;
					logEvent.Properties["ID"] = id;
					if (logEvent.Parameters != null)
					{
						for (int i = 0; i < logEvent.Parameters.Length; i++)
						{
							if (logEvent.Parameters[i] != null)
								logEvent.Parameters[i] = logEvent.Parameters[i].ToString();
						}
					}

					var json = JsonConvert.SerializeObject(logEvent, NLogLevelConverter.Instance);

					_pending++;
					_ws.SendAsync(json, (done) =>
					{
						_pending--;
						_evt.Set();
					});
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("LogReponse.Log> Exception: {0}", ex.Message);
			}
		}
	}

	class LogTarget : TargetWithLayout
	{
		private readonly List<LogResponse> _loggers = new List<LogResponse>();
		private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

		public void Add(LogResponse logger)
		{
			_lock.EnterWriteLock();
			try { _loggers.Add(logger); }
			finally { _lock.ExitWriteLock(); }
		}

		public void Remove(LogResponse logger)
		{
			_lock.EnterWriteLock();
			try { _loggers.Remove(logger); }
			finally { _lock.ExitWriteLock(); }
		}

		protected override void Write(LogEventInfo logEvent)
		{
			// prevent logging loops
			if (logEvent.Properties.ContainsKey("PreventLoop"))
				return;

			_lock.EnterReadLock();
			try { _loggers.ForEach(log => log.Log(logEvent)); }
			finally { _lock.ExitReadLock(); }
		}
	}
}
