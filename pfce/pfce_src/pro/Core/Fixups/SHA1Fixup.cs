


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
	[Description("Standard SHA1 checksum.")]
	[Fixup("Sha1", true)]
	[Fixup("SHA1Fixup")]
	[Fixup("checksums.SHA1Fixup")]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Parameter("DefaultValue", typeof(HexString), "Default value to use when recursing (default is parent's DefaultValue)", "")]
	[Serializable]
	public class SHA1Fixup : HashFixup<SHA1CryptoServiceProvider>
	{
		public SHA1Fixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}
	}
}

// end
