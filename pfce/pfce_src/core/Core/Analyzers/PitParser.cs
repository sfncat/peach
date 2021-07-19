


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Xml;
using System.Xml.Schema;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;
using System.Linq;

using NLog;
using Peach.Core.Agent;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.Net;
using Monitor = Peach.Core.Dom.Monitor;

namespace Peach.Core.Analyzers
{
	/// <summary>
	/// This is the default analyzer for Peach.  It will
	/// parse a Peach PIT file (XML document) into a Peach DOM.
	/// </summary>
	public class PitParser : Analyzer
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public new static readonly bool supportParser = true;
		public new static readonly bool supportDataElement = false;
		public new static readonly bool supportCommandLine = false;
		public new static readonly bool supportTopLevel = false;

		/// <summary>
		/// args key for passing a dictionary of defined values to replace.
		/// </summary>
		public static string DEFINED_VALUES = "DefinedValues";
		public static string USED_DEFINED_VALUES = "UsedDefinedValues";

		static readonly string PEACH_NAMESPACE_URI = "http://peachfuzzer.com/2012/Peach";

		/// <summary>
		/// Contains default attributes for DataElements
		/// </summary>
		Dictionary<Type, Dictionary<string, string>> dataElementDefaults = new Dictionary<Type, Dictionary<string, string>>();

		/// <summary>
		/// Mapping of XML ELement names to type as provided by PitParsableAttribute
		/// </summary>
		static Dictionary<string, Type> dataElementPitParsable = new Dictionary<string, Type>();
		static Dictionary<string, Type> dataModelPitParsable = new Dictionary<string, Type>();
		static readonly string[] dataElementCommon = { "Relation", "Fixup", "Transformer", "Hint", "Analyzer", "Placement" };

		// Cache of XmlSchema for pits
		static XmlSchema pitSchema;

		static PitParser()
		{
			populatePitParsable();

			Analyzer.defaultParser = new PitParser();
		}

		[Obsolete("This method is obsolete and should not be used.")]
		public static List<KeyValuePair<string, string>> parseDefines(string definedValuesFile)
		{
			var ret = new OrderedDictionary<string, string>();
			var keys = new HashSet<string>();

			string normalized = Path.GetFullPath(definedValuesFile);

			if (!File.Exists(normalized))
				throw new PeachException("Error, defined values file \"" + definedValuesFile + "\" does not exist.");

			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.Load(normalized);

			var root = xmlDoc.FirstChild;
			if (root.Name != "PitDefines")
			{
				root = xmlDoc.FirstChild.NextSibling;
				if (root.Name != "PitDefines")
					throw new PeachException("Error, definition file root element must be PitDefines.");
			}

			foreach (XmlNode node in root.ChildNodes)
			{
				if (node is XmlComment)
					continue;

				if (node.hasAttr("platform"))
				{
					switch (node.getAttrString("platform").ToLower())
					{
						case "osx":
							if (Platform.GetOS() != Platform.OS.OSX)
								continue;
							break;
						case "linux":
							if (Platform.GetOS() != Platform.OS.Linux)
								continue;
							break;
						case "windows":
							if (Platform.GetOS() != Platform.OS.Windows)
								continue;
							break;
						default:
							throw new PeachException("Error, unknown platform name \"" + node.getAttrString("platform") + "\" in definition file.");
					}
				}
				else if (!node.hasAttr("include"))
				{
					switch (node.Name.ToLower())
					{
						case "osx":
							if (Platform.GetOS() != Platform.OS.OSX)
								continue;
							break;
						case "linux":
							if (Platform.GetOS() != Platform.OS.Linux)
								continue;
							break;
						case "windows":
							if (Platform.GetOS() != Platform.OS.Windows)
								continue;
							break;
						case "all":
							break;
						default:
							throw new PeachException("Error, unknown node name \"" + node.Name + "\" in definition file. Expecting All, Linux, OSX, or Windows.");
					}
				}

				string include = node.getAttr("include", null);
				if (include != null)
				{
					var other = parseDefines(include);
					foreach (var kv in other)
						ret[kv.Key] = kv.Value;
				}

				foreach (XmlNode defNode in node.ChildNodes)
				{
					if (defNode is XmlComment)
						continue;

					string key = defNode.getAttr("key", null);
					string value = defNode.getAttr("value", null);

					if (key == null || value == null)
						throw new PeachException("Error, Define elements in definition file must have both key and value attributes.");

					if (!keys.Add(key))
						throw new PeachException("Error, defines file '" + definedValuesFile + "' contains multiple entries for key '" + key + "'.");

					ret[key] = value;
				}
			}

			return ret.ToList();
		}

		public Dom.Dom asParser(Dictionary<string, object> args, TextReader data)
		{
			return asParser(args, data, string.Empty, true);
		}

		public override Dom.Dom asParser(Dictionary<string, object> args, Stream data)
		{
			return asParser(args, new StreamReader(data), getName(data), true);
		}

		public override void asParserValidation(Dictionary<string, object> args, Stream data)
		{
			asParser(args, new StreamReader(data), getName(data), false);
		}

		class Resetter : DataElement
		{
			public static void Reset()
			{
				DataElement._uniqueName = 0;
			}

			public override void WritePit(XmlWriter pit)
			{
				throw new NotImplementedException();
			}
		}

		protected virtual Dom.Dom CreateDom()
		{
			return new Dom.Dom();
		}

		protected virtual Dom.StateModel CreateStateModel()
		{
			return new Dom.StateModel();
		}

		public virtual Dom.Dom asParser(Dictionary<string, object> args, TextReader data, string dataName, bool parse)
		{
			// Reset the data element auto-name suffix back to zero
			Resetter.Reset();

			var xml = readWithDefines(args, data);
			var xmldoc = validatePit(xml, dataName);

			// Must reload doc using LoadXml() to support newlines in attribute values
			xmldoc.LoadXml(xml);

			if (!parse)
				return null;

			var dom = CreateDom();

			foreach (XmlNode child in xmldoc.ChildNodes)
			{
				if (child.Name == "Peach")
				{
					handlePeach(dom, child, args);
					break;
				}
			}

			dom.evaulateDataModelAnalyzers();

			return dom;
		}

		private static string readWithDefines(Dictionary<string, object> args, TextReader data)
		{
			var xml = data.ReadToEnd();

			object obj;
			if (args != null && args.TryGetValue(DEFINED_VALUES, out obj))
			{
				var definedValues = (IEnumerable<KeyValuePair<string, string>>)obj;

				foreach (var kv in definedValues)
				{
					var newXml = xml.Replace("##" + kv.Key + "##", kv.Value);
					if (xml != newXml)
					{
						HashSet<string> used;
						object objUsed;
						if (!args.TryGetValue(USED_DEFINED_VALUES, out objUsed))
						{
							used = new HashSet<string>();
							args.Add(USED_DEFINED_VALUES, used);
						}
						else
						{
							used = (HashSet<string>)objUsed;
						}
						used.Add(kv.Key);
						xml = newXml;
					}
				}
			}

			return xml;
		}

		static protected void populatePitParsable()
		{
			foreach (var kv in ClassLoader.GetAllByAttribute<PitParsableAttribute>(null))
			{
				if (kv.Key.topLevel)
					dataModelPitParsable[kv.Key.xmlElementName] = kv.Value;
				else
					dataElementPitParsable[kv.Key.xmlElementName] = kv.Value;
			}
		}

