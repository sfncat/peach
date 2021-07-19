using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Peach.Core;

namespace Peach.Pro.Core
{
	public static class XmlTools
	{
		static Dictionary<Type, XmlSchema> schemas = new Dictionary<Type, XmlSchema>();

		static Dictionary<string, XmlSchemaSimpleType> simpleTypes = MakeSimpleTypes();

		private static readonly Dictionary<Type, XmlSerializer> serializers = new Dictionary<Type, XmlSerializer>();

		public static XmlSerializer GetSerializer(Type t)
		{
			lock (serializers)
			{
				XmlSerializer ret;
				if (!serializers.TryGetValue(t, out ret))
				{
					ret = new XmlSerializer(t);
					serializers.Add(t, ret);
				}
				return ret;
			}
		}

		public static XmlSchema GetSchema(Type type)
		{
			var xmlRoot = type.GetAttributes<XmlRootAttribute>(null).FirstOrDefault();
			if (xmlRoot == null)
				throw new ArgumentException("Type '{0}' is missing XmlRootAttribute.".Fmt(type.FullName));

			lock (schemas)
			{
				XmlSchema ret;
				if (schemas.TryGetValue(type, out ret))
					return ret;

				var importer = new XmlReflectionImporter();
				var schema = new XmlSchemas();
				var exporter = new XmlSchemaExporter(schema);

				var xmlTypeMapping = importer.ImportTypeMapping(type);
				exporter.ExportTypeMapping(xmlTypeMapping);

				PrintSchema(schema[0]);

				foreach (XmlSchemaObject obj in schema[0].Items)
				{
					if (obj is XmlSchemaElement || obj is XmlSchemaSimpleType)
						continue;

					var asType = (XmlSchemaComplexType)obj;

					FixupComplexType(asType);

					FixupAttributes(asType.Attributes);

					var content = asType.ContentModel as XmlSchemaComplexContent;
					if (content != null)
					{
						var ext = (XmlSchemaComplexContentExtension)content.Content;

						// If xs:complexType isMixed='true' we need to set
						// the xs:complexContent to have isMixed='true' also
						if (content.IsMixed == false && asType.IsMixed == true)
							content.IsMixed = true;

						FixupAttributes(ext.Attributes);
					}
				}

				var errors = new StringBuilder();

				ValidationEventHandler handler = (o, e) =>
				{
					var ex = e.Exception;

					errors.AppendFormat("Line: {0}, Position: {1} - ", ex.LineNumber, ex.LinePosition);
					errors.Append(ex.Message);
					errors.AppendLine();
				};

				schema.Compile(handler, false);

				if (errors.Length > 0)
					throw new PeachException("{0} schema failed to generate: \r\n{0}".Fmt(type.Name, errors));

				ret = schema[0];

				PrintSchema(ret);

				schemas.Add(type, ret);
			
				return ret;
			}
		}

