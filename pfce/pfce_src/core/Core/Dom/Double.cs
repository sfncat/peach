using System;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using Peach.Core.Analyzers;
using Peach.Core.IO;
// ReSharper disable DoNotCallOverridableMethodsInConstructor

namespace Peach.Core.Dom
{
	[DataElement("Double")]
	[PitParsable("Double")]
	[Parameter("name", typeof(string), "Name of element", "")]
	[Parameter("fieldId", typeof(string), "Element field ID", "")]
	[Parameter("size", typeof(uint), "Size in bits")]
	[Parameter("endian", typeof(EndianType), "Byte order of number", "little")]
	[Parameter("mutable", typeof(bool), "Is element mutable", "true")]
	[Parameter("valueType", typeof(ValueType), "Format of value attribute", "string")]
	[Parameter("value", typeof(string), "Default value", "")]
	[Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
	[Parameter("minOccurs", typeof(int), "Minimum occurances", "1")]
	[Parameter("maxOccurs", typeof(int), "Maximum occurances", "1")]
	[Parameter("occurs", typeof(int), "Actual occurances", "1")]
	[Serializable]
	public class Double : Number
	{
		protected double Max = double.MaxValue;
		protected double Min = double.MinValue;

		// Precision limit on integer values for exact representation
		private const long DoublePrecision = 4503599627370496;
		private const long FloatPrecision = 16777216;

		protected long Precision = DoublePrecision;

		public Double()
			: base(false)
		{
			lengthType = LengthType.Bits;
			length = 64;
			Signed = true;
			DefaultValue = new Variant(0.0);
		}

		public Double(string name)
			: base(false, name)
		{
			lengthType = LengthType.Bits;
			length = 64;
			Signed = true;
			DefaultValue = new Variant(0.0);
		}

		public static new DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			if (node.Name != "Double")
				return null;

			var num = Generate<Double>(node, parent);

			if (node.hasAttr("size"))
			{
				var size = node.getAttrInt("size");

				if (size != 32 && size != 64)
					throw new PeachException(string.Format("Error, unsupported size '{0}' for {1}.", size, num.debugName));

				num.lengthType = LengthType.Bits;
				num.length = size;
			}

			string strEndian = null;
			if (node.hasAttr("endian"))
				strEndian = node.getAttrString("endian");
			if (strEndian == null)
				strEndian = context.getDefaultAttr(typeof(Double), "endian", null);

			if (strEndian != null)
			{
				switch (strEndian.ToLower())
				{
					case "little":
						num.LittleEndian = true;
						break;
					case "big":
						num.LittleEndian = false;
						break;
					case "network":
						num.LittleEndian = false;
						break;
					default:
						throw new PeachException(
							string.Format("Error, unsupported value '{0}' for 'endian' attribute on {1}.", strEndian, num.debugName));
				}
			}

			context.handleCommonDataElementAttributes(node, num);
			context.handleCommonDataElementChildren(node, num);
			context.handleCommonDataElementValue(node, num);

			return num;
		}

		public override void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("Double");

			pit.WriteAttributeString("size", lengthAsBits.ToString(CultureInfo.InvariantCulture));

			if (!LittleEndian)
				pit.WriteAttributeString("endian", "big");

			WritePitCommonAttributes(pit);
			WritePitCommonValue(pit);
			WritePitCommonChildren(pit);

			pit.WriteEndElement();
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
				if (value != 32 && value != 64)
					throw new ArgumentOutOfRangeException("value", value, "Value must be equal to 32 or 64.");

				if (value == 32)
				{
					Min = float.MinValue;
					Max = float.MaxValue;
					Precision = FloatPrecision;
				}
				else
				{
					Min = double.MinValue;
					Max = double.MaxValue;
					Precision = DoublePrecision;
				}

				base.length = value;

				Invalidate();
			}
		}

		public override bool hasLength
		{
			get
			{
				return true;
			}
		}


		#region Sanitize

		private double SanitizeString(string str)
		{
			var conv = str;
			var style = NumberStyles.AllowLeadingSign;

			if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
			{
				conv = str.Substring(2);
				style = NumberStyles.AllowHexSpecifier;

				ulong val;
				if (ulong.TryParse(conv, style, CultureInfo.InvariantCulture, out val))
					return val;

				throw new PeachException(string.Format("Error, {0} value '{1}' could not be converted to a {2}-bit double.", debugName, str, lengthAsBits));
			}

			if (str.Contains("."))
				style = style | NumberStyles.AllowDecimalPoint;

			if (str.Contains("E+") || str.Contains("e+")
					|| str.Contains("E-") || str.Contains("e-"))
				style = style | NumberStyles.AllowExponent;

			double value;
			if (double.TryParse(conv, style, CultureInfo.InvariantCulture, out value))
				return value;

			throw new PeachException(string.Format("Error, {0} value '{1}' could not be converted to a {2}-bit double.", debugName, str, lengthAsBits));
		}

		private double SanitizeStream(BitwiseStream bs)
		{
			if (bs.LengthBits < lengthAsBits || (bs.LengthBits + 7) / 8 != (lengthAsBits + 7) / 8)
				throw new PeachException(string.Format("Error, {0} value has an incorrect length for a {1}-bit double, expected {2} bytes.", debugName, lengthAsBits, (lengthAsBits + 7) / 8));

			return FromBitstream(bs);
		}

		private double FromBitstream(BitwiseStream bs)
		{
			var b = new byte[length / 8];
			var len = bs.Read(b, 0, b.Length);
			Debug.Assert(len == lengthAsBits / 8);

			if (BitConverter.IsLittleEndian != LittleEndian)
				System.Array.Reverse(b);

			return BitConverter.ToDouble(b, 0);
		}

		protected override Variant Sanitize(Variant variant)
		{
			var value = GetNumber(variant);

			if (value < Min && !double.IsNegativeInfinity(value))
				throw new PeachException(string.Format("Error, {0} value '{1}' is less than the minimum {2}-bit double.", debugName, value, lengthAsBits));
			if (value > Max && !double.IsPositiveInfinity(value))
				throw new PeachException(string.Format("Error, {0} value '{1}' is greater than the maximum {2}-bit double.", debugName, value, lengthAsBits));

			return new Variant(value);
		}

		private double GetNumber(Variant variant)
		{
			double value;

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
					value = (double)variant;
					break;
				case Variant.VariantType.ULong:
					value = (double)variant;
					break;
				case Variant.VariantType.Double:
					value = (double)variant;
					break;
				default:
					throw new ArgumentException("Variant type '" + variant.GetVariantType().ToString() + "' is unsupported.", "variant");
			}

			return value;
		}

		#endregion

		public override ulong MaxValue
		{
			get
			{
				// Maximum integer we can exactly represent
				return (ulong)Precision;
			}
		}

		public override long MinValue
		{
			get
			{
				// Minimum integer we can exactly represent
				return -Precision;
			}
		}

		protected override BitwiseStream InternalValueToBitStream()
		{
			var value = GetNumber(InternalValue);

			if (value > 0 && value > Max && !double.IsPositiveInfinity(value))
			{
				var msg = string.Format("Error, {0} value '{1}' is greater than the maximum {2}-bit number.", debugName, value, lengthAsBits);
				var inner = new OverflowException(msg);
				throw new SoftException(inner);
			}

			if (value < 0 && value < Min && !double.IsNegativeInfinity(value))
			{
				var msg = string.Format("Error, {0} value '{1}' is less than the minimum {2}-bit number.", debugName, value, lengthAsBits);
				var inner = new OverflowException(msg);
				throw new SoftException(inner);
			}

			var b = length == 32 ? BitConverter.GetBytes((float)value) : BitConverter.GetBytes(value);

			if (BitConverter.IsLittleEndian != LittleEndian)
				System.Array.Reverse(b);

			var bs = new BitStream();
			bs.Write(b, 0, b.Length);
			return bs;
		}
	}
}