		private string getName(Stream data)
		{
			var fs = data as FileStream;
			return fs != null ? fs.Name : null;
		}

		/// <summary>
		/// Validate PIT XML using Schema file.
		/// </summary>
		/// <param name="xmlData">Pit file to validate</param>
		/// <param name="sourceName">Name of pit file</param>
		private XmlDocument validatePit(string xmlData, string sourceName)
		{
			if (pitSchema == null)
			{
				var builder = new Peach.Core.Xsd.SchemaBuilder(typeof(Peach.Core.Xsd.Dom));
				pitSchema = builder.Compile();
			}

			// Collect the errors
			var errors = new StringBuilder();

			// Load the schema
			var set = new XmlSchemaSet();
			set.Add(pitSchema);

			var settings = new XmlReaderSettings
			{
				ValidationType = ValidationType.Schema,
				Schemas = set,
				NameTable = new NameTable()
			};
			settings.ValidationEventHandler += delegate(object sender, ValidationEventArgs e)
			{
				var ex = e.Exception;

				errors.AppendFormat("Line: {0}, Position: {1} - ", ex.LineNumber, ex.LinePosition);
				errors.Append(ex.Message);
				errors.AppendLine();
			};

			// Default the namespace to peach
			var nsMgr = new XmlNamespaceManager(settings.NameTable);
			nsMgr.AddNamespace("", PEACH_NAMESPACE_URI);

			var parserCtx = new XmlParserContext(settings.NameTable, nsMgr, null, XmlSpace.Default);
			var ret = new XmlDocument();

			using (var rdr = XmlReader.Create(new StringReader(xmlData), settings, parserCtx))
			{
				try
				{
					ret.Load(rdr);
				}
				catch (XmlException ex)
				{
					throw new PeachException("Error: XML Failed to load: " + ex.Message, ex);
				}
			}

			if (errors.Length > 0)
			{
				if (!string.IsNullOrEmpty(sourceName))
					throw new PeachException("Error, Pit file \"{0}\" failed to validate: \r\n{1}".Fmt(sourceName, errors));
				else
					throw new PeachException("Error, Pit file failed to validate: \r\n{0}".Fmt(errors));
			}

			return ret;
		}

		public static void displayDataModel(DataElement elem, int indent = 0)
		{
			string sIndent = "";
			for (int i = 0; i < indent; i++)
				sIndent += "  ";

			Console.WriteLine(sIndent + string.Format("{0}: {1}", elem.GetHashCode(), elem.Name));

			var cont = elem as DataElementContainer;

			if (cont == null)
				return;

			foreach (var child in cont)
			{
				displayDataModel(child, indent + 1);
			}
		}

		#region Peach

		/// <summary>
		/// Handle parsing the top level Peach node.
		/// </summary>
		/// <remarks>
		/// NOTE: This method is intended to be overriden (hence the virtual) and is 
		///			currently in use by Godel to extend the Pit Parser.
		/// </remarks>
		/// <param name="dom">Dom object</param>
		/// <param name="node">XmlNode to parse</param>
		/// <param name="args">Parser arguments</param>
		/// <returns>Returns the parsed Dom object.</returns>
		protected virtual void handlePeach(Dom.Dom dom, XmlNode node, Dictionary<string, object> args)
		{
			// Pass 0 - Basic check if Peach 2.3 ns  
			if (node.NamespaceURI.Contains("2008"))
				throw new PeachException("Error, Peach 2.3 namespace detected please upgrade the pit");

			// Pass 1 - Handle imports, includes, python path
			foreach (XmlNode child in node)
			{
				switch (child.Name)
				{
					case "Include":
						handleInclude(dom, args, child);
						break;

					case "Import":
						var module = child.getAttrString("import");
						try
						{
							dom.Python.ImportModule(module);
						}
						catch (ArgumentException ex)
						{
							// If the user tries to import a python extension via their pit,
							// IronPython will try to generate the C# class bindings and
							// subsequently fail because the class name already exists.
							// Provide a more useful error message in this case.

							if (ex.Message != "Duplicate type name within an assembly.")
								throw;

							throw new PeachException("Failed to import python module '{0}' because it was already loaded from the plugins folder.  Remove <Import import=\"{0}\" /> from your pit and try again.".Fmt(module), ex);
						}
						break;

					case "PythonPath":
						dom.Python.AddSearchPath(child.getAttrString("path"));
						break;

					case "Python":
						break;

					case "Defaults":
						handleDefaults(child);
						break;
				}
			}

			// Pass 3 - Handle data model

			foreach (XmlNode child in node)
			{
				var dm = handleDataModel(child, dom, null);

				if (dm != null)
				{
					try
					{
						dom.dataModels.Add(dm);
					}
					catch (ArgumentException)
					{
						var entry = dataModelPitParsable.Where(kv => kv.Value == dm.GetType()).Select(kv => kv.Key).FirstOrDefault();
						var name = entry != null ? "<" + entry + ">" : "Data Model";
						throw new PeachException("Error, a " + name + " element named '" + dm.Name + "' already exists.");
					}

					// Resolve relations in the data model
					foreach (var item in dm.EnumerateAllElements())
						foreach (var rel in item.relations.From<Binding>())
							rel.Resolve();
				}
			}

			// Pass 4 - Handle Data

			foreach (XmlNode child in node)
			{
				if (child.Name == "Data")
				{
					var data = handleData(child, dom, dom.datas.UniqueName());

					try
					{
						dom.datas.Add(data);
					}
					catch (ArgumentException)
					{
						throw new PeachException("Error, a <Data> element named '" + data.Name + "' already exists.");
					}
				}
			}

			// Pass 5 - Handle State model

			foreach (XmlNode child in node)
			{
				if (child.Name == "StateModel")
				{
					StateModel sm = handleStateModel(child, dom);

					try
					{
						dom.stateModels.Add(sm);
					}
					catch (ArgumentException)
					{
						throw new PeachException("Error, a <StateModel> element named '" + sm.Name + "' already exists.");
					}
				}

				if (child.Name == "Agent")
				{
					Dom.Agent agent = handleAgent(child);

					try
					{
						dom.agents.Add(agent);
					}
					catch (ArgumentException)
					{
						throw new PeachException("Error, a <Agent> element named '" + agent.Name + "' already exists.");
					}
				}
			}

			// Pass 6 - Handle Test

			foreach (XmlNode child in node)
			{
				if (child.Name == "Test")
				{
					Test test = handleTest(child, dom);

					try
					{
						dom.tests.Add(test);
					}
					catch (ArgumentException)
					{
						throw new PeachException("Error, a <Test> element named '" + test.Name + "' already exists.");
					}
				}
			}
		}

		protected virtual void handleInclude(Dom.Dom dom, Dictionary<string, object> args, XmlNode child)
		{
			var ns = child.getAttrString("ns");
			var fileName = child.getAttrString("src");
			fileName = fileName.Replace("file:", "");
			var normalized = Path.GetFullPath(fileName);

			if (!File.Exists(normalized))
			{
				string newFileName = Utilities.GetAppResourcePath(fileName);
				normalized = Path.GetFullPath(newFileName);
				if (!File.Exists(normalized))
					throw new PeachException("Error, Unable to locate Pit file [" + normalized + "].\n");
				fileName = newFileName;
			}

			var newDom = asParser(args, fileName);
			newDom.Name = ns;
			dom.ns.Add(newDom);

			foreach (var item in newDom.Python.Paths)
				dom.Python.AddSearchPath(item);

			foreach (var item in newDom.Python.Modules)
				dom.Python.ImportModule(item);
		}

