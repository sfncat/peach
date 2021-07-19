

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers.Encode
{
	[Description("Encode on output from a colon notation ipv6 address into a 16 byte octect representation.")]
	[Transformer("Ipv6StringToOctet", true)]
	[Transformer("encode.Ipv6StringToOctet")]
	[Serializable]
	public class Ipv6StringToOctet : Transformer
	{
		public Ipv6StringToOctet(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			var reader = new BitReader(data);
			string sip = reader.ReadString();

			IPAddress ip;
			if (!IPAddress.TryParse(sip, out ip))
				throw new PeachException("Error, can't transform IP to bytes, '{0}' is not a valid IP address.".Fmt(sip));

			var ret = new BitStream();
			var writer = new BitWriter(ret);
			writer.WriteBytes(ip.GetAddressBytes());
			ret.Seek(0, SeekOrigin.Begin);
			return ret;
		}

		protected override BitStream internalDecode(BitStream data)
		{
			if (data.Length != 16)
				throw new PeachException("Error, can't transform bytes to IP, expected 16 bytes but got {0} bytes.".Fmt(data.Length));

			var reader = new BitReader(data);
			var bytes = reader.ReadBytes(16);
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
