

using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Agent.Channels;
using Peach.Core.IO;
using Peach.Pro.Core.Runtime;
using Logger = NLog.Logger;

namespace Peach.Pro.Core.Agent.Channels
{
	#region TCP Agent Client

	[Agent("legacy")]
	public class AgentClientTcpRemoting : AgentClient
	{
		#region Publisher Proxy

		class PublisherProxy : IPublisher
		{
			#region Serialization Helpers

			private static byte[] ToBytes<T>(T t) where T: class
			{
				if (t == null)
					return null;

				var ms = new MemoryStream();
				var fmt = new BinaryFormatter();
				fmt.Serialize(ms, t);
				return ms.ToArray();
			}

			private static T FromBytes<T>(byte[] bytes) where T: class
			{
				if (bytes == null)
					return null;

				var ms = new MemoryStream(bytes);
				var fmt = new BinaryFormatter();
				var obj = fmt.Deserialize(ms);
				return (T)obj;
			}

			#endregion

			#region Run Action On Proxy

			private static void Exec(string what, Action action)
			{
				Exception remotingException = null;

				var th = new Thread(delegate()
				{
					try
					{
						action();
					}
					catch (RemotingException ex)
					{
						Logger.Trace("Ignoring remoting exception during {0}", what);
						Logger.Trace("\n{0}", ex);
					}
					catch (PeachException ex)
					{
						remotingException = new PeachException(ex.Message, ex);
					}
					catch (SoftException ex)
					{
						remotingException = new SoftException(ex.Message, ex);
					}
					catch (Exception ex)
					{
						remotingException = new AgentException(ex.Message, ex);
					}
				});

				th.Start();

				if (!th.Join(RemotingWaitTime))
				{
					th.Abort();
					th.Join();

					Logger.Trace("Ignoring remoting timeout during {0}", what);
				}

				if (remotingException != null)
					throw remotingException;
			}

			private static T Exec<T>(string what, Func<T> action)
			{
				T t = default(T);

				Exec(what, () => { t = action(); });

				return t;
			}

			#endregion

			#region Private Members

			PublisherTcpRemote _remotePub;
			readonly MemoryStream _stream;

			#endregion

			#region Public Members

			public string Name { get; private set; }

			public string Class { get; private set; }

			public List<KeyValuePair<string, string>> Args { get; private set; }

			public PublisherTcpRemote Proxy
			{
				private get
				{
					return _remotePub;
				}
				set
				{
					// Proxy initialized or changed due to reconnect.
					// Don't have to Exec() this in a worker thread
					// since its called from a worker thread inside
					// of AgentClientTcpRemoting
					_remotePub = value;

					// Reset the stream since we have a new remote publisher
					_stream.Seek(0, SeekOrigin.Begin);
					_stream.SetLength(0);
				}
			}

			#endregion

			#region Constructor

			public PublisherProxy(string name, string cls, Dictionary<string, string> args)
			{
				Name = name;
				Class = cls;
				Args = args.ToList();
				_stream = new MemoryStream();

			}

			#endregion

			#region IPublisher

			public Stream InputStream
			{
				get { return _stream; }
			}

			public void Dispose()
			{
				Exec("Stop", () => Proxy.Dispose());
			}

			public void Open(uint iteration, bool isControlIteration, bool isControlRecordingIteration, bool isIterationAfterFault)
			{
				//NOTE: not updating legacy code.  depricated and should get removed.

				Exec("Open", () => Proxy.Open(iteration, isControlIteration));
			}

			public void Close()
			{
				Exec("Close", () => Proxy.Close());
			}

			public void Accept()
			{
				Exec("Accept", () => Proxy.Accept());
			}

			public Variant Call(string method, List<BitwiseStream> args)
			{
				throw new NotSupportedException();
			}

			public void SetProperty(string property, Variant value)
			{
				var bytes = ToBytes(value);

				Exec("SetProperty", () => Proxy.SetProperty(property, bytes));
			}

			public Variant GetProperty(string property)
			{
				var bytes = Exec("GetProperty", () => Proxy.GetProperty(property));

				return FromBytes<Variant>(bytes);
			}

