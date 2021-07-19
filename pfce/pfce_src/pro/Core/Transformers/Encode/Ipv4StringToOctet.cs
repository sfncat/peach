


// Authors:
//   Mikhail Davidov (sirus@haxsys.net)

// $Id$

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers.Encode
{
	[Description("Encode on output from a dot notation string to a 4 byte octet representaiton.")]
	[Transformer("Ipv4StringToOctet", true)]
	[Transformer("encode.Ipv4StringToOctet")]
	[Serializable]
	public class Ipv4StringToOctet : Transformer
	{
		public Ipv4StringToOctet(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			var reader = new BitReader(data);
			string sip = reader.ReadString();

			IPAddress ip;
			if (sip.Count(c => c == '.') != 3 || !IPAddress.TryParse(sip, out ip))
				throw new SoftException("Error, can't transform IP to bytes, '{0}' is not a valid IP address.".Fmt(sip));

			var ret = new BitStream();
			var writer = new BitWriter(ret);
			writer.WriteBytes(ip.GetAddressBytes());
			ret.Seek(0, SeekOrigin.Begin);
			return ret;
		}

		protected override BitStream internalDecode(BitStream data)
		{
			if (data.Length != 4)
				throw new PeachException("Error, can't transform bytes to IP, expected 4 bytes but got {0} bytes.".Fmt(data.Length));

			var reader = new BitReader(data);
			var bytes = reader.ReadBytes(4);
			IPAddress ip = new IPAddress(bytes);

			var ret = new BitStream();
			var writer = new BitWriter(ret);
			writer.WriteString(ip.ToString());
			ret.Seek(0, SeekOrigin.Begin);
			return ret;
		}
	}
}

// end
