using System.Collections.Generic;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Fixups
{
	public abstract class BasePythonFixup : Fixup
	{
		protected BasePythonFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
			__init__(parent, args);
		}

		protected virtual void __init__(DataElement parent, Dictionary<string, Variant> args)
		{
		}

		[ShouldClone]
		private bool ShouldClone(object context)
		{
			return false;
		}
	}
}