		#endregion

		#region Defaults

		protected virtual void handleDefaults(XmlNode node)
		{
			foreach (XmlNode child in node.ChildNodes)
			{
				if (child is XmlComment)
					continue;

				var args = new Dictionary<string, string>();
				switch (child.Name)
				{
					case "Number":
						if (child.hasAttr("endian"))
							args["endian"] = child.getAttrString("endian");
						if (child.hasAttr("signed"))
							args["signed"] = child.getAttrString("signed");
						if (child.hasAttr("valueType"))
							args["valueType"] = child.getAttrString("valueType");

						dataElementDefaults[typeof(Number)] = args;
						break;
					case "String":
						if (child.hasAttr("lengthType"))
							args["lengthType"] = child.getAttrString("lengthType");
						if (child.hasAttr("padCharacter"))
							args["padCharacter"] = child.getAttrString("padCharacter");
						if (child.hasAttr("type"))
							args["type"] = child.getAttrString("type");
						if (child.hasAttr("nullTerminated"))
							args["nullTerminated"] = child.getAttrString("nullTerminated");
						if (child.hasAttr("valueType"))
							args["valueType"] = child.getAttrString("valueType");

						dataElementDefaults[typeof(Dom.String)] = args;
						break;
					case "Flags":
						if (child.hasAttr("endian"))
							args["endian"] = child.getAttrString("endian");
						if (child.hasAttr("size"))
							args["size"] = child.getAttrString("size");

						dataElementDefaults[typeof(Flags)] = args;
						break;
					case "Blob":
						if (child.hasAttr("lengthType"))
							args["lengthType"] = child.getAttrString("lengthType");
						if (child.hasAttr("valueType"))
							args["valueType"] = child.getAttrString("valueType");

						dataElementDefaults[typeof(Blob)] = args;
						break;
					default:
						throw new PeachException("Error, defaults not supported for '" + child.Name + "'.");
				}
			}
		}

		#endregion

		#region Agent

		protected virtual Dom.Agent handleAgent(XmlNode node)
		{
			var agent = new Dom.Agent
			{
				Name = node.getAttrString("name"),
				location = node.getAttr("location", null),
				password = node.getAttr("password", null)
			};

			if (agent.location == null)
				agent.location = "local://";

			foreach (XmlNode child in node.ChildNodes)
			{
				if (child.Name == "Monitor")
				{
					Dom.Monitor monitor = new Monitor();

					monitor.cls = child.getAttrString("class");
					monitor.Name = child.getAttr("name", agent.monitors.UniqueName());
					monitor.parameters = handleParams(child);

					try
					{
						agent.monitors.Add(monitor);
					}
					catch (ArgumentException)
					{
						throw new PeachException("Error, a <Monitor> element named '{0}' already exists in agent '{1}'.".Fmt(monitor.Name, agent.Name));
					}
				}
			}

			return agent;
		}

		#endregion

		#region DataElement Default Attributes

		static string getDefaultError(Type type, string key)
		{
			return string.Format("Error, element '{0}' has an invalid default value for attribute '{1}'.", type.Name, key);
		}

		public string getDefaultAttr(Type type, string key, string defaultValue)
		{
			Dictionary<string, string> defaults = null;
			if (!dataElementDefaults.TryGetValue(type, out defaults))
				return defaultValue;

			string value = null;
			if (!defaults.TryGetValue(key, out value))
				return defaultValue;

			return value;
		}

		public bool getDefaultAttr(Type type, string key, bool defaultValue)
		{
			string value = getDefaultAttr(type, key, null);

			switch (value)
			{
				case null:
					return defaultValue;
				case "1":
				case "true":
					return true;
				case "0":
				case "false":
					return false;
				default:
					throw new PeachException(getDefaultError(type, key) + "  Could not convert value '" + value + "' to a boolean.");
			}
		}

		public int getDefaultAttr(Type type, string key, int defaultValue)
		{
			string value = getDefaultAttr(type, key, null);
			if (value == null)
				return defaultValue;

			int ret;
			if (!int.TryParse(value, out ret))
				throw new PeachException(getDefaultError(type, key) + "  Could not convert value '" + value + "' to an integer.");

			return ret;
		}

		public char getDefaultAttr(Type type, string key, char defaultValue)
		{
			string value = getDefaultAttr(type, key, null);
			if (value == null)
				return defaultValue;

			if (value.Length != 1)
				throw new PeachException(getDefaultError(type, key) + "  Could not convert value '" + value + "' to a character.");

			return value[0];
		}

		#endregion

		#region Common DataElement Attributes & Children

		#region Value Attribute Escaping

		static Regex reHexWhiteSpace = new Regex(@"[h{},\s\r\n:-]+", RegexOptions.Singleline);
		static Regex reEscapeSlash = new Regex(@"\\\\|\\n|\\r|\\t");

		static string ReplaceSlash(Match m)
		{
			string s = m.ToString();

			switch (s)
			{
				case "\\\\": return "\\";
				case "\\n": return "\n";
				case "\\r": return "\r";
				case "\\t": return "\t";
			}

			throw new ArgumentOutOfRangeException("m");
		}

		#endregion

		/// <summary>
		/// Handle common attributes such as the following:
		/// 
		///  * mutable
		///  * contraint
		///  * pointer
		///  * pointerDepth
		///  * token
		///  
		/// </summary>
		/// <param name="node">XmlNode to read attributes from</param>
		/// <param name="element">Element to set attributes on</param>
		public void handleCommonDataElementAttributes(XmlNode node, DataElement element)
		{
			if (node.hasAttr("fieldId"))
				element.FieldId = node.getAttrString("fieldId");

			if (node.hasAttr("token"))
				element.isToken = node.getAttrBool("token");

			if (node.hasAttr("mutable"))
				element.isMutable = node.getAttrBool("mutable");

			if (node.hasAttr("constraint"))
				element.constraint = node.getAttrString("constraint");

			if (node.hasAttr("pointer"))
				throw new NotSupportedException("Implement pointer attribute");

			if (node.hasAttr("pointerDepth"))
				throw new NotSupportedException("Implement pointerDepth attribute");

			string strLenType = null;
			if (node.hasAttr("lengthType"))
				strLenType = node.getAttrString("lengthType");
			else
				strLenType = getDefaultAttr(element.GetType(), "lengthType", null);

			switch (strLenType)
			{
				case null:
					break;
				case "bytes":
					element.lengthType = LengthType.Bytes;
					break;
				case "bits":
					element.lengthType = LengthType.Bits;
					break;
				case "chars":
					element.lengthType = LengthType.Chars;
					break;
				default:
					throw new PeachException("Error, parsing lengthType on '" + element.Name +
						"', unknown value: '" + strLenType + "'.");
			}

			if (node.hasAttr("length"))
			{
				int length = node.getAttrInt("length");

				try
				{
					element.length = length;
				}
				catch (Exception e)
				{
					throw new PeachException("Error, setting length on element '" + element.Name + "'.  " + e.Message, e);
				}
			}
		}