			public void Output(BitwiseStream data)
			{
				Exec("BeginOutput", () => Proxy.BeginOutput());

				var total = data.Length;
				var len = Math.Min(total - data.Position, 1024 * 1024);

				while (len > 0)
				{
					var buf = new byte[len];
					data.Read(buf, 0, buf.Length);

					Exec("Output", () => Proxy.Output(buf));

					len = Math.Min(total - data.Position, 1024 * 1024);
				}

				Exec("EndOutput", () => Proxy.EndOutput());
			}

			public void Input()
			{
				var reset = Exec("Input", () => Proxy.Input());

				if (reset)
				{
					// If remote reset the input position back to zero
					// we need to do the same. This reset happens on
					// datagram publishers like Udp and RawV4.
					_stream.Seek(0, SeekOrigin.Begin);
					_stream.SetLength(0);
				}

				ReadAllBytes();
			}

			public void WantBytes(long count)
			{
				count -= (InputStream.Length - InputStream.Position);
				if (count <= 0)
					return;

				Exec("WantBytes", () => Proxy.WantBytes(count));

				ReadAllBytes();
			}

			#endregion

			#region Read Input Bytes

			private void ReadAllBytes()
			{
				var pos = _stream.Position;
				var buf = ReadBytes();

				_stream.Seek(0, SeekOrigin.End);

				while (buf.Length > 0)
				{
					_stream.Write(buf, 0, buf.Length);
					buf = ReadBytes();
				}

				_stream.Seek(pos, SeekOrigin.Begin);
			}

			private byte[] ReadBytes()
			{
				return Exec("ReadBytes", () => Proxy.ReadBytes());
			}

			#endregion
		}

		#endregion

		#region Private Members

		static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		class MonitorInfo
		{
			public string Name { get; set; }
			public string Class { get; set; }
			public List<KeyValuePair<string, string>> Args { get; set; }
		}

		readonly List<MonitorInfo> _monitors = new List<MonitorInfo>();
		readonly List<PublisherProxy> _publishers = new List<PublisherProxy>();

		private const int RemotingWaitTime = 1000*60*1;

		TcpClientChannel _channel;
		AgentTcpRemote _proxy;
		readonly string _serviceUrl;

		#endregion

		#region Constructor

		public AgentClientTcpRemoting(string name, string url, string password)
			: base(name, url, password)
		{
			var uri = new Uri(new Uri(url), "/PeachAgent");
			if (uri.IsDefaultPort)
				uri = new Uri("{0}://{1}:{2}{3}".Fmt(uri.Scheme, uri.Host, AgentServerTcpRemoting.DefaultPort, uri.PathAndQuery));
			uri = new Uri("tcp://{0}:{1}{2}".Fmt(uri.Host, uri.Port, uri.PathAndQuery));
			_serviceUrl = uri.ToString();
		}

		#endregion

		#region Run Action Proxy

		private static void Exec(Action action)
		{
			Exception remotingException = null;

			var th = new Thread(delegate()
			{
				try
				{
					action();
				}
				catch (PeachException ex)
				{
					remotingException = new PeachException(ex.Message, ex);
				}
				catch (SoftException ex)
				{
					remotingException = new SoftException(ex.Message, ex);
				}
				catch (RemotingException ex)
				{
					remotingException = new RemotingException(ex.Message, ex);
				}
				catch (Exception ex)
				{
					remotingException = new AgentException(ex.Message, ex);
				}
			});

			th.Start();

			if (!th.Join(RemotingWaitTime))
			{
				th.Abort();
				th.Join();
				remotingException = new RemotingException("Remoting call timed out.");
			}

			if (remotingException != null)
				throw remotingException;
		}

		private static T Exec<T>(Func<T> action)
		{
			T t = default(T);

			Exec(() => { t = action(); });

			return t;
		}

		#endregion

		#region Remote Channel Control

		private void CreateProxy()
		{
			// Perform server activation
			var server = (AgentServiceTcpRemote)Activator.GetObject(typeof(AgentServiceTcpRemote), _serviceUrl);

			if (server == null)
				throw new PeachException("Error, unable to create proxy for remote agent '" + _serviceUrl + "'.");

			// Activate the proxy on the client side
			Exec(() => { _proxy = server.GetProxy(); });
		}

		private void RemoveProxy()
		{
			_proxy = null;
		}

