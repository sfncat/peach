

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers.Crypto
{
	[Description("HMAC as described in RFC 2104.")]
	[Transformer("Hmac", true)]
	[Transformer("crypto.Hmac")]
	[Serializable]
	public class Hmac : Transformer
	{
		public Hmac(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			HMAC hmacTool = HMAC.Create();
			return new BitStream(hmacTool.ComputeHash(data));
		}

		protected override BitStream internalDecode(BitStream data)
		{
			throw new NotImplementedException();
		}
	}
}

// end
