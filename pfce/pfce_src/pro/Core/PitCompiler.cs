using System;
using System.Collections.Generic;
using System.Linq;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Core.Dom.Actions;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.Core.Publishers;
using Newtonsoft.Json;
using System.IO;
using System.Xml;
using System.Xml.XPath;
#if DEBUG
using System.Xml.Schema;
#endif
using Peach.Pro.Core.Storage;

namespace Peach.Pro.Core
{
	public class PitCompiler
	{
		class NinjaSample
		{
			public string SamplePath { get; set; }
			public DataModel DataModel { get; set; }
		}

		private readonly string _pitLibraryPath;
		private readonly string _pitPath;
		private readonly string _pitMetaPath;
		private readonly string _pitNinjaPath;
		private readonly List<NinjaSample> _samples = new List<NinjaSample>();
		private readonly List<string> _errors = new List<string>();

		private const string Namespace = "http://peachfuzzer.com/2012/Peach";
#if DEBUG
		private const string SchemaLocation = "http://peachfuzzer.com/2012/Peach peach.xsd";
#endif

		private static readonly Dictionary<string, string[]> OptionalParams = new Dictionary<string, string[]> {
			{ "RawEther", new[] { "MinMTU", "MaxMTU", "MinFrameSize", "MaxFrameSize", "PcapTimeout" } },
			{ "RawV4", new[] { "MinMTU", "MaxMTU" } },
			{ "RawV6", new[] { "MinMTU", "MaxMTU" } },
			{ "Udp", new[] { "MinMTU", "MaxMTU" } },
			{ "DTls", new[] { "MinMTU", "MaxMTU" } },
			{ "File", new[] { "Append", "Overwrite" } },
			{ "ConsoleHex", new[] { "BytesPerLine" } },
			{ "Null", new[] { "MaxOutputSize" } }
		};

		public PitCompiler(string pitLibraryPath, string pitPath)
		{
			_pitLibraryPath = pitLibraryPath;
			_pitPath = pitPath;
			_pitMetaPath = MetaPath(pitPath);
			_pitNinjaPath = NinjaPath(pitPath);
		}

		public static PitMetadata LoadMetadata(string pitPath)
		{
			var input = MetaPath(pitPath);
			if (!File.Exists(input))
				return null;

			var serializer = new JsonSerializer();
			using (var stream = new StreamReader(input))
			using (var reader = new JsonTextReader(stream))
			{
				return serializer.Deserialize<PitMetadata>(reader);
			}
		}

		private static string MetaPath(string pitPath)
		{
			return Path.ChangeExtension(pitPath, ".meta.json");
		}

		private static string NinjaPath(string pitPath)
		{
			return Path.ChangeExtension(pitPath, ".ninja");
		}

		public int TotalNodes { get; private set; }

		public IEnumerable<string> Run(
			bool verifyConfig = true,
			bool doLint = true,
			bool createMetadata = true,
			bool createNinja = true)
		{
			var dom = Parse(verifyConfig, doLint);
			if (createMetadata)
				SaveMetadata(dom);
			if (createNinja && _samples.Any())
				CreateNinjaDatabase(dom);
			return _errors;
		}

		internal void SaveMetadata(Peach.Core.Dom.Dom dom)
		{
			var metadata = MakeMetadata(dom);
			var serializer = new JsonSerializer();
			using (var stream = new StreamWriter(_pitMetaPath))
			using (var writer = new JsonTextWriter(stream))
			{
				writer.Formatting = Newtonsoft.Json.Formatting.Indented;
				serializer.Serialize(writer, metadata);
			}
		}

		void CreateNinjaDatabase(Peach.Core.Dom.Dom dom)
		{
			using (var db = new SampleNinjaDatabase(_pitNinjaPath))
			{
				foreach (var sample in _samples)
				{
					sample.DataModel.dom = dom;
					db.ProcessSample(sample.DataModel, sample.SamplePath);
				}
			}
		}

		class CustomParser : ProPitParser
		{
			protected override void handlePublishers(XmlNode node, Test parent)
			{
				// ignore publishers
				var args = new Dictionary<string, Variant>();
				var pub = new NullPublisher(args)
				{
					Name = node.getAttr("name", null) ?? parent.publishers.UniqueName()
				};
				parent.publishers.Add(pub);
			}
		}

