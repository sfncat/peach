using System;
using System.Collections.Generic;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Fixups
{
	/// <summary>
	/// A helper class for fixups that are volatile and need to
	/// be computed every time an action is run.
	/// Any fixup that requires per-iteration state stored in the
	/// RunContext should derive from this class and override OnActionRun.
	/// </summary>
	[Obsolete("This class is obsolete.  Use Peach.Core.Fixups.VolatileFixup instead.")]
	[Serializable]
	public abstract class VolatileFixup : Peach.Core.Fixups.VolatileFixup
	{
		protected VolatileFixup(DataElement parent, Dictionary<string, Variant> args, params string[] refs)
			: base(parent, args, refs)
		{
		}
	}
}
