


// Authors:
//   Mick Ayzenberg (mick@dejavusecurity.com)

// $Id$

using System;
using System.IO;
using System.Collections.Generic;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers
{
	[Description("Truncates a Value")]
	[Transformer("Truncate", true)]
	[Parameter("Length", typeof(int), "Length to truncate in bytes")]
	[Parameter("Offset", typeof(int), "Starting offset", "0")]
	[Serializable]
	public class Truncate : Transformer
	{
		public int Offset { get; protected set; }
		public int Length { get; protected set; }

		public Truncate(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
			ParameterParser.Parse(this, args);
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			try
			{
				var ret = new BitStream();
				data.CopyTo(ret, Offset, Length);
				ret.Seek(0, SeekOrigin.Begin);
				return ret;
			}
			catch (Exception ex)
			{
				throw new SoftException(ex);
			}
		}

		protected override BitStream internalDecode(BitStream data)
		{
			throw new NotImplementedException();
		}
	}
}

// end
