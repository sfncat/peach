


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Xml.Serialization;
using System.ComponentModel;
using Peach.Core.Agent;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Configure a local or remote agent. Agents can perform various tasks during
	/// a fuzzing run. This element must include at least one Monitor child.
	/// </summary>
	[Serializable]
	// TODO: Old XSD defines <PythonPath> and <Import> children
	public class Agent : INamed
	{
		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		#endregion

		public Agent()
		{
			platform = Platform.OS.All;
			monitors = new NamedCollection<Monitor>();
		}

		/// <summary>
		/// Name of agent. May not contain spaces or periods (.).
		/// </summary>
		[XmlAttribute("name")]
		public string Name { get; set; }

		/// <summary>
		/// Specify location of agent. Value is "&lt;channel%gt;://&lt;hostname&gt;" where
		/// &lt;channel%gt; specifies the remoting channel (tcp or local) and
		/// &lt;hostname%gt; specifies the hostname/ipaddress of the agent.
		/// If this attribute is not set a local agent will be used.
		/// </summary>
		[XmlAttribute]
		[DefaultValue(null)]
		public string location { get; set; }

		/// <summary>
		/// Password to the remote agent if needed.
		/// </summary>
		[XmlAttribute]
		[DefaultValue(null)]
		public string password { get; set; }

		/// <summary>
		/// Limit Agent to specific platform.
		/// </summary>
		public Platform.OS platform { get; set; }

		/// <summary>
		/// List of monitors Agent should spin up.
		/// </summary>
		[PluginElement("class", typeof(IMonitor), Named = true)]
		[DefaultValue(null)]
		public NamedCollection<Monitor> monitors { get; set; }
	}
}

// END