		/// <summary>
		/// Handle parsing common dataelement children liek relation, fixupImpl and
		/// transformer.
		/// </summary>
		/// <param name="node">Node to read values from</param>
		/// <param name="element">Element to set values on</param>
		public void handleCommonDataElementChildren(XmlNode node, DataElement element)
		{
			foreach (XmlNode child in node.ChildNodes)
			{
				switch (child.Name)
				{
					case "Relation":
						handleRelation(child, element);
						break;

					case "Fixup":
						handleFixup(child, element);
						break;

					case "Transformer":
						handleTransformer(child, element);
						break;

					case "Hint":
						handleHint(child, element);
						break;

					case "Analyzer":
						handleAnalyzer(child, element);
						break;

					case "Placement":
						handlePlacement(child, element);
						break;
				}
			}
		}

		/// <summary>
		/// Handle parsing child data types into containers.
		/// </summary>
		/// <param name="node">XmlNode tor read children elements from</param>
		/// <param name="element">Element to add items to</param>
		public void handleDataElementContainer(XmlNode node, DataElementContainer element)
		{
			foreach (XmlNode child in node.ChildNodes)
			{
				DataElement elem = null;

				if (child.Name == "#comment")
					continue;

				Type dataElementType;

				if (!dataElementPitParsable.TryGetValue(child.Name, out dataElementType))
				{
					if (((IList<string>)dataElementCommon).Contains(child.Name))
						continue;
					else
						throw new PeachException("Error, found unknown data element in pit file: " + child.Name);
				}

				MethodInfo pitParsableMethod = dataElementType.GetMethod("PitParser");
				if (pitParsableMethod == null)
					throw new PeachException("Error, type with PitParsableAttribute is missing static PitParser(...) method: " + dataElementType.FullName);

				PitParserDelegate delegateAction = Delegate.CreateDelegate(typeof(PitParserDelegate), pitParsableMethod) as PitParserDelegate;

				// Prevent dots from being in the name for element construction, they get resolved afterwards
				var childName = child.getAttr("name", string.Empty);
				var nameParts = new string[0];
				var parent = element;

				if (element.isReference && !string.IsNullOrEmpty(childName))
				{
					nameParts = childName.Split('.');
					child.Attributes["name"].InnerText = nameParts[nameParts.Length - 1];

					for (var i = 0; i < nameParts.Length - 1; ++i)
					{
						DataElement newParent;

						var choice = parent as Choice;
						if (choice != null)
							choice.choiceElements.TryGetValue(nameParts[i], out newParent);
						else
							parent.TryGetValue(nameParts[i], out newParent);

						if (newParent == null)
							throw new PeachException(
								"Error parsing {0} named '{1}', {2} has no child element named '{3}'.".Fmt(
									child.Name, childName, parent.debugName, nameParts[i]));

						var asArray = newParent as Dom.Array;
						if (asArray != null)
							newParent = asArray.OriginalElement;

						parent = newParent as DataElementContainer;
						if (parent == null)
							throw new PeachException(
								"Error parsing {0} named '{1}', {2} is not a container element.".Fmt(
									child.Name, childName, newParent.debugName));
					}
				}

				elem = delegateAction(this, child, parent);

				if (elem == null)
					throw new PeachException("Error, type failed to parse provided XML: " + dataElementType.FullName);

				// Wrap elements that are arrays with an Array object
				if (child.hasAttr("minOccurs") || child.hasAttr("maxOccurs") || child.hasAttr("occurs"))
				{
					// Ensure the array has the same name as the 1st element
					((System.Xml.XmlElement)child).SetAttribute("name", elem.Name);

					var array = Dom.Array.PitParser(this, child, element) as Dom.Array;

					// Set the original element
					array.OriginalElement = elem;

					// NOTE: the array will auto expand the 1st time .Value is called
					// Deferring expansion lets future references to this element
					// adjust the array's OriginalElement

					// Copy over hints, some may be for array
					foreach (var key in elem.Hints.Keys)
						array.Hints[key] = elem.Hints[key];

					// Move the field id up to the  array element so that
					// mutations on the array get correlated to the field id
					array.FieldId = elem.FieldId;

					// Clear the field id on the element so it doesn't get duplicated
					// when using the FullFieldId property.
					elem.FieldId = null;

					elem = array;
				}

				// If parent was created by a reference (ref attribute)
				// then allow replacing existing elements with new
				// elements.  This includes "deep" replacement using "."
				// notation.
				if (element.isReference && !string.IsNullOrEmpty(childName))
				{
					parent.ApplyReference(elem);
				}
				else
				{
					// Otherwise enforce unique element names.
					element.Add(elem);
				}
			}
		}

