


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Peach.Core.Agent
{
	[Obsolete("This base class has been deprecated. Monitors should now derive from Peach.Core.Agent.Monitor2.")]
	public abstract class Monitor : IMonitor
	{
		protected Monitor(IAgent agent, string name, Dictionary<string, Variant> args)
		{
			Agent = agent;
			Name = name;
			Class = GetType().GetAttributes<MonitorAttribute>().First().Name;
		}

		[Obsolete("This enum is deprecated. Please use Peach.Core.Agent.Monitor2.MonitorWhen instead.")]
		public enum When
		{
			DetectFault,
			OnCall,
			OnStart,
			OnEnd,
			OnIterationStart,
			OnIterationEnd,
			OnFault,
			OnIterationStartAfterFault
		};

		/// <summary>
		/// The agent that is running this monitor.
		/// </summary>
		public IAgent Agent { get; private set; }

		/// <summary>
		/// The name of this monitor.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// The class of this monitor.
		/// </summary>
		public string Class { get; private set; }

		/// <summary>
		/// Stop the monitor instance.
		/// </summary>
		public abstract void StopMonitor();

		/// <summary>
		/// Starting a fuzzing session.  A session includes a number of test iterations.
		/// </summary>
		public abstract void SessionStarting();

		/// <summary>
		/// Finished a fuzzing session.
		/// </summary>
		public abstract void SessionFinished();

		/// <summary>
		/// Starting a new iteration
		/// </summary>
		/// <param name="iterationCount">Iteration count</param>
		/// <param name="isReproduction">Are we re-running an iteration</param>
		public abstract void IterationStarting(uint iterationCount, bool isReproduction);

		/// <summary>
		/// Iteration has completed.
		/// </summary>
		/// <returns>Returns true to indicate iteration should be re-run, else false.</returns>
		public abstract bool IterationFinished();

		/// <summary>
		/// Was a fault detected during current iteration?
		/// </summary>
		/// <returns>True if a fault was detected, else false.</returns>
		public abstract bool DetectedFault();

		/// <summary>
		/// Return a Fault instance
		/// </summary>
		/// <returns></returns>
		public abstract Fault GetMonitorData();

		/// <summary>
		/// Can the fuzzing session continue, or must we stop?
		/// </summary>
		/// <returns>True if session must stop, else false.</returns>
		public abstract bool MustStop();

		/// <summary>
		/// Send a message to the monitor and possibly get data back.
		/// </summary>
		/// <param name="name">Message name</param>
		/// <param name="data">Message data</param>
		/// <returns>Returns data or null.</returns>
		public abstract Variant Message(string name, Variant data);

		/// <summary>
		/// Process query from another monitor.
		/// </summary>
		/// <remarks>
		/// This method is used to respond to an information request
		/// from another monitor.  Debugger monitors may expose specific
		/// queryies such as "QueryPid" to get the running processes PID.
		/// </remarks>
		/// <param name="query">Query</param>
		/// <returns>Non-null response indicates query was handled.</returns>
		public virtual object ProcessQueryMonitors(string query)
		{
			return null;
		}

		string INamed.name
		{
			get { return Name; }
		}

		string INamed.Name
		{
			get { return Name; }
		}

		string IMonitor.Class
		{
			get { return Class; }
		}

		void IMonitor.StartMonitor(Dictionary<string, string> args)
		{
		}

		void IMonitor.StopMonitor()
		{
			StopMonitor();
		}

		void IMonitor.SessionStarting()
		{
			SessionStarting();
		}

		void IMonitor.SessionFinished()
		{
			SessionFinished();
		}

		void IMonitor.IterationStarting(IterationStartingArgs args)
		{
			IterationStarting(0, args.IsReproduction);
		}

		void IMonitor.IterationFinished()
		{
			IterationFinished();
		}

		bool IMonitor.DetectedFault()
		{
			return DetectedFault();
		}

		MonitorData IMonitor.GetMonitorData()
		{
			var fault = GetMonitorData();
			if (fault == null)
				return null;

			var ret = new MonitorData
			{
				DetectionSource = fault.detectionSource,
				Title = fault.title,
				Data = fault.collectedData.ToDictionary(
					i => i.Key,
					i => (Stream)new MemoryStream(i.Value)
				),
			};

			if (fault.type == FaultType.Fault)
			{
				ret.Fault = new MonitorData.Info
				{
					Description = fault.detectionSource,
					MajorHash = fault.majorHash,
					MinorHash = fault.minorHash,
					Risk = fault.exploitability,
					MustStop = MustStop(),
				};
			}

			return ret;
		}

		void IMonitor.Message(string msg)
		{
			Message("Action.Call", new Variant(msg));
		}

		event EventHandler IMonitor.InternalEvent
		{
			add {}
			remove {}
		}
	}
}

// end
