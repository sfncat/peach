

// Authors:
//  Mick Ayzenberg (mick@dejavusecurity.com)
//  Jordyn Puryear (jordyn@dejavusecurity.com)

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
	[Description("Standard TCP checksum.")]
	[Fixup("TcpChecksum", true)]
	[Fixup("TCPChecksumFixup")]
	[Fixup("checksums.TCPChecksumFixup")]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Parameter("src", typeof(IPAddress), "Source IP address")]
	[Parameter("dst", typeof(IPAddress), "Destination IP address")]
	[Serializable]
	public class TCPChecksumFixup : InternetFixup
	{
		public TCPChecksumFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args, "ref")
		{
		}

		protected override ushort Protocol
		{
			get { return (ushort)ProtocolType.Tcp; }
		}

		protected override bool AddLength
		{
			get { return true; }
		}
	}
}
