using System;
using System.Xml;

using NLog;

using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Core.IO;
// ReSharper disable DoNotCallOverridableMethodsInConstructor

namespace Peach.Pro.Core.Dom
{
	[DataElement("Bool", DataElementTypes.Hint)]
	[DataElementChildSupported("Placement")]
	[PitParsable("Bool")]
	[Parameter("name", typeof(string), "Name of element", "")]
	[Parameter("fieldId", typeof(string), "Element field ID", "")]
	[Parameter("mutable", typeof(bool), "Is element mutable", "true")]
	[Parameter("value", typeof(string), "Default value", "")]
	[Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
	[Parameter("minOccurs", typeof(int), "Minimum occurances", "1")]
	[Parameter("maxOccurs", typeof(int), "Maximum occurances", "1")]
	[Parameter("occurs", typeof(int), "Actual occurances", "1")]
	[Serializable]
	public class Bool : Number
	{
		protected static NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public Bool()
		{
			lengthType = LengthType.Bits;
			length = 1;
		}

		public Bool(string name)
			: base(name)
		{
			lengthType = LengthType.Bits;
			length = 1;
		}

		public static new DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			if (node.Name != "Bool")
				return null;

			var elem = Generate<Bool>(node, parent);

			context.handleCommonDataElementAttributes(node, elem);
			context.handleCommonDataElementChildren(node, elem);
			context.handleCommonDataElementValue(node, elem);

			return elem;
		}

		public override void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("Bool");

			WritePitCommonAttributes(pit);
			WritePitCommonValue(pit);
			WritePitCommonChildren(pit);

			pit.WriteEndElement();
		}

		protected override BitwiseStream InternalValueToBitStream()
		{
			return new BitStream();
		}
	}
}

