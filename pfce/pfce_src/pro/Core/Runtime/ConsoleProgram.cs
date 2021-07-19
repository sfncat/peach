

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Core.Runtime;
using Peach.Core.Xsd;
using Peach.Pro.Core.Loggers;
using Peach.Pro.Core.Publishers;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.Core.License;
using Monitor = System.Threading.Monitor;

namespace Peach.Pro.Core.Runtime
{
	/// <summary>
	/// Command line interface for Peach 3.  Mostly backwards compatable with
	/// Peach 2.3.
	/// </summary>
	public class ConsoleProgram : BaseProgram
	{
		private static readonly string PitLibraryPath = "PitLibraryPath";
		public static ConsoleColor DefaultForground = Console.ForegroundColor;

		/// <summary>
		/// The list of .config files to be parsed for defines
		/// </summary>
		protected List<string> _configFiles = new List<string>();

		/// <summary>
		/// List of key,value pairs of extra defines to use
		/// </summary>
		protected Dictionary<string, string> _definedValues = new Dictionary<string, string>();

		/// <summary>
		/// Configuration options for the engine
		/// </summary>
		protected RunConfiguration _config = new RunConfiguration();

		/// <summary>
		/// The exit code of the process
		/// </summary>
		public int ExitCode = 1;

		private string _analyzer;
		private string _agent;
		private bool _test;
		private Uri _webUri;
		private string _pitLibraryPath;
		private string _defPitLibraryPath;
		private bool _noweb;
		private bool _nobrowser;
		private static volatile bool _shouldStop;
		private bool _polite;
		private bool _forceActivation;
		private bool _forceDeactivation;

		#region Public Properties

		/// <summary>
		/// Copyright message
		/// </summary>
		public virtual string Copyright
		{
			get { return Assembly.GetExecutingAssembly().GetCopyright(); }
		}

		/// <summary>
		/// Product name
		/// </summary>
		public virtual string ProductName
		{
			get { return "Peach Pro v" + Assembly.GetExecutingAssembly().GetName().Version; }
		}

		#endregion

		public ConsoleProgram()
		{
		}

		public ConsoleProgram(string[] args)
		{
			ExitCode = Run(args);
		}

		protected override void AddCustomOptions(OptionSet options)
		{
			options.Add(
				"activate",
				"Force license activation. " +
				"Licensing usually automatically reactivates as necessary, " +
				"but if your license has recently changed, " +
				"you can force an immediate activation.",
				v => _forceActivation = true
			);
			options.Add(
				"deactivate",
				"Return activated license. " +
				"Licensing usually automatically reactivates as necessary, " +
				"but if your license has recently changed, " +
				"you can force an immediate activation.",
				v => _forceDeactivation = true
			);
			options.Add(
				"1",
				"Perform a single test case",
				v => _config.singleIteration = true
			);
			options.Add(
				"polite",
				"Disable interactive console mode, which is based on curses",
				v => _polite = true
			);
			options.Add(
				"debug",
				"Enable debug messages. " +
				"Useful when debugging your Peach Pit file. " +
				"Warning: Messages are very cryptic sometimes.",
				v => _verbosity = 1
			);
			options.Add(
				"trace",
				"Enable even more verbose debug messages.",
				v => _verbosity = 2
			);
			options.Add(
				"range=",
				"Provide a range of test #'s to be run.",
				v => ParseRange("range", v)
			);
			options.Add(
				"duration=",
				"How long to run the fuzzer for.",
				(TimeSpan v) => _config.Duration = v
			);

			options.Add(
				"skipto=",
				"Skip to a specific test #. This replaced -r for restarting a Peach run.",
				(uint v) => _config.skipToIteration = v
			);
			options.Add(
				"seed=",
				"Sets the seed used by the random number generator.",
				(uint v) => _config.randomSeed = v
			);
			// Defined values & .config files
			options.Add(
				"D|define=",
				"Define a substitution value. " +
				"In your PIT you can specify ##KEY## and it will be replaced with VALUE.",
				AddNewDefine
			);

			// Global actions, get run and immediately exit
			options.Add(
				"showdevices",
				"Display the list of PCAP devices",
				var => _cmd = ShowDevices
			);
			options.Add(
				"showenv",
				"Print a list of all DataElements, Fixups, Agents, " +
				"Publishers and their associated parameters.",
				var => _cmd = ShowEnvironment
			);

			// web ui
			options.Add(
				"pits=",
				"The path to the pit library.",
				v => _pitLibraryPath = v
			);
			options.Add(
				"noweb",
				"Disable the Peach web interface.",
				v => _noweb = true
			);
			options.Add(
				"nobrowser",
				"Disable launching browser on start.",
				v => _nobrowser = true
			);
			options.Add(
				"webport=",
				"Specifies port web interface runs on.",
				(int v) => _webPort = v
			);

			// DEPRECATED - Not in syntax help
			options.Add(
				"analyzer=",
				"Launch Peach Analyzer",
				v => _analyzer = v
			);
			options.Add(
				"c|count",
				"Count test cases",
				v => _config.countOnly = true
			);
			options.Add(
				"a|agent=",
				"Launch Peach Agent",
				v => _agent = v
			);
			options.Add(
				"definedvalues=",
				v => _configFiles.Add(v)
			);
			options.Add(
				"config=",
				"XML file containing defined values",
				v => _configFiles.Add(v)
			);
			options.Add(
				"t|test",
				"Validate a Peach Pit file",
				v => _test = true
			);
			options.Add(
				"makexsd",
				"Generate peach.xsd",
				var => _cmd = MakeSchema
		   );
		}