		private void CreateChannel()
		{
			var props = (IDictionary)new Hashtable();
			props["timeout"] = (uint)RemotingWaitTime;
			props["connectionTimeout"] = (uint)RemotingWaitTime;

#if !MONO
			// ReSharper disable once RedundantCheckBeforeAssignment
			if (RemotingConfiguration.CustomErrorsMode != CustomErrorsModes.Off)
				RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;
#endif

			var clientProvider = new BinaryClientFormatterSinkProvider();
			_channel = new TcpClientChannel(props, clientProvider);

			try
			{
				ChannelServices.RegisterChannel(_channel, false); // Disable security for speed
			}
			catch
			{
				_channel = null;
				throw;
			}
		}

		private void RemoveChannel()
		{
			if (_channel != null)
			{
				try
				{
					ChannelServices.UnregisterChannel(_channel);
				}
				finally
				{
					_channel = null;
				}
			}
		}

		private void ReconnectProxy(IterationStartingArgs args)
		{
			Logger.Debug("ReconnectProxy: Attempting to reconnect");

			CreateProxy();

			Exec(() =>
			{
				_proxy.AgentConnect();

				foreach (var item in _monitors)
					_proxy.StartMonitor(item.Name, item.Class, item.Args);

				_proxy.SessionStarting();
				_proxy.IterationStarting(args.IsReproduction, args.LastWasFault);

				// ReconnectProxy is only called via IterationStart()
				// IterationStart is called on the agents before the current
				// Iteration/IsControlIteration is set on the publishers
				// Therefore we just need to recreate the publisher proxy
				foreach (var item in _publishers)
					item.Proxy = _proxy.CreatePublisher(item.Name, item.Class, item.Args);
			});
		}

		#endregion

		#region AgentClient Overrides

		public override void AgentConnect()
		{
			Debug.Assert(_channel == null);

			try
			{
				CreateChannel();
				CreateProxy();

				try
				{
					Exec(() => _proxy.AgentConnect());
				}
				catch (Exception ex)
				{
					throw new PeachException("Error, unable to connect to remote agent '{0}'. {1}".Fmt(_serviceUrl, ex.Message), ex);
				}
			}
			catch
			{
				// If this throws, OnAgentDisconnect will not be called
				// so cleanup the proxt and channel

				RemoveProxy();
				RemoveChannel();

				throw;
			}
		}

		public override void AgentDisconnect()
		{
			try
			{
				Exec(() => _proxy.AgentDisconnect());
			}
			finally
			{
				RemoveProxy();
				RemoveChannel();
			}
		}

		public override IPublisher CreatePublisher(string pubName, string cls, Dictionary<string, string> args)
		{
			var pub = new PublisherProxy(pubName, cls, args);

			_publishers.Add(pub);

			Exec(() => { pub.Proxy = _proxy.CreatePublisher(pub.Name, pub.Class, pub.Args); });

			return pub;
		}

		public override void StartMonitor(string monName, string cls, Dictionary<string, string> args)
		{
			// Remote 'args' as a List to support mono/microsoft interoperability
			var asList = args.ToList();

			// Keep track of monitor info so we can recreate them if the proxy disappears
			_monitors.Add(new MonitorInfo { Name = monName, Class = cls, Args = asList });

			Exec(() => _proxy.StartMonitor(monName, cls, asList));
		}

		public override void StopAllMonitors()
		{
			Exec(() => _proxy.StopAllMonitors());
		}

		public override void SessionStarting()
		{
			Exec(() => _proxy.SessionStarting());
		}

		public override void SessionFinished()
		{
			Exec(() => _proxy.SessionFinished());
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			try
			{
				Exec(() => _proxy.IterationStarting(args.IsReproduction, args.LastWasFault));
			}
			catch (RemotingException ex)
			{
				Logger.Debug("IterationStarting: {0}", ex.Message);

				ReconnectProxy(args);
			}
			catch (SocketException ex)
			{
				Logger.Debug("IterationStarting: {0}", ex.Message);

				ReconnectProxy(args);
			}
		}

		public override void IterationFinished()
		{
			Exec(() => _proxy.IterationFinished());
		}

		public override bool DetectedFault()
		{
			return Exec(() => _proxy.DetectedFault());
		}

		public override IEnumerable<MonitorData> GetMonitorData()
		{
			return Exec(() => _proxy.GetMonitorData().Select(FromRemoteData));
		}

		public override void Message(string msg)
		{
			Exec(() => _proxy.Message(msg));
		}

