


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
	/// Byte size relation.
	/// </summary>
	[Serializable]
	[Relation("size", true)]
	[Description("Byte size relation")]
	[Parameter("of", typeof(string), "Element used to generate relation value", "")]
	[Parameter("expressionGet", typeof(string), "Scripting expression that is run when getting the value", "")]
	[Parameter("expressionSet", typeof(string), "Scripting expression that is run when setting the value", "")]
	[Parameter("lengthType", typeof(LengthType), "Units to compute the size in", "bytes")]
	public class SizeRelation : Relation
	{
		private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

		// This massive hack is to allow the SnmpFixup to initialize itself
		public static bool DisableRecursionCheck = false;

		protected bool _isRecursing;
		protected LengthType _lengthType = LengthType.Bytes;

		public SizeRelation(DataElement parent)
			: base(parent)
		{
		}

		public override void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("Relation");
			pit.WriteAttributeString("type", "size");
			pit.WriteAttributeString("of", OfName);

			if (ExpressionGet != null)
				pit.WriteAttributeString("expressionGet", ExpressionGet);
			if (ExpressionSet != null)
				pit.WriteAttributeString("expressionSet", ExpressionSet);

			pit.WriteEndElement();
		}

		public LengthType lengthType
		{
			get
			{
				return _lengthType;
			}
			set
			{
				_lengthType = value;
			}
		}

		public override long GetValue()
		{
			if (_isRecursing && !DisableRecursionCheck)
				return 0;

			try
			{
				_isRecursing = true;

				var size = From.DefaultValue;

				long ret;

				if (_expressionGet != null)
				{
					var state = new Dictionary<string, object>
					{
						{ "self", From }
					};

					if (size.GetVariantType() == Variant.VariantType.ULong)
					{
						state["size"] = (ulong)size;
						state["value"] = (ulong)size;
					}
					else
					{
						state["size"] = (long)size;
						state["value"] = (long)size;
					}

					var value = From.EvalExpression(_expressionGet, state);
					ret = Convert.ToInt64(value, CultureInfo.InvariantCulture);
				}
				else
				{
					ret = (long)size;
				}

				if (lengthType == LengthType.Bytes)
					ret *= 8;

				return ret;
			}
			finally
			{
				_isRecursing = false;
			}
		}

		public override Variant CalculateFromValue()
		{
			if (_isRecursing && !DisableRecursionCheck)
				return new Variant(0);

			if (Of == null)
			{
				logger.Error("Error, Of returned null");
				return null;
			}

			try
			{
				_isRecursing = true;

				var size = Of.Value.LengthBits;

				if (lengthType == LengthType.Bytes)
					size /= 8;

				if (_expressionSet == null)
					return new Variant(size);

				var state = new Dictionary<string, object>
				{
					{ "self", From },
					{ "size", size },
					{ "value", size }
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
