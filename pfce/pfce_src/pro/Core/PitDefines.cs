using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Peach.Core;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Core
{
	[XmlRoot("PitDefines", IsNullable = false, Namespace = "http://peachfuzzer.com/2012/PitDefines")]
	public class PitDefines
	{
		#region Defines

		public abstract class Define
		{
			protected Define()
			{
				Defines = new List<Define>();
			}

			public abstract ParameterType ConfigType { get; }

			[XmlIgnore]
			public string Key { get; set; }

			[XmlIgnore]
			public string Value { get; set; }

			[XmlIgnore]
			public string Description { get; set; }

			[XmlIgnore]
			public string Name { get; set; }

			[XmlIgnore]
			public bool Optional { get; set; }

			public virtual string[] Defaults
			{
				get { return new string[0]; }
			}

			// only used by RangeDefine
			public virtual long? Min
			{
				get { return null; }
			}

			// only used by RangeDefine
			public virtual ulong? Max
			{
				get { return null; }
			}

			[XmlIgnore]
			public List<Define> Defines { get; set; }
		}

		public abstract class Collection : Define
		{
			public override ParameterType ConfigType
			{
				get { return ParameterType.Group; }
			}

			[XmlIgnore]
			public bool Collapsed { get; set; }

			[XmlIgnore]
			public abstract Platform.OS Platform { get; }

			[XmlElement("String", Type = typeof(StringDefine))]
			[XmlElement("Hex", Type = typeof(HexDefine))]
			[XmlElement("Range", Type = typeof(RangeDefine))]
			[XmlElement("Ipv4", Type = typeof(Ipv4Define))]
			[XmlElement("Ipv6", Type = typeof(Ipv6Define))]
			[XmlElement("Hwaddr", Type = typeof(HwaddrDefine))]
			[XmlElement("Iface", Type = typeof(IfaceDefine))]
			[XmlElement("Strategy", Type = typeof(StrategyDefine))]
			[XmlElement("Enum", Type = typeof(EnumDefine))]
			[XmlElement("Define", Type = typeof(UserDefine))]
			[XmlElement("Bool", Type = typeof(BoolDefine))]
			[XmlElement("Space", Type = typeof(Space))]
			[XmlElement("Group", Type = typeof(Group))]
			public List<Define> Children
			{
				get { return Defines; }
				set { Defines = value; }
			}

			public override string[] Defaults
			{
				get { return null; }
			}
		}

		public class Space : Define
		{
			public override ParameterType ConfigType
			{
				get { return ParameterType.Space; }
			}

			public override string[] Defaults
			{
				get { return null; }
			}
		}

		public class Group : Collection
		{
			[XmlAttribute("name")]
			public string NameAttr
			{
				get { return Name; }
				set { Name = value; }
			}

			[XmlAttribute("collapsed")]
			[DefaultValue(false)]
			public bool CollapsedAttr
			{
				get { return Collapsed; }
				set { Collapsed = value; }
			}

			[XmlAttribute("description")]
			[DefaultValue("")]
			public string DescriptionAttr
			{
				get { return Description; }
				set { Description = value; }
			}

			public override Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.All; }
			}
		}

		public abstract class PrimitiveDefine : Define
		{
			[XmlAttribute("name")]
			public string NameAttr
			{
				get { return Name; }
				set { Name = value; }
			}

			[XmlAttribute("key")]
			public string KeyAttr
			{
				get { return Key; }
				set { Key = value; }
			}

			[XmlAttribute("value")]
			public string ValueAttr
			{
				get { return Value; }
				set { Value = value; }
			}

			[XmlAttribute("description")]
			[DefaultValue("")]
			public string DescriptionAttr
			{
				get { return Description; }
				set { Description = value; }
			}
		}

		public class UserDefine : PrimitiveDefine
		{
			public override ParameterType ConfigType
			{
				get { return ParameterType.User; }
			}
		}

		/// <summary>
		/// Free form string
		/// </summary>
		public class StringDefine : PrimitiveDefine
		{
			[XmlAttribute("optional")]
			[DefaultValue(false)]
			public bool OptionalAttr
			{
				get { return Optional; }
				set { Optional = value; }
			}

			public override ParameterType ConfigType
			{
				get { return ParameterType.String; }
			}
		}

		public class HexDefine : PrimitiveDefine
		{
			public override ParameterType ConfigType
			{
				get { return ParameterType.Hex; }
			}
		}

		public class RangeDefine : PrimitiveDefine
		{
			[XmlAttribute("min")]
			public long MinValue { get; set; }

			[XmlAttribute("max")]
			public ulong MaxValue { get; set; }

			public override ParameterType ConfigType
			{
				get { return ParameterType.Range; }
			}

			public override long? Min
			{
				get { return MinValue; }
			}

			public override ulong? Max
			{
				get { return MaxValue; }
			}
		}

		public class Ipv4Define : PrimitiveDefine
		{
			public override ParameterType ConfigType
			{
				get { return ParameterType.Ipv4; }
			}
		}

		public class Ipv6Define : PrimitiveDefine
		{
			public override ParameterType ConfigType
			{
				get { return ParameterType.Ipv6; }
			}
		}

		public class HwaddrDefine : PrimitiveDefine
		{
			public override ParameterType ConfigType
			{
				get { return ParameterType.Hwaddr; }
			}
		}

		public class IfaceDefine : PrimitiveDefine
		{
			public override ParameterType ConfigType
			{
				get { return ParameterType.Iface; }
			}
		}

		public class BoolDefine : PrimitiveDefine
		{
			public override ParameterType ConfigType
			{
				get { return ParameterType.Bool; }
			}

			public override string[] Defaults
			{
				get
				{
					return new[] { "true", "false" };
				}
			}
		}

		public class StrategyDefine : PrimitiveDefine
		{
			public override ParameterType ConfigType
			{
				get { return ParameterType.Enum; }
			}

			public override string[] Defaults
			{
				get
				{
					return ClassLoader
						.GetAllByAttribute<MutationStrategyAttribute>(null)
						.Where(kv => kv.Key.IsDefault && kv.Key.Scope == PluginScope.Release)
						.Select(kv => kv.Key.Name)
						.ToArray();
				}
			}
		}

		public class EnumDefine : PrimitiveDefine
		{
			[XmlIgnore]
			public Type EnumType { get; protected set; }

			[XmlAttribute("enumType")]
			public string EnumTypeName
			{
				get
				{
					return EnumType != null ? EnumType.FullName : null;
				}
				set
				{
					if (value == null)
					{
						EnumType = null;
						return;
					}

					foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
					{
						EnumType = asm.GetType(value);
						if (EnumType != null)
							return;
					}

					throw new ArgumentException();
				}
			}

			public override ParameterType ConfigType
			{
				get { return ParameterType.Enum; }
			}

			public override string[] Defaults
			{
				get
				{
					return Enum.GetNames(EnumType);
				}
			}
		}

		public class SystemDefine : PrimitiveDefine
		{
			public override ParameterType ConfigType
			{
				get { return ParameterType.System; }
			}
		}

		#endregion

		#region Platforms

		public class None : Collection
		{
			public None()
			{
				Name = Description = "None";
			}

			[XmlIgnore]
			public override Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.None; }
			}
		}

		public class Windows : Collection
		{
			public Windows()
			{
				Name = Description = "Windows";
			}

			[XmlIgnore]
			public override Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.Windows; }
			}
		}

		public class OSX : Collection
		{
			public OSX()
			{
				Name = Description = "OSX";
			}

			[XmlIgnore]
			public override Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.OSX; }
			}
		}

		public class Linux : Collection
		{
			public Linux()
			{
				Name = Description = "Linux";
			}

			[XmlIgnore]
			public override Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.Linux; }
			}
		}

		public class Unix : Collection
		{
			public Unix()
			{
				Name = Description = "Unix";
			}

			[XmlIgnore]
			public override Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.Unix; }
			}
		}

		public class All : Collection
		{
			public All()
			{
				Name = Description = "All";
			}

			[XmlIgnore]
			public override Platform.OS Platform
			{
				get { return Peach.Core.Platform.OS.All; }
			}
		}

		#endregion

		#region Public Members

		public PitDefines()
		{
			Children = new List<Collection>();
			SystemDefines = new List<Define>();
		}

		[XmlElement("None", Type = typeof(None))]
		[XmlElement("OSX", Type = typeof(OSX))]
		[XmlElement("Windows", Type = typeof(Windows))]
		[XmlElement("Linux", Type = typeof(Linux))]
		[XmlElement("Unix", Type = typeof(Unix))]
		[XmlElement("All", Type = typeof(All))]
		[XmlElement("Group", Type = typeof(Group))]
		public List<Collection> Children { get; set; }

		[XmlIgnore]
		public List<Define> SystemDefines { get; set; }

		[XmlIgnore]
		public List<Collection> Platforms { get { return Children; } }

		#endregion

		#region Parse

		/// <summary>
		/// Deserialize a pit defines file.
		/// 1. If the file does not exist, it will be treated as an empty file.
		/// 2. The returned object will include all the SystemDefines.
		/// 3. PitLibraryPath system define will be set to Peach.Cwd
		/// </summary>
		/// <param name="fileName">Config file to deserialize.</param>
		/// <returns></returns>
		public static PitDefines ParseFile(string fileName)
		{
			return ParseFile(fileName, null, null);
		}

		/// <summary>
		/// Deserialize a pit defines file.
		/// 1. If the file does not exist, an empty defines object will be returned.
		/// 2. The returned object will include all the SystemDefines.
		/// 3. PitLibraryPath system define will be set to pitLibraryPath argument.
		/// </summary>
		/// <param name="fileName">Config file to deserialize.</param>
		/// <param name="pitLibraryPath">Value of PitLibraryPath system define.</param>
		/// <param name="jobId">Job identifer.</param>
		/// <param name="includeSystemDefs">Whether to include system defines</param>
		/// <returns></returns>
		public static PitDefines ParseFile(string fileName, string pitLibraryPath, Guid jobId, bool includeSystemDefs = true)
		{
			if (pitLibraryPath == null)
				throw new ArgumentNullException("pitLibraryPath");

			return ParseFile(fileName, pitLibraryPath, null, jobId, includeSystemDefs);
		}

		/// <summary>
		/// Deserialize a pit defines file.
		/// 1. If the file does not exist, an empty defines object will be returned.
		/// 2. The returned object will include all the SystemDefines.
		/// 3. PitLibraryPath system define will be set to pitLibraryPath argument.
		/// </summary>
		/// <param name="fileName">Config file to deserialize.</param>
		/// <param name="pitLibraryPath">Value of PitLibraryPath system define.</param>
		/// <param name="includeSystemDefs">Whether to include system defines</param>
		/// <returns></returns>
		public static PitDefines ParseFile(string fileName, string pitLibraryPath, bool includeSystemDefs = true)
		{
			if (pitLibraryPath == null)
				throw new ArgumentNullException("pitLibraryPath");

			return ParseFile(fileName, pitLibraryPath, null, Guid.Empty, includeSystemDefs);
		}

		/// <summary>
		/// Deserialize a pit defines file.
		/// 1. If the file does not exist, it will be treated as an empty file.
		/// 2. The returned object will include all the SystemDefines.
		/// 3. PitLibraryPath system define will be set to Peach.Cwd
		/// 4. All K/V in overrides will be considered system defines
		/// </summary>
		/// <param name="fileName">Config file to deserialize.</param>
		/// <param name="overrides">Command line overrides.</param>
		/// <param name="jobId">Job identifer.</param>
		/// <returns></returns>
		public static PitDefines ParseFile(string fileName, IEnumerable<KeyValuePair<string, string>> overrides, Guid jobId)
		{
			if (overrides == null)
				throw new ArgumentNullException("overrides");

			return ParseFile(fileName, null, overrides, jobId);
		}

		public static PitDefines ParseFile(
			string fileName,
			string pitLibraryPath,
			IEnumerable<KeyValuePair<string, string>> overrides,
			bool includeSystemDefs = true)
		{
			return ParseFile(fileName, pitLibraryPath, overrides, Guid.Empty, includeSystemDefs);
		}

		public static PitDefines ParseFile(
			string fileName,
			string pitLibraryPath,
			IEnumerable<KeyValuePair<string, string>> overrides,
			Guid jobId,
			bool includeSystemDefs = true)
		{
			if (!File.Exists(fileName))
				return Parse(null, pitLibraryPath, overrides, jobId, includeSystemDefs);

			using (var stream = File.OpenRead(fileName))
			{
				return Parse(stream, pitLibraryPath, overrides, jobId, includeSystemDefs);
			}
		}

		public static PitDefines Parse(
			Stream stream,
			string pitLibraryPath,
			IEnumerable<KeyValuePair<string, string>> overrides,
			Guid jobId,
			bool includeSystemDefs = true)
		{
			var defs = stream != null ? XmlTools.Deserialize<PitDefines>(stream) : new PitDefines();

			if (includeSystemDefs)
			{
				defs.SystemDefines.AddRange(new Define[] {
					new SystemDefine
					{
						Key = "Peach.JobId",
						Name = "Peach Job Id",
						Description = "Job ID for current fuzzing run",
						Value = jobId.ToString()
					},
					new SystemDefine {
						Key = "Peach.OS",
						Name = "Peach OS",
						Description = "Operating System that Peach is running on",
						Value = Platform.GetOS().ToString().ToLower()
					},
					new SystemDefine {
						Key = "Peach.Pwd",
						Name = "Peach Installation Directory",
						Description = "Full path to Peach installation",
						Value = Utilities.ExecutionDirectory,
					},
					new SystemDefine {
						Key = "Peach.Cwd",
						Name = "Peach Working Directory",
						Description = "Full path to the current working directory",
						Value = Environment.CurrentDirectory,
					},
					new SystemDefine {
						Key = "Peach.LogRoot",
						Name = "Root Log Directory",
						Description = "Full path to the root log directory",
						Value = Configuration.LogRoot,
					},
					new SystemDefine {
						Key = "Peach.Plugins",
						Name = "Plugins Directory",
						Description = "Full path to the plugins directory",
						Value = Configuration.PluginsPath,
					},
					new SystemDefine {
						Key = "Peach.Scripts",
						Name = "Scripts Directory",
						Description = "Full path to the scripts directory",
						Value = Configuration.ScriptsPath,
					},
					new SystemDefine {
						Key = "PitLibraryPath",
						Name = "Pit Library Path",
						Description = "Path to root of Pit Library",
						Value = pitLibraryPath ?? Environment.CurrentDirectory,
					}
				});
			}

			if (overrides != null)
			{
				defs.SystemDefines.AddRange(
					overrides.Select(kv => new SystemDefine
					{
						Name = kv.Key,
						Key = kv.Key,
						Value = kv.Value
					})
				);
			}
			return defs;
		}

		public static List<KeyValuePair<string, string>> ParseFileWithDefaults(string pitLibraryPath, string fileName)
		{
			var defs = ParseFile(fileName + ".config", pitLibraryPath).Evaluate();

			// Some defines are expected to be empty if they are required to be
			// set by the user.  The pit will not parse w/o them being set however
			// so inject parsable defaults in this case
			defs = defs.Select(PopulateRequiredDefine).ToList();

			return defs;
		}

		public static KeyValuePair<string, string> PopulateRequiredDefine(KeyValuePair<string, string> item)
		{
			if (!String.IsNullOrEmpty(item.Value))
				return item;

			if (item.Key.EndsWith("MAC"))
				return new KeyValuePair<string, string>(item.Key, "00:00:00:00:00:00");

			if (item.Key.EndsWith("IPv4"))
				return new KeyValuePair<string, string>(item.Key, "0.0.0.0");

			if (item.Key.EndsWith("IPv6"))
				return new KeyValuePair<string, string>(item.Key, "::1");

			if (item.Key.EndsWith("Port"))
				return new KeyValuePair<string, string>(item.Key, "0");

			return new KeyValuePair<string, string>(item.Key, "0");
		}

		#endregion

		#region Evaluate

		private static IEnumerable<Define> Flatten(Define defines)
		{
			var toVisit = new List<Define> { null };

			var it = defines;

			while (it != null)
			{
				yield return it;

				var index = toVisit.Count;
				foreach (var item in it.Defines)
					toVisit.Insert(index, item);

				index = toVisit.Count - 1;
				it = toVisit[index];
				toVisit.RemoveAt(index);
			}
		}

		public IEnumerable<Define> Walk()
		{
			var os = Platform.GetOS();

			return Children.Where(x => x.Platform.HasFlag(os)).SelectMany(Flatten);
		}

		public List<KeyValuePair<string, string>> Flatten()
		{
			var ret = Walk()
				.Where(x => x.ConfigType != ParameterType.Group && x.ConfigType != ParameterType.Space)
				.Concat(SystemDefines)
				.Reverse()
				.Distinct(DefineComparer.Instance)
				.Select(x => new KeyValuePair<string, string>(x.Key, x.Value))
				.Reverse()
				.ToList();
			return ret;
		}

		/// <summary>
		/// Returns a list of key/value pairs that can be passed to the PitParser.
		/// This function will:
		/// 1) Flatten all groups.
		/// 2) Substitute defines in defines.
		/// 3) Ensure all keys are unique.
		/// 4) If duplicate keys are found, the last key/value pair is returned.
		/// </summary>
		/// <returns></returns>
		public List<KeyValuePair<string, string>> Evaluate()
		{
			var ret = Flatten();

			var re = new Regex("##(\\w+?)##");

			var evaluator = new MatchEvaluator(delegate(Match m)
			{
				var key = m.Groups[1].Value;
				var val = ret.Where(x => x.Key == key).Select(x => x.Value).FirstOrDefault();

				return val ?? m.Groups[0].Value;
			});

			for (var i = 0; i < ret.Count; )
			{
				var oldVal = ret[i].Value;
				if (oldVal == null)
					throw new PeachException("Undefined PitDefine: \"{0}\"".Fmt(ret[i].Key));

				var newVal = re.Replace(oldVal, evaluator);

				if (oldVal != newVal)
					ret[i] = new KeyValuePair<string, string>(ret[i].Key, newVal);
				else
					++i;
			}

			return ret;
		}

		class DefineComparer : IEqualityComparer<Define>
		{
			public static readonly DefineComparer Instance = new DefineComparer();

			public bool Equals(Define lhs, Define rhs)
			{
				return lhs.Key.Equals(rhs.Key);
			}

			public int GetHashCode(Define obj)
			{
				return obj.Key.GetHashCode();
			}
		}

		#endregion
	}
}
