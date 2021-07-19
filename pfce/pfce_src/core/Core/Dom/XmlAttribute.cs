


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Xml;

using Peach.Core.Analyzers;

namespace Peach.Core.Dom
{
	[DataElement("XmlAttribute")]
	[PitParsable("XmlAttribute")]
	[Parameter("name", typeof(string), "Name of element", "")]
	[Parameter("fieldId", typeof(string), "Element field ID", "")]
	[Parameter("attributeName", typeof(string), "Name of XML attribute")]
	[Parameter("ns", typeof(string), "XML Namespace", "")]
	[Parameter("length", typeof(uint?), "Length in data element", "")]
	[Parameter("lengthType", typeof(LengthType), "Units of the length attribute", "bytes")]
	[Parameter("mutable", typeof(bool), "Is element mutable", "true")]
	[Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
	[Parameter("minOccurs", typeof(int), "Minimum occurances", "1")]
	[Parameter("maxOccurs", typeof(int), "Maximum occurances", "1")]
	[Parameter("occurs", typeof(int), "Actual occurances", "1")]
	[Serializable]
	public class XmlAttribute : DataElementContainer
	{
		string _attributeName = null;
		string _ns = null;

		public XmlAttribute()
		{
		}

		public XmlAttribute(string name)
			: base(name)
		{
		}

		public static DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			if (node.Name != "XmlAttribute" || !(parent is XmlElement))
				return null;

			var xmlAttribute = DataElement.Generate<XmlAttribute>(node, parent);

			xmlAttribute.attributeName = node.getAttrString("attributeName");

			if (node.hasAttr("ns"))
				xmlAttribute.ns = node.getAttrString("ns");

			context.handleCommonDataElementAttributes(node, xmlAttribute);
			context.handleCommonDataElementChildren(node, xmlAttribute);
			context.handleDataElementContainer(node, xmlAttribute);

			return xmlAttribute;
		}

		public override void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("XmlAttribute");

			pit.WriteAttributeString("attributeName", attributeName);
			if (!string.IsNullOrEmpty(ns))
				pit.WriteAttributeString("ns", ns);

			WritePitCommonAttributes(pit);
			WritePitCommonChildren(pit);

			foreach (var child in this)
				child.WritePit(pit);

			pit.WriteEndElement();
		}

		/// <summary>
		/// XML attribute name
		/// </summary>
		public virtual string attributeName
		{
			get { return _attributeName; }
			set
			{
				_attributeName = value;
				// DefaultValue isn't used internally, but this makes the Validator show helpful text
				_defaultValue = new Variant("'{0}' Attribute".Fmt(value));
				Invalidate();
			}
		}

		/// <summary>
		/// XML Namespace for element
		/// </summary>
		public virtual string ns
		{
			get { return _ns; }
			set
			{
				_ns = value;
				Invalidate();
			}
		}

		protected override Variant GenerateInternalValue()
		{
			return null;
		}
	}
}

// end
