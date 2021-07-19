using System;
using System.Globalization;
using System.IO;
using System.Xml;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Pro.Core.Dom
{
	[PitParsable("VarNumber")]
	[DataElement("VarNumber", DataElementTypes.NonDataElements)]
	[Parameter("name", typeof(string), "Element name", "")]
	[Parameter("fieldId", typeof(string), "Element field ID", "")]
	[Parameter("value", typeof(string), "Default value", "")]
	[Parameter("valueType", typeof(Peach.Core.Dom.ValueType), "Format of value attribute", "string")]
	[Parameter("token", typeof(bool), "Is element a token", "false")]
	[Parameter("mutable", typeof(bool), "Is element mutable", "true")]
	[Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
	[Serializable]
	public class VarNumber : Number
	{
		public VarNumber()
			: base()
		{
			// Signed is true since C# streams use long for sizes
			lengthType = LengthType.Bits;
			length = 64;
			Signed = false;
			LittleEndian = false;
		}

		public VarNumber(string name)
			: base(name)
		{
			// Signed is true since C# streams use long for sizes
			lengthType = LengthType.Bits;
			length = 64;
			Signed = false;
			LittleEndian = false;
		}

		public override long length
		{
			get
			{
				switch (_lengthType)
				{
					case LengthType.Bytes:
						return _length;
					case LengthType.Bits:
						return _length;
					case LengthType.Chars:
						throw new NotSupportedException("Length type of Chars not supported by Number.");
					default:
						throw new NotSupportedException("Error calculating length.");
				}
			}
			set
			{
				if (value <= 0 || value > 64)
					throw new ArgumentOutOfRangeException("value", value, "Value must be greater than 0 and less than 65.");

				base.length = value;

				if (_signed)
				{
					_max = (ulong)((ulong)1 << ((int)lengthAsBits - 1)) - 1;
					_min = 0 - (long)((ulong)1 << ((int)lengthAsBits - 1));
				}
				else
				{
					_max = (ulong)((ulong)1 << ((int)lengthAsBits - 1));
					_max += (_max - 1);
					_min = 0;
				}

				Invalidate();
			}
		}

		public override bool hasLength { get { return false; } }

		public override bool isDeterministic { get { return true; } }

		public new static DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			if (node.Name != "VarNumber")
				return null;

			var len = Generate<VarNumber>(node, parent);

			context.handleCommonDataElementAttributes(node, len);
			context.handleCommonDataElementChildren(node, len);
			context.handleCommonDataElementValue(node, len);

			return len;
		}

		public override void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("VarNumber");

			WritePitCommonAttributes(pit);
			WritePitCommonChildren(pit);
			//WritePitCommonValue(pit);

			pit.WriteEndElement();
		}

		#region Sanitize

		private dynamic SanitizeString(string str)
		{
			var conv = str;
			var style = NumberStyles.AllowLeadingSign;

			if (str.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
			{
				conv = str.Substring(2);
				style = NumberStyles.AllowHexSpecifier;
			}

			if (Signed)
			{
				long value;
				if (long.TryParse(conv, style, CultureInfo.InvariantCulture, out value))
					return value;
			}
			else
			{
				ulong value;
				if (ulong.TryParse(conv, style, CultureInfo.InvariantCulture, out value))
					return value;
			}

			throw new PeachException(string.Format("Error, {0} value '{1}' could not be converted to a {2}-bit {3} number.", debugName, str, lengthAsBits, Signed ? "signed" : "unsigned"));
		}

		private dynamic SanitizeStream(BitwiseStream bs)
		{
			var pos = bs.PositionBits;

			try
			{
				//if (bs.LengthBits < lengthAsBits || (bs.LengthBits + 7) / 8 != (lengthAsBits + 7) / 8)
				//	throw new PeachException(string.Format("Error, {0} value has an incorrect length for a {1}-bit {2} number, expected {3} bytes.", debugName, lengthAsBits, Signed ? "signed" : "unsigned", (lengthAsBits + 7) / 8));

				//ulong extra;
				//bs.ReadBits(out extra, (int) lengthAsBits);

				//if (extra != 0)
				//	throw new PeachException(string.Format("Error, {0} value has an invalid bytes for a {1}-bit {2} number.", debugName, lengthAsBits, Signed ? "signed" : "unsigned"));

				return FromBitstream(bs);
			}
			finally
			{
				bs.PositionBits = pos;
			}
		}

		private dynamic FromBitstream(BitwiseStream bs)
		{
			ulong bits;
			var len = bs.ReadBits(out bits, (int)lengthAsBits);
			//System.Diagnostics.Debug.Assert(len == lengthAsBits);

			if (Signed)
				return _endian.GetInt64(bits, len);

			return _endian.GetUInt64(bits, len);
		}

		protected override Variant Sanitize(Variant variant)
		{
			dynamic value = GetNumber(variant);

			if (value < 0 && (long)value < MinValue)
				throw new PeachException(string.Format("Error, {0} value '{1}' is less than the minimum {2}-bit {3} number.", debugName, value, lengthAsBits, Signed ? "signed" : "unsigned"));
			if (value > 0 && (ulong)value > MaxValue)
				throw new PeachException(string.Format("Error, {0} value '{1}' is greater than the maximum {2}-bit {3} number.", debugName, value, lengthAsBits, Signed ? "signed" : "unsigned"));

			if (Signed)
				return new Variant((long)value);

			return new Variant((ulong)value);
		}

		private dynamic DoubleToInteger(double value)
		{
			if (Math.Floor(value) != value)
				throw new PeachException(string.Format("Error, {0} value '{1}' can not be converted to a {2}-bit {3} number.", debugName, value, lengthAsBits, Signed ? "signed" : "unsigned"));

			if (value < 0)
			{
				try
				{
					return Convert.ToInt64(value);
				}
				catch (OverflowException)
				{
					throw new PeachException(string.Format("Error, {0} value '{1}' is less than the minimum {2}-bit {3} number.", debugName, value, lengthAsBits, Signed ? "signed" : "unsigned"));
				}
			}

			try
			{
				return Convert.ToUInt64(value);
			}
			catch (OverflowException)
			{
				throw new PeachException(string.Format("Error, {0} value '{1}' is greater than the maximum {2}-bit {3} number.", debugName, value, lengthAsBits, Signed ? "signed" : "unsigned"));
			}
		}

		private dynamic GetNumber(Variant variant)
		{
			dynamic value = 0;

			switch (variant.GetVariantType())
			{
				case Variant.VariantType.String:
					value = SanitizeString((string)variant);
					break;
				case Variant.VariantType.ByteString:
					value = SanitizeStream(new BitStream((byte[])variant));
					break;
				case Variant.VariantType.BitStream:
					value = SanitizeStream((BitwiseStream)variant);
					break;
				case Variant.VariantType.Int:
				case Variant.VariantType.Long:
					value = (long)variant;
					break;
				case Variant.VariantType.ULong:
					value = (ulong)variant;
					break;
				case Variant.VariantType.Double:
					value = DoubleToInteger((double)variant);
					break;
				default:
					throw new ArgumentException("Variant type '" + variant.GetVariantType().ToString() + "' is unsupported.", "variant");
			}

			return value;
		}

		#endregion

		protected override BitwiseStream InternalValueToBitStream()
		{
			var value = (ulong)InternalValue;
			var ret = new BitStream();

			var n = 0;
			for (var tmp = value; tmp > 0; tmp >>= 8)
				++n;

			if (n == 0)
			{
				++n;
			}

			for (var i = 0; i < n; ++i)
			{
				var b = (byte)((value >> (8 * (n - i - 1))) & 0xFF);
				ret.WriteByte(b);
			}

			ret.Seek(0, SeekOrigin.Begin);
			return ret;
		}

	}
}