		public Peach.Core.Dom.Dom Parse(bool verifyConfig, bool doLint)
		{
			var defs = PitDefines.ParseFile(_pitPath + ".config", _pitLibraryPath);
			var defsWithDefaults = defs.Evaluate().Select(PitDefines.PopulateRequiredDefine);

			var args = new Dictionary<string, object> {
				{ PitParser.DEFINED_VALUES, defsWithDefaults }
			};

			var parser = new CustomParser();
			var dom = parser.asParser(args, _pitPath);
			dom.context = new RunContext { test = dom.tests.First() };

			if (verifyConfig)
				VerifyConfig(defs, args);

			if (doLint)
				VerifyPitFiles(dom, new PitLintContext { IsTest = true });

			return dom;
		}

		static IEnumerable<string> GetDataModels(Peach.Core.Dom.Dom dom, string prefix)
		{
			return dom.dataModels.Select(dm =>
			{
				if (!string.IsNullOrEmpty(dom.Name))
					return "{0}{1}:{2}".Fmt(prefix, dom.Name, dm.Name);
				return "{0}{1}".Fmt(prefix, dm.Name);
			}).Concat(dom.ns.SelectMany(x => GetDataModels(x, x.Name + ":")));
		}

		public static void RaiseMissingDataModel(Peach.Core.Dom.Dom dom, string name, string pitPath)
		{
			var error = "DataModel '{0}' not found in '{1}'.\nAvailable models:\n".Fmt(name, pitPath);
			var models = string.Join("\n", GetDataModels(dom, null).Select(x => "    {0}".Fmt(x)));
			throw new ArgumentException(error + models);
		}

		private void VerifyConfig(PitDefines defs, IReadOnlyDictionary<string, object> args)
		{
			var defsList = defs.Walk().ToList();
			_errors.AddRange(defsList
				.Where(d => d.ConfigType != ParameterType.Space && string.IsNullOrEmpty(d.Name))
				.Select(d => "PitDefine '{0}' missing 'Name' attribute.".Fmt(d.Key))
			);
			_errors.AddRange(defsList
				.Where(d => d.ConfigType != ParameterType.Space && string.IsNullOrEmpty(d.Description))
				.Select(d => "PitDefine '{0}' missing 'Description' attribute.".Fmt(d.Key ?? d.Name))
			);

			object objUsed;
			if (args.TryGetValue(PitParser.USED_DEFINED_VALUES, out objUsed))
			{
				var used = (HashSet<string>)objUsed;
				used.ForEach(x => defsList.RemoveAll(d => d.Key == x));
			}

			defsList.RemoveAll(d => d.ConfigType == ParameterType.Space || d.ConfigType == ParameterType.Group);

			_errors.AddRange(defsList.Select(d => "Detected unused PitDefine: '{0}'.".Fmt(d.Key)));

			if (defs.Platforms.Any(p => p.Platform != Platform.OS.All))
				_errors.Add("Configuration file should not have platform specific defines.");
		}

		class PitLintContext
		{
			public bool IsTest { get; set; }
			public string StateModel { get; set; }
			public bool NonDeterministicActions { get; set; }
		}

		private void VerifyPitFiles(Peach.Core.Dom.Dom dom, PitLintContext ctx)
		{
			VerifyPit(dom.fileName, ctx);

			ctx.IsTest = false;

			foreach (var ns in dom.ns)
			{
				VerifyPitFiles(ns, ctx);
			}
		}

		internal PitMetadata MakeMetadata(Peach.Core.Dom.Dom dom)
		{
			TotalNodes = 0;

			var calls = new List<string>();
			var root = new PitField();
			var stateModel = dom.context.test.stateModel;
			var useFieldIds = stateModel.HasFieldIds;

			foreach (var state in stateModel.states)
			{
				foreach (var action in state.actions)
				{
					_samples.AddRange(
						from actionData in action.allData
						from data in actionData.allData
						let file = data as DataFile
						where file != null
						select new NinjaSample
						{
							SamplePath = file.FileName,
							DataModel = actionData.dataModel,
						});

					var node = new PitField();
					foreach (var actionData in action.outputData)
					{
						foreach (var mask in actionData.allData.OfType<DataFieldMask>())
						{
							mask.Apply(action, actionData.dataModel);
						}

						var kvs = actionData.dataModel
							.TuningTraverse(useFieldIds, true)
							.Where(x => x.Key != null);

						foreach (var kv in kvs)
						{
							var parent = node;
							var parts = kv.Key.Split('.');
							parts.Aggregate(parent, AddNode);
						}
					}

					if (node.Fields.Any())
					{
						var parent = AddParent(useFieldIds, root, state);
						parent = AddParent(useFieldIds, parent, action);
						MergeFields(parent.Fields, node.Fields);
					}

					var callAction = action as Call;
					if (callAction != null && !calls.Contains(callAction.method))
						calls.Add(callAction.method);
				}
			}

			return new PitMetadata
			{
				Calls = calls,
				Fields = root.Fields,
			};
		}