		[Conditional("DISABLED")]
		private static void PrintSchema(XmlSchema schema)
		{
			var sb = new StringBuilder();
			var writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true });
			schema.Write(writer);
			Console.WriteLine(sb);
		}

		private static Dictionary<string, XmlSchemaSimpleType> MakeSimpleTypes()
		{
			// Mono has issues with xs:int, xs:unsignedInt, xs:long, xs:unsignedLong
			// So use xs:integer with min/max restrictions

			var ret = new Dictionary<string, XmlSchemaSimpleType>();

			ret["int"] = MakeSimpleType(typeof(int));
			ret["unsignedInt"] = MakeSimpleType(typeof(uint));
			ret["long"] = MakeSimpleType(typeof(long));
			ret["unsignedLong"] = MakeSimpleType(typeof(ulong));

			return ret;
		}

		private static XmlSchemaSimpleType MakeSimpleType(Type type)
		{
			var content = new XmlSchemaSimpleTypeRestriction()
			{
				BaseTypeName = new XmlQualifiedName("integer", XmlSchema.Namespace),
			};

			var minFacet = new XmlSchemaMinInclusiveFacet
			{
				Value = type.GetField("MinValue", BindingFlags.Static | BindingFlags.Public).GetValue(null).ToString()
			};
			content.Facets.Add(minFacet);

			var maxFacet = new XmlSchemaMaxInclusiveFacet
			{
				Value = type.GetField("MaxValue", BindingFlags.Static | BindingFlags.Public).GetValue(null).ToString()
			};
			content.Facets.Add(maxFacet);

			var ret = new XmlSchemaSimpleType()
			{
				Content = content,
			};

			return ret;
		}

		private static void FixupAttributes(XmlSchemaObjectCollection attributes)
		{
			foreach (var attr in attributes.Cast<XmlSchemaAttribute>())
			{
				attr.Use = attr.DefaultValue == null ? XmlSchemaUse.Required : XmlSchemaUse.Optional;

				// If needed, replace numeric data types with xs:integer and min/max restrictions
				XmlSchemaSimpleType simpleType;

				if (attr.SchemaTypeName != null &&
				    attr.SchemaTypeName.Namespace == XmlSchema.Namespace &&
				    simpleTypes.TryGetValue(attr.SchemaTypeName.Name, out simpleType))
				{
					attr.SchemaTypeName = null;
					attr.SchemaType = simpleType;
				}
			}
		}

		private static void MoveElement(XmlSchemaParticle elem, XmlSchemaGroupBase group)
		{
			var asGroup = elem as XmlSchemaGroupBase;
			if (asGroup == null)
			{
				elem.MaxOccursString = "1";
				group.Items.Add(elem);
			}
			else
			{
				foreach (var item in asGroup.Items)
					group.Items.Add(item);
			}
		}

		private static void FixupComplexType(XmlSchemaComplexType type)
		{
			var seq = type.Particle as XmlSchemaSequence;

			if (seq == null)
				return;

			// If the complex type is a sequence, look at how many times each
			// element can occur and modify for order independence.
			// If all children occur 0/1 times, replace xs:sequence with xs:all
			// Otherwise, replace xs:sequence with xs:choice

			var all = new XmlSchemaAll();
			var choice = new XmlSchemaChoice
			{
				MinOccursString = "0",
				MaxOccursString = "unbounded"
			};

			foreach (var item in seq.Items.OfType<XmlSchemaParticle>())
			{
				if (item.MaxOccursString == "unbounded")
					MoveElement(item, choice);
				else
					MoveElement(item, all);
			}

			if (choice.Items.Count > 0)
			{
				if (all.Items.Count > 0)
				{
					//throw new NotSupportedException("Complex type '{0}' uses mix of maxOccurs='unbounded' and maxOccurs='1'.".Fmt(type.Name));
					MoveElement(all, choice);
				}

				type.Particle = choice;
			}
			else
			{
				type.Particle = all;
			}
		}

		#region Reader

		class Reader<T> : IDisposable
		{
			StringBuilder errors;
			XmlReaderSettings settings;
			XmlParserContext parserCtx;
			XmlReader xmlReader;

			public Reader(string inputUri)
			{
				Initialize();

				xmlReader = XmlReader.Create(inputUri, settings, parserCtx);
			}

			public Reader(Stream stream)
			{
				Initialize();
				
				xmlReader = XmlReader.Create(stream, settings, parserCtx);
			}

			public Reader(TextReader textReader)
			{
				Initialize();

				xmlReader = XmlReader.Create(textReader, settings, parserCtx);
			}

			public void Dispose()
			{
				if (xmlReader != null)
					xmlReader.Close();
			}

			private Exception MakeError(Exception inner, string msg, params object[] args)
			{
				var suffix = msg.Fmt(args);

				if (string.IsNullOrEmpty(xmlReader.BaseURI))
					return new PeachException("Error, {0} file {1}".Fmt(typeof(T).Name, suffix), inner);

				return new PeachException("Error, {0} file '{1}' {2}".Fmt(typeof(T).Name, xmlReader.BaseURI, suffix), inner);
			}

			public T Deserialize()
			{
				try
				{
					var s = GetSerializer(typeof(T));
					var o = s.Deserialize(xmlReader);
					var r = (T)o;

					if (errors.Length > 0)
						throw MakeError(null, "failed to validate:\r\n{0}", errors);

					return r;
				}
				catch (XmlException ex)
				{
					// mono throws XmlException
					throw MakeError(ex, "failed to load. {0}", ex.Message);
				}
				catch (InvalidOperationException ex)
				{
					// microsoft throws InvalidOperationException with an inner XmlException
					var inner = ex.InnerException as XmlException;
					if (inner != null && !string.IsNullOrEmpty(inner.Message))
						throw MakeError(ex, "failed to load. {0}", inner.Message);
					else
						throw MakeError(ex, "failed to load. {0}", ex.Message);
				}
			}

			void Initialize()
			{
				var schema = GetSchema(typeof(T));

				var schemas = new XmlSchemaSet();
				schemas.Add(schema);

				errors = new StringBuilder();

				settings = new XmlReaderSettings();
				settings.ValidationType = ValidationType.Schema;
				settings.Schemas = schemas;
				settings.NameTable = new NameTable();
				settings.ValidationEventHandler += delegate(object sender, ValidationEventArgs e)
				{
					var ex = e.Exception;

					errors.AppendFormat("Line: {0}, Position: {1} - ", ex.LineNumber, ex.LinePosition);
					errors.Append(ex.Message);
					errors.AppendLine();
				};

				// Default the namespace to the namespace of the XmlRootAttribute
				var nsMgr = new XmlNamespaceManager(settings.NameTable);

				if (schema.TargetNamespace != null)
					nsMgr.AddNamespace("", schema.TargetNamespace);

				parserCtx = new XmlParserContext(settings.NameTable, nsMgr, null, XmlSpace.Default);
			}
		}

		#endregion

		#region Writer

		class Writer<T> : IDisposable
		{
			XmlWriterSettings settings;
			XmlWriter xmlWriter;

			public Writer(string outputFileName)
			{
				Initialize();

				xmlWriter = XmlWriter.Create(outputFileName, settings);
			}

			public Writer(Stream stream)
			{
				Initialize();

				xmlWriter = XmlWriter.Create(stream, settings);
			}

			public Writer(TextWriter textWriter)
			{
				Initialize();

				xmlWriter = XmlWriter.Create(textWriter, settings);
			}

			public void Dispose()
			{
				if (xmlWriter != null)
					xmlWriter.Close();
			}

			public void Serialize(T obj)
			{
				var s = GetSerializer(typeof(T));
				s.Serialize(xmlWriter, obj);
			}

			void Initialize()
			{
				settings = new XmlWriterSettings()
				{
					Encoding = System.Text.Encoding.UTF8,
					Indent = true,
				};
			}
		}

		#endregion

		public static T Deserialize<T>(string inputUri)
		{
			using (var rdr = new Reader<T>(inputUri))
			{
				return rdr.Deserialize();
			}
		}

		public static T Deserialize<T>(Stream stream)
		{
			using (var rdr = new Reader<T>(stream))
			{
				return rdr.Deserialize();
			}
		}

		public static T Deserialize<T>(TextReader textReader)
		{
			using (var rdr = new Reader<T>(textReader))
			{
				return rdr.Deserialize();
			}
		}

		public static void Serialize<T>(string outputFileName, T obj)
		{
			using (var writer = new Writer<T>(outputFileName))
			{
				writer.Serialize(obj);
			}
		}

		public static void Serialize<T>(Stream stream, T obj)
		{
			using (var writer = new Writer<T>(stream))
			{
				writer.Serialize(obj);
			}
		}

		public static void Serialize<T>(TextWriter textWriter, T obj)
		{
			using (var writer = new Writer<T>(textWriter))
			{
				writer.Serialize(obj);
			}
		}
	}
}
