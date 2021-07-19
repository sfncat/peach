

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using NLog;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Publishers
{
	public enum TcpClientRetry
	{
		/// <summary>
		/// Never retry connection to remote host
		/// </summary>
		Never,
		/// <summary>
		/// Retry on first connection or after fault.
		/// </summary>
		FirstAndAfterFault,
		/// <summary>
		/// Always retry connection.
		/// </summary>
		Always,
	}

	[Publisher("Tcp")]
	[Alias("TcpClient")]
	[Alias("tcp.Tcp")]
	[Parameter("Host", typeof(string), "Hostname or IP address of remote host")]
	[Parameter("Port", typeof(ushort), "Remote port to connect to")]
	[Parameter("RetryMode", typeof(TcpClientRetry), "Connection retry method, defaults to FirstAndAfterFault", "FirstAndAfterFault")]
	[Parameter("FaultOnConnectionFailure", typeof(bool), "Log a fault when unable to connect to remote host.  Defaults to true.", "true")]
	[Parameter("Lifetime", typeof(Test.Lifetime), "Lifetime of connection (Iteration, Session)", "Iteration")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait when receiving data (default 3000)", "3000")]
	[Parameter("SendTimeout", typeof(int), "How many milliseconds to wait when sending data (default infinite)", "-1")]
	[Parameter("ConnectTimeout", typeof(int), "Max milliseconds to wait for connection (default 10000)", "10000")]
	[Parameter("ReadAvailableMode", typeof(bool), "During input operation wait 'Timeout' ms for first byte, then continue reading till end of available data (default false)", "false")]
	[Parameter("ReadAvailableTimeout", typeof(int), "Continue reading available data until timeout occurs waiting for data. Default 150ms.", "150")]
	public class TcpClientPublisher : TcpPublisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		public string Host { get; protected set; }
		public int ConnectTimeout { get; protected set; }
		public Test.Lifetime Lifetime { get; protected set; }
		public TcpClientRetry RetryMode { get; protected set; }
		public bool FaultOnConnectionFailure { get; protected set; }

		public TcpClientPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected void Connect()
		{
			var timeout = ConnectTimeout;
			var sw = new Stopwatch();
			var retryConnection = true;

			switch (RetryMode)
			{
				case TcpClientRetry.Never:
					retryConnection = false;
					break;
				
				case TcpClientRetry.FirstAndAfterFault:
					retryConnection = IsControlRecordingIteration || IsIterationAfterFault;
					break;

				case TcpClientRetry.Always:
				default:
					retryConnection = true;
					break;
			}

			for (var i = 1; _tcp == null; i *= 2)
			{
				try
				{
					// Must build a new client object after every failed attempt to connect.
					// For some reason, just calling BeginConnect again does not work on mono.
					_tcp = new TcpClient();

					sw.Restart();

					var ar = _tcp.BeginConnect(Host, Port, null, null);
					if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeout)))
						throw new TimeoutException("Timed out connecting to remote host {0} port {1}.".Fmt(Host, Port));

					_tcp.EndConnect(ar);
				}
				catch (Exception ex)
				{
					sw.Stop();

					if (_tcp != null)
					{
						_tcp.Close();
						_tcp = null;
					}

					timeout -= (int)sw.ElapsedMilliseconds;

					if (retryConnection && timeout > 0)
					{
						var waitTime = Math.Min(timeout, i);
						timeout -= waitTime;

						Logger.Debug("Unable to connect to remote host {0} on port {1}.  Trying again in {2}ms...", Host, Port, waitTime);
						Thread.Sleep(waitTime);

						continue;
					}

					Logger.Debug("Unable to connect to remote host {0} on port {1}.", Host, Port);

					if(!FaultOnConnectionFailure || IsControlIteration || IsControlRecordingIteration)
						throw new SoftException(ex);

					var fault = new FaultSummary
					{
						Title = "Unable to connect to remote host {0} on port {1}.".Fmt(Host, Port),
						Description = 
@"Peach was unable to create a TCP connection to a remote host.

Host: {0}
Port: (1)

This usually means the device/software under test:

 1. Crashed or exited during testing
 2. Overwhelmed and could not respond correctly 
 3. In an invalid state and non responsive 
 4. Had just restarted and was unable to process the request 

This can happen during testing when a series of test cases cause the 
target service to misbehave or even crash.

Extended error information:

{2}".Fmt(Host, Port, ex.Message),

						MajorHash = FaultSummary.Hash("{0}:{1}".Fmt(Host,Port)),
						MinorHash = FaultSummary.Hash("{0}:{1}:{2}".Fmt(Host,Port,Iteration)),
						Exploitablity = "Unknown"
					};

					throw new FaultException(fault);
				}
			}

			StartClient();
		}

		protected override void OnStart()
		{
			base.OnStart();

			if (Lifetime == Test.Lifetime.Session)
				Connect();
		}

		protected override void OnStop()
		{
			if (Lifetime == Test.Lifetime.Session)
				base.OnClose();

			base.OnStop();
		}

		protected override void OnOpen()
		{
			// Complete socket shutdown if CloseClient was called
			// but not OnClose.
			// Note: CloseClient can happen after OnClose is called
			//  so this code is required in both OnOpen and OnClose.
			lock (_clientLock)
			{
				if (_client == null && _buffer != null)
					base.OnClose();
			}

			if (Lifetime == Test.Lifetime.Iteration || _tcp == null ||  _tcp.Connected == false)
			{
				base.OnOpen();
				Connect();
			}
		}

		protected override void OnClose()
		{
			if (Lifetime == Test.Lifetime.Iteration)
				base.OnClose();

			// _client will be null if CloseClient was called
			// so we need to complete the shutdown
			lock (_clientLock)
			{
				if (_client == null)
					base.OnClose();
			}
		}
	}
}
