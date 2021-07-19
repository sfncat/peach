using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using NLog;
using Peach.Core;

namespace Peach.Pro.Core.Publishers
{
	[Publisher("SslListener")]
	[Parameter("Interface", typeof(IPAddress), "IP of interface to bind to")]
	[Parameter("Port", typeof(ushort), "Local port to listen on")]
	[Parameter("ServerCertPath", typeof(string), "Path to server certificate file")]
	[Parameter("ServerCertPass", typeof(string), "Password for cert file", "")]
	[Parameter("ClientCertRequired", typeof(bool), "Require client to auth via certificate", "false")]
	[Parameter("CheckCertRevocation", typeof(bool), "Check revocation of certificate", "false")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data (default 3000)", "3000")]
	[Parameter("AcceptTimeout", typeof(int), "How many milliseconds to wait for a connection (default 3000)", "3000")]
	public class SslListenerPublisher : Peach.Core.Publishers.BufferedStreamPublisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		public IPAddress Interface { get; protected set; }
		public ushort Port { get; protected set; }
		public int AcceptTimeout { get; protected set; }
		public string ServerCertPath { get; protected set; }
		public string ServerCertPass { get; protected set; }
		public bool ClientCertRequired { get; protected set; }
		public bool CheckCertRevocation { get; protected set; }

		protected TcpClient _tcp = null;
		protected TcpListener _listener = null;
		protected EndPoint _localEp = null;
		protected EndPoint _remoteEp = null;
		private X509Certificate2 _serverCertificate = null;

		private SslStream _sslStream = null;

		public SslListenerPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			// On mono, sometimes OnOutput waits forever. Attempt to work around this.
			_sendTimeout = 10000;
			try
			{
				//Mono fix. If the password is empty and still provided it will explode trying to decrypt.
				if (String.IsNullOrEmpty(ServerCertPass))
					_serverCertificate = new X509Certificate2(ServerCertPath);
				else
					_serverCertificate = new X509Certificate2(ServerCertPath, ServerCertPass);
			}
			catch (Exception ex)
			{
				throw new PeachException(string.Format("Error, unable to load certificate '{0}': ",
					ServerCertPath), ex);
			}
		}

		protected override void OnOpen()
		{
			base.OnOpen();
			System.Diagnostics.Debug.Assert(_listener == null);

			try
			{
				_listener = new TcpListener(Interface, Port);
				_listener.Start();
			}
			catch (Exception ex)
			{
				throw new PeachException("Error, unable to bind to interface " +
					Interface + " on port " + Port + ": " + ex.Message, ex);
			}
		}

		protected override void OnClose()
		{
			if (_listener != null)
			{
				_listener.Stop();
				_listener = null;
			}

			base.OnClose();
		}

		protected override void OnAccept()
		{
			// Ensure any open stream is closed...
			base.OnClose();

			try
			{
				var ar = _listener.BeginAcceptTcpClient(null, null);
				if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(AcceptTimeout)))
					throw new TimeoutException();
				_tcp = _listener.EndAcceptTcpClient(ar);
				_tcp.SendTimeout = Timeout;
				_tcp.ReceiveTimeout = Timeout;
			}
			catch (Exception ex)
			{
				throw new SoftException(ex);
			}

			// Start receiving on the client
			StartClient();
		}

		protected override void StartClient()
		{
			System.Diagnostics.Debug.Assert(_tcp != null);
			System.Diagnostics.Debug.Assert(_client == null);
			System.Diagnostics.Debug.Assert(_localEp == null);
			System.Diagnostics.Debug.Assert(_remoteEp == null);
			System.Diagnostics.Debug.Assert(_sslStream == null);

			try
			{
				_sslStream = new SslStream(_tcp.GetStream(), false);

				_sslStream.AuthenticateAsServer(_serverCertificate, ClientCertRequired, SslProtocols.Default, CheckCertRevocation);
				_sslStream.ReadTimeout = Timeout;
				_sslStream.WriteTimeout = Timeout;
				_client = _sslStream;

				_localEp = _tcp.Client.LocalEndPoint;
				_remoteEp = _tcp.Client.RemoteEndPoint;
				_clientName = _remoteEp.ToString();
			}
			catch (Exception ex)
			{
				Logger.Error("SSL Stream failed to start. {0}.", ex.Message);
				throw new SoftException(ex);
			}
			base.StartClient();
		}

		protected override void ClientClose()
		{
			_sslStream.Close();
			_sslStream = null;
			_tcp = null;
			_remoteEp = null;
			_localEp = null;
		}

		protected override void ClientShutdown()
		{
			_tcp.Client.Shutdown(SocketShutdown.Send);
		}
	}
}
