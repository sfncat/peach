using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Peach.Core;
using Peach.Core.Runtime;
using Peach.Core.Xsd;
using Peach.Pro.Core;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Core.Storage;
using Peach.Pro.PitTester;

namespace PitTool
{
	public partial class PitTool : BaseProgram
	{
		// global options
		string _pitLibraryPath;

		readonly NamedCollection<Command> _cmds = new NamedCollection<Command>();

		// compile options
		bool _no_verify;

		bool _no_lint;
		bool _no_meta;
		bool _no_ninja;

#if DEBUG

		// protect options
		string _prefix = "";

		string _salt;
#endif

#if DEBUG

		// test options
		uint? _seed;

		bool _notest;
		bool _nodata;
		bool _keepGoing;
		bool _profile;
		uint? _stop;
		readonly List<string> _errors = new List<string>();
#endif

		static int Main(string[] args)
		{
			using (var program = new PitTool())
			{
				return program.Run(args);
			}
		}

		protected override void AddCustomOptions(OptionSet options)
		{
			options.Add("pits=", "Specify the PitLibraryPath.", x => _pitLibraryPath = x);

			_cmds.Add(new Command
			{
				Name = "help",
				Usage = "{0} {1} <command>",
				Description = "Show help for commands.",
				Action = Help
			});
			_cmds.Add(new Command
			{
				Name = "compile",
				Usage = "{0} {1} [options] <PitPath>",
				Description = "Validate and compile pit into .meta.json and .ninja files.",
				Options = MakeCompileOptions(),
				Action = Compile,
			});
			_cmds.Add(new Command
			{
				Name = "ninja",
				Usage = "{0} {1} <PitPath> <DataModel> <SamplesPath>",
				Description = "Create a sample ninja database.",
				Action = Ninja,
			});
#if DEBUG
			_cmds.Add(new Command
			{
				Name = "test",
				Usage = "{0} {1} [options] <PitTestPath>",
				Description = "Run standard pit tests.",
				Options = MakeTestOptions(),
				Action = Test,
			});
#endif
#if DEBUG
			_cmds.Add(new Command
			{
				Name = "nunit",
				Usage = "{0} {1} [options] <PitPath> <OutputPath>",
				Description = "Create an assembly that can be used by NUnit.",
				Action = MakeTestAssembly,
			});
#endif
#if DEBUG
			_cmds.Add(new Command
			{
				Name = "protect",
				Usage = "{0} {1} [options] <Input> <Output>",
				Description = "Protect a resource assembly.",
				Options = MakeProtectOptions(),
				Action = Protect,
			});
#endif
			_cmds.Add(new Command
			{
				Name = "crack",
				Usage = "{0} {1} [options] <PitPath> <DataModel> <SamplePath>",
				Description = "Crack a sample file.",
				Action = Crack,
			});
			_cmds.Add(new Command
			{
				Name = "makexsd",
				Usage = "{0} {1}",
				Description = "Generate a peach.xsd file.",
				Action = MakeXsd,
			});
			_cmds.Add(new Command
			{
				Name = "analyzer",
				Usage = "{0} {1} <analyzer> [options]",
				Description = "Run a Peach analyzer.",
				Action = Analyzer,
				Help = ShowAnalyzerHelp,
			});
		}

		OptionSet MakeCompileOptions()
		{
			var options = new OptionSet
			{
				{"no-verify", "Don't verify PitDefines.", x => _no_verify = true},
				{"no-lint", "Don't perform lint checks.", x => _no_lint = true},
				{"no-meta", "Don't generate metadata used for tuning.", x => _no_meta = true},
				{"no-ninja", "Don't generate a sample ninja database.", x => _no_ninja = true},
			};
			return options;
		}

#if DEBUG

		OptionSet MakeTestOptions()
		{
			var options = new OptionSet
			{
				{"seed=", "Sets the seed used by the random number generator", v => _seed = uint.Parse(v)},
				{"notest", "Skip PitTest", v => _notest = true},
				{"nodata", "Skip VerifyDataSets", v => _nodata = true},
				{"profile", "Enable testing suitable for profiling", v => _profile = true},
				{"1|single", "Run a single iteration", v => _stop = 1},
				{"stop=", "Specify how many iterations to run", v => _stop = Convert.ToUInt32(v)},
				{"k|keepGoing", "Don't stop on the first failure", v => _keepGoing = true}
			};
			return options;
		}

#endif

#if DEBUG

		OptionSet MakeProtectOptions()
		{
			var options = new OptionSet
			{
				{"prefix=", "Prefix for resources in an assembly", x => _prefix = x},
				{"salt=", "Path to file containing salt", x => _salt = x}
			};
			return options;
		}

#endif