		/// <summary>
		/// Handle parsing value/valueType attributes on elements
		/// transformer.
		/// </summary>
		/// <param name="node">Node to read values from</param>
		/// <param name="element">Element to set values on</param>
		/// <param name="context">If element is detached (no parent) provide context Dom instance</param>
		public void handleCommonDataElementValue(XmlNode node, DataElement element, Dom.Dom context = null)
		{
			if (!node.hasAttr("value"))
				return;

			string value = node.getAttrString("value");

			value = reEscapeSlash.Replace(value, new MatchEvaluator(ReplaceSlash));

			string valueType = null;
			IPAddress asIp = null;

			if (node.hasAttr("valueType"))
				valueType = node.getAttrString("valueType");
			else
				valueType = getDefaultAttr(element.GetType(), "valueType", "string");

			switch (valueType.ToLower())
			{
				case "hex":
					// Handle hex data.

					// 1. Remove white space
					value = reHexWhiteSpace.Replace(value, "");

					// 3. Remove 0x
					value = value.Replace("0x", "");

					// 4. remove \x
					value = value.Replace("\\x", "");

					if (value.Length % 2 != 0)
						throw new PeachException(
							"Error, the hex value of {0} must contain an even number of characters: {1}".Fmt(
								element.debugName,
								value)
							);

					var array = HexString.ToArray(value);
					if (array == null)
						throw new PeachException(
							"Error, the value of {0} contains invalid hex characters: {1}".Fmt(
								element.debugName,
								value
							));

					element.DefaultValue = new Variant(new BitStream(array));
					break;
				case "literal":

					var localScope = new Dictionary<string, object>();
					localScope["self"] = element;
					localScope["node"] = node;
					localScope["Parser"] = this;
					localScope["Context"] = context ?? ((DataModel)element.root).dom;

					object obj;

					try
					{
						obj = element.EvalExpression(value, localScope, context);
					}
					catch (SoftException ex)
					{
						throw new PeachException(ex.Message, ex);
					}

					if (obj == null)
						throw new PeachException("Error, the value of the eval statement of " + element.debugName + " returned null.");

					var asVariant = Scripting.ToVariant(obj);

					if (asVariant == null)
						throw new PeachException("Error, the value of the eval statement of " + element.debugName + " returned unsupported type '" + obj.GetType() +"'.");

					element.DefaultValue = asVariant;
					break;
				case "string":
					// No action requried, default behaviour
					element.DefaultValue = new Variant(value);
					break;
				case "ipv4":
					if (!IPAddress.TryParse(value, out asIp) || asIp.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
						throw new PeachException("Error, the value '" + value + "' of " + element.debugName + " is not a valid IPv4 address.");

					element.DefaultValue = new Variant(asIp.GetAddressBytes());
					break;
				case "ipv6":
					if (!IPAddress.TryParse(value, out asIp) || asIp.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
						throw new PeachException("Error, the value '" + value + "' of " + element.debugName + " is not a valid IPv6 address.");

					element.DefaultValue = new Variant(asIp.GetAddressBytes());
					break;

				default:
					throw new PeachException("Error, invalid value for 'valueType' attribute: " + valueType);
			}
		}

		#endregion

		#region DataModel

		protected DataModel handleDataModel(XmlNode node, Dom.Dom dom, DataModel old)
		{
			Type type;
			if (!dataModelPitParsable.TryGetValue(node.Name, out type))
				return old;

			if (old != null)
				throw new PeachException("Error, more than one {0} not allowed. XML:\n{1}".Fmt(
					string.Join(",", dataModelPitParsable.Keys), node.OuterXml));

			MethodInfo pitParsableMethod = type.GetMethod("PitParser");
			if (pitParsableMethod == null)
				throw new PeachException("Error, type with PitParsableAttribute is missing static PitParser(...) method: " + type.FullName);

			PitParserTopLevelDelegate delegateAction = Delegate.CreateDelegate(typeof(PitParserTopLevelDelegate), pitParsableMethod) as PitParserTopLevelDelegate;

			var dataModel = delegateAction(this, node, dom);
			if (dataModel == null)
				throw new PeachException("Error, type failed to parse provided XML: " + type.FullName);

			return dataModel;
		}

		protected void handleFixup(XmlNode node, DataElement element)
		{
			if (element.fixup != null)
				throw new PeachException("Error, multiple fixups defined on element '" + element.Name + "'.");

			element.fixup = handlePlugin<Fixup, FixupAttribute>(node, element, true);
		}

		protected void handleAnalyzer(XmlNode node, DataElement element)
		{
			if (element.analyzer != null)
				throw new PeachException("Error, multiple analyzers are defined on element '" + element.Name + "'.");

			element.analyzer = handlePlugin<Analyzer, AnalyzerAttribute>(node, element, false);
		}

		protected void handleTransformer(XmlNode node, DataElement element)
		{
			if (element.transformer != null)
				throw new PeachException("Error, multiple transformers are defined on element '" + element.Name + "'.");

			element.transformer = handlePlugin<Transformer, TransformerAttribute>(node, element, true);

			handleNestedTransformer(node, element, element.transformer);
		}

		protected void handleNestedTransformer(XmlNode node, DataElement element, Transformer transformer)
		{
			foreach (XmlNode child in node.ChildNodes)
			{
				if (child.Name == "Transformer")
				{
					if (transformer.anotherTransformer != null)
						throw new PeachException("Error, multiple nested transformers are defined on element '" + element.Name + "'.");

					transformer.anotherTransformer = handlePlugin<Transformer, TransformerAttribute>(child, element, true);

					handleNestedTransformer(child, element, transformer.anotherTransformer);
				}
			}
		}

		protected void handleHint(XmlNode node, DataElement element)
		{
			var hint = new Hint(node.getAttrString("name"), node.getAttrString("value"));
			element.Hints.Add(hint.Name, hint);
			logger.Trace("handleHint: " + hint.Name + ": " + hint.Value);
		}

		protected void handlePlacement(XmlNode node, DataElement element)
		{
			Dictionary<string, Variant> args = new Dictionary<string, Variant>();

			if (node.hasAttr("after"))
				args["after"] = new Variant(node.getAttrString("after"));
			else if (node.hasAttr("before"))
				args["before"] = new Variant(node.getAttrString("before"));

			Placement placement = new Placement(args);
			element.placement = placement;
		}

		protected void handleRelation(XmlNode node, DataElement parent)
		{
			string value = node.getAttrString("type");
			switch (value)
			{
				case "size":
					if (node.hasAttr("of"))
					{
						SizeRelation rel = new SizeRelation(parent);
						rel.OfName = node.getAttrString("of");

						if (node.hasAttr("expressionGet"))
							rel.ExpressionGet = node.getAttrString("expressionGet");

						if (node.hasAttr("expressionSet"))
							rel.ExpressionSet = node.getAttrString("expressionSet");

						var strType = node.getAttr("lengthType", rel.lengthType.ToString());
						LengthType lenType;
						if (!Enum.TryParse(strType, true, out lenType))
							throw new PeachException("Error, size relation on element '" + parent.Name + "' has invalid lengthType '" + strType + "'.");

						rel.lengthType = lenType;
					}

					break;

				case "count":
					if (node.hasAttr("of"))
					{
						CountRelation rel = new CountRelation(parent);
						rel.OfName = node.getAttrString("of");

						if (node.hasAttr("expressionGet"))
							rel.ExpressionGet = node.getAttrString("expressionGet");

						if (node.hasAttr("expressionSet"))
							rel.ExpressionSet = node.getAttrString("expressionSet");
					}
					break;

				case "offset":
					if (node.hasAttr("of"))
					{
						OffsetRelation rel = new OffsetRelation(parent);
						rel.OfName = node.getAttrString("of");

						if (node.hasAttr("expressionGet"))
							rel.ExpressionGet = node.getAttrString("expressionGet");

						if (node.hasAttr("expressionSet"))
							rel.ExpressionSet = node.getAttrString("expressionSet");

						if (node.hasAttr("relative"))
							rel.isRelativeOffset = true;

						if (node.hasAttr("relativeTo"))
						{
							rel.isRelativeOffset = true;
							rel.relativeTo = node.getAttrString("relativeTo");
						}
					}
					break;

				default:
					throw new PeachException("Error, element '" + parent.Name + "' has unknown relation type '" + value + "'.");
			}
		}

		#endregion

		#region StateModel

		protected virtual StateModel handleStateModel(XmlNode node, Dom.Dom parent)
		{
			var name = node.getAttrString("name");
			var initialState = node.getAttrString("initialState");
			string finalState = null;
			if (node.hasAttr("finalState"))
				finalState = node.getAttrString("finalState");
			var stateModel = CreateStateModel();
			stateModel.Name = name;
			stateModel.parent = parent;
			stateModel.initialStateName = initialState;
			stateModel.finalStateName = finalState;

			foreach (XmlNode child in node.ChildNodes)
			{
				if (child.Name == "State")
				{
					var state = handleState(child, stateModel);

					try
					{
						stateModel.states.Add(state);
					}
					catch (ArgumentException)
					{
						throw new PeachException("Error, a <State> element named '" + state.Name + "' already exists in state model '" + stateModel.Name + "'.");
					}

					if (state.Name == initialState)
						stateModel.initialState = state;
					else if (state.Name == finalState)
						stateModel.finalState = state;
				}
			}

			if (stateModel.initialState == null)
				throw new PeachException("Error, did not locate inital ('" + initialState + "') for state model '" + name + "'.");
			if (finalState != null && stateModel.finalState == null)
				throw new PeachException("Error, did not locate final ('" + finalState + "') for state model '" + name + "'.");

			return stateModel;
		}

		#endregion

		#region State

		protected virtual State handleState(XmlNode node, StateModel parent)
		{
			var state = new State
			{
				parent = parent,
				Name = node.getAttr("name", parent.states.UniqueName()),
				FieldId = node.getAttr("fieldId", null),
				onStart = node.getAttr("onStart", null),
				onComplete = node.getAttr("onComplete", null)
			};

			foreach (XmlNode child in node.ChildNodes)
			{
				if (child.Name == "Action")
				{
					var action = handleAction(child, state);

					try
					{
						state.actions.Add(action);
					}
					catch (ArgumentException)
					{
						throw new PeachException("Error, a <Action> element named '" + action.Name + "' already exists in state '" + state.parent.Name + "." + state.Name + "'.");
					}
				}
			}

			return state;
		}

		#endregion

		#region Action

		protected virtual void handleActionAttr(XmlNode node, Dom.Action action, params string[] badAttrs)
		{
			foreach (var attr in badAttrs)
				if (node.hasAttr(attr))
					throw new PeachException("Error, action '{0}.{1}.{2}' has invalid attribute '{3}'.".Fmt(
						action.parent.parent.Name,
						action.parent.Name,
						action.Name,
						attr));
		}

		protected virtual void handleActionParameter(XmlNode node, Dom.Actions.Call action)
		{
			string strType = node.getAttr("type", "in");
			ActionParameter.Type type;
			if (!Enum.TryParse(strType, true, out type))
				throw new PeachException("Error, type attribute '{0}' on <Param> child of action '{1}.{2}.{3}' is invalid.".Fmt(
					strType,
					action.parent.parent.Name,
					action.parent.Name,
					action.Name));

			var name = node.getAttr("name", action.parameters.UniqueName());
			var data = new ActionParameter(name)
			{
				action = action,
				type = type,
			};

			// 'Out' params are input and can't have <Data>
			handleActionData(node, data, "<Param> child of ", type != ActionParameter.Type.Out);

			try
			{
				action.parameters.Add(data);
			}
			catch (ArgumentException)
			{
				throw new PeachException("Error, a <Param> element named '{0}' already exists in action '{1}.{2}.{3}'.".Fmt(
					data.Name,
					action.parent.parent.Name,
					action.parent.Name,
					action.Name));
			}
		}

		protected virtual void handleActionResult(XmlNode node, Dom.Actions.Call action)
		{
			var name = node.getAttr("name", "Result");

			action.result = new ActionResult(name)
			{
				action = action
			};

			handleActionData(node, action.result, "<Result> child of ", false);
		}

		protected virtual void handleActionCall(XmlNode node, Dom.Actions.Call action)
		{
			action.method = node.getAttrString("method");

			foreach (XmlNode child in node.ChildNodes)
			{
				if (child.Name == "Param")
					handleActionParameter(child, action);
				else if (child.Name == "Result")
					handleActionResult(child, action);
			}

			handleActionAttr(node, action, "ref", "property", "setXpath", "valueXpath");
		}

		protected virtual void handleActionChangeState(XmlNode node, Dom.Actions.ChangeState action)
		{
			action.reference = node.getAttrString("ref");

			handleActionAttr(node, action, "method", "property", "setXpath", "valueXpath");
		}

		protected virtual void handleActionSlurp(XmlNode node, Dom.Actions.Slurp action)
		{
			action.setXpath = node.getAttrString("setXpath");
			action.valueXpath = node.getAttrString("valueXpath");

			handleActionAttr(node, action, "ref", "method", "property");
		}

		protected virtual void handleActionSetProperty(XmlNode node, Dom.Actions.SetProperty action)
		{
			action.property = node.getAttrString("property");
			action.data = new ActionData()
			{
				action = action
			};

			handleActionData(node, action.data, "", true);

			handleActionAttr(node, action, "ref", "method", "setXpath", "valueXpath");
		}

		protected virtual void handleActionGetProperty(XmlNode node, Dom.Actions.GetProperty action)
		{
			action.property = node.getAttrString("property");
			action.data = new ActionData()
			{
				action = action
			};

			handleActionData(node, action.data, "", false);

			handleActionAttr(node, action, "ref", "method", "setXpath", "valueXpath");
		}

		protected virtual void handleActionOutput(XmlNode node, Dom.Actions.Output action)
		{
			action.data = new ActionData()
			{
				action = action
			};

			handleActionData(node, action.data, "", true);

			handleActionAttr(node, action, "ref", "method", "property", "setXpath", "valueXpath");
		}

		protected virtual void handleActionInput(XmlNode node, Dom.Actions.Input action)
		{
			action.data = new ActionData()
			{
				action = action
			};

			handleActionData(node, action.data, "", false);

			handleActionAttr(node, action, "ref", "method", "property", "setXpath", "valueXpath");
		}

		public virtual void handleActionData(XmlNode node, ActionData data, string type, bool hasData)
		{
			var dom = data.action.parent.parent.parent;

			foreach (XmlNode child in node.ChildNodes)
			{
				data.dataModel = handleDataModel(child, dom, data.dataModel);

				if (data.dataModel != null)
				{
					data.dataModel.dom = null;
					data.dataModel.actionData = data;
				}

				if (child.Name == "Data")
				{
					if (!hasData)
						throw new PeachException("Error, {0}action '{1}.{2}.{3}' has unsupported child element <Data>.".Fmt(
							type,
							data.action.parent.parent.Name,
							data.action.parent.Name,
							data.action.Name));

					var item = handleData(child, dom, data.dataSets.UniqueName());

					try
					{
						data.dataSets.Add(item);
					}
					catch (ArgumentException)
					{
						throw new PeachException("Error, a <Data> element named '{0}' already exists in {1}action '{2}.{3}.{4}'.".Fmt(
							item.Name,
							type,
							data.action.parent.parent.Name,
							data.action.parent.Name,
							data.action.Name));
					}
				}
			}

			if (data.dataModel == null)
				throw new PeachException("Error, {0}action '{1}.{2}.{3}' is missing required child element <DataModel>.".Fmt(
					type,
					data.action.parent.parent.Name,
					data.action.parent.Name,
					data.action.Name));
		}

		protected virtual Dom.Action handleAction(XmlNode node, State parent)
		{
			var strType = node.getAttrString("type");
			var type = ClassLoader.FindTypeByAttribute<ActionAttribute>((t, a) => 0 == string.Compare(a.Name, strType, true));
			if (type == null)
				throw new PeachException("Error, state '" + parent.Name + "' has an invalid action type '" + strType + "'.");

			var name = node.getAttr("name", parent.actions.UniqueName());

			var action = (Dom.Action)Activator.CreateInstance(type);

			action.Name = name;
			action.parent = parent;
			action.FieldId = node.getAttr("fieldId", null);
			action.when = node.getAttr("when", null);
			action.publisher = node.getAttr("publisher", null);
			action.onStart = node.getAttr("onStart", null);
			action.onComplete = node.getAttr("onComplete", null);

			if (action is Dom.Actions.Call)
				handleActionCall(node, (Dom.Actions.Call)action);
			else if (action is Dom.Actions.ChangeState)
				handleActionChangeState(node, (Dom.Actions.ChangeState)action);
			else if (action is Dom.Actions.Slurp)
				handleActionSlurp(node, (Dom.Actions.Slurp)action);
			else if (action is Dom.Actions.GetProperty)
				handleActionGetProperty(node, (Dom.Actions.GetProperty)action);
			else if (action is Dom.Actions.SetProperty)
				handleActionSetProperty(node, (Dom.Actions.SetProperty)action);
			else if (action is Dom.Actions.Input)
				handleActionInput(node, (Dom.Actions.Input)action);
			else if (action is Dom.Actions.Output)
				handleActionOutput(node, (Dom.Actions.Output)action);

			return action;
		}

		#endregion

		#region Data

		protected virtual DataSet handleData(XmlNode node, Dom.Dom dom, string uniqueName)
		{
			DataSet dataSet;

			if (node.hasAttr("ref"))
			{
				var refName = node.getAttrString("ref");

				var other = dom.getRef(refName, a => a.datas);
				if (other == null)
					throw new PeachException("Error, could not resolve Data element ref attribute value '" + refName + "'.");

				dataSet = ObjectCopier.Clone(other);
			}
			else
			{
				dataSet = new DataSet();
			}

			dataSet.Name = node.getAttr("name", uniqueName);
			dataSet.FieldId = node.getAttr("fieldId", null);

			if (node.hasAttr("fileName"))
			{
				dataSet.Clear();

				var dataFileName = node.getAttrString("fileName");

				if (dataFileName.Contains('*'))
				{
					var pattern = Path.GetFileName(dataFileName);
					var dir = dataFileName.Substring(0, dataFileName.Length - pattern.Length);

					if (dir == "")
						dir = ".";

					try
					{
						dir = Path.GetFullPath(dir);
						var files = Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly);
						foreach (var item in files)
							dataSet.Add(new DataFile(dataSet, item));
					}
					catch (ArgumentException ex)
					{
						// Directory is not legal
						throw new PeachException("Error parsing Data element, fileName contains invalid characters: " + dataFileName, ex);
					}

					if (dataSet.Count == 0)
						throw new PeachException("Error parsing Data element, no matching files found: " + dataFileName);
				}
				else
				{
					try
					{
						var normalized = Path.GetFullPath(dataFileName);

						if (Directory.Exists(normalized))
						{
							foreach (string fileName in Directory.GetFiles(normalized))
								dataSet.Add(new DataFile(dataSet, fileName));

							if (dataSet.Count == 0)
								throw new PeachException("Error parsing Data element, folder contains no files: " + dataFileName);
						}
						else if (File.Exists(normalized))
						{
							dataSet.Add(new DataFile(dataSet, normalized));
						}
						else
						{
							throw new PeachException("Error parsing Data element, file or folder does not exist: " + dataFileName);
						}
					}
					catch (ArgumentException ex)
					{
						throw new PeachException("Error parsing Data element, fileName contains invalid characters: " + dataFileName, ex);
					}
				}
			}

			var children = node.ChildNodes.AsEnumerable().ToArray();
			var fields = children.Where(x => x.Name == "Field").ToArray();
			if (fields.Any())
			{
				// If this ref'd an existing Data element, clear all non DataField children
				if (dataSet.Any(o => !(o is DataField)))
					dataSet.Clear();

				// Ensure there is a field data record we can populate
				if (dataSet.Count == 0)
					dataSet.Add(new DataField(dataSet));

				var dupes = new HashSet<string>();

				foreach (var child in fields)
				{
					var xpath = child.getAttr("xpath", null);
					var name = child.getAttr("name", null);
					if (name == null && xpath == null)
						throw new PeachException("Error, 'Field' element is missing required attribute 'name' or 'xpath'.");
					if (name == null)
						name = xpath;

					if (!dupes.Add(name))
						throw new PeachException("Error, Data element has multiple entries for field '" + name + "'.");

					DataElement tmp;
					if (child.getAttr("valueType", "string").ToLower() == "string")
						tmp = new Dom.String {stringType = StringType.utf8};
					else
						tmp = new Blob();

					tmp.debugName = "Field '{0}'".Fmt(name);

					// Hack to call common value parsing code.
					handleCommonDataElementValue(child, tmp, dom);
	
					foreach (var fieldData in dataSet.OfType<DataField>())
					{
						fieldData.Fields.Remove(name);
						fieldData.Fields.Add(new DataField.Field
						{
							IsXpath = xpath != null,
							Name = name,
							Value = tmp.DefaultValue
						});
					}
				}
			}

			var masks = children.Where(x => x.Name == "FieldMask");
			foreach (var child in masks)
			{
				var selector = child.getAttrString("select");
				dataSet.Add(new DataFieldMask(selector));
			}

			if (dataSet.Count == 0)
				throw new PeachException("Error, <Data> element is missing required 'fileName' attribute or <Field> child element.");

			return dataSet;
		}

		#endregion

		#region Test

		protected virtual List<string> handleMutators(XmlNode node)
		{
			var ret = new List<string>();

			foreach (XmlNode child in node)
			{
				if (child.Name == "Mutator")
				{
					string name = child.getAttrString("class");
					ret.Add(name);
				}
			}

			return ret;
		}

		protected virtual Test handleTest(XmlNode node, Dom.Dom parent)
		{
			Test test = new Test();
			test.parent = parent;

			test.Name = node.getAttrString("name");

			if (node.hasAttr("waitTime"))
				test.waitTime = double.Parse(node.getAttrString("waitTime"), CultureInfo.InvariantCulture);

			if (node.hasAttr("faultWaitTime"))
				test.faultWaitTime = double.Parse(node.getAttrString("faultWaitTime"), CultureInfo.InvariantCulture);

			if (node.hasAttr("controlIteration"))
				test.controlIteration = int.Parse(node.getAttrString("controlIteration"), CultureInfo.InvariantCulture);

			if (node.hasAttr("nonDeterministicActions"))
				test.nonDeterministicActions = node.getAttrBool("nonDeterministicActions");

			if (node.hasAttr("maxOutputSize"))
				test.maxOutputSize = node.getAttrUInt64("maxOutputSize");

			if (node.hasAttr("maxBackSearch"))
				test.MaxBackSearch = node.getAttrUInt32("maxBackSearch");

			var lifetime = node.getAttr("targetLifetime", null);
			if (lifetime != null)
			{
				switch (lifetime.ToLower())
				{
					case "session":
						test.TargetLifetime = Test.Lifetime.Session;
						break;
					case "iteration":
						test.TargetLifetime = Test.Lifetime.Iteration;
						break;
					default:
						throw new PeachException("Error, Test '{1}' attribute targetLifetime has invalid value '{0}'.".Fmt(lifetime, test.Name));
				}
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (child.Name == "Logger")
				{
					test.loggers.Add(handlePlugin<Logger, LoggerAttribute>(child, null, false));
				}

				// Include
				else if (child.Name == "Include")
				{
					var attr = child.getAttr("ref", null);

					if (attr != null)
						attr = string.Format("//{0}", attr);
					else
						attr = child.getAttr("xpath", null);

					if (attr == null)
						attr = "//*";

					test.mutables.Add(new IncludeMutable() { xpath = attr });
				}

				// Exclude
				else if (child.Name == "Exclude")
				{
					var attr = child.getAttr("ref", null);

					if (attr != null)
						attr = string.Format("//{0}", attr);
					else
						attr = child.getAttr("xpath", null);

					if (attr == null)
						attr = "//*";

					test.mutables.Add(new ExcludeMutable() { xpath = attr });
				}

				// Weight
				else if (child.Name == "Weight")
				{
					var attr = child.getAttrString("xpath");
					var val = child.getAttrString("weight");

					ElementWeight weight;
					if (!Enum.TryParse(val, out weight))
						throw new PeachException("Error, '{0}' is an invalid enumeration value for weight attribute.".Fmt(val));

					test.mutables.Add(new WeightMutable { xpath = attr, Weight = weight });
				}

				// Strategy
				else if (child.Name == "Strategy")
				{
					test.strategy = handlePlugin<MutationStrategy, MutationStrategyAttribute>(child, null, false);
				}

				// Agent
				else if (child.Name == "Agent")
				{
					var refName = child.getAttrString("ref");

					var agent = parent.getRef<Dom.Agent>(refName, a => a.agents);
					if (agent == null)
						throw new PeachException("Error, could not locate Agent named '" +
							refName + "' for Test '" + test.Name + "'.");

					// Make a copy to ensure the platform attribute doesn't
					// cross test boundaries and update the name just incase
					// it is in a different namespace
					agent = ObjectCopier.Clone(agent);

					agent.Name = refName;

					var platform = child.getAttr("platform", null);
					if (platform != null)
					{
						switch (platform.ToLower())
						{
							case "all":
								agent.platform = Platform.OS.All;
								break;
							case "none":
								agent.platform = Platform.OS.None;
								break;
							case "windows":
								agent.platform = Platform.OS.Windows;
								break;
							case "osx":
								agent.platform = Platform.OS.OSX;
								break;
							case "linux":
								agent.platform = Platform.OS.Linux;
								break;
							case "unix":
								agent.platform = Platform.OS.Unix;
								break;
						}
					}

					test.agents.Add(agent);
				}

				// StateModel
				else if (child.Name == "StateModel")
				{
					string strRef = child.getAttrString("ref");

					test.stateModel = parent.getRef<Dom.StateModel>(strRef, a => a.stateModels);
					if (test.stateModel == null)
						throw new PeachException("Error, could not locate StateModel named '" +
							strRef + "' for Test '" + test.Name + "'.");

					test.stateModel.Name = strRef;
					test.stateModel.parent = test.parent;
				}

				// Publisher
				else if (child.Name == "Publisher")
				{
					handlePublishers(child, test);
				}

				// Mutator
				else if (child.Name == "Mutators")
				{
					string mode = child.getAttrString("mode");

					var list = handleMutators(child);

					switch (mode.ToLower())
					{
						case "include":
							test.includedMutators.AddRange(list);
							break;
						case "exclude":
							test.excludedMutators.AddRange(list);
							break;
						default:
							throw new PeachException("Error, Mutators element has invalid 'mode' attribute '" + mode + "'");
					}
				}

				else
				{
					handleTestChild(child, test);
				}
			}

			if (test.stateModel == null)
				throw new PeachException("Test '" + test.Name + "' missing StateModel element.");
			if (test.publishers.Count == 0)
				throw new PeachException("Test '" + test.Name + "' missing Publisher element.");

			if (test.strategy == null)
			{
				var type = ClassLoader.FindTypeByAttribute<DefaultMutationStrategyAttribute>(null);
				test.strategy = Activator.CreateInstance(type, new Dictionary<string, Variant>()) as MutationStrategy;
			}

			return test;
		}

		protected virtual void handleTestChild(XmlNode node, Test test)
		{
			
		}
		protected virtual void handlePublishers(XmlNode node, Dom.Test parent)
		{
			var cls = node.getAttrString("class");
			var agent = node.getAttr("agent", null);

			Publisher pub;

			if (agent == null && cls != "Remote")
			{
				pub = handlePlugin<Publisher, PublisherAttribute>(node, null, false);
			}
			else
			{
				var arg = handleParams(node);

				if (cls == "Remote")
				{
					Variant val;
					if (!arg.TryGetValue("Agent", out val))
						throw new PeachException("Publisher 'RemotePublisher' is missing required parameter 'Agent'.");

					agent = (string)val;
					arg.Remove("Agent");

					if (!arg.TryGetValue("Class", out val))
						throw new PeachException("Publisher 'RemotePublisher' is missing required parameter 'Class'.");

					cls = (string)val;
					arg.Remove("Class");
				}

				pub = new RemotePublisher
				{
					Agent = agent,
					Class = cls,
					Args = arg.ToDictionary(i => i.Key, i => (string)i.Value)
				};
			}

			pub.Name = node.getAttr("name", null) ?? parent.publishers.UniqueName();

			try
			{
				parent.publishers.Add(pub);
			}
			catch (ArgumentException)
			{
				throw new PeachException("Error, a <Publisher> element named '{0}' already exists in test '{1}'.".Fmt(pub.Name, parent.Name));
			}
		}

		#endregion

		#region Plugin Helpers

		protected T handlePlugin<T, A>(XmlNode node, DataElement parent, bool useParent)
			where T : class
			where A : PluginAttribute
		{
			var pluginType = typeof(T).Name;

			var cls = node.getAttrString("class");
			var arg = handleParams(node);

			var type = ClassLoader.FindPluginByName<A>(cls);
			if (type == null)
				throw new PeachException(string.Format("Error, unable to locate {0} '{1}'.", pluginType, cls));

			validateParameterAttributes<A>(type, pluginType, cls, arg);

			try
			{
				if (useParent)
				{
					return Activator.CreateInstance(type, parent, arg) as T;
				}
				else
				{
					return Activator.CreateInstance(type, arg) as T;
				}
			}
			catch (Exception e)
			{
				if (e.InnerException != null)
				{
					throw new PeachException(string.Format(
						"Error, unable to create instance of '{0}' named '{1}'.\nExtended error: Exception during object creation: {2}",
						pluginType, cls, e.InnerException.Message
					), e);
				}

				throw new PeachException(string.Format(
					"Error, unable to create instance of '{0}' named '{1}'.\nExtended error: Exception during object creation: {2}",
					pluginType, cls, e.Message
				), e);
			}
		}

		protected void validateParameterAttributes<A>(Type type, string pluginType, string name, IDictionary<string, Variant> xmlParameters) where A : PluginAttribute
		{
			var objParams = type.GetAttributes<ParameterAttribute>(null);

			var missing = objParams.Where(a => a.required && !xmlParameters.ContainsKey(a.name)).Select(a => a.name).FirstOrDefault();
			if (missing != null)
			{
				throw new PeachException(
					string.Format("Error, {0} '{1}' is missing required parameter '{2}'.\n{3}",
						pluginType, name, missing, formatParameterAttributes(objParams)));
			}

			var extraParams = xmlParameters.Select(kv => kv.Key).Where(k => null == objParams.FirstOrDefault(a => a.name == k)).ToList();

			foreach (var extra in extraParams)
			{
				var obsolete = type.GetAttributes<ObsoleteParameterAttribute>().FirstOrDefault(a => a.Name == extra);
				if (obsolete == null)
				{
					throw new PeachException(
						string.Format("Error, {0} '{1}' has unknown parameter '{2}'.\n{3}",
							pluginType, name, extra, formatParameterAttributes(objParams)));
				}

				logger.Info(obsolete.Message);
				xmlParameters.Remove(obsolete.Name);
			}
		}

		protected string formatParameterAttributes(IEnumerable<ParameterAttribute> publisherParameters)
		{
			// XXX: why is this reversed?
			var reversed = new List<ParameterAttribute>(publisherParameters);
			reversed.Reverse();

			string s = "\nSupported Parameters:\n\n";
			foreach (var p in reversed)
			{
				if (p.required)
					s += "  " + p.name + ": [REQUIRED] " + p.description + "\n";
				else
					s += "  " + p.name + ": " + p.description + "\n";
			}
			s += "\n";

			return s;
		}

		protected Dictionary<string, Variant> handleParams(XmlNode node)
		{
			Dictionary<string, Variant> ret = new Dictionary<string, Variant>();
			foreach (XmlNode child in node.ChildNodes)
			{
				if (child.Name != "Param")
					continue;

				string name = child.getAttrString("name");
				string value = child.getAttrString("value");

				if (child.hasAttr("valueType"))
				{
					ret.Add(name, new Variant(value, child.getAttrString("valueType")));
				}
				else
				{
					ret.Add(name, new Variant(value));
				}
			}

			return ret;
		}

		#endregion
	}
}

// end
