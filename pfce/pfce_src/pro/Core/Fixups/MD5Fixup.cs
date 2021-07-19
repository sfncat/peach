


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
	[Description("Standard MD5 checksum.")]
	[Fixup("Md5", true)]
	[Fixup("MD5Fixup")]
	[Fixup("checksums.MD5Fixup")]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Parameter("DefaultValue", typeof(HexString), "Default value to use when recursing (default is parent's DefaultValue)", "")]
	[Serializable]
	public class MD5Fixup : HashFixup<MD5CryptoServiceProvider>
	{
		public MD5Fixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}
	}
}

// end
