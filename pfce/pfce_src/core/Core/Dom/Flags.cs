


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Globalization;
using System.Xml;

using Peach.Core.Analyzers;
using Peach.Core.IO;
using Peach.Core.Cracker;
using System.IO;

namespace Peach.Core.Dom
{
	[DataElement("Flags", DataElementTypes.NonDataElements)]
	[PitParsable("Flags")]
	[DataElementChildSupported("Flag")]
	[DataElementChildSupported("Placement")]
	[Parameter("name", typeof(string), "Element name", "")]
	[Parameter("fieldId", typeof(string), "Element field ID", "")]
	[Parameter("size", typeof(uint), "size in bits.  Typically [8, 16, 24, 32, 64]")]
	[Parameter("endian", typeof(string), "Byte order of number (default 'little')", "little")]
	[Parameter("token", typeof(bool), "Is element a token", "false")]
	[Parameter("mutable", typeof(bool), "Is element mutable", "true")]
	[Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
	[Parameter("minOccurs", typeof(int), "Minimum occurances", "1")]
	[Parameter("maxOccurs", typeof(int), "Maximum occurances", "1")]
	[Parameter("occurs", typeof(int), "Actual occurances", "1")]
	[Serializable]
	public class Flags : DataElementContainer
	{
		protected int _size = 0;
		protected bool _isLittleEndian = true;

		public Flags()
		{
		}

		public Flags(string name)
			: base(name)
		{
		}

		public override void Crack(DataCracker context, BitStream data, long? size)
		{
			BitStream sizedData = ReadSizedData(data, size);
			long pos = sizedData.PositionBits;

			if (_isLittleEndian)
			{
				ulong value;
				int len = sizedData.ReadBits(out value, (int)lengthAsBits);
				System.Diagnostics.Debug.Assert(len == lengthAsBits);
				value <<= (8 - (len % 8)) % 8;
				var buf = Endian.Little.GetBytes(value, len);

				sizedData = new BitStream(buf);
				sizedData.SetLengthBits(lengthAsBits);
				pos = 0;
			}

			foreach (DataElement child in this)
			{
				var flag = child as Flag;

				if (flag == null)
					throw new CrackingFailure("Found non-Flag child '{0}'.".Fmt(flag.Name), this, data);

				sizedData.SeekBits(flag.position + pos, SeekOrigin.Begin);
				context.CrackData(child, sizedData);
			}
		}

		public static DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			if (node.Name != "Flags")
				return null;

			var flags = DataElement.Generate<Flags>(node, parent);

			string strSize = null;
			if (node.hasAttr("size"))
				strSize = node.getAttrString("size");
			if (strSize == null)
				strSize = context.getDefaultAttr(typeof(Flags), "size", null);
			if (strSize == null)
				throw new PeachException("Error, Flags elements must have 'size' attribute!");

			int size;

			if (!int.TryParse(strSize, out size))
				throw new PeachException("Error, " + flags.Name + " size attribute is not valid number.");

			if (size < 1 || size > 64)
				throw new PeachException(string.Format("Error, unsupported size '{0}' for {1}.", size, flags.debugName));

			flags.lengthType = LengthType.Bits;
			flags.length = size;

			string strEndian = null;
			if (node.hasAttr("endian"))
				strEndian = node.getAttrString("endian");
			if (strEndian == null)
				strEndian = context.getDefaultAttr(typeof(Flags), "endian", null);

			if (strEndian != null)
			{
				switch (strEndian.ToLower())
				{
					case "little":
						flags.LittleEndian = true;
						break;
					case "big":
						flags.LittleEndian = false;
						break;
					case "network":
						flags.LittleEndian = false;
						break;
					default:
						throw new PeachException(
							string.Format("Error, unsupported value '{0}' for 'endian' attribute on {1}.", strEndian, flags.debugName));
				}
			}

			context.handleCommonDataElementAttributes(node, flags);
			context.handleCommonDataElementChildren(node, flags);

			foreach (XmlNode child in node.ChildNodes)
			{
				// Looking for "Flag" element
				if (child.Name == "Flag")
				{
					flags.Add(Flag.PitParser(context, child, flags));
				}
			}

			return flags;
		}

		public override void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement(elementType);

			if (referenceName != null)
				pit.WriteAttributeString("ref", referenceName);

			pit.WriteAttributeString("size", lengthAsBits.ToString(CultureInfo.InvariantCulture));

			if (!LittleEndian)
				pit.WriteAttributeString("endian", "big");

			WritePitCommonAttributes(pit);
			WritePitCommonChildren(pit);

			foreach (var obj in this)
				obj.WritePit(pit);

			pit.WriteEndElement();
		}


		public bool LittleEndian
		{
			get { return _isLittleEndian; }
			set
			{
				_isLittleEndian = value;
				Invalidate();
			}
		}

		protected override Variant GenerateInternalValue()
		{
			BitStream bits = new BitStream();

			long shift;

			if (_isLittleEndian)
			{
				// Expand to 64 bits and skip remaining bits at the beginning
				shift = 64 - lengthAsBits;
				bits.SetLengthBits(64);
			}
			else
			{
				// Expand to 'size' bits
				shift = 0;
				bits.SetLengthBits(lengthAsBits);
			}

			foreach (DataElement child in this)
			{
				var flag = child as Flag;
				if (flag == null)
					throw new ApplicationException("Flags has child thats not a flag!");

				bits.SeekBits(flag.position + shift, SeekOrigin.Begin);
				flag.Value.SeekBits(0, SeekOrigin.Begin);
				flag.Value.CopyTo(bits);
			}


			if (_isLittleEndian)
			{
				bits.SeekBits(0, SeekOrigin.Begin);
				ulong value;
				int len = bits.ReadBits(out value, 64);
				System.Diagnostics.Debug.Assert(len == 64);
				ulong final = Endian.Little.GetBits(value, (int)lengthAsBits);
				bits.Seek(0, SeekOrigin.Begin);
				bits.WriteBits(final, (int)lengthAsBits);
				bits.SetLengthBits(bits.PositionBits);
			}

			bits.Seek(0, SeekOrigin.Begin);

			return new Variant(bits);
		}
	}
}

// end
