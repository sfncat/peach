

using System;
using System.Collections.Generic;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers
{
	[Description("Evaluate a statement.")]
	[Transformer("Eval", true)]
	[Transformer("misc.Eval")]
	[Parameter("eval", typeof(string), "Formatter for data.")]
	[Serializable]
	public class Eval : Transformer
	{
		//Dictionary<string, Variant> m_args;

		public Eval(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
			//m_args = args;
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			//string format;
			//if (m_args.ContainsKey("eval"))
			//    format = (string)(m_args["eval"]);

			return data;
		}

		protected override BitStream internalDecode(BitStream data)
		{
			throw new NotImplementedException();
		}
	}
}

// end
