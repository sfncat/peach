

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers.Encode
{
	[Description("Decode on output from Base64.")]
	[Transformer("Base64Decode", true)]
	[Transformer("encode.Base64Decode")]
	[Serializable]
	public class Base64Decode : Transformer
	{
		public Base64Decode(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			return CryptoStream(data, new FromBase64Transform(), CryptoStreamMode.Read);
		}

		protected override BitStream internalDecode(BitStream data)
		{
			return CryptoStream(data, new ToBase64Transform(), CryptoStreamMode.Write);
		}
	}
}

// end
