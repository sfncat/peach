using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using NLog;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Publishers
{
	[Publisher("TcpListener")]
	[Alias("tcp.TcpListener")]
	[Parameter("Interface", typeof(IPAddress), "IP of interface to bind to")]
	[Parameter("Port", typeof(ushort), "Local port to listen on")]
	[Parameter("Lifetime", typeof(Test.Lifetime), "Lifetime of connection (Iteration, Session)", "Iteration")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait when receiving data (default 3000)", "3000")]
	[Parameter("SendTimeout", typeof(int), "How many milliseconds to wait when sending data (default infinite)", "-1")]
	[Parameter("AcceptTimeout", typeof(int), "How many milliseconds to wait for a connection (default 3000)", "3000")]
	[Parameter("ReadAvailableMode", typeof(bool), "During input operation wait 'Timeout' ms for first byte, then continue reading till end of available data (default false)", "false")]
	[Parameter("ReadAvailableTimeout", typeof(int), "Continue reading available data until timeout occurs waiting for data. Default 150ms.", "150")]
	public class TcpListenerPublisher : TcpPublisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		public IPAddress Interface { get; protected set; }
		public int AcceptTimeout { get; protected set; }
		public Test.Lifetime Lifetime { get; protected set; }

		protected TcpListener _listener = null;

		public TcpListenerPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override void OnOpen()
		{
			if (Lifetime == Test.Lifetime.Session && _listener != null)
				return;

			System.Diagnostics.Debug.Assert(_listener == null);

			try
			{
				_listener = new TcpListener(Interface, Port);
				_listener.Start();

				Port = (ushort)((IPEndPoint)_listener.LocalEndpoint).Port;
			}
			catch (Exception ex)
			{
				throw new PeachException("Error, unable to bind to interface " +
					Interface + " on port " + Port + ": " + ex.Message, ex);
			}

			base.OnOpen();
		}

		protected override void OnClose()
		{
			if (Lifetime == Test.Lifetime.Session)
			{
				lock (_clientLock)
				{
					if (_client == null)
						base.OnClose();
				}

				return;
			}

			if (_listener != null)
			{
				_listener.Stop();
				_listener = null;
			}

			base.OnClose();
		}

		protected override void OnStop()
		{
			if (Lifetime == Test.Lifetime.Session)
			{
				base.OnClose();

				if (_listener != null)
				{
					_listener.Stop();
					_listener = null;
				}
			}

			base.OnStop();
		}

		protected override void OnAccept()
		{
			lock (_clientLock)
			{
				if (Lifetime == Test.Lifetime.Session && _client != null && _tcp != null)
					return;
			}

			// Ensure any open stream is closed...
			base.OnClose();

			try
			{
				var ar = _listener.BeginAcceptTcpClient(null, null);
				if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(AcceptTimeout)))
					throw new TimeoutException("Timed out waiting for an incoming connection on interface {0} port {1}.".Fmt(Interface, Port));
				_tcp = _listener.EndAcceptTcpClient(ar);
			}
			catch (Exception ex)
			{
				throw new SoftException(ex);
			}

			// Start receiving on the client
			StartClient();
		}
	}
}
