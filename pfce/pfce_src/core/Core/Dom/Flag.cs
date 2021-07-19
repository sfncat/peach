


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Globalization;
using System.Xml;
using System.Linq;

using Peach.Core.Analyzers;

namespace Peach.Core.Dom
{
	[PitParsable("Flag")]
	[DataElement("Flag", DataElementTypes.NonDataElements)]
	[Parameter("name", typeof(string), "Element name", "")]
	[Parameter("fieldId", typeof(string), "Element field ID", "")]
	[Parameter("position", typeof(int), "Bit position of flag")]
	[Parameter("size", typeof(int), "size in bits")]
	[Parameter("value", typeof(string), "Default value", "")]
	[Parameter("valueType", typeof(ValueType), "Format of value attribute", "string")]
	[Parameter("token", typeof(bool), "Is element a token", "false")]
	[Parameter("mutable", typeof(bool), "Is element mutable", "true")]
	[Serializable]
	public class Flag : Number
	{
		protected int _position = 0;

		public Flag()
		{
		}

		public Flag(string name)
			: base(name)
		{
		}

		/// <summary>
		/// Determines if a flag at position 'position' with size 'size' overlapps this element
		/// </summary>
		/// <param name="position">Position to test</param>
		/// <param name="size">Size to test</param>
		/// <returns>True if overlapps, false otherwise</returns>
		protected bool Overlapps(int position, int size)
		{
			if (position >= this.position)
			{
				if (position < (this.position + this.lengthAsBits))
					return true;
			}
			else
			{
				int end = position + size;
				if (end > this.position && end <= (this.position + size))
					return true;
			}

			return false;
		}

		public new static DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			var flags = parent as Flags;

			if (flags == null)
				throw new PeachException("Error, {0} has unsupported child element '{1}'.".Fmt(parent.debugName, node.Name));

			var flag = DataElement.Generate<Flag>(node, parent);

			int position = node.getAttrInt("position");
			int size = node.getAttrInt("size");

			if (position < 0 || size < 0 || (position + size) > parent.lengthAsBits)
				throw new PeachException("Error, " + flag.debugName + " is placed outside its parent.");

			if (flags.LittleEndian)
				position = (int)parent.lengthAsBits - size - position;

			foreach (var other in parent.OfType<Flag>())
			{
				if (flag.Name != other.Name && other.Overlapps(position, size))
					throw new PeachException("Error, " + flag.debugName + " overlapps with " + other.debugName + ".");
			}

			flag.position = position;
			flag.lengthType = LengthType.Bits;
			flag.length = size;

			// The individual flag is always big endian, it is up to the flags container
			// to change the order after all the flags are packed.
			flag.LittleEndian = false;

			context.handleCommonDataElementAttributes(node, flag);
			context.handleCommonDataElementChildren(node, flag);
			context.handleCommonDataElementValue(node, flag);

			return flag;
		}

		public override void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement(elementType);

			if (referenceName != null)
				pit.WriteAttributeString("ref", referenceName);

			pit.WriteAttributeString("size", lengthAsBits.ToString(CultureInfo.InvariantCulture));
			pit.WriteAttributeString("position", position.ToString(CultureInfo.InvariantCulture));

			if (!LittleEndian)
				pit.WriteAttributeString("endian", "big");

			WritePitCommonAttributes(pit);
			WritePitCommonChildren(pit);
			WritePitCommonValue(pit);

			pit.WriteEndElement();
		}


		public int position
		{
			get { return _position; }
			set
			{
				_position = value;
				Invalidate();
			}
		}
	}
}

// end
