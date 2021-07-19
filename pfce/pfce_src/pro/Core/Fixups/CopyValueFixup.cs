


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)
//   Mikhail Davidov (sirus@haxsys.net)

// $Id$

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Fixups
{
	[Description("Fixup used in testing.  Will copy another elements value into us.")]
	[Fixup("CopyValue", true)]
	[Fixup("CopyValueFixup")]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Serializable]
	public class CopyValueFixup : Fixup
	{
		public CopyValueFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args, "ref")
		{
		}

		protected override Variant fixupImpl()
		{
			var elem = elements["ref"];

			// Use InternalValue so type information is preserved
			return elem.InternalValue;
		}
	}
}

// end
