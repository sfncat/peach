using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using Peach.Core;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.Text.RegularExpressions;
using Peach.Core.Runtime;
using String = Peach.Core.Dom.String;
using XmlCharacterData = Peach.Core.Dom.XmlCharacterData;

namespace Peach.Pro.Core.Analyzers
{
	[Analyzer("Xml", true)]
	[Analyzer("XmlAnalyzer")]
	[Analyzer("xml.XmlAnalyzer")]
	[Usage("<infile> <outfile>")]
	[Description("Generate a data model based on an XML document.")]
	[Serializable]
	public class XmlAnalyzer : Analyzer
	{
		public new static readonly bool supportParser = false;
		public new static readonly bool supportDataElement = true;
		public new static readonly bool supportCommandLine = true;
		public new static readonly bool supportTopLevel = false;
		private static readonly Regex nameSanitizerRegex = new Regex(@"[:\.]");

		private bool useFieldId = false;

		public XmlAnalyzer()
		{
		}

		public XmlAnalyzer(Dictionary<string, Variant> args)
		{
		}

		public override void asCommandLine(List<string> args)
		{
			if (args.Count != 2)
				throw new SyntaxException("Missing required arguments.");

			var inFile = args[0];
			var outFile = args[1];
			var data = new BitStream(File.ReadAllBytes(inFile));
			var model = new DataModel(Path.GetFileName(inFile).Replace(".", "_"));

			model.Add(new String() { stringType = StringType.utf8 });
			model[0].DefaultValue = new Variant(data);

			asDataElement(model[0], null);

			var settings = new XmlWriterSettings();
			settings.Encoding = System.Text.Encoding.UTF8;
			settings.Indent = true;

			using (var sout = new FileStream(outFile, FileMode.Create))
			using (var xml = XmlWriter.Create(sout, settings))
			{
				xml.WriteStartDocument();
				xml.WriteStartElement("Peach");

				model.WritePit(xml);

				xml.WriteEndElement();
				xml.WriteEndDocument();
			}
		}

		public override void asDataElement(DataElement parent, Dictionary<DataElement, Position> positions)
		{
			var strElement = parent as String;
			if (strElement == null)
				throw new PeachException("Error, XmlAnalyzer analyzer only operates on String elements!");

			useFieldId = !string.IsNullOrEmpty(parent.FieldId);

			var doc = new XmlDocument();

			try
			{
				try
				{
					var stream = (BitStream)strElement.Value;
					if (stream.Length == 0)
						return;

					var rdr = XmlReader.Create(stream, new XmlReaderSettings
					{
						DtdProcessing = DtdProcessing.Ignore,
						ValidationFlags = XmlSchemaValidationFlags.None,
						XmlResolver = null,
					});

					doc.Load(rdr);
				}
				catch
				{
					doc.LoadXml((string)strElement.InternalValue);
				}
			}
			catch (Exception ex)
			{
				throw new PeachException("Error, XmlAnalyzer failed to analyze element '" + parent.Name + "'.  " + ex.Message, ex);
			}

			var elem = new Peach.Core.Dom.XmlElement(strElement.Name);

			foreach (XmlNode node in doc.ChildNodes)
			{
				handleXmlNode(elem, node, strElement.stringType);
			}

			var decl = doc.FirstChild as XmlDeclaration;
			if (decl != null)
			{
				elem.version = decl.Version;
				elem.encoding = decl.Encoding;
				elem.standalone = decl.Standalone;
			}

			parent.parent[parent.Name] = elem;
		}

		private static string sanitizeXmlName(string name)
		{
			return nameSanitizerRegex.Replace(name, "_");
		}

		protected void handleXmlNode(Peach.Core.Dom.XmlElement elem, XmlNode node, StringType type)
		{
			if (node is XmlComment || node is XmlDeclaration || node is XmlEntity || node is XmlDocumentType)
				return;

			elem.elementName = node.Name;
			elem.ns = node.NamespaceURI;

			foreach (System.Xml.XmlAttribute attr in node.Attributes)
			{
				var strElem = makeString("value", attr.Value, type);
				var attrName = elem.UniqueName(sanitizeXmlName(attr.Name));
				var attrElem = new Peach.Core.Dom.XmlAttribute(attrName)
				{
					attributeName = attr.Name,
					ns = attr.NamespaceURI,
				};

				if (useFieldId)
					attrElem.FieldId = attrElem.Name;

				attrElem.Add(strElem);
				elem.Add(attrElem);
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (child is XmlCDataSection)
				{
					var data = new String("Value");
					data.DefaultValue = new Variant(child.Value);

					var childElem = new XmlCharacterData(elem.UniqueName("CDATA"));
					childElem.Add(data);

					if (useFieldId)
						childElem.FieldId = childElem.Name;

					elem.Add(childElem);
				}
				else if (child.Name == "#text")
				{
					var str = makeString("Value", child.Value, type);
					if (useFieldId)
						str.FieldId = str.Name;
					elem.Add(str);
				}
				else if (!child.Name.StartsWith("#"))
				{
					var name = sanitizeXmlName(child.Name);
					var childName = elem.UniqueName(name);
					var childElem = new Peach.Core.Dom.XmlElement(childName);
					if (useFieldId)
						childElem.FieldId = childElem.Name;

					elem.Add(childElem);

					handleXmlNode(childElem, child, type);
				}
			}
		}

		private static String makeString(string name, string value, StringType type)
		{
			var str = new String(name)
			{
				stringType = type,
				DefaultValue = new Variant(value),
			};

			var hint = new Hint("Peach.TypeTransform", "false");
			str.Hints.Add(hint.Name, hint);

			return str;
		}
	}
}
