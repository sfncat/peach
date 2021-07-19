


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Xml;

using Peach.Core.Analyzers;
using Peach.Core.IO;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Block element
	/// </summary>
	[DataElement("Block")]
	[PitParsable("Block")]
	[Parameter("name", typeof(string), "Element name", "")]
	[Parameter("fieldId", typeof(string), "Element field ID", "")]
	[Parameter("ref", typeof(string), "Element to reference", "")]
	[Parameter("length", typeof(uint?), "Length in data element", "")]
	[Parameter("lengthType", typeof(LengthType), "Units of the length attribute", "bytes")]
	[Parameter("mutable", typeof(bool), "Is element mutable", "true")]
	[Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
	[Parameter("minOccurs", typeof(int), "Minimum occurances", "1")]
	[Parameter("maxOccurs", typeof(int), "Maximum occurances", "1")]
	[Parameter("occurs", typeof(int), "Actual occurances", "1")]
	[Serializable]
	public class Block : DataElementContainer
	{
		public Block()
		{
		}

		public Block(string name)
			: base(name)
		{
		}

		public static DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			if (node.Name != "Block")
				return null;

			Block block = null;

			if (node.hasAttr("ref"))
			{
				var name = node.getAttr("name", null);
				var refName = node.getAttrString("ref");
				var dom = ((DataModel)parent.root).dom;
				var refObj = dom.getRef(refName, parent);

				if (refObj == null)
					throw new PeachException("Error, Block {0}could not resolve ref '{1}'. XML:\n{2}".Fmt(
						name == null ? "" : "'" + name + "' ", refName, node.OuterXml));

				if (!(refObj is Block))
					throw new PeachException("Error, Block {0}resolved ref '{1}' to unsupported element {2}. XML:\n{3}".Fmt(
						name == null ? "" : "'" + name + "' ", refName, refObj.debugName, node.OuterXml));
				
				if (string.IsNullOrEmpty(name))
					name = new Block().Name;

				block = refObj.Clone(name) as Block;
				block.parent = parent;
				block.isReference = true;
				block.referenceName = refName;
			}
			else
			{
				block = Generate<Block>(node, parent);
				block.parent = parent;
			}

			context.handleCommonDataElementAttributes(node, block);
			context.handleCommonDataElementChildren(node, block);
			context.handleDataElementContainer(node, block);

			return block;
		}

		public override void WritePit(XmlWriter pit)
		{
			var elem = elementType;

			// If we reference another element and are not a root
			// element then force our name to <Block>
			if (referenceName != null && parent != null)
				elem = "Block";

			pit.WriteStartElement(elem);

			if(referenceName != null)
				pit.WriteAttributeString("ref", referenceName);

			WritePitCommonAttributes(pit);
			WritePitCommonChildren(pit);

			foreach (var obj in this)
				obj.WritePit(pit);

			pit.WriteEndElement();
		}

		protected override Variant GenerateDefaultValue()
		{
			var stream = new BitStreamList() { Name = fullName };

			foreach (var child in this)
			{
				var val = child.Value;
				val.Name = child.fullName;
				stream.Add(val);
			}

			return new Variant(stream);
		}
	}
}

// end
