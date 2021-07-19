


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)
//   Ross Salpino (rsal42@gmail.com)
//   Mikhail Davidov (sirus@haxsys.net)

// $Id$

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Fixups
{
	[Description("Standard SHA256 checksum.")]
	[Fixup("Sha224", true)]
	[Fixup("SHA224Fixup")]
	[Fixup("checksums.SHA224Fixup")]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Parameter("DefaultValue", typeof(HexString), "Default value to use when recursing (default is parent's DefaultValue)", "")]
	[Serializable]
	public class SHA224Fixup : HashFixup<Libraries.SHA224Managed>
	{
		public SHA224Fixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}
	}
}

// end