		#endregion

		private MonitorData FromRemoteData(RemoteData data)
		{
			var ret = new MonitorData
			{
				AgentName = Name,
				MonitorName = data.MonitorName,
				DetectionSource = data.DetectionSource,
				Title = data.Title,
				Data = data.Data.ToDictionary(
					i => i.Key,
					i => (Stream)new MemoryStream(i.Value)
				),
			};

			if (data.Fault != null)
			{
				ret.Fault = new MonitorData.Info
				{
					Description = data.Fault.Description,
					MajorHash = data.Fault.MajorHash,
					MinorHash = data.Fault.MinorHash,
					Risk = data.Fault.Risk,
					MustStop = data.Fault.MustStop,
				};
			}

			return ret;
		}
	}

	#endregion

	#region Remoting Objects

	#region Publisher Remoting Object

	internal class PublisherTcpRemote  : MarshalByRefObject, IDisposable
	{
		#region Serialization Helpers

		private static byte[] ToBytes<T>(T t) where T : class
		{
			if (t == null)
				return null;

			var ms = new MemoryStream();
			var fmt = new BinaryFormatter();
			fmt.Serialize(ms, t);
			return ms.ToArray();
		}

		private static T FromBytes<T>(byte[] bytes) where T : class
		{
			if (bytes == null)
				return null;

			var ms = new MemoryStream(bytes);
			var fmt = new BinaryFormatter();
			var obj = fmt.Deserialize(ms);
			return (T)obj;
		}

		#endregion

		BitStreamList _data;
		Publisher _pub;

		public PublisherTcpRemote(Publisher pub)
		{
			_pub = pub;
			_pub.start();
		}

		public void Dispose()
		{
			_pub.stop();
			_pub = null;
		}

		public void Open(uint iteration, bool isControlIteration)
		{
			//NOTE: not updating legacy code.  depricated and should get removed.
			_pub.Iteration = iteration;
			_pub.IsControlIteration = isControlIteration;
			_pub.open();
		}

		public void Close()
		{
			_pub.close();
		}

		public void Accept()
		{
			_pub.accept();
		}

		public bool Input()
		{
			_pub.input();

			return _pub.Position == 0;
		}

		public byte[] ReadBytes()
		{
			var len = Math.Min(_pub.Length - _pub.Position, 1024 * 1024);
			var buf = new byte[len];

			_pub.Read(buf, 0, buf.Length);

			return buf;
		}

		public void WantBytes(long count)
		{
			_pub.WantBytes(count);
		}

		public void BeginOutput()
		{
			_data = new BitStreamList();
		}

		public void Output(byte[] buf)
		{
			_data.Add(new BitStream(buf));
		}

		public void EndOutput()
		{
			_pub.output(_data);

			_data.Dispose();
			_data = null;
		}

		public void SetProperty(string property, byte[] bytes)
		{
			var value = FromBytes<Variant>(bytes);
			_pub.setProperty(property, value);
		}

		public byte[] GetProperty(string property)
		{
			var value = _pub.getProperty(property);
			return ToBytes(value);
		}
	}

	#endregion

	#region Agent Remoting Object

	[Serializable]
	internal class RemoteData
	{
		[Serializable]
		internal class Info
		{
			public string Description { get; set; }
			public string MajorHash { get; set; }
			public string MinorHash { get; set; }
			public string Risk { get; set; }
			public bool MustStop { get; set; }
		}

		public string MonitorName { get; set; }
		public string DetectionSource { get; set; }
		public string Title { get; set; }
		public Info Fault { get; set; }
		public List<KeyValuePair<string, byte[]>> Data { get; set; }
	}

	/// <summary>
	/// Implement agent service running over .NET TCP remoting
	/// </summary>
	internal class AgentTcpRemote : MarshalByRefObject
	{
		private readonly AgentLocal _agent = new AgentLocal(null, null, null);

		public PublisherTcpRemote CreatePublisher(string name, string cls, IEnumerable<KeyValuePair<string, string>> args)
		{
			return new PublisherTcpRemote(AgentLocal.ActivatePublisher(name, cls, args.ToDictionary(i => i.Key, i => i.Value)));
		}

		public void AgentConnect()
		{
			_agent.AgentConnect();
		}

		public void AgentDisconnect()
		{
			_agent.AgentDisconnect();
		}

