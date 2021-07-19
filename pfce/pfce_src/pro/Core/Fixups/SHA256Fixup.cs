


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)
//   Ross Salpino (rsal42@gmail.com)
//   Mikhail Davidov (sirus@haxsys.net)

// $Id$

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Fixups
{
	[Description("Standard SHA256 checksum.")]
	[Fixup("Sha256", true)]
	[Fixup("SHA256Fixup")]
	[Fixup("checksums.SHA256Fixup")]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Parameter("DefaultValue", typeof(HexString), "Default value to use when recursing (default is parent's DefaultValue)", "")]
	[Serializable]
	public class SHA256Fixup : HashFixup<SHA256CryptoServiceProvider>
	{
		public SHA256Fixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}
	}
}

// end