		protected override bool VerifyCompatibility()
		{
			if (!base.VerifyCompatibility())
				return false;

			var type = Type.GetType("Mono.Runtime");

			// If we are not on mono, no checks need to be performed.
			if (type == null)
				return true;

			try
			{
				Console.ForegroundColor = DefaultForground;
				Console.ResetColor();
				return true;
			}
			catch
			{
				var term = Environment.GetEnvironmentVariable("TERM");

				if (term == null)
					Console.WriteLine("An incompatible terminal type was detected.");
				else
					Console.WriteLine("An incompatible terminal type '{0}' was detected.", term);

				Console.WriteLine("Change your terminal type to 'linux', 'xterm', or 'rxvt' and try again.");
				return false;
			}
		}

		protected override int OnRun(List<string> args)
		{
			if (!string.IsNullOrEmpty(_pitLibraryPath) && !string.IsNullOrEmpty(_defPitLibraryPath))
			{
				if (_pitLibraryPath != _defPitLibraryPath)
					throw new PeachException("--pits and -DPitLibraryPath should both specify the same path.");
			}

			if (string.IsNullOrEmpty(_pitLibraryPath))
				_pitLibraryPath = _defPitLibraryPath;

			_pitLibraryPath = FindPitLibrary(_pitLibraryPath);
			_definedValues[PitLibraryPath] = _pitLibraryPath;

			PrepareLicensing(_pitLibraryPath, _forceActivation, _forceDeactivation);

			_config.commandLine = args.ToArray();

			try
			{
				Console.Write("\n");
				Console.ForegroundColor = ConsoleColor.DarkRed;
				Console.Write("[[ ");
				Console.ForegroundColor = ConsoleColor.DarkCyan;
				Console.WriteLine(ProductName);
				Console.ForegroundColor = ConsoleColor.DarkRed;
				Console.Write("[[ ");
				Console.ForegroundColor = ConsoleColor.DarkCyan;
				Console.WriteLine(Copyright);
				if (_license.IsNearingExpiration())
				{
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine();
					Console.WriteLine(_license.ExpirationWarning());
				}
				Console.ForegroundColor = DefaultForground;
				Console.WriteLine();

				if (_agent != null)
					return OnRunAgent(_agent, args);

				if (_analyzer != null)
					return OnRunAnalyzer(_analyzer, args);

				return OnRunJob(_test, args);
			}
			finally
			{
				// Reset console colors
				Console.ForegroundColor = DefaultForground;
			}
		}

		protected override int ShowUsage(List<string> args)
		{
			Syntax();
			return 0;
		}

