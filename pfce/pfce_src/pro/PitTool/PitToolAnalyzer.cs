using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Peach.Core;
using Peach.Core.Runtime;
using Peach.Pro.Core;
using Peach.Pro.Core.Runtime;

namespace PitTool
{
	public partial class PitTool
	{
		int Analyzer(Command cmd, List<string> args)
		{
			var name = args.FirstOrDefault();
			if (name == null)
			{
				Console.WriteLine("Missing required arguments");
				Console.WriteLine();
				ShowHelp(cmd, args);
				return 1;
			}

			var type = ClassLoader.FindPluginByName<AnalyzerAttribute>(name);
			if (type == null)
				throw new SyntaxException("Error, unable to locate analyzer named '{0}'.".Fmt(name));

			if (!IsAnalyzerSupported(type))
				throw new NotSupportedException("Analyzer is not configured to run from the command line.");

			var analyzer = (Analyzer)Activator.CreateInstance(type);

			InteractiveConsoleWatcher.WriteInfoMark();
			Console.WriteLine("Starting Analyzer");

			analyzer.asCommandLine(args.Skip(1).ToList());

			return 0;
		}

		bool IsAnalyzerSupported(Type type)
		{
			var field = type.GetField("supportCommandLine", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
			return field != null && (bool)field.GetValue(null);
		}

		bool IsAnalyzerInternal(PluginAttribute plugin)
		{
			return plugin.Scope == PluginScope.Internal;
		}

		class Plugin
		{
			private readonly PluginAttribute _attr;
			private readonly Type _type;

			public Plugin(PluginAttribute attr, Type type)
			{
				_attr = attr;
				_type = type;
			}

			public string Name
			{
				get { return _attr.Name; }
			}

			public string DisplayName
			{
				get
				{
					if (_attr.Scope == PluginScope.Beta)
						return _attr.Name + " (beta)";
					return _attr.Name;
				}
			}

			public string Usage
			{
				get
				{
					var usage = _type.GetAttributes<UsageAttribute>().SingleOrDefault();
					if (usage != null)
						return usage.Message;
					return "";
				}
			}

			public string Description
			{
				get
				{
					var desc = _type.GetAttributes<DescriptionAttribute>().SingleOrDefault();
					if (desc != null)
						return desc.Description;
					return "";
				}
			}

			public string[] LongDescription
			{
				get
				{
					var desc = _type.GetAttributes<LongDescriptionAttribute>().SingleOrDefault();
					if (desc != null)
						return desc.Text.Trim().Split('\n');
					return new string[0];
				}
			}
			
			public string Obsolete
			{
				get
				{
					var obsolete = _type.GetAttributes<ObsoleteAttribute>().SingleOrDefault();
					if (obsolete != null)
						return obsolete.Message;
					return null;
				}
			}
			
			public bool IsObsolete { get { return Obsolete != null; } }

			public int ShowHelp(OptionSet general, Command cmd)
			{
				Console.WriteLine("Usage:");
				Console.WriteLine("  {0} {1} {2} {3}".Fmt(Utilities.ExecutableName, cmd.name, Name, Usage));
				Console.WriteLine();

				Console.WriteLine("Description:");
				Console.WriteLine("  {0} {1}", Description, Obsolete);
				var longDesc = LongDescription;
				if (longDesc.Any())
				{
					Console.WriteLine();
					foreach(var line in longDesc)
						Console.WriteLine("  {0}", line);
				}
				Console.WriteLine();

				Console.WriteLine("General Options:");
				general.WriteOptionDescriptions(Console.Out);
				Console.WriteLine();

				return 0;
			}
		}

		int ShowAnalyzerHelp(Command cmd, List<string> args)
		{
			var query = from x in ClassLoader.GetAllByAttribute<AnalyzerAttribute>()
						where
							x.Key.IsDefault &&
							IsAnalyzerSupported(x.Value) &&
							!IsAnalyzerInternal(x.Key)
						select new Plugin(x.Key, x.Value);
			var analyzers = query.ToList();

			var first = args.FirstOrDefault();
			if (first != null)
			{
				var found = analyzers.SingleOrDefault(x => x.Name == first);
				if (found != null)
					return found.ShowHelp(_options, cmd);
			}

			Console.WriteLine("Usage:");
			Console.WriteLine("  " + cmd.Usage.Fmt(Utilities.ExecutableName, cmd.Name));
			Console.WriteLine();

			Console.WriteLine("Description:");
			Console.WriteLine("  {0}", cmd.Description);
			Console.WriteLine();

			if (analyzers.Any())
			{
				Console.WriteLine("Analyzers:");
				foreach (var analyzer in analyzers.Where(x => !x.IsObsolete))
				{
					Console.WriteLine("  {0,-27}{1}", analyzer.DisplayName, analyzer.Description);
				}
				Console.WriteLine();
			}

			var obsolete = analyzers.Where(x => x.IsObsolete).ToList();
			if (obsolete.Any())
			{
				Console.WriteLine("Deprecated Analyzers:");
				foreach (var analyzer in obsolete)
				{
					Console.WriteLine("  {0,-27}{1} {2}", analyzer.DisplayName, analyzer.Description, analyzer.Obsolete);
				}
				Console.WriteLine();
			}


			if (cmd.Options != null)
			{
				Console.WriteLine("{0} Options:", cmd.Name);
				cmd.Options.WriteOptionDescriptions(Console.Out);
				Console.WriteLine();
			}

			Console.WriteLine("General Options:");
			_options.WriteOptionDescriptions(Console.Out);
			Console.WriteLine();

			return 0;
		}
	}
}
