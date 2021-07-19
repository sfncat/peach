using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;
using System.Globalization;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Xml;
using Peach.Core;
using Peach.Pro.Core.WebServices.Models;
using Encoding = System.Text.Encoding;
using File = System.IO.File;
using Newtonsoft.Json;
using Peach.Pro.Core.License;

namespace Peach.Pro.Core.WebServices
{
	#region Peach Pit Xml Elements

	[XmlRoot("Peach", Namespace = Namespace)]
	[Serializable]
	public class PeachElement
	{
		public const string Namespace = "http://peachfuzzer.com/2012/Peach";
		public const string SchemaLocation = "http://peachfuzzer.com/2012/Peach peach.xsd";

		public PeachElement()
		{
			Children = new List<ChildElement>();
		}

		public abstract class ChildElement
		{
		}

		[XmlRoot("Test", Namespace = Namespace)]
		public class TestElement : ChildElement
		{
			public class AgentReferenceElement : ChildElement
			{
				[XmlAttribute("ref")]
				public string Ref { get; set; }
			}

			public class StateModelReferenceElement : ChildElement
			{
				[XmlAttribute("ref")]
				public string Ref { get; set; }
			}

			public TestElement()
			{
				Children = new List<ChildElement>();
			}

			[XmlAttribute("name")]
			public string Name { get; set; }

			[XmlElement("Agent", typeof(AgentReferenceElement))]
			[XmlElement("StateModel", typeof(StateModelReferenceElement))]
			public List<ChildElement> Children { get; set; }

			public IEnumerable<AgentReferenceElement> AgentRefs
			{
				get { return Children.OfType<AgentReferenceElement>(); }
			}

			public IEnumerable<StateModelReferenceElement> StateModelRefs
			{
				get { return Children.OfType<StateModelReferenceElement>(); }
			}
		}

		[XmlRoot("Agent", Namespace = Namespace)]
		public class AgentElement : ChildElement
		{
			public class MonitorElement : ChildElement
			{
				[XmlAttribute("class")]
				public string Class { get; set; }

				[XmlAttribute("name")]
				public string Name { get; set; }

				[XmlElement("Param", typeof(ParamElement))]
				public List<ParamElement> Params { get; set; }
			}

			[XmlAttribute("name")]
			public string Name { get; set; }

			[XmlAttribute("location")]
			[DefaultValue("")]
			public string Location { get; set; }

			[XmlElement("Monitor", typeof(MonitorElement))]
			public List<MonitorElement> Monitors { get; set; }
		}

		[XmlRoot("Param", Namespace = Namespace)]
		public class ParamElement : ChildElement
		{
			[XmlAttribute("name")]
			public string Name { get; set; }

			[XmlAttribute("value")]
			public string Value { get; set; }
		}

		[XmlRoot("Include", Namespace = Namespace)]
		public class IncludeElement : ChildElement
		{
			[XmlAttribute("ns")]
			public string Ns { get; set; }

			[XmlAttribute("src")]
			public string Source { get; set; }
		}

		[XmlRoot("StateModel", Namespace = Namespace)]
		public class StateModelElement : ChildElement
		{
			[XmlRoot("State", Namespace = Namespace)]
			public class StateElement
			{
				[XmlRoot("Action", Namespace = Namespace)]
				public class ActionElement
				{
					[XmlAttribute("type")]
					public string Type { get; set; }

					[XmlAttribute("method")]
					public string Method { get; set; }

					[XmlAttribute("publisher")]
					public string Publisher { get; set; }
				}

				public StateElement()
				{
					Actions = new List<ActionElement>();
				}

				[XmlElement("Action", typeof(ActionElement))]
				public List<ActionElement> Actions { get; set; }
			}

			public StateModelElement()
			{
				States = new List<StateElement>();
			}

			[XmlAttribute("name")]
			public string Name { get; set; }

			[XmlAttribute("initialState")]
			public string InitialState { get; set; }

			[XmlElement("State", typeof(StateElement))]
			public List<StateElement> States { get; set; }
		}

		[XmlAttribute("author")]
		[DefaultValue("")]
		public string Author { get; set; }

		[XmlAttribute("description")]
		[DefaultValue("")]
		public string Description { get; set; }