		protected virtual Watcher GetUIWatcher()
		{
			try
			{
				if (_verbosity > 0 || _polite)
					return new ConsoleWatcher();

				// Ensure console is interactive
				Console.Clear();

				var title = _webUri == null ? "" : " ({0})".Fmt(_webUri);

				return new InteractiveConsoleWatcher(_license, title);
			}
			catch (IOException)
			{
				return new ConsoleWatcher();
			}
		}

		/// <summary>
		/// Create an engine and run the fuzzing job
		/// </summary>
		protected virtual void RunEngine(Peach.Core.Dom.Dom dom, PitConfig pitConfig)
		{
			// Ensure the database has been migrated prior to
			// creating the Job, as it will insert itself.
			using (var db = new NodeDatabase())
			{
				db.Migrate();
			}

			// Add the JobLogger as necessary
			Test test;

			if (!dom.tests.TryGetValue(_config.runName, out test))
				throw new PeachException("Unable to locate test named '{0}'.".Fmt(_config.runName));

			if (pitConfig != null && pitConfig.Weights != null)
			{
				foreach (var item in pitConfig.Weights)
				{
					test.weights.Add(new SelectWeight
					{
						Name = item.Id,
						Weight = (ElementWeight)item.Weight
					});
				}
			}

			// Add the JobLogger as necessary
			var jobLogger = test.loggers.OfType<JobLogger>().SingleOrDefault();
			if (jobLogger == null)
			{
				jobLogger = new JobLogger();
				test.loggers.Insert(0, jobLogger);
			}

			var configName = pitConfig != null ? pitConfig.Name : _config.pitFile;
			var jobLicense = _license.NewJob(_config.pitFile, configName, _config.id.ToString());
			jobLogger.Initialize(_config, _license, jobLicense);

			var job = new Job(_config);

			if (_noweb || CreateWeb == null)
			{
				var e = new Engine(GetUIWatcher());
				e.startFuzzing(dom, _config);
				return;
			}

			var jobMonitor = new ConsoleJobMonitor(job);

			using (var svc = CreateWeb(_license, "", jobMonitor))
			{
				svc.Start(_webPort);

				_webUri = svc.Uri;

				InteractiveConsoleWatcher.WriteInfoMark();
				Console.WriteLine("Web site running at: {0}", svc.Uri);

				var e = new Engine(GetUIWatcher());
				e.startFuzzing(dom, _config);
			}
		}

