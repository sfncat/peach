


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;

namespace Peach.Core.Agent
{
	/// <summary>
	/// Agent Servers are required to implement this interface
	/// </summary>
	public interface IAgentServer
	{
		void Run(Dictionary<string, string> args);
	}

	/// <summary>
	/// Indicate class is an Agent Server.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class AgentServerAttribute : PluginAttribute
	{
		public AgentServerAttribute(string name)
			: base(typeof(IAgentServer), name, true)
		{
		}
	}
}