		[XmlAttribute("version")]
		[DefaultValue("")]
		public string Version { get; set; }

		[XmlElement("Include", typeof(IncludeElement))]
		[XmlElement("Test", typeof(TestElement))]
		[XmlElement("Agent", typeof(AgentElement))]
		[XmlElement("StateModel", typeof(StateModelElement))]
		public List<ChildElement> Children { get; set; }
	}

	#endregion

	public class ValidationEventArgs : EventArgs
	{
		public ValidationEventArgs(Exception exception, string fileName)
		{
			Exception = exception;
			FileName = fileName;
		}

		public Exception Exception { get; private set; }

		public string FileName { get; private set; }
	}

	[Serializable]
	public class PitDetail : INamed
	{
		public string Id { get; set; }
		public string Path { get; set; }
		public string PitUrl { get; set; }
		public List<Tag> Tags { get; set; }
		public PitConfig PitConfig { get; set; }
		public bool Locked { get; set; }

		[Obsolete]
		string INamed.name
		{
			get { return PitUrl; }
		}

		string INamed.Name
		{
			get { return PitUrl; }
		}
	}

	public interface IPitDatabase
	{
		IEnumerable<PitDetail> PitDetails { get; }
		IEnumerable<PitDetail> Entries { get; }
		IEnumerable<LibraryPit> LibraryPits { get; }
		IEnumerable<Library> Libraries { get; }

		Library GetLibraryById(string guid);

		Pit GetPitById(string guid);
		Pit GetPitByUrl(string url);
		PitDetail GetPitDetailByUrl(string url);

		Pit UpdatePitById(string guid, PitConfig data);
		Pit UpdatePitByUrl(string url, PitConfig data);

		void DeletePitById(string guid);

		Tuple<Pit, PitDetail> NewConfig(string pitUrl, string name, string description);
		Tuple<Pit, PitDetail> MigratePit(string legacyPitUrl, string pitUrl);
	}

	public class PitDatabase : IPitDatabase
	{
		public static readonly string PitServicePrefix = "/p/pits";
		public static readonly string LibraryServicePrefix = "/p/libraries";

		#region Static Helpers

		private static PeachElement Parse(string fileName, Stream input)
		{
			try
			{
				var settingsRdr = new XmlReaderSettings
				{
					ValidationType = ValidationType.Schema,
					NameTable = new NameTable(),
				};

				// Default the namespace to peach
				var nsMgrRdr = new XmlNamespaceManager(settingsRdr.NameTable);
				nsMgrRdr.AddNamespace("", PeachElement.Namespace);

				var parserCtx = new XmlParserContext(settingsRdr.NameTable, nsMgrRdr, null, XmlSpace.Default);

				using (var rdr = XmlReader.Create(input, settingsRdr, parserCtx))
				{
					var s = XmlTools.GetSerializer(typeof(PeachElement));
					var elem = (PeachElement)s.Deserialize(rdr);
					return elem;
				}
			}
			catch (Exception ex)
			{
				throw new PeachException(
					"Dependency error: {0} -- {1}".Fmt(fileName, ex.Message), ex
				);
			}
		}

		private static string MakeGuid(string value)
		{
			using (var md5 = new MD5CryptoServiceProvider())
			{
				var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(value));
				var sb = new StringBuilder();
				foreach (var b in bytes)
					sb.Append(b.ToString("x2"));
				return sb.ToString();
			}
		}

		private static readonly PeachVersion Version = MakePeachVer();
		private static PeachVersion MakePeachVer()
		{
			var ver = Assembly.GetExecutingAssembly().GetName().Version;

			return new PeachVersion {
				PeachUrl = "",
				Major = ver.Major.ToString(CultureInfo.InvariantCulture),
				Minor = ver.Minor.ToString(CultureInfo.InvariantCulture),
				Build = ver.Build.ToString(CultureInfo.InvariantCulture),
				Revision = ver.Revision.ToString(CultureInfo.InvariantCulture),
			};
		}

		#endregion

		internal static readonly string LegacyDir = "User";
		internal static readonly string ConfigsDir = "Configs";

		private readonly ILicense _license;
		private string _pitLibraryPath;

