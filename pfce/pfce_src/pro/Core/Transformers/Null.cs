using System;
using System.Collections.Generic;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers
{
	[Description("Null transformer. Returns that data unaltered.")]
	[Transformer("Null", true, Scope = PluginScope.Internal)]
	[Serializable]
	public class Null : Transformer
	{
		public Null(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			return data;
		}

		protected override BitStream internalDecode(BitStream data)
		{
			return data;
		}
	}
}