		protected override int ShowUsage(List<string> args)
		{
			if (args != null)
			{
				var first = args.FirstOrDefault();
				if (first != null)
				{
					Command cmd;
					if (_cmds.TryGetValue(first, out cmd))
						return ShowHelp(cmd, args);
				}
			}

			var cmds = _cmds.ToList();
			cmds.Sort();

			Console.WriteLine("Usage:");
			Console.WriteLine("  {0} <command> [options]".Fmt(Utilities.ExecutableName));
			Console.WriteLine();

			Console.WriteLine("Commands:");
			foreach (var cmd in cmds)
			{
				Console.WriteLine("  {0,-27}{1}", cmd.Name, cmd.Description);
			}
			Console.WriteLine();

			Console.WriteLine("General Options:");
			_options.WriteOptionDescriptions(Console.Out);
			Console.WriteLine();

			return 0;
		}

		protected override int ShowVersion(List<string> args)
		{
			Console.WriteLine("{0}: v{1} ({2})",
				Utilities.ExecutableName,
				Assembly.GetExecutingAssembly().GetName().Version,
				ComputeVersionHash()
			);
			return 0;
		}

		protected override int OnRun(List<string> args)
		{
			var key = args.FirstOrDefault();
			if (key == null)
				throw new SyntaxException("Missing command");

			Command cmd;
			if (!_cmds.TryGetValue(key, out cmd))
				throw new SyntaxException("Unknown command: {0}".Fmt(key));

			var extra = args.Skip(1).ToList();
			if (cmd.Options != null)
				extra = cmd.Options.Parse(extra);

			return cmd.Action(cmd, extra);
		}

		int Help(Command cmd, List<string> args)
		{
			var key = args.FirstOrDefault();
			if (key == null)
				return ShowUsage(args);

			Command found;
			if (!_cmds.TryGetValue(key, out found))
				throw new SyntaxException("Unknown command: {0}".Fmt(key));

			return ShowHelp(found, args);
		}

