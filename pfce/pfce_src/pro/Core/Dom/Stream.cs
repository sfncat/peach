using System;
using System.Xml;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Pro.Core.Dom
{
	/// <summary>
	/// Block element
	/// </summary>
	[DataElement("Stream")]
	[PitParsable("Stream")]
	[Parameter("name", typeof(string), "Element name", "")]
	[Parameter("fieldId", typeof(string), "Element field ID", "")]
	[Parameter("ref", typeof(string), "Element to reference", "")]
	[Parameter("streamName", typeof(string), "Stream name")]
	[Parameter("streamAttribute", typeof(int), "Stream attributes", "0")]
	[Parameter("length", typeof(uint?), "Length in data element", "")]
	[Parameter("lengthType", typeof(LengthType), "Units of the length attribute", "bytes")]
	[Parameter("mutable", typeof(bool), "Is element mutable", "true")]
	[Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
	[Parameter("minOccurs", typeof(int), "Minimum occurances", "1")]
	[Parameter("maxOccurs", typeof(int), "Maximum occurances", "1")]
	[Parameter("occurs", typeof(int), "Actual occurances", "1")]
	[Serializable]
	public class Stream : Block
	{
		public Stream()
		{
			Initialize();
		}

		public Stream(string name)
			: base(name)
		{
			Initialize();
		}

		private void Initialize()
		{
			// It seems we don't want the stream element
			// to be mutable, only the children elements.
			isMutable = false;

			// Stream a container with three things:
			// 1) <String> Name
			// 2) <Number> Attribute
			// 3) <Block>  Content

			var nameElem = new Peach.Core.Dom.String("Name")
			{
				stringType = StringType.ascii,
			};

			var attrElem = new Peach.Core.Dom.Number("Attribute")
			{
				Signed = false,
				LittleEndian = false,
				lengthType = LengthType.Bits,
				length = 32,
			};

			var blockElem = new Peach.Core.Dom.Block("Content");

			Add(nameElem);
			Add(attrElem);
			Add(blockElem);
		}

		public static new DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			if (node.Name != "Stream")
				return null;

			Stream stream = null;

			if (node.hasAttr("ref"))
			{
				var name = node.getAttr("name", null);
				var refName = node.getAttrString("ref");
				var dom = ((DataModel)parent.root).dom;
				var refObj = dom.getRef(refName, parent);

				if (refObj == null)
					throw new PeachException("Error, Stream {0}could not resolve ref '{1}'. XML:\n{2}".Fmt(
						name == null ? "" : "'" + name + "' ", refName, node.OuterXml));

				if (!(refObj is Stream))
					throw new PeachException("Error, Stream {0}resolved ref '{1}' to unsupported element {2}. XML:\n{3}".Fmt(
						name == null ? "" : "'" + name + "' ", refName, refObj.debugName, node.OuterXml));

				if (string.IsNullOrEmpty(name))
					name = new Stream().Name;

				stream = refObj.Clone(name) as Stream;
				stream.parent = parent;
				stream.isReference = true;
				stream.referenceName = refName;
			}
			else
			{
				stream = DataElement.Generate<Stream>(node, parent);
				stream.parent = parent;
			}

			stream.FieldId = node.getAttr("fieldId", null);

			var nameElem = stream["Name"];
			nameElem.DefaultValue = new Variant(node.getAttrString("streamName"));
			nameElem.isMutable = node.getAttr("mutable", nameElem.isMutable);

			var attrElem = stream["Attribute"];
			attrElem.DefaultValue = new Variant(node.getAttr("streamAttribute", "0"));
			attrElem.isMutable = node.getAttr("mutable", attrElem.isMutable);

			var contentElem = stream["Content"] as Block;
			context.handleCommonDataElementAttributes(node, contentElem);
			context.handleCommonDataElementChildren(node, contentElem);
			context.handleDataElementContainer(node, contentElem);

			contentElem.FieldId = null;

			return stream;
		}

		protected override Variant GenerateInternalValue()
		{
			DataElement elem;
			if (!TryGetValue("Content", out elem))
				return new Variant(new BitStream());

			return elem.InternalValue;
		}

		public override void Crack(DataCracker context, BitStream data, long? size)
		{
			throw new NotSupportedException();
		}
	}
}