		private readonly NamedCollection<PitDetail> _entries = new NamedCollection<PitDetail>();
		private readonly NamedCollection<LibraryDetail> _libraries = new NamedCollection<LibraryDetail>();
		LibraryDetail _configsLib;

		public event EventHandler<ValidationEventArgs> ValidationEventHandler;
		public event EventHandler<PitDetail> LoadEventHandler;

		class LibraryDetail : INamed
		{
			public Library Library { get; set; }

			[Obsolete]
			string INamed.name
			{
				get { return Library.LibraryUrl; }
			}

			string INamed.Name
			{
				get { return Library.LibraryUrl; }
			}
		}

		public PitDatabase(ILicense license)
		{
			_license = license;
		}

		public void Load(string path)
		{
			_pitLibraryPath = Path.GetFullPath(path);

			_entries.Clear();
			_libraries.Clear();

			AddLibrary("", "Pits", true, false);
			_configsLib = AddLibrary(ConfigsDir, "Configurations", false, false);
			AddLibrary(LegacyDir, "Legacy", true, true);
		}

		private LibraryDetail AddLibrary(string subdir, string name, bool locked, bool legacy)
		{
			var path = Path.Combine(_pitLibraryPath, subdir);

			var guid = MakeGuid(name);

			var lib = new LibraryDetail
			{
				Library = new Library
				{
					LibraryUrl = LibraryServicePrefix + "/" + guid,
					Name = name,
					Description = name,
					Locked = locked,
					Groups = new List<Group>
					{
						new Group
						{
							GroupUrl = "",
							Access = locked ? GroupAccess.Read : GroupAccess.Read | GroupAccess.Write
						}
					},
					User = Environment.UserName,
					Versions = new List<LibraryVersion>
					{
						new LibraryVersion
						{
							Version = legacy ? 1 : 2,
							Locked = locked,
							Pits = new List<LibraryPit>()
						}
					}
				}
			};

			_libraries.Add(lib);

			if (Directory.Exists(path))
			{
				lib.Library.Timestamp = Directory.GetCreationTime(path);

				foreach (var dir in Directory.EnumerateDirectories(path))
				{
					var dirName = Path.GetDirectoryName(dir);
					if (locked && (dirName == LegacyDir || dirName == ConfigsDir))
						continue;

					var searchPattern = locked ? "*.xml" : "*.peach";
					foreach (var file in Directory.EnumerateFiles(dir, searchPattern))
					{
						try
						{
							var item = AddEntry(lib, file);
							if (item != null && LoadEventHandler != null)
								LoadEventHandler(this, item);
						}
						catch (Exception ex)
						{
							if (ValidationEventHandler != null)
								ValidationEventHandler(this, new ValidationEventArgs(ex, file));
						}
					}
				}
			}

			return lib;
		}

		private PitDetail AddEntry(LibraryDetail lib, string fileName)
		{
			var detail = MakePitDetail(fileName, lib.Library.Locked);

			var feature = _license.CanUsePit(detail.PitConfig.OriginalPit);
			if (!feature.IsValid)
				return null;

			_entries.Add(detail);

			// To maintain compatibility with older jobs, we need to continue
			// to map absolute paths to PitDetails, but only for legacy pit configs
			if (lib.Library.Versions[0].Version == 1)
			{
				var absDetail = ObjectCopier.Clone(detail);
				absDetail.PitUrl = PitServicePrefix + "/" + MakeGuid(fileName);
				_entries.Add(absDetail);
			}

			lib.Library.Versions[0].Pits.Add(new LibraryPit {
				Id = detail.Id,
				PitUrl = detail.PitUrl,
				Name = detail.PitConfig.Name,
				Description = detail.PitConfig.Description,
				Tags = detail.Tags,
				Locked = detail.Locked,
			});

			return detail;
		}

		private string GetCategory(string path)
		{
			return (Path.GetDirectoryName(path) ?? "").Split(Path.DirectorySeparatorChar).Last();
		}

