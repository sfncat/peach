

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers.Crypto
{
	[Description("MD5 transform (hex & binary).")]
	[Transformer("Md5", true)]
	[Transformer("crypto.Md5")]
	[Serializable]
	public class Md5 : Transformer
	{
		public Md5(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			MD5 md5Tool = MD5.Create();
			return new BitStream(md5Tool.ComputeHash(data));
		}

		protected override BitStream internalDecode(BitStream data)
		{
			throw new NotImplementedException();
		}
	}
}

// end