		/// <summary>
		/// Run a command line analyzer of the specified name
		/// </summary>
		/// <param name="analyzer"></param>
		/// <param name="extra"></param>
		protected virtual int OnRunAnalyzer(string analyzer, List<string> extra)
		{
			var analyzerType = ClassLoader.FindPluginByName<AnalyzerAttribute>(analyzer);
			if (analyzerType == null)
				throw new PeachException("Error, unable to locate analyzer called '" + analyzer + "'.\n");

			var field = analyzerType.GetField("supportCommandLine", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
			System.Diagnostics.Debug.Assert(field != null);
			if ((bool)field.GetValue(null) == false)
				throw new PeachException("Error, analyzer not configured to run from command line.");

			var analyzerInstance = (Analyzer)Activator.CreateInstance(analyzerType);

			InteractiveConsoleWatcher.WriteInfoMark();
			Console.WriteLine("Starting Analyzer");
			Console.WriteLine();

			analyzerInstance.asCommandLine(extra);

			return 0;
		}

		/// <summary>
		/// Run a peach agent of the specified protocol
		/// </summary>
		/// <param name="agent"></param>
		/// <param name="extra"></param>
		protected virtual int OnRunAgent(string agent, List<string> extra)
		{
			var agentType = ClassLoader.FindPluginByName<AgentServerAttribute>(agent);
			if (agentType == null)
				throw new PeachException("Error, unable to locate agent server for protocol '" + agent + "'.\n");

			var agentServer = (IAgentServer)Activator.CreateInstance(agentType);

			InteractiveConsoleWatcher.WriteInfoMark();
			Console.WriteLine("Starting agent server");

			var args = new Dictionary<string, string>();
			for (var i = 0; i < extra.Count; i++)
				args[i.ToString()] = extra[i];

			agentServer.Run(args);

			return 0;
		}

		/// <summary>
		/// Run a fuzzing job
		/// </summary>
		/// <param name="test">Test only. Parses the pit and exits.</param>
		/// <param name="extra">Extra command line options</param>
		protected virtual int OnRunJob(bool test, List<string> extra)
		{
			if (extra.Count > 0)
			{
				// Pit was specified on the command line, do normal behavior
				// Ensure the EULA has been accepted before running a job
				// on the command line.  The WebUI will present a EULA
				// in the later case.

				if (!_license.EulaAccepted)
					ShowEula();

				// Let Web-UI show errors when no command line args are specified
				if (_license.Status != LicenseStatus.Valid)
				{
					Console.WriteLine(_license.ErrorText);
					return -1;
				}

				_config.shouldStop = () => _shouldStop;
				Console.CancelKeyPress += Console_CancelKeyPress;

				var pitPath = _config.pitFile = extra[0];

				PitConfig pitConfig = null;
				if (Path.GetExtension(pitPath) == ".peach")
				{
					pitConfig = PitDatabase.LoadPitConfig(pitPath);
					pitPath = Path.Combine(_pitLibraryPath, pitConfig.OriginalPit);
				}

				if (extra.Count > 1)
					_config.runName = extra[1];

				var defs = ParseDefines(pitPath + ".config", pitConfig);

				var parserArgs = new Dictionary<string, object>();
				parserArgs[PitParser.DEFINED_VALUES] = defs;

				var parser = new ProPitParser(_license, _pitLibraryPath, pitPath);

				if (test)
				{
					InteractiveConsoleWatcher.WriteInfoMark();
					Console.Write("Validating file [" + pitPath + "]... ");
					parser.asParserValidation(parserArgs, pitPath);
					Console.WriteLine("No Errors Found.");
				}
				else
				{
					var dom = parser.asParser(parserArgs, pitPath);

					if (pitConfig != null)
						PitInjector.InjectAgents(pitConfig, defs, dom);

					RunEngine(dom, pitConfig);
				}
			}
			else if (!_noweb && CreateWeb != null)
			{
				RunWeb(_pitLibraryPath, !_nobrowser, new InternalJobMonitor(_license));
			}

			return 0;
		}

		/// <summary>
		/// Combines define files and define arguments into a single list
		/// Command line arguments override any .config file's defines
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerable<KeyValuePair<string, string>> ParseDefines(string xmlConfig, PitConfig pitConfig)
		{
			// Parse pit.xml.config to poopulate system defines and add
			// -D command line overrides.
			// This will succeed even if pitConfig doesn't exist.
			
			var defs = PitDefines.ParseFile(xmlConfig, _definedValues, _config.id);

			foreach (var item in _configFiles)
			{
				var normalized = Path.GetFullPath(item);

				if (!File.Exists(normalized))
					throw new PeachException("Error, defined values file \"" + item + "\" does not exist.");

				var cfg = XmlTools.Deserialize<PitDefines>(normalized);

				// Add defines from extra config files in order
				defs.Children.AddRange(cfg.Children);
			}

			var ret = defs.Evaluate();

			if (pitConfig != null)
				PitInjector.InjectDefines(pitConfig, defs, ret);

			return ret;
		}

		/// <summary>
		/// Override to change syntax message.
		/// </summary>
		protected virtual void Syntax()
		{
			const string syntax1 =
@"This is the core Peach application which provides the fuzzing engine
and also some utility functions for the custom pit developer. This
application can be used to start the Peach Web Application and also to
launch fuzzing jobs from the command line.

Some options may be disabled depending on your Peach License options.

Please submit any bugs to support@peachfuzzer.com.

Peach Web Application

  Syntax: peach [options]

   --nobrowser        Disable launching browser on start
   --webport=PORT     Specified port the web application runs on
   --plugins=PATH     Change the plugins folder location. Defaults to
                      the 'Plugins' folder relative to the Peach installation.

  Starts Peach and provides a web application for configuring, running,
  and viewing results of a fuzzing job.

Fuzzing from Command Line

  Syntax: peach [options] <PEACH_PIT.xml | PEACH_CONFIG.peach> [test_name]

   PEACH_CONFIG.peach Peach Pit Configuration generated by Peach Application
   PEACH_PIT.xml      The Peach Pit XML file

   test_name          Name of test to run (defaults to 'Default')
  
   -1                 Perform a single test case
   --debug            Enable debug messages. Useful when debugging 
                      Peach Pit Files.
   -DKEY=VALUE        Define a configuration variable via the command line. 
                      Multiple defines can be provided as needed.
                      Example: -DTargetIPv4=127.0.0.1 -DTargetPort=80
   --duration=DUR     Duration of fuzzing run. Peach will run for DUR length
                      of time. Useful for integrating Peach into an automated 
                      test cycle or continuous integration environment. 
                      Argument format is DD.HH:MM:SS.
                      Example: --duration=12     Duration of 12 days
                               --duration=0:20   Duration of 20 min
                               --duration=5:00   Duration of 5 hours
                               --duration=1.5:00 Duration of 1 day, 5 hrs
   --noweb            Disable the Peach Web Application
   --plugins=PATH     Change the plugins folder location. Defaults to
                      the 'Plugins' folder relative to the Peach installation.
   --polite           Disable interactive console mode
   --range=S,F        Perform a range of testcases start at test case S and 
                      ending with test case F. Typically combined with the 
                      --seed argument.
   --seed=SEED        Set the fuzzing jobs seed. The same seed will always
                      produce the same test case sequence. Should only be
                      set when reproducing a historical fuzzing job. Default
                      is a random seed.
   --skipto=NUM       Skip to NUM test case and start fuzzing. Normally 
                      combined with --seed to reproduce a specific sequence 
                      of test cases.
   --trace            Enable even more verbose debug messages.
   --webport=PORT     Specified port the web application runs on

  A fuzzing run is started by specifying the Peach Pit Configuration or
  Peach XML file and the name of a test to perform.
  
  If a run is interrupted for some reason it can be restarted using the
  --skipto and --seed parameters to provide the test case to start 
  fuzzing at and the seed of the first job.

Debug Peach XML File

  Syntax: peach -1 --debug <PEACH_PIT.xml | PEACH_CONFIG.peach> [test_name]
  
  This will perform a single iteration (-1) of your pit file while displaying
  a lot of debugging information (--debug).  The debugging information is
  intended for custom pit developers.

Display List of Network Capture Devices

  Syntax: peach --showdevices

  Display a list of all known devices Peach can perform network capture
  on.

Display Known Elements

  Syntax: peach --showenv

  Print a list of all known: 

   - Actions
   - Agent Channels
   - Analyzers
   - DataElements
   - Fixups
   - Loggers
   - Monitors
   - Mutation Strategies
   - Mutators
   - Publishers
   - Relations
   - Transformers

  Including any parameters with description and default values. This can
  be used to verify any custom extensions are found.

Peach Agent

  The Peach Agent functionality has been moved to a separate executable
  'PeachAgent.exe' or 'peachagent' on Linux/OS X.

Running Analyzers from Command Line

  This functionality has been moved to the PitTool.

Generate XML Schema File

  This functionality has been moved to the PitTool.
";

			Console.WriteLine(syntax1);
			// We now have some deprecated options that should not be shown.
			//_options.WriteOptionDescriptions(Console.Out);
			//Console.WriteLine(syntax2);
		}

		private void ShowEula()
		{
			Console.WriteLine(_license.EulaText);

			Console.WriteLine(
@"BY TYPING ""YES"" YOU ACKNOWLEDGE THAT YOU HAVE READ, UNDERSTAND, AND
AGREE TO BE BOUND BY THE TERMS ABOVE.
");

			while (true)
			{
				Console.WriteLine("Do you accept the end user license agreement?");

				Console.Write("(yes/no) ");
				var answer = Console.ReadLine();
				Console.WriteLine();

				if (answer == "no")
					Environment.Exit(-1);

				if (answer == "yes")
				{
					_license.EulaAccepted = true;
					return;
				}

				Console.WriteLine("The answer \"{0}\" is invalid. It must be one of \"yes\" or \"no\".", answer);
				Console.WriteLine();
			}
		}

		#region Command line option parsing helpers

		protected void ParseRange(string arg, string v)
		{
			var parts = v.Split(',');
			if (parts.Length != 2)
				throw new PeachException("Invalid range: " + v);

			try
			{
				_config.rangeStart = Convert.ToUInt32(parts[0]);
			}
			catch (Exception ex)
			{
				throw new PeachException("Invalid range start iteration: " + parts[0], ex);
			}

			try
			{
				_config.rangeStop = Convert.ToUInt32(parts[1]);
			}
			catch (Exception ex)
			{
				throw new PeachException("Invalid range stop iteration: " + parts[1], ex);
			}

			_config.range = true;
		}

		protected void AddNewDefine(string arg)
		{
			var parts = arg.Split('=');
			if (parts.Length != 2)
				throw new PeachException("Error, defined values supplied via -D/--define must have an equals sign providing a key-pair set.");

			var key = parts[0];
			var value = parts[1];

			// Allow command line options to override others
			_definedValues[key] = value;

			if (key == PitLibraryPath)
				_defPitLibraryPath = value;
		}

		static int MakeSchema(List<string> args)
		{
			try
			{
				Console.WriteLine();

				using (var stream = new FileStream("peach.xsd", FileMode.Create, FileAccess.Write))
				{
					SchemaBuilder.Generate(typeof(Peach.Core.Xsd.Dom), stream);

					Console.WriteLine("Successfully generated {0}", stream.Name);
				}

				return 0;
			}
			catch (UnauthorizedAccessException ex)
			{
				throw new PeachException("Error creating schema. {0}".Fmt(ex.Message), ex);
			}
		}

		#endregion

		#region Global Actions

		static int ShowDevices(List<string> args)
		{
			var devices = RawEtherPublisher.Devices();

			if (devices.Count == 0)
			{
				Console.WriteLine();
				Console.WriteLine("No capture devices were found.  Ensure you have the proper");
				Console.WriteLine("permissions for performing PCAP captures and try again.");
				Console.WriteLine();
			}
			else
			{
				Console.WriteLine();
				Console.WriteLine("The following devices are available on this machine:");
				Console.WriteLine("----------------------------------------------------");
				Console.WriteLine();

				// Print out all available devices
				foreach (var dev in devices)
				{
					Console.WriteLine("Name: {0}\nDescription: {1}\n\n", dev.Interface.FriendlyName, dev.Description);
				}
			}

			return 0;
		}

		static int ShowEnvironment(List<string> args)
		{
			Usage.Print();
			return 0;
		}

		#endregion

		protected static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
		{
			e.Cancel = true;

			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine(" --- Ctrl+C Detected --- ");

			if (!_shouldStop)
			{
				Console.WriteLine(" --- Waiting for last iteration to complete --- ");
				_shouldStop = true;
			}
			else
			{
				Console.WriteLine(" --- Aborting --- ");

				// Need to call Environment.Exit from outside this event handler
				// to ensure the finalizers get called...
				// http://www.codeproject.com/Articles/16164/Managed-Application-Shutdown
				new Thread(() => Environment.Exit(0)).Start();
			}
		}
	}

	class ConsoleJobMonitor : IJobMonitor
	{
		readonly Guid _guid;
		readonly int _pid = Utilities.GetCurrentProcessId();

		public ConsoleJobMonitor(Job job)
		{
			_guid = job.Guid;
		}

		public void Dispose()
		{
		}

		public int Pid { get { return _pid; } }

		public bool IsTracking(Job job)
		{
			lock (this)
			{
				return _guid == job.Guid;
			}
		}

		public bool IsControllable { get { return false; } }

		public Job GetJob()
		{
			using (var db = new NodeDatabase())
			{
				return db.GetJob(_guid);
			}
		}

		#region Not Implemented

		public Job Start(string pitLibraryPath, string pitFile, JobRequest jobRequest)
		{
			throw new NotImplementedException();
		}

		public bool Pause()
		{
			throw new NotImplementedException();
		}

		public bool Continue()
		{
			throw new NotImplementedException();
		}

		public bool Stop()
		{
			throw new NotImplementedException();
		}

		public bool Kill()
		{
			throw new NotImplementedException();
		}

		public EventHandler InternalEvent
		{
			set { throw new NotImplementedException(); }
		}

		#endregion
	}
}