		private string GetRelativePath(string path)
		{
			var len = _pitLibraryPath.Length;
			if (!_pitLibraryPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
				len++;
			return path.Substring(len);
		}

		private PitDetail MakePitDetail(string fileName, bool locked)
		{
			var relativePath = GetRelativePath(fileName);
			var guid = MakeGuid(relativePath);
			var dir = GetCategory(fileName);
			var tag = new Tag {
				Name = "Category." + dir,
				Values = new List<string> { "Category", dir },
			};

			PitConfig pitConfig;
			if (locked)
			{
				pitConfig = new PitConfig {
					OriginalPit = relativePath,
					Description = "", // TODO: get actual description
					Config = new List<Param>(),
					Agents = new List<Models.Agent>(),
					Weights = new List<PitWeight>(),
				};
			}
			else
			{
				pitConfig = LoadPitConfig(fileName);
			}

			pitConfig.Name = Path.GetFileNameWithoutExtension(fileName);

			return new PitDetail {
				Id = guid,
				Path = fileName,
				PitUrl = PitServicePrefix + "/" + guid,
				Tags = new List<Tag> { tag },
				PitConfig = pitConfig,
				Locked = locked,
			};
		}

		public IEnumerable<PitDetail> PitDetails
		{
			get { return _entries; }
		}

		public IEnumerable<PitDetail> Entries
		{
			get { return _entries; }
		}

		public IEnumerable<LibraryPit> LibraryPits 
		{
			get
			{
				return _entries.Select(x => new LibraryPit {
					Id = x.Id,
					PitUrl = x.PitUrl,
					Name = x.PitConfig.Name,
					Description = x.PitConfig.Description,
					Tags = x.Tags,
					Locked = x.Locked,
				});
			}
		}

		public IEnumerable<Library> Libraries
		{
			get { return _libraries.Select(item => item.Library); }
		}

		public Pit GetPitById(string guid)
		{
			var detail = GetPitDetailById(guid);
			if (detail == null)
				return null;

			return MakePit(detail);
		}

		public Pit GetPitByUrl(string url)
		{
			var detail = GetPitDetailByUrl(url);
			if (detail == null)
				return null;

			return MakePit(detail);
		}

		public Library GetLibraryById(string guid)
		{
			return GetLibraryByUrl(LibraryServicePrefix + "/" + guid);
		}

		private Library GetLibraryByUrl(string url)
		{
			var detail = GetLibraryDetailByUrl(url);
			if (detail == null)
				return null;

			return detail.Library;
		}

		private LibraryDetail GetLibraryDetailByUrl(string url)
		{
			LibraryDetail detail;
			_libraries.TryGetValue(url, out detail);
			return detail;
		}

		/// <summary>
		/// 
		/// Throws:
		///   KeyNotFoundException if libraryUrl/pitUrl is not valid.
		///   ArgumentException if a pit with the specified name already exists.
		/// </summary>
		/// <param name="pitUrl">The url of the source pit to copy.</param>
		/// <param name="name">The name of the newly copied pit.</param>
		/// <param name="description">The description of the newly copied pit.</param>
		/// <returns>The newly copied pit.</returns>
		public Tuple<Pit, PitDetail> NewConfig(string pitUrl, string name, string description)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("A non-empty pit name is required.", "name");

			if (Path.GetFileName(name) != name)
				throw new ArgumentException("A valid pit name is required.", "name");

			var srcPit = GetPitDetailByUrl(pitUrl);
			if (srcPit == null)
				throw new KeyNotFoundException("The original pit could not be found.");

			var srcFile = srcPit.Path;
			var srcCat = GetCategory(srcFile);

			var dstDir = Path.Combine(_pitLibraryPath, ConfigsDir, srcCat);
			if (!Directory.Exists(dstDir))
				Directory.CreateDirectory(dstDir);

			var dstFile = Path.Combine(dstDir, name + ".peach");
			if (File.Exists(dstFile))
				throw new ArgumentException("A pit already exists with the specified name.");

			PitConfig pitConfig;
			if (srcPit.Locked)
			{
				pitConfig = new PitConfig
				{
					OriginalPit = srcPit.PitConfig.OriginalPit,
					Config = new List<Param>(),
					Agents = new List<Models.Agent>(),
					Weights = new List<PitWeight>(),
				};
			}
			else
			{
				pitConfig = LoadPitConfig(srcPit.Path);
			}

			pitConfig.Name = name;
			pitConfig.Description = description;

			SavePitConfig(dstFile, pitConfig);

			var detail = AddEntry(_configsLib, dstFile);

			return new Tuple<Pit, PitDetail>(MakePit(detail), detail);
		}

