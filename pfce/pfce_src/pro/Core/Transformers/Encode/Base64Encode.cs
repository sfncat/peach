


// Authors:
//   Mikhail Davidov (sirus@haxsys.net)

// $Id$

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers.Encode
{
	[Description("Encode on output as Base64.")]
	[Transformer("Base64Encode", true)]
	[Transformer("encode.Base64Encode")]
	[Serializable]
	public class Base64Encode : Transformer
	{
		public Base64Encode(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			return CryptoStream(data, new ToBase64Transform(), CryptoStreamMode.Write);
		}

		protected override BitStream internalDecode(BitStream data)
		{
			return CryptoStream(data, new FromBase64Transform(), CryptoStreamMode.Read);
		}
	}
}

// end
