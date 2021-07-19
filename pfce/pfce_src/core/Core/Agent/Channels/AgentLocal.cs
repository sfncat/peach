


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using NLog;
using Peach.Core.IO;

namespace Peach.Core.Agent.Channels
{
	/// <summary>
	/// This is an agent that runs in the local
	/// process, instead of a remote process.  This
	/// is much faster for things like file fuzzing.
	/// </summary>
	[Agent("local")]
	public class AgentLocal : AgentClient
	{
		#region Publisher Proxy

		class PublisherProxy : IPublisher
		{
			private Publisher _publisher;

			public PublisherProxy(Publisher publisher)
			{
				_publisher = publisher;
				_publisher.start();
			}

			#region IPublisher

			public Stream InputStream
			{
				get { return _publisher; }
			}

			public void Dispose()
			{
				_publisher.stop();
				_publisher = null;
			}

			public void Open(uint iteration, bool isControlIteration, bool isControlRecordingIteration, bool isIterationAfterFault)
			{
				_publisher.Iteration = iteration;
				_publisher.IsControlIteration = isControlIteration;
				_publisher.IsControlRecordingIteration = isControlRecordingIteration;
				_publisher.IsIterationAfterFault = isIterationAfterFault;
				_publisher.open();
			}

			public void Close()
			{
				_publisher.close();
			}

			public void Accept()
			{
				_publisher.accept();
			}

			public Variant Call(string method, List<BitwiseStream> args)
			{
				return _publisher.call(method, args);
			}

			public void SetProperty(string property, Variant value)
			{
				_publisher.setProperty(property, value);
			}

			public Variant GetProperty(string property)
			{
				return _publisher.getProperty(property);
			}

			public void Output(BitwiseStream data)
			{
				_publisher.output(data);
			}

			public void Input()
			{
				_publisher.input();
			}

			public void WantBytes(long count)
			{
				_publisher.WantBytes(count);
			}

			#endregion
		}

		#endregion

		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		internal readonly List<IMonitor> Monitors = new List<IMonitor>();

		public AgentLocal(string name, string uri, string password)
			: base(name, uri, password)
		{
		}

		public override void AgentConnect()
		{
			Logger.Trace("AgentConnect");
		}

		public override void AgentDisconnect()
		{
			Logger.Trace("AgentDisconnect");
		}

		public override IPublisher CreatePublisher(string pubName, string cls, Dictionary<string, string> args)
		{
			Logger.Debug("CreatePublisher: {0} {1}", pubName, cls);

			return new PublisherProxy(ActivatePublisher(pubName, cls, args));
		}

		public override void StartMonitor(string monitorName, string cls, Dictionary<string, string> args)
		{
			Logger.Debug("StartMonitor: {0} {1}", monitorName, cls);

			Monitors.Add(ActivateMonitor(monitorName, cls, args));
		}

		public override void StopAllMonitors()
		{
			Logger.Trace("StopAllMonitors");

			foreach (var mon in Monitors.Reverse<IMonitor>())
			{
				Guard(mon, "StopMonitor", m => m.StopMonitor());
			}

			Monitors.Clear();
		}

		public override void SessionStarting()
		{
			foreach (var mon in Monitors)
			{
				Logger.Debug("SessionStarting");
				mon.SessionStarting();
			}
		}

		public override void SessionFinished()
		{
			foreach (var mon in Monitors.Reverse<IMonitor>())
			{
				Guard(mon, "SessionFinished", m => m.SessionFinished());
			}
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			Logger.Trace("IterationStarting: {0} {1}", args.IsReproduction, args.LastWasFault);

			foreach (var mon in Monitors)
			{
				mon.IterationStarting(args);
			}
		}

		public override void IterationFinished()
		{
			Logger.Trace("IterationFinished");

			foreach (var mon in Monitors.Reverse<IMonitor>())
			{
				Guard(mon, "IterationFinished", m => m.IterationFinished());
			}
		}

		public override bool DetectedFault()
		{
			Logger.Trace("DetectedFault");

			var detectedFault = false;

			foreach (var mon in Monitors)
			{
				Guard(mon, "DetectedFault", m => detectedFault |= m.DetectedFault());
			}

			return detectedFault;
		}

		public override IEnumerable<MonitorData> GetMonitorData()
		{
			Logger.Trace("GetMonitorData");

			var ret = new List<MonitorData>();

			foreach (var mon in Monitors)
			{
				Guard(mon, "GetMonitorData", m =>
				{
					var data = m.GetMonitorData();

					if (data == null)
						return;
					data.AgentName = Name;
					data.MonitorName = m.Name;
					data.DetectionSource = data.DetectionSource ?? m.Class;

					ret.Add(data);
				});
			}

			return ret;
		}

		public override void Message(string msg)
		{
			Logger.Trace("Message: {0}", msg);

			foreach (var monitor in Monitors)
				monitor.Message(msg);
		}

		public static Publisher ActivatePublisher(string name, string cls, Dictionary<string, string> args)
		{
			Logger.Trace("ActivatePublisher: {0} {1}", name, cls);

			var type = ClassLoader.FindPluginByName<PublisherAttribute>(cls);
			if (type == null)
				throw new PeachException("Error, unable to locate publisher '" + cls + "'");

			try
			{
				var parms = args.ToDictionary(i => i.Key, i => new Variant(i.Value));
				var pub = (Publisher)Activator.CreateInstance(type, parms);
				pub.Name = name;
				return pub;
			}
			catch (TargetInvocationException ex)
			{
				var baseEx = ex.GetBaseException();
				if (baseEx is ThreadAbortException)
					throw baseEx;

				throw new PeachException("Could not start publisher \"" + cls + "\".  " + ex.InnerException.Message, ex);
			}
		}

		public static IMonitor ActivateMonitor(string name, string cls, Dictionary<string, string> args)
		{
			Logger.Trace("ActivateMonitor: {0} {1}", name, cls);

			var type = ClassLoader.FindPluginByName<MonitorAttribute>(cls);
			if (type == null)
				throw new PeachException("Error, unable to locate monitor '{0}'.".Fmt(cls));

			IMonitor mon;

			try
			{
				mon = (IMonitor)Activator.CreateInstance(type, new object[] { name });
			}
			catch (MissingMethodException)
			{
				try
				{
					mon = (IMonitor)Activator.CreateInstance(type, new object[]
					{
						null,
						name,
						args.ToDictionary(kv => kv.Key, kv => new Variant(kv.Value))
					});
				}
				catch (TargetInvocationException ex)
				{
					var baseEx = ex.GetBaseException();
					if (baseEx is ThreadAbortException)
						throw baseEx;

					throw new PeachException("Could not start monitor \"" + cls + "\".  " + ex.InnerException.Message, ex);
				}
			}

			try
			{
				mon.StartMonitor(args);
			}
			catch (ThreadAbortException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new PeachException("Could not start monitor \"" + cls + "\".  " + ex.Message, ex);
			}

			return mon;
		}

		private static void Guard(IMonitor mon, string what, Action<IMonitor> action)
		{
			try
			{
				action(mon);
			}
			catch (Exception ex)
			{
				Logger.Warn("Ignoring {0} calling '{1}' on {2} monitor {3}: {4}",
					ex.GetType().Name, what, mon.Class, mon.Name, ex.Message);

				Logger.Trace(ex, "\n{0}", ex);
			}
		}
	}
}