		private string MakeUniquePath(string dir, string name, string ext)
		{
			var unique = "";
			var counter = 1;
			var path = Path.Combine(dir, name + unique + ext);
			while (File.Exists(path))
			{
				unique = "-Legacy-{0}".Fmt(counter++);
				path = Path.Combine(dir, name + unique + ext);
			}
			return path;
		}

		public Tuple<Pit, PitDetail> MigratePit(string legacyPitUrl, string pitUrl)
		{
			var legacyPit = GetPitDetailByUrl(legacyPitUrl);
			if (legacyPit == null)
				throw new KeyNotFoundException("The legacy pit could not be found.");

			var legacyFile = legacyPit.Path;
			var legacyConfigFile = legacyFile + ".config";
			var legacyName = Path.GetFileNameWithoutExtension(legacyFile);
			var legacyCat = GetCategory(legacyFile);

			var cfgDir = Path.Combine(_pitLibraryPath, ConfigsDir, legacyCat);
			if (!Directory.Exists(cfgDir))
				Directory.CreateDirectory(cfgDir);

			var xmlDir = (legacyPitUrl == pitUrl) ? Path.Combine(_pitLibraryPath, legacyCat) : cfgDir;
			if (!Directory.Exists(xmlDir))
				Directory.CreateDirectory(xmlDir);

			var cfgFile = MakeUniquePath(cfgDir, legacyName, ".peach");
			var cfgName = Path.GetFileNameWithoutExtension(cfgFile);
			var xmlFile = MakeUniquePath(xmlDir, legacyName, ".xml");
			var xmlConfigFile = MakeUniquePath(xmlDir, legacyName, ".xml.config");

			var originalPit = GetPitDetailByUrl(pitUrl);
			if (originalPit == null)
				throw new KeyNotFoundException("The original pit could not be found.");

			var originalPitPath = (legacyPitUrl == pitUrl) ? 
				GetRelativePath(xmlFile) : 
				originalPit.PitConfig.OriginalPit;

			// 1. Parse legacyPit.xml.config
			var defs = PitDefines.ParseFile(legacyConfigFile, _pitLibraryPath, false);

			// 2. Extract Configs
			var cfg = defs.Flatten()
				.Where(def => def.Key != "PitLibraryPath")
				.Select(def => new Param { Key = def.Key, Value = def.Value })
				.ToList();

			// 3. Parse legacyPit.xml
			PeachElement contents;
			using (var stream = File.OpenRead(legacyFile))
			{
				contents = Parse(legacyFile, stream);
			}

			// 4. Extract Agents
			var agents = contents.Children.OfType<PeachElement.AgentElement>();

			// 5. Write new .peach
			var pitConfig = new PitConfig {
				Name = cfgName,
				Description = contents.Description,
				OriginalPit = originalPitPath,
				Config = cfg,
				Agents = agents.ToWeb(),
				Weights = new List<PitWeight>(),
			};
			SavePitConfig(cfgFile, pitConfig);

			// 6. Move legacyPit.xml to target dir
			// TODO: strip <Agent></Agent> and <Test><Agent ref='XXX'/></Test>
			File.Move(legacyFile, xmlFile);

			// 7. Move legacyPit.xml.config to target dir
			if (File.Exists(legacyConfigFile))
				File.Move(legacyConfigFile, xmlConfigFile);

			var detail = AddEntry(_configsLib, cfgFile);

			return new Tuple<Pit, PitDetail>(MakePit(detail), detail);
		}

		public Pit UpdatePitById(string guid, PitConfig data)
		{
			var detail = GetPitDetailById(guid);
			if (detail == null)
				throw new KeyNotFoundException();

			if (detail.Locked)
				throw new UnauthorizedAccessException();

			detail.PitConfig.Description = data.Description;
			detail.PitConfig.Config = data.Config; // TODO: defines.ApplyWeb(config);
			detail.PitConfig.Agents = data.Agents.FromWeb();
			detail.PitConfig.Weights = data.Weights;

			SavePitConfig(detail.Path, detail.PitConfig);

			return MakePit(detail);
		}

