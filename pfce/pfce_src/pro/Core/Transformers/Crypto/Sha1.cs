

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers.Crypto
{
	[Description("SHA-1 transform (hex & binary).")]
	[Transformer("Sha1", true)]
	[Transformer("crypto.Sha1")]
	[Serializable]
	public class Sha1 : Transformer
	{
		public Sha1(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			SHA1 sha1Tool = SHA1.Create();
			return new BitStream(sha1Tool.ComputeHash(data));
		}

		protected override BitStream internalDecode(BitStream data)
		{
			throw new NotImplementedException();
		}
	}
}

// end
