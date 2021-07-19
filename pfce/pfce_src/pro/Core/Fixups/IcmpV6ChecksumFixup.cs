


// Authors:
//   Mick Ayzenberg (mick@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Pro.Core.Fixups.Libraries;

namespace Peach.Pro.Core.Fixups
{
	[Description("Standard ICMPv6 checksum.")]
	[Fixup("IcmpV6Checksum", true)]
	[Fixup("IcmpV6ChecksumFixup")]
	[Fixup("checksums.IcmpV6ChecksumFixup")]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Parameter("src", typeof(IPAddress), "Reference to data element")]
	[Parameter("dst", typeof(IPAddress), "Reference to data element")]
	[Serializable]
	public class IcmpV6ChecksumFixup : InternetFixup
	{
		public IcmpV6ChecksumFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args, "ref")
		{
		}

		protected override ushort Protocol
		{
			get { return (ushort)ProtocolType.IcmpV6; }
		}

		protected override bool AddLength
		{
			get { return true; }
		}
	}
}

// end