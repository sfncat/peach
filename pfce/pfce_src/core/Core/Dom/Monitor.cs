


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Monitors are agent modules that can perform a number of tasks such as
	/// monitoring a target application to detect faults, restarting virtual machines,
	/// recording network traffic, etc. Custom monitors can easily be created and used along
	/// with the included monitors.
	/// </summary>
	[Serializable]
	public class Monitor : INamed
	{
		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		#endregion

		public string cls;
		public string Name { get; set; }
		public Dictionary<string, Variant> parameters = new Dictionary<string, Variant>();
	}

}


// END