		private void MergeFields(List<PitField> lhs, List<PitField> rhs)
		{
			foreach (var item in rhs)
			{
				var node = lhs.SingleOrDefault(x => x.Id == item.Id);
				if (node == null)
				{
					lhs.Add(item);
				}
				else
				{
					MergeFields(node.Fields, item.Fields);
				}
			}
		}

		private PitField AddParent(bool hasFieldIds, PitField parent, IFieldNamed node)
		{
			if (hasFieldIds)
			{
				if (!string.IsNullOrEmpty(node.FieldId))
				{
					return AddNode(parent, node.FieldId);
				}
			}
			else
			{
				return AddNode(parent, node.Name);
			}

			return parent;
		}

		private PitField AddNode(PitField parent, string name)
		{
			var next = parent.Fields.SingleOrDefault(x => x.Id == name);
			if (next == null)
			{
				next = new PitField { Id = name };
				parent.Fields.Add(next);
				TotalNodes++;
			}
			return next;
		}

		private void VerifyPit(string fileName, PitLintContext ctx)
		{
			var justFileName = Path.GetFileName(fileName);
			var idxDeclaration = 0;
#if DEBUG
			var idxCopyright = 0;
#endif
			var idx = 0;

			using (var rdr = XmlReader.Create(fileName))
			{
				while (++idx > 0)
				{
					do
					{
						if (!rdr.Read())
							throw new ApplicationException("Failed to read xml.");
					} while (rdr.NodeType == XmlNodeType.Whitespace);

					if (rdr.NodeType == XmlNodeType.XmlDeclaration)
					{
						idxDeclaration = idx;
					}
#if DEBUG
					else if (rdr.NodeType == XmlNodeType.Comment)
					{
						idxCopyright = idx;

						var split = rdr.Value.Split('\n');
						if (split.Length <= 1)
							_errors.Add("{0}: Long form copyright message is missing.".Fmt(justFileName));
					}
#endif
					else if (rdr.NodeType == XmlNodeType.Element)
					{
						if (rdr.Name != "Peach")
						{
							_errors.Add("{0}: The first xml element is not <Peach>.".Fmt(justFileName));
							break;
						}

#if DEBUG
						if (!rdr.MoveToAttribute("description"))
							_errors.Add("{0} Pit is missing description attribute.".Fmt(justFileName));
						else if (string.IsNullOrEmpty(rdr.Value))
							_errors.Add("{0} Pit description is empty.".Fmt(justFileName));

						const string author = "Peach Fuzzer, LLC";

						if (!rdr.MoveToAttribute("author"))
							_errors.Add("{0}: Pit is missing author attribute.".Fmt(justFileName));
						else if (author != rdr.Value)
							_errors.Add("{2}: Pit author is '{0}' but should be '{1}'.".Fmt(rdr.Value, author, justFileName));
	
						if (!rdr.MoveToAttribute("schemaLocation", XmlSchema.InstanceNamespace))
							_errors.Add("{0}: Pit is missing xsi:schemaLocation attribute.".Fmt(justFileName));
						else if (SchemaLocation != rdr.Value)
							_errors.Add("{2}: Pit xsi:schemaLocation is '{0}' but should be '{1}'.".Fmt(rdr.Value, SchemaLocation, justFileName));
#endif

						if (!rdr.MoveToAttribute("xmlns"))
							_errors.Add("{0}: Pit is missing xmlns attribute.".Fmt(justFileName));
						else if (Namespace != rdr.Value)
							_errors.Add("{2}: Pit xmlns is '{0}' but should be '{1}'.".Fmt(rdr.Value, Namespace, justFileName));

						break;
					}
				}

				if (idxDeclaration != 1)
					_errors.Add("{0}: Pit is missing xml declaration.".Fmt(justFileName));

#if DEBUG
				if (idxCopyright == 0)
					_errors.Add("{0}: Pit is missing top level copyright message.".Fmt(justFileName));
#endif
			}

			{
				var doc = new XmlDocument();

				// Must call LoadXml() so that we can catch embedded newlines!
				doc.LoadXml(File.ReadAllText(fileName));

				var nav = doc.CreateNavigator();
				var nsMgr = new XmlNamespaceManager(nav.NameTable);
				nsMgr.AddNamespace("p", Namespace);

				var it = nav.Select("/p:Peach/p:Test", nsMgr);

				var expected = ctx.IsTest ? 1 : 0;

				if (it.Count != expected)
					_errors.Add("{2}: Number of <Test> elements is {0} but should be {1}.".Fmt(it.Count, expected, justFileName));

				while (it.MoveNext())
				{
					var maxSize = it.Current.GetAttribute("maxOutputSize", string.Empty);
					if (string.IsNullOrEmpty(maxSize))
						_errors.Add("{0}: <Test> element is missing maxOutputSize attribute.".Fmt(justFileName));

					var lifetime = it.Current.GetAttribute("targetLifetime", string.Empty);
					if (string.IsNullOrEmpty(lifetime))
						_errors.Add("{0}: <Test> element is missing targetLifetime attribute.".Fmt(justFileName));

					var nonDeterminisitic = it.Current.GetAttribute("nonDeterministicActions", string.Empty);
					ctx.NonDeterministicActions = !string.IsNullOrEmpty(nonDeterminisitic) && bool.Parse(nonDeterminisitic);

#if DEBUG
					if (!ShouldSkipRule(it, "Skip_Lifetime"))
					{
						var parts = fileName.Split(Path.DirectorySeparatorChar);
						var fileFuzzing = new[] { "Image", "Video", "Application" };
						if (parts.Any(fileFuzzing.Contains) || parts.Last().Contains("Client"))
						{
							if (lifetime != "iteration")
								_errors.Add("{1}: <Test> element has incorrect targetLifetime attribute. Expected 'iteration' but found '{0}'.".Fmt(lifetime, justFileName));
						}
						else
						{
							if (lifetime != "session")
								_errors.Add("{1}: <Test> element has incorrect targetLifetime attribute. Expected 'session' but found '{0}'.".Fmt(lifetime, justFileName));
						}
					}

					var loggers = it.Current.Select("p:Logger", nsMgr);
					if (loggers.Count != 0)
						_errors.Add("{1}: Number of <Logger> elements is {0} but should be 0.".Fmt(loggers.Count, justFileName));
#endif
					var stateModel = it.Current.Select("p:StateModel", nsMgr);
					if (stateModel.Count == 1 && stateModel.MoveNext())
						ctx.StateModel = stateModel.Current.GetAttribute("ref", string.Empty);

					var pubs = it.Current.Select("p:Publisher", nsMgr);
					while (pubs.MoveNext())
					{
						var cls = pubs.Current.GetAttribute("class", string.Empty);
						var parms = new List<string>();

						var parameters = pubs.Current.Select("p:Param", nsMgr);
						while (parameters.MoveNext())
						{
							var name = parameters.Current.GetAttribute("name", string.Empty);
							var value = parameters.Current.GetAttribute("value", string.Empty);
							if (!ShouldSkipRule(parameters, "Allow_HardCodedParamValue") &&
								(!value.StartsWith("##") || !value.EndsWith("##")))
							{
								_errors.Add(
									"{1}: <Publisher> parameter '{0}' is hard-coded, use a PitDefine ".Fmt(name, justFileName) +
									"(suppress with 'Allow_HardCodedParamValue')"
								);
							}

							parms.Add(name);
						}

						var comments = pubs.Current.SelectChildren(XPathNodeType.Comment);
						while (comments.MoveNext())
						{
							var value = comments.Current.Value.Trim();
							const string ignore = "PitLint: Allow_MissingParamValue=";
							if (value.StartsWith(ignore))
								parms.Add(value.Substring(ignore.Length));
						}

						var pub = ClassLoader.FindPluginByName<PublisherAttribute>(cls);
						if (pub == null)
						{
							_errors.Add("{1}: <Publisher> class '{0}' is not recognized.".Fmt(cls, justFileName));
						}
						else
						{
							var pri = pub.GetAttributes<PublisherAttribute>().First();
							if (pri.Name != cls)
								_errors.Add("{2}: '{0}' <Publisher> is referenced with deprecated name '{1}'.".Fmt(pri.Name, cls, justFileName));

							string[] optionalParams;
							if (!OptionalParams.TryGetValue(pri.Name, out optionalParams))
								optionalParams = new string[0];

							foreach (var attr in pub.GetAttributes<ParameterAttribute>())
							{
								if (!optionalParams.Contains(attr.name) && !parms.Contains(attr.name))
									_errors.Add("{2}: {0} publisher missing configuration for parameter '{1}'.".Fmt(pri.Name, attr.name, justFileName));
							}
						}
					}
				}

				var sm = nav.Select("/p:Peach/p:StateModel", nsMgr);
				while (sm.MoveNext())
				{
					var smName = sm.Current.GetAttribute("name", "");

					if (!string.IsNullOrEmpty(ctx.StateModel) && !ctx.StateModel.EndsWith(smName))
						continue;

					if (string.IsNullOrEmpty(smName))
						smName = "<unknown>";

					var actions = sm.Current.Select("p:State/p:Action[@type='call' and @publisher='Peach.Agent']", nsMgr);

					var gotStart = false;
					var gotEnd = false;

					while (actions.MoveNext())
					{
						var meth = actions.Current.GetAttribute("method", "");
						if (meth == "StartIterationEvent")
							gotStart = true;
						else if (meth == "ExitIterationEvent")
							gotEnd = true;
						else if (!gotStart && !ShouldSkipRule(actions, "Skip_StartIterationEvent"))
							_errors.Add(string.Format("{2}: StateModel '{0}' has an unexpected call action.  Method is '{1}' and should be 'StartIterationEvent' or 'ExitIterationEvent'.", smName, meth, justFileName));
					}

					if (!gotStart)
						_errors.Add(string.Format("{1}: StateModel '{0}' does not call agent with 'StartIterationEvent'.", smName, justFileName));

					if (!gotEnd)
						_errors.Add(string.Format("{1}: StateModel '{0}' does not call agent with 'ExitIterationEvent'.", smName, justFileName));

					var whenAction = sm.Current.Select("p:State/p:Action[@when]", nsMgr);
					while (whenAction.MoveNext())
					{
						var when = whenAction.Current.GetAttribute("when", string.Empty);
						if (when.Contains("controlIteration") && !ShouldSkipRule(whenAction, "Allow_WhenControlIteration"))
							_errors.Add("{1}: Action has when attribute containing controlIteration: {0}".Fmt(whenAction.Current.OuterXml, justFileName));

						// "context.controlIteration" is deterministic, so allow that to pass the lint check
						if (when != "context.controlIteration" && !ctx.NonDeterministicActions && !ShouldSkipRule(whenAction, "Allow_WhenNonDeterministicActions"))
							_errors.Add("{1}: Action has when attribute but <Test> doesn't have 'nonDeterministicActions' attribute set to 'true': {0}".Fmt(whenAction.Current.OuterXml, justFileName));
					}
				}

				var badValues = nav.Select("//*[contains(@value, '\n')]", nsMgr);
				while (badValues.MoveNext())
				{
					if (badValues.Current.GetAttribute("valueType", "") != "hex")
						_errors.Add("{1}: Element has value attribute with embedded newline: {0}".Fmt(badValues.Current.OuterXml, justFileName));
				}
			}
		}

		private static bool ShouldSkipRule(XPathNodeIterator it, string rule)
		{
			var stack = new Stack<string>();
			var preceding = it.Current.Select("preceding-sibling::*|preceding-sibling::comment()");

			while (preceding.MoveNext())
			{
				if (preceding.Current.NodeType == XPathNodeType.Comment)
					stack.Push(preceding.Current.Value);
				else
					stack.Clear();
			}

			return stack.Any(item => item.Contains("PitLint: {0}".Fmt(rule)));
		}
	}
}