		int ShowHelp(Command cmd, List<string> args)
		{
			if (cmd.Help != null)
				return cmd.Help(cmd, args.Skip(1).ToList());

			Console.WriteLine("Usage:");
			Console.WriteLine("  " + cmd.Usage.Fmt(Utilities.ExecutableName, cmd.Name));
			Console.WriteLine();

			Console.WriteLine("Description:");
			Console.WriteLine("  {0}", cmd.Description);
			Console.WriteLine();

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

		int Compile(Command cmd, List<string> args)
		{
			if (args.Count != 1)
			{
				Console.WriteLine("Missing required argument");
				Console.WriteLine();
				ShowHelp(cmd, args);
				return 1;
			}

			var pitPath = args.First();

			_pitLibraryPath = FindPitLibrary(_pitLibraryPath);
			var compiler = new PitCompiler(_pitLibraryPath, pitPath);
			var errors = compiler.Run(!_no_verify, !_no_lint, !_no_meta, !_no_ninja);

			foreach (var error in errors)
			{
				Console.Error.WriteLine(error);
			}

			if (compiler.TotalNodes > 1000)
			{
				Console.Error.WriteLine("'{0}' has too many nodes: {1}",
					Path.GetFileName(pitPath),
					compiler.TotalNodes
				);
			}

			return errors.Any() ? -1 : 0;
		}

#if DEBUG

		int Protect(Command cmd, List<string> args)
		{
			if (args.Count != 2)
			{
				Console.WriteLine("Missing required arguments");
				Console.WriteLine();
				ShowHelp(cmd, args);
				return 1;
			}

			var input = args[0];
			var output = args[1];
			var dir = Path.GetDirectoryName(output);

			if (!File.Exists(_salt))
				throw new ApplicationException("File specified by --salt does not exist");

			var salt = File.ReadAllLines(_salt).FirstOrDefault();

			var root = new ResourceRoot
			{
				Assembly = Assembly.LoadFile(input),
				Prefix = _prefix,
			};
			var manifest = PitResourceLoader.EncryptResources(root, output, salt);

			var privatePath = Path.Combine(dir, "private.json");
			using (var stream = File.OpenWrite(privatePath))
			{
				PitResourceLoader.SaveManifest(stream, manifest);
			}

			return 0;
		}

#endif

#if DEBUG

		int MakeTestAssembly(Command cmd, List<string> args)
		{
			if (args.Count != 2)
			{
				Console.WriteLine("Missing required arguments");
				Console.WriteLine();
				ShowHelp(cmd, args);
				return 1;
			}

			_pitLibraryPath = FindPitLibrary(_pitLibraryPath);
			var pitTestFile = Path.GetFullPath(args[0]);
			var pitAssemblyFile = Path.GetFullPath(args[1]);

			ThePitTester.MakeTestAssembly(_pitLibraryPath, pitTestFile, pitAssemblyFile);

			return 0;
		}

#endif

		int Ninja(Command cmd, List<string> args)
		{
			if (args.Count != 3)
			{
				Console.WriteLine("Missing required arguments");
				Console.WriteLine();
				ShowHelp(cmd, args);
				return 1;
			}

			_pitLibraryPath = FindPitLibrary(_pitLibraryPath);
			var pitFile = Path.GetFullPath(args[0]);
			var dataModel = args[1];
			var samplesPath = args[2];

			SampleNinjaDatabase.Create(_pitLibraryPath, pitFile, dataModel, samplesPath);

			return 0;
		}

#if DEBUG

		int Test(Command cmd, List<string> args)
		{
			if (args.Count != 1)
			{
				Console.WriteLine("Missing required argument");
				Console.WriteLine();
				ShowHelp(cmd, args);
				return 1;
			}

			var pitTestPath = args[0];

			_pitLibraryPath = FindPitLibrary(_pitLibraryPath);

			PrepareLicensing(_pitLibraryPath, false);

			if (_profile)
			{
				ThePitTester.ProfilePit(_pitLibraryPath, pitTestPath);
				Console.WriteLine("Successful profile of '{0}'", pitTestPath);
				return 0;
			}

			if (!File.Exists(pitTestPath))
				throw new FileNotFoundException("{0} could not be found".Fmt(pitTestPath), pitTestPath);

			if (!_nodata)
				VerifyDataSets(pitTestPath);
			if (!_notest)
				TestPit(pitTestPath);

			if (_errors.Any())
			{
				Console.WriteLine();
				Console.WriteLine("Errors:");
				Console.WriteLine();
				foreach (var line in _errors)
					Console.WriteLine(line);
				return -1;
			}

			Console.WriteLine();

			return 0;
		}

#endif

		int Crack(Command cmd, List<string> args)
		{
			if (args.Count != 3)
			{
				Console.WriteLine("Missing required arguments");
				Console.WriteLine();
				ShowHelp(cmd, args);
				return 1;
			}

			_pitLibraryPath = FindPitLibrary(_pitLibraryPath);

			PrepareLicensing(_pitLibraryPath, false);

			// 0 = pit library path
			// 1 = pit path
			// 2 = data model
			// 3 = sample path
			ThePitTester.Crack(_pitLibraryPath, args[0], args[1], args[2]);
			return 0;
		}

		int MakeXsd(Command cmd, List<string> args)
		{
			using (var stream = File.OpenWrite("peach.xsd"))
			{
				SchemaBuilder.Generate(typeof(Dom), stream);

				Console.WriteLine("Successfully generated {0}", stream.Name);
			}

			return 0;
		}

#if DEBUG

		void VerifyDataSets(string pitTestFile)
		{
			Console.WriteLine("Verifying pit data sets: {0}", pitTestFile);
			Console.WriteLine("----------------------------------------------------");

			try
			{
				ThePitTester.VerifyDataSets(_pitLibraryPath, pitTestFile);
			}
			catch (Exception ex)
			{
				_errors.Add("{0}: {1}".Fmt(pitTestFile, ex.Message));
				if (_verbosity > 0)
					_errors.Add(ex.ToString());
			}

			Console.WriteLine();
		}

		void TestPit(string pitTestFile)
		{
			Console.WriteLine("Testing pit file: {0}", pitTestFile);
			Console.WriteLine("----------------------------------------------------");

			try
			{
				ThePitTester.TestPit(
					_pitLibraryPath,
					pitTestFile,
					_seed,
					_keepGoing,
					_stop ?? 500
				);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);

				_errors.Add("{0}: {1}".Fmt(pitTestFile, ex.Message));

				var ae = ex as AggregateException;
				if (ae != null)
				{
					foreach (var inner in ae.InnerExceptions)
					{
						_errors.Add("{0}: {1}".Fmt(pitTestFile, inner.Message));
					}
				}

				var innerEx = ex.InnerException;
				while (innerEx != null)
				{
					_errors.Add("{0}: {1}".Fmt(pitTestFile, innerEx.Message));
					innerEx = innerEx.InnerException;
				}

				if (_verbosity > 0)
					_errors.Add(ex.ToString());
			}

			Console.WriteLine();
		}

#endif

		string ComputeVersionHash()
		{
			using (var algorithm = HashAlgorithm.Create("MD5"))
			using (var cs = new CryptoStream(Stream.Null, algorithm, CryptoStreamMode.Write))
			{
				ComputeVersionHash(Assembly.GetEntryAssembly(), cs, new HashSet<string>());
				cs.FlushFinalBlock();
				return BitConverter.ToString(algorithm.Hash).Replace("-", string.Empty);
			}
		}

		void ComputeVersionHash(Assembly asm, CryptoStream cs, HashSet<string> seen)
		{
			if (seen.Contains(asm.Location) || asm.GlobalAssemblyCache)
				return;
			seen.Add(asm.Location);

			using (var stream = new FileStream(asm.Location, FileMode.Open, FileAccess.Read, FileShare.Read))
				stream.CopyTo(cs);

			//Console.WriteLine(asm.FullName);

			foreach (var asmRef in asm.GetReferencedAssemblies())
			{
				ComputeVersionHash(Assembly.Load(asmRef), cs, seen);
			}
		}
	}
}