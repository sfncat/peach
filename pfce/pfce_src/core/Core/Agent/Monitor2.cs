


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Peach.Core.Agent
{
	/// <summary>
	/// Monitors are hosted by agent processes and are
	/// able to report detected faults and gather information
	/// that is usefull when a fualt is detected.
	/// </summary>
	public abstract class Monitor2 : IMonitor
	{
		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		#endregion

		protected Monitor2(string name)
		{
			Name = name;
			Class = GetType().GetAttributes<MonitorAttribute>().First().Name;
		}

		[Flags]
		public enum MonitorWhen
		{
			DetectFault = 0x01,
			OnCall = 0x02,
			OnStart = 0x04,
			OnEnd = 0x08,
			OnIterationStart = 0x10,
			OnIterationEnd = 0x20,
			OnFault = 0x40,
			OnIterationStartAfterFault = 0x80
		};

		/// <summary>
		/// The name of this monitor.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// The class of this monitor.
		/// </summary>
		public string Class { get; private set; }

		/// <summary>
		/// Start the monitor instance.
		/// If an exception is thrown, StopMonitor will not be called.
		/// </summary>
		public virtual void StartMonitor(Dictionary<string, string> args)
		{
			ParameterParser.Parse(this, args);
		}

		/// <summary>
		/// Stop the monitor instance.
		/// </summary>
		public virtual void StopMonitor()
		{
		}

		/// <summary>
		/// Starting a fuzzing session.  A session includes a number of test iterations.
		/// </summary>
		public virtual void SessionStarting()
		{
		}

		/// <summary>
		/// Finished a fuzzing session.
		/// </summary>
		public virtual void SessionFinished()
		{
		}

		/// <summary>
		/// Starting a new iteration
		/// </summary>
		/// <param name="args">Information about the current iteration</param>
		public virtual void IterationStarting(IterationStartingArgs args)
		{
		}

		/// <summary>
		/// Iteration has completed.
		/// </summary>
		public virtual void IterationFinished()
		{
		}

		/// <summary>
		/// Was a fault detected during current iteration?
		/// </summary>
		/// <returns>True if a fault was detected, else false.</returns>
		public virtual bool DetectedFault()
		{
			return false;
		}

		/// <summary>
		/// Return data from the monitor.
		/// </summary>
		/// <returns></returns>
		public virtual MonitorData GetMonitorData()
		{
			return null;
		}

		/// <summary>
		/// Send a message to the monitor and possibly get data back.
		/// </summary>
		/// <param name="msg">Message name</param>
		public virtual void Message(string msg)
		{
		}

		/// <summary>
		/// An event handler that can be used by monitor implementations
		/// to alert others about interesting events occuring.
		/// The peach core does not make use of this event.
		/// </summary>
		/// <remarks>
		/// This event handler is completly ignroed by the peach core.
		/// It can be useful for writing tests against monitors so the
		/// testing framework can get notified when interesting things happen.
		/// </remarks>
		public event EventHandler InternalEvent;

		/// <summary>
		/// Raises the InternalEvent event.
		/// </summary>
		/// <param name="args">Arbitrary arguments to pass to event subscribers.</param>
		protected void OnInternalEvent(EventArgs args)
		{
			if (InternalEvent != null)
				InternalEvent(this, args);
		}

		/// <summary>
		/// Compute the hash of a value for use as either
		/// the MajorHash or MinorHash of a MonitorData.Info object.
		/// </summary>
		/// <param name="value">String value to hash</param>
		/// <returns>The first 4 bytes of the md5 has as a hex string</returns>
		public static string Hash(string value)
		{
			using (var md5 = MD5.Create())
			{
				const int hashLen = 4;

				var data = md5.ComputeHash(Encoding.UTF8.GetBytes(value));
				var sb = new StringBuilder(hashLen * 2);

				for (var i = 0; i < hashLen; i++)
					sb.Append(data[i].ToString("X2", CultureInfo.InvariantCulture));

				return sb.ToString();
			}
		}
	}
}
