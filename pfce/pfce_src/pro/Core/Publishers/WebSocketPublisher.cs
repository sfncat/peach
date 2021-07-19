
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json.Linq;
using NLog;
using Peach.Core;
using Peach.Core.IO;
using System.ComponentModel;
using System.Diagnostics;
using vtortola.WebSockets;

#pragma warning disable 4014

namespace Peach.Pro.Core.Publishers
{
	[Publisher("WebSocket")]
	[Description("WebSocket Publisher")]
	[Parameter("Port", typeof(int), "Port to listen for connections on", "8080")]
	[Parameter("Template", typeof(string), "Data template for publishing")]
	[Parameter("Publish", typeof(string), "How to publish data, base64 or url.", "base64")]
	[Parameter("DataToken", typeof(string), "Token to replace with data in template", "##DATA##")]
	[Parameter("Timeout", typeof(int), "Time in milliseconds to wait for client response", "60000")]
	public class WebSocketPublisher : Publisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		readonly WebSocketListener _socketServer;
		readonly BufferBlock<string> _msgQueue = new BufferBlock<string>();

		readonly AutoResetEvent _evaluated = new AutoResetEvent(false);
		readonly ManualResetEvent _clientReady = new ManualResetEvent(false);

		private readonly CancellationTokenSource _cancelAccept = new CancellationTokenSource();

		public int Port { get; protected set; }
		public string Template { get; protected set; }
		public string Publish { get; protected set; }
		public string DataToken { get; protected set; }
		public int Timeout { get; protected set; }

		string _template = null;
		readonly JObject _jsonTemplateMessage = new JObject();


		public WebSocketPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			_socketServer = new WebSocketListener(new IPEndPoint(IPAddress.Any, Port));
			var rfc6455 = new vtortola.WebSockets.Rfc6455.WebSocketFactoryRfc6455(_socketServer);
			_socketServer.Standards.RegisterStandard(rfc6455);

			_template = File.ReadAllText(Template);

			_jsonTemplateMessage["type"] = "template";
		}

		static async Task AcceptWebSocketClientAsync(WebSocketListener server, CancellationToken token,
			BufferBlock<string> queue, EventWaitHandle clientReady, EventWaitHandle evaluated)
		{
			CancellationTokenSource cancelConnection = null;
			Task reader = null;
			Task writer = null;

			while (!token.IsCancellationRequested)
			{
				try
				{
					var ws = await server.AcceptWebSocketAsync(token).ConfigureAwait(false);
					if (ws == null) continue;

					if (cancelConnection != null)
					{
						logger.Debug("New web socket connection. Closing down existing connection.");

						cancelConnection.Cancel();

						// Wait to see if our task threads will exit okay.
						// We want to avoid having an old reader thread that sets clientReady or evaluated.
						// Also avoid our writer thread de-queuing from queue.

						if (reader != null)
							reader.Wait(1000);

						if (writer != null)
							writer.Wait(1000);
					}
					else
					{
						logger.Debug("New web socket connection");
					}

					cancelConnection = new CancellationTokenSource();

					reader = Task.Run(() => HandleConnectionAsync(ws, cancelConnection.Token, clientReady, evaluated));
					writer = Task.Run(() => HandleSendQueueAsync(ws,  cancelConnection.Token, queue));
				}
				catch (Exception aex)
				{
					logger.Debug("Error Accepting clients: {0}", aex.GetBaseException().Message);
				}
			}

			if(cancelConnection != null)
				cancelConnection.Cancel();

			logger.Debug("Server Stop accepting clients");
		}

		static async Task HandleConnectionAsync(WebSocket ws, CancellationToken cancellation, EventWaitHandle clientReady, EventWaitHandle evaluated)
		{
			try
			{
				if(cancellation.IsCancellationRequested)
					logger.Debug("HandleConnectionAsync, IsCancellationRequested == true");

				while (ws.IsConnected && !cancellation.IsCancellationRequested)
				{
					var msg = await ws.ReadStringAsync(cancellation).ConfigureAwait(false);
					if (msg == null) continue;

					logger.Trace("NewMessageReceived: {0}", msg);

					var json = JObject.Parse(msg);
					if ((string) json["msg"] == "Client ready")
					{
						logger.Debug("Client ready message received");
						clientReady.Set();
					}
					else if ((string) json["msg"] == "Evaluation complete")
					{
						logger.Debug("Evaluated message received");
						evaluated.Set();
					}
					else
					{
						logger.Debug("Unknown message received: {0}", msg);
					}
				}
			}
			catch (Exception aex)
			{
				logger.Debug("Error Handling connection: {0}", aex.GetBaseException().Message);
				try { ws.Close(); }
				catch { }
			}
			finally
			{
				clientReady.Reset();
				ws.Dispose();
			}
		}

		static async Task HandleSendQueueAsync(WebSocket ws, CancellationToken cancellation, BufferBlock<string> queue)
		{
			try
			{
				while (ws.IsConnected && !cancellation.IsCancellationRequested)
				{
					var msg = await queue.ReceiveAsync(cancellation);
					if (msg == null) continue;

					logger.Trace("Dequeued and sending message");
					ws.WriteString(msg);
				}
			}
			catch (Exception aex)
			{
				logger.Debug("Error handling queue: {0}", aex.GetBaseException().Message);
				try { ws.Close(); }
				catch { }
			}
			finally
			{
				ws.Dispose();
			}
		}

		protected override void OnStart()
		{
			base.OnStart();

			_socketServer.Start();
			Task.Run(() => AcceptWebSocketClientAsync(_socketServer, _cancelAccept.Token, 
				_msgQueue, _clientReady, _evaluated));
		}

		protected override void OnStop()
		{
			base.OnStop();

			_cancelAccept.Cancel();
			_socketServer.Stop();
		}

		protected override void OnOpen()
		{
			base.OnOpen();

			IList<string> msgs;
			_msgQueue.TryReceiveAll(out msgs);
			_evaluated.Reset();

			if (!_clientReady.WaitOne(Timeout))
				throw new SoftException("Timeout waiting for web socket connection.");
		}

		protected override void OnOutput(BitwiseStream data)
		{
			_jsonTemplateMessage["content"] = BuildTemplate(data);
			var msg = _jsonTemplateMessage.ToString(Newtonsoft.Json.Formatting.None) + "\n";

			_msgQueue.Post(msg);

			var sw = Stopwatch.StartNew();
			while (sw.ElapsedMilliseconds < Timeout)
			{
				if (!_clientReady.WaitOne(0))
					throw new SoftException("Web socket connection lost.");

				if (_evaluated.WaitOne(200))
					return;
			}

			throw new SoftException("Timeout waiting for WebSocket evaluated.");
		}

		protected string BuildTemplate(BitwiseStream data)
		{
			var value = Publish;

			if (Publish == "base64")
			{
				data.Seek(0, SeekOrigin.Begin);
				var buf = new BitReader(data).ReadBytes((int)data.Length);
				value = Convert.ToBase64String(buf);
			}

			return _template.Replace(DataToken, value);
		}
	}
}

#pragma warning restore 4014

// end
