


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Xml;
using NLog;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Array count relation
	/// </summary>
	[Serializable]
	[Relation("count", true)]
	[Description("Array count relation")]
	[Parameter("of", typeof(string), "Element used to generate relation value", "")]
	[Parameter("expressionGet", typeof(string), "Scripting expression that is run when getting the value", "")]
	[Parameter("expressionSet", typeof(string), "Scripting expression that is run when setting the value", "")]
	public class CountRelation : Relation
	{
		private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger(); 
		protected bool _isRecursing;

		public CountRelation(DataElement parent)
			: base(parent)
		{
		}

		public override void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("Relation");
			pit.WriteAttributeString("type", "count");
			pit.WriteAttributeString("of", OfName);

			if (ExpressionGet != null)
				pit.WriteAttributeString("expressionGet", ExpressionGet);
			if (ExpressionSet != null)
				pit.WriteAttributeString("expressionSet", ExpressionSet);

			pit.WriteEndElement();
		}


		public override long GetValue()
		{
			if (_isRecursing)
				return 0;

			try
			{
				_isRecursing = true;

				var count = From.DefaultValue;

				if (_expressionGet == null)
					return (long)count;

				var state = new Dictionary<string, object>
				{
					{ "self", From }
				};

				if (count.GetVariantType() == Variant.VariantType.ULong)
				{
					state["count"] = (ulong)count;
					state["value"] = (ulong)count;
				}
				else
				{
					state["count"] = (long)count;
					state["value"] = (long)count;
				}

				var value = From.EvalExpression(_expressionGet, state);

				return Convert.ToInt64(value, CultureInfo.InvariantCulture);
			}
			finally
			{
				_isRecursing = false;
			}
		}

		public override Variant CalculateFromValue()
		{
			if (_isRecursing)
				return new Variant(0);

			if (Of == null)
			{
				logger.Error("Error, Of returned null");
				return null;
			}

			try
			{
				_isRecursing = true;

				var OfArray = Of as Sequence;

				if (OfArray == null)
				{
					logger.Error("Count Relation requires '{0}' to be an array.  Set the minOccurs and maxOccurs properties.", OfName);
					return null;
				}

				var count = OfArray.GetCountOverride();

				if (_expressionSet == null)
					return new Variant(count);

				var state = new Dictionary<string, object>
				{
					{ "self", From },
					{ "count", count },
					{ "value", count }
				};

				var value = From.EvalExpression(_expressionSet, state);

				return Scripting.ToVariant(value);
			}
			finally
			{
				_isRecursing = false;
			}
		}
	}
}

// end
