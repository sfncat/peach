


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.IO;
using System.Xml;

using Peach.Core.Analyzers;
using Peach.Core.IO;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Binary large object data element
	/// </summary>
	[DataElement("Blob", DataElementTypes.NonDataElements)]
	[PitParsable("Blob")]
	[DataElementChildSupported("Placement")]
	[Parameter("name", typeof(string), "Element name", "")]
	[Parameter("fieldId", typeof(string), "Element field ID", "")]
	[Parameter("length", typeof(uint?), "Length in data element", "")]
	[Parameter("lengthType", typeof(LengthType), "Units of the length attribute", "bytes")]
	[Parameter("value", typeof(string), "Default value", "")]
	[Parameter("valueType", typeof(ValueType), "Format of value attribute", "string")]
	[Parameter("token", typeof(bool), "Is element a token", "false")]
	[Parameter("mutable", typeof(bool), "Is element mutable", "true")]
	[Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
	[Parameter("minOccurs", typeof(int), "Minimum occurances", "1")]
	[Parameter("maxOccurs", typeof(int), "Maximum occurances", "1")]
	[Parameter("occurs", typeof(int), "Actual occurances", "1")]
	[Serializable]
	public class Blob : DataElement
	{
		public Blob()
		{
			_defaultValue = new Variant(new BitStream());
		}
		
		public Blob(string name)
			: base(name)
		{
			_defaultValue = new Variant(new BitStream());
		}

		public static DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			if (node.Name != "Blob")
				return null;

			var blob = DataElement.Generate<Blob>(node, parent);

			context.handleCommonDataElementAttributes(node, blob);
			context.handleCommonDataElementChildren(node, blob);
			context.handleCommonDataElementValue(node, blob);

			if (blob.DefaultValue == null)
				blob.DefaultValue = new Variant(new BitStream());

			BitwiseStream bs;

			if (blob.DefaultValue.GetVariantType() == Variant.VariantType.String)
			{
				bs = new BitStream();
				new BitWriter(bs).WriteString((string)blob.DefaultValue);
			}
			else
			{
				System.Diagnostics.Debug.Assert(blob.DefaultValue.GetVariantType() == Variant.VariantType.BitStream);
				bs = (BitwiseStream)blob.DefaultValue;
			}

			bs.Seek(0, SeekOrigin.Begin);
			blob.DefaultValue = new Variant(bs);

			if (blob.hasLength)
			{
				if (bs.LengthBits > blob.lengthAsBits)
					throw new PeachException("Error, value of " + blob.debugName + " is longer than specified length.");

				if (bs.LengthBits < blob.lengthAsBits)
					bs.SetLengthBits(blob.lengthAsBits);
			}

			return blob;
		}

		public override Variant DefaultValue
		{
			get
			{
				return base.DefaultValue;
			}
			set
			{
				base.DefaultValue = Sanitize(value);
			}
		}

		private Variant Sanitize(Variant value)
		{
			var type = value.GetVariantType();

			if (type == Variant.VariantType.BitStream)
				return value;

			var asStr = (string)value;

			if (type != Variant.VariantType.String)
				throw new PeachException(string.Format("Error, {0} {1} value '{2}' could not be converted to a BitStream.", debugName, type.ToString().ToLower(), asStr));

			// ReSharper disable once LoopCanBeConvertedToQuery
			// ReSharper disable once ForCanBeConvertedToForeach
			for (var i = 0; i < asStr.Length; ++i)
			{
				if (asStr[i] > 0xff)
					throw new PeachException(string.Format("Error, {0} {1} value '{2}' contains unicode characters and could not be converted to a BitStream.", debugName, type.ToString().ToLower(), asStr));
			}

			return value;
		}

		public override void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("Blob");
			WritePitCommonAttributes(pit);
			WritePitCommonValue(pit);
			WritePitCommonChildren(pit);
			pit.WriteEndElement();
		}

		protected override Variant GenerateDefaultValue()
		{
			// Return a read-only slice of the DefaultValue
			// This way every data element has a different object
			// for its InternalValue, even multiple data elements
			// use the same DefaultValue object.

			if (DefaultValue.GetVariantType() == Variant.VariantType.BitStream)
			{
				var bs = (BitwiseStream)DefaultValue;
				bs.SeekBits(0, SeekOrigin.Begin);
				return new Variant(bs.SliceBits(bs.LengthBits));
			}

			System.Diagnostics.Debug.Assert(DefaultValue.GetVariantType() == Variant.VariantType.String);
			return new Variant(Encoding.ISOLatin1.GetBytes((string)DefaultValue));
		}
	}
}

// end
