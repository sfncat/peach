


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Runtime.Serialization;
using Peach.Core.Agent;

namespace Peach.Core
{
	/// <summary>
	/// Unrecoverable error.  Causes Peach to exit with an error
	/// message, but no stack trace.
	/// </summary>
	[Serializable]
	public class PeachException : ApplicationException
	{
		public PeachException(string message)
			: base(message)
		{
		}

		public PeachException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected PeachException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}

	/// <summary>
	/// Thrown to stop current test case and move to next.
	/// </summary>
	/// <remarks>
	/// Used to indicate an error that should stop the current test case, but not the fuzzing job.
	/// </remarks>
	[Serializable]
	public class SoftException : ApplicationException
	{
		public SoftException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		public SoftException(string message)
			: base(message)
		{
		}

		public SoftException(Exception innerException)
			: base(innerException.Message, innerException)
		{
		}

		protected SoftException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}

	/// <summary>
	/// Thrown when peach catches an exception from an agent.
	/// </summary>
	[Serializable]
	public class AgentException : ApplicationException
	{
		public AgentException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		public AgentException(string message)
			: base(message)
		{
		}

		public AgentException(Exception innerException)
			: base(innerException.Message, innerException)
		{
		}

		protected AgentException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}

	/// <summary>
	/// Thrown to indicate a fault has occured.
	/// </summary>
	/// <remarks>
	/// This exception can be thrown by Publishers or Scripting code to
	/// indicate a fault has occured.  The exception extends from
	/// SoftException, so normal cleanup code will run when this exception
	/// is thrown.
	/// </remarks>
	[Serializable]
	public class FaultException : SoftException
	{
		public FaultSummary Fault { get; private set; }

		public FaultException(FaultSummary fault)
			: base(fault.Title)
		{
			Fault = fault;
		}

		public FaultException(FaultSummary fault, Exception innerException)
			: base(fault.Title, innerException)
		{
			Fault = fault;
		}

		protected FaultException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}

// end
