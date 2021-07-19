


// Authors:
//   Mikhail Davidov (sirus@haxsys.net)

// $Id$

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Fixups
{
	[Description("Provide scripting expression to perform fixup.")]
	[Fixup("Expression", true)]
	[Fixup("ExpressionFixup")]
	[Fixup("checksums.ExpressionFixup")]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Parameter("expression", typeof(string), "Expression returning string or int")]
	[Serializable]
	public class ExpressionFixup : Fixup
	{
		public ExpressionFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args, "ref")
		{
			if (!args.ContainsKey("expression"))
				throw new PeachException("Error, ExpressionFixup requires an 'expression' argument!");
		}

		protected override Variant fixupImpl()
		{
			var from = elements["ref"];
			var expression = (string)args["expression"];

			var state = new Dictionary<string, object>();
			state["self"] = this;
			state["ref"] = from;
			state["data"] = from.Value;

			object data;

			try
			{
				data = parent.EvalExpression(expression, state);
			}
			catch (Exception ex)
			{
				throw new PeachException(
					"ExpressionFixup expression threw an exception!\nExpression: {0}\n Exception: {1}".Fmt(expression, ex.ToString()), ex
				);
			}

			if (data == null)
				throw new PeachException("Error, expression fixup returned null.");

			var asVariant = Scripting.ToVariant(data);

			if (asVariant == null)
				throw new PeachException("Error, expression fixup returned unknown type '{0}'.".Fmt(data.GetType()));

			if (parent is Blob && asVariant.GetVariantType() == Variant.VariantType.String)
				return new Variant(Encoding.ISOLatin1.GetBytes((string)asVariant));

			return asVariant;
		}
	}
}

// end
