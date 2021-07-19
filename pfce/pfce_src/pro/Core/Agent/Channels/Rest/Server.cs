//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Pro.Core.Runtime;

namespace Peach.Pro.Core.Agent.Channels.Rest
{
	[AgentServer("tcp")]
	public class Server : IAgentServer
	{
		internal const string PublisherPath = "/pa/publisher";
		internal const string MonitorPath = "/pa/agent";
		internal const string FilePath = "/pa/file";
		internal const string LogPath = "/pa/log";

		public const ushort DefaultPort = 9001;

		private const string PortOption = "--port=";

		private Listener _listener;

		private void CancelEventHandler(object sender, ConsoleCancelEventArgs args)
		{
			args.Cancel = true;

			Stop();
		}

		/// <summary>
		/// Raised when the listener is initialized but
		/// before starting to service requests.
		/// </summary>
		public EventHandler Started;

		/// <summary>
		/// Raised when the listener stops after servicing
		/// the last request.
		/// </summary>
		public EventHandler Stopped;

		/// <summary>
		/// The URI of the listener.  Is only valid after the
		/// Started event is raised and before the Stopped
		/// event is raised.
		/// </summary>
		public Uri Uri
		{
			get
			{
				if (_listener == null)
					throw new InvalidOperationException();

				return _listener.Uri;
			}
		}

		/// <summary>
		/// Run the REST agent server on the command line.
		/// Status messages will be displayed to the console.
		/// </summary>
		/// <param name="args">Command line arguments</param>
		public void Run(Dictionary<string, string> args)
		{
			var port = DefaultPort;

			foreach (var val in args.Values)
			{
				if (!val.StartsWith(PortOption))
					continue;

				var opt = val.Substring(PortOption.Length);
				if (!ushort.TryParse(opt, out port))
					throw new PeachException("An invalid option for --port was specified.  The value '{0}' is not a valid port number.".Fmt(opt));
			}

			Started += (s, e) =>
			{
				ConsoleWatcher.WriteInfoMark();
				Console.WriteLine("Listening for connections on port {0}", _listener.Uri.Port);
				Console.WriteLine();
				ConsoleWatcher.WriteInfoMark();
				Console.WriteLine("Press Ctrl-C to exit.");

				Console.CancelKeyPress += CancelEventHandler;
			};

			Stopped += (s, e) =>
			{
				Console.CancelKeyPress -= CancelEventHandler;
			};

			Run(port);
		}

		/// <summary>
		/// Run the REST agent server on the specified port.
		/// No status messages are displayed and the function
		/// will not complete until Stop() is called.
		/// </summary>
		/// <param name="port">Port to listen on</param>
		public void Run(ushort port)
		{
			Run(port, port);
		}

		/// <summary>
		/// Run the REST agent server on the first free port,
		/// starting with minport and searching until maxport.
		/// No status messages are displayed and the function
		/// will not complete until Stop() is called.
		/// </summary>
		/// <param name="minport">First port to try</param>
		/// <param name="maxport">Last port to try</param>
		public void Run(ushort minport, ushort maxport)
		{
			if (minport == 0 || minport > maxport)
				throw new ArgumentOutOfRangeException("minport");

			Debug.Assert(_listener == null);

			while (true)
			{
				try
				{
					var prefix = "http://+:{0}/".Fmt(minport);

					_listener = Listener.Create(prefix);

					break;
				}
				catch (Exception)
				{
					if (++minport > maxport)
						throw;
				}
			}

			try
			{
				using (new LogHandler(_listener.Routes))
				using (new MonitorHandler(_listener.Routes))
				using (new PublisherHandler(_listener.Routes))
				{
					if (Started != null)
						Started(this, EventArgs.Empty);
					_listener.Start();
				}
			}
			finally
			{
				if (Stopped != null)
					Stopped(this, EventArgs.Empty);

				_listener.Dispose();
				_listener = null;
			}
		}

		/// <summary>
		/// Stops the listener from accepting new requests.
		/// Causes Run() to complete.
		/// </summary>
		public void Stop()
		{
			if (_listener != null)
				_listener.Stop();
		}
	}
}
