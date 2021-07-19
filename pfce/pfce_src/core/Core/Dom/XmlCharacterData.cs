
using System;
using System.Text;
using System.Xml;

using Peach.Core.Analyzers;
using Peach.Core.IO;
using NLog;
using System.Security;
using System.IO;
using System.Reflection;

namespace Peach.Core.Dom
{
	[DataElement("XmlCharacterData")]
	[PitParsable("XmlCharacterData")]
	[Parameter("name", typeof(string), "Name of element", "")]
	[Parameter("fieldId", typeof(string), "Element field ID", "")]
	[Parameter("length", typeof(uint?), "Length in data element", "")]
	[Parameter("lengthType", typeof(LengthType), "Units of the length attribute", "bytes")]
	[Parameter("mutable", typeof(bool), "Is element mutable", "true")]
	[Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
	[Serializable]
	public class XmlCharacterData : DataElementContainer
	{
		protected static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public string version { get; set; }
		public string encoding { get; set; }
		public string standalone { get; set; }

		public XmlCharacterData()
		{
		}

		public XmlCharacterData(string name)
			: base(name)
		{
		}

		public static DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			if (node.Name != "XmlCharacterData")
				return null;

			var xmlElement = DataElement.Generate<XmlCharacterData>(node, parent);

			context.handleCommonDataElementAttributes(node, xmlElement);
			context.handleCommonDataElementChildren(node, xmlElement);
			context.handleDataElementContainer(node, xmlElement);

			return xmlElement;
		}

		public override void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("XmlCharacterData");

			WritePitCommonAttributes(pit);
			WritePitCommonChildren(pit);

			foreach (var child in this)
				child.WritePit(pit);

			pit.WriteEndElement();
		}

		protected static string ElemToStr(DataElement elem)
		{
			var iv = elem.InternalValue;
			if (iv.GetVariantType() != Variant.VariantType.BitStream)
				return (string)iv;

			var bs = elem.Value;
			var ret = new BitReader(bs).ReadString(Encoding.ISOLatin1);
			bs.Seek(0, System.IO.SeekOrigin.Begin);
			return ret;
		}

		protected static string ContToStr(DataElementContainer cont)
		{
			var sb = new StringBuilder();
			foreach (var item in cont)
				sb.Append(ElemToStr(item));
			return sb.ToString();
		}

		static DataElement ResolveChoices(DataElement elem)
		{
			while (true)
			{
				var asChoice = elem as Choice;
				if (asChoice == null)
					return elem;

				if (asChoice.SelectedElement == null)
					asChoice.SelectDefault();

				elem = asChoice.SelectedElement;
			}
		}

		public void GenXmlNode(XmlDocument doc, XmlNode xmlParent)
		{
			var node = doc.CreateCDataSection(ContToStr(this));
			xmlParent.AppendChild(node);
		}

		protected override Variant GenerateInternalValue()
		{
			if (mutationFlags.HasFlag(MutateOverride.TypeTransform))
				return MutatedValue;

			var doc = new XmlDocument();

			if (!string.IsNullOrEmpty(version) || !string.IsNullOrEmpty(encoding) || !string.IsNullOrEmpty(standalone))
			{
				var decl = doc.CreateXmlDeclaration(version, encoding, standalone);
				doc.AppendChild(decl);
			}

			var node = doc.CreateCDataSection(ContToStr(this));
			var xml = node.OuterXml;

			var stream = new BitStream();
			var writer = new BitWriter(stream);
			writer.WriteString(xml);

			stream.Position = 0;
			return new Variant(stream);
		}

		protected override BitwiseStream InternalValueToBitStream()
		{
			return (BitwiseStream)InternalValue;
		}

		class PeachXmlWriter : XmlTextWriter
		{
#if MONO
			private static readonly bool MonoRawMethod = false;

			static PeachXmlWriter()
			{
				var version = Platform.MonoRuntimeVersion;

				if (version.Major > 4 || (version.Major == 4 && version.Minor >= 2))
					MonoRawMethod = true;
			}
#endif

			public PeachXmlWriter(BitStream stream, string encoding)
				: base(stream, Encoding.GetEncoding(encoding).RawEncoding)
			{
			}

			public override void WriteString(string text)
			{
#if MONO
				if(MonoRawMethod)
				{
					var encoded = SecurityElement.Escape(text);
					char[] raw = encoded.ToCharArray();

					WriteRaw(raw, 0, raw.Length);
				}
				else
				{
					base.WriteString(text);
				}
#else
				var encoded = SecurityElement.Escape(text);
				char[] raw = encoded.ToCharArray();

				WriteRaw(raw, 0, raw.Length);
#endif
			}
		}
	}
}

// end