		public Pit UpdatePitByUrl(string url, PitConfig data)
		{
			PitDetail pit;
			_entries.TryGetValue(url, out pit);
			return UpdatePitById(pit.Id, data);
		}

		public void DeletePitById(string guid)
		{
			var detail = GetPitDetailById(guid);
			if (detail == null)
				throw new KeyNotFoundException();
		
			if (detail.Locked)
				throw new UnauthorizedAccessException();

			File.Delete(detail.Path);
			_entries.Remove(detail);
		}

		private PitDetail GetPitDetailById(string guid)
		{
			return GetPitDetailByUrl(PitServicePrefix + "/" + guid);	
		}

		public PitDetail GetPitDetailByUrl(string url)
		{
			PitDetail pit;
			_entries.TryGetValue(url, out pit);
			return pit;
		}

		public static PitConfig LoadPitConfig(string path)
		{
			using (var stream = new StreamReader(path))
			using (var reader = new JsonTextReader(stream))
				return JsonUtilities.CreateSerializer().Deserialize<PitConfig>(reader);
		}

		public static void SavePitConfig(string path, PitConfig pit)
		{
			using (var stream = new StreamWriter(path))
			using (var writer = new JsonTextWriter(stream))
				JsonUtilities.CreateSerializer().Serialize(writer, pit);
		}

		#region Pit Config/Agents/Metadata

		bool ExtractCalls(PitResource pitResource,
		                  string xmlPath, 
		                  Stream input, 
		                  string stateModel, 
		                  string ns,
		                  HashSet<string> calls)
		{
			var contents = Parse(xmlPath, input);

			if (string.IsNullOrEmpty(stateModel))
			{

				stateModel = contents.Children.OfType<PeachElement.TestElement>()
									 .SelectMany(x => x.StateModelRefs)
									 .Select(x => x.Ref)
									 .FirstOrDefault();
			}

			foreach (var sm in contents.Children.OfType<PeachElement.StateModelElement>())
			{
				var name = string.IsNullOrEmpty(ns) ? sm.Name : "{0}:{1}".Fmt(ns, sm.Name);
				if (name == stateModel)
				{
					var methods = sm.States.SelectMany(x => x.Actions)
					                .Where(x => x.Type == "call" && x.Publisher == "Peach.Agent")
					                .Select(x => x.Method);
					foreach (var method in methods)
						calls.Add(method);
					return false;
				}
			}

			foreach (var inc in contents.Children.OfType<PeachElement.IncludeElement>())
			{
				using (var stream = pitResource.Load(inc.Source))
				{
					ExtractCalls(pitResource, inc.Source, stream, stateModel, inc.Ns, calls);
				}
			}

			return false;
		}

		private Pit MakePit(PitDetail detail)
		{
			var pitXml = Path.Combine(_pitLibraryPath, detail.PitConfig.OriginalPit);
			var pitConfig = pitXml + ".config";
			var defs = PitDefines.ParseFile(pitConfig, _pitLibraryPath);
			var metadata = PitCompiler.LoadMetadata(pitXml);

			var calls = new HashSet<string>();
			var pitResource = new PitResource(_license, _pitLibraryPath, pitXml);
			using (var stream = File.OpenRead(pitXml))
			{
				ExtractCalls(pitResource, pitXml, stream, null, null, calls);
			}

			var pit = new Pit {
				Id = detail.Id,
				PitUrl = detail.PitUrl,
				Name = detail.PitConfig.Name,
				Description = detail.PitConfig.Description,
				Tags = detail.Tags,
				Locked = detail.Locked,
				Peaches = new List<PeachVersion> { Version },
				User = Environment.UserName,
				Timestamp = File.GetLastWriteTimeUtc(detail.Path),
				Config = detail.PitConfig.Config,
				Agents = detail.PitConfig.Agents,
				Weights = detail.PitConfig.Weights,
				Metadata = new PitMetadata {
					Defines = defs.ToWeb(detail.PitConfig.Config),
					Monitors = MonitorMetadata.Generate(calls.ToList()),
					Fields = metadata != null ? metadata.Fields : null,
				}
			};

			return pit;
		}

		#endregion
	}
}
