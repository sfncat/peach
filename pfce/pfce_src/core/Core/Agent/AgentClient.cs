


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;

namespace Peach.Core.Agent
{
	/// <summary>
	/// Abstract base class for all Agent servers.
	/// </summary>
	public abstract class AgentClient : INamed
	{
		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		#endregion

		/// <summary>
		/// Construct an agent client
		/// </summary>
		/// <param name="name">Name of agent</param>
		/// <param name="url">Agent URL</param>
		/// <param name="password">Agent Password</param>
		protected AgentClient(string name, string url, string password)
		{
			Name = name;
			Url = url;
			Password = password;
		}

		/// <summary>
		/// Agent name
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Agent URL
		/// </summary>
		public string Url { get; private set; }

		/// <summary>
		/// Agent password
		/// </summary>
		public string Password { get; private set; }

		/// <summary>
		/// Connect to agent
		/// </summary>
		public abstract void AgentConnect();

		/// <summary>
		/// Disconnect from agent
		/// </summary>
		public abstract void AgentDisconnect();

		/// <summary>
		/// Creates a publisher on the remote agent
		/// </summary>
		/// <param name="name">Name for publisher instance</param>
		/// <param name="cls">Class of publisher to create</param>
		/// <param name="args">Arguments for publisher</param>
		/// <returns>Instance of remote publisher</returns>
		public abstract IPublisher CreatePublisher(string name, string cls, Dictionary<string, string> args);

		/// <summary>
		/// Start a specific monitor
		/// </summary>
		/// <param name="name">Name for monitor instance</param>
		/// <param name="cls">Class of monitor to start</param>
		/// <param name="args">Arguments</param>
		public abstract void StartMonitor(string name, string cls, Dictionary<string, string> args);

		/// <summary>
		/// Stop all monitors currently running
		/// </summary>
		public abstract void StopAllMonitors();

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
		/// <param name="args">Metadata about the type of iteration</param>
		public abstract void IterationStarting(IterationStartingArgs args);

		/// <summary>
		/// Iteration has completed.
		/// </summary>
		/// <returns>Returns true to indicate iteration should be re-run, else false.</returns>
		public abstract void IterationFinished();

		/// <summary>
		/// Was a fault detected during current iteration?
		/// </summary>
		/// <returns>True if a fault was detected, else false.</returns>
		public abstract bool DetectedFault();

		/// <summary>
		/// Get the fault information
		/// </summary>
		/// <returns>Returns array of Fault instances</returns>
		public abstract IEnumerable<MonitorData> GetMonitorData();

		/// <summary>
		/// Send a message to all monitors.
		/// </summary>
		/// <param name="msg">Message</param>
		public abstract void Message(string msg);
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class AgentAttribute : PluginAttribute
	{
		public AgentAttribute(string name)
			: base(typeof(AgentClient), name, true)
		{
		}
	}
}