		public void StartMonitor(string name, string cls, IEnumerable<KeyValuePair<string, string>> args)
		{
			_agent.StartMonitor(name, cls, args.ToDictionary(i => i.Key, i => i.Value));
		}

		public void StopAllMonitors()
		{
			_agent.StopAllMonitors();
		}

		public void SessionStarting()
		{
			_agent.SessionStarting();
		}

		public void SessionFinished()
		{
			_agent.SessionFinished();
		}

		public void IterationStarting(bool isReproduction, bool lastWasFault)
		{
			var args = new IterationStartingArgs
			{
				IsReproduction = isReproduction,
				LastWasFault = lastWasFault
			};

			_agent.IterationStarting(args);
		}

		public void IterationFinished()
		{
			_agent.IterationFinished();
		}

		public bool DetectedFault()
		{
			return _agent.DetectedFault();
		}

		public RemoteData[] GetMonitorData()
		{
			return _agent.GetMonitorData().Select(ToRemoteData).ToArray();
		}

		public void Message(string msg)
		{
			_agent.Message(msg);
		}

		private static RemoteData ToRemoteData(MonitorData data)
		{
			var ret = new RemoteData
			{
				MonitorName = data.MonitorName,
				DetectionSource = data.DetectionSource,
				Title = data.Title,
				Data = data.Data.Select(ToByteArray).ToList(),
			};

			if (data.Fault != null)
			{
				ret.Fault = new RemoteData.Info
				{
					Description = data.Fault.Description,
					MajorHash = data.Fault.MajorHash,
					MinorHash = data.Fault.MinorHash,
					Risk = data.Fault.Risk,
					MustStop = data.Fault.MustStop,
				};
			}

			return ret;
		}

		private static KeyValuePair<string, byte[]> ToByteArray(KeyValuePair<string, Stream> kv)
		{
			var buf = new byte[kv.Value.Length];

			kv.Value.Seek(0, SeekOrigin.Begin);
			kv.Value.Read(buf, 0, buf.Length);

			return new KeyValuePair<string, byte[]>(kv.Key, buf);
		}
	}

	#endregion

	#region Agent Remote Service

	internal class AgentServiceTcpRemote : MarshalByRefObject
	{
		public AgentTcpRemote GetProxy()
		{
			return new AgentTcpRemote();
		}
	}

	#endregion

	#endregion

	#region TCP Agent Server

	[AgentServer("legacy")]
	public class AgentServerTcpRemoting : IAgentServer
	{
		private const string PortOption = "--port=";

		public const ushort DefaultPort = 9001;

		#region IAgentServer Members

		public void Run(Dictionary<string, string> args)
		{
#if !MONO
			RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;
#endif

			var port = DefaultPort;

			foreach (var kv in args)
			{
				if (kv.Value.StartsWith(PortOption))
				{
					var opt = kv.Value.Substring(PortOption.Length);
					if (!ushort.TryParse(opt, out port))
						throw new PeachException("An invalid option for --port was specified.  The value '{0}' is not a valid port number.".Fmt(opt));
				}
			}

			// select channel to communicate
			var props = (IDictionary)new Hashtable();
			props["port"] = (int)port;
			props["name"] = string.Empty;

			var agentBindIp = ConfigurationManager.AppSettings["AgentBindIp"];
			if (!string.IsNullOrEmpty(agentBindIp))
				props["bindTo"] = agentBindIp;

			var serverProvider = new BinaryServerFormatterSinkProvider
			{
				TypeFilterLevel = TypeFilterLevel.Full
			};
			var chan = new TcpServerChannel(props, serverProvider);

			// register channel
			ChannelServices.RegisterChannel(chan, false);

			// register remote object
			// mono doesn't work with client activation so
			// use singleton activation with a function to
			// provide the actual client instance
			RemotingConfiguration.RegisterWellKnownServiceType(
				typeof(AgentServiceTcpRemote),
				"PeachAgent", WellKnownObjectMode.Singleton);

			// inform console
			ConsoleWatcher.WriteInfoMark();
			Console.WriteLine("Listening for connections on port {0}", port);
			Console.WriteLine();
			Console.WriteLine(" -- Press ENTER to quit agent -- ");
			Console.ReadLine();
		}

		#endregion
	}

	#endregion
}
