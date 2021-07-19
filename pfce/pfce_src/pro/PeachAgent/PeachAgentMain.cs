
using System;
using System.Collections.Generic;
using System.Reflection;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Core.Runtime;
using Peach.Pro.Core.Runtime;

namespace PeachAgent
{
	/// <summary>
	/// Command line interface for Peach 3.
	/// Mostly backwards compatable with Peach 2.3.
	/// </summary>
	public class PeachAgentMain : BaseProgram
	{
		static int Main(string[] args)
		{
			return new PeachAgentMain().Run(args);
		}

		public static ConsoleColor DefaultForground = Console.ForegroundColor;

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
			get { return "Peach Agent v" + Assembly.GetExecutingAssembly().GetName().Version; }
		}

		#endregion

		protected string _port = "9001";

		protected override int OnRun(List<string> args)
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
			Console.ForegroundColor = DefaultForground;
			Console.WriteLine();

			var agent = "tcp";
			var agentType = ClassLoader.FindPluginByName<AgentServerAttribute>(agent);
			if (agentType == null)
				throw new PeachException("Error, unable to locate agent server for protocol '" + agent + "'.\n");

			var agentServer = (IAgentServer)Activator.CreateInstance(agentType);

			InteractiveConsoleWatcher.WriteInfoMark();
			Console.WriteLine("Starting agent server");

			var argdict = new Dictionary<string, string>();
			argdict["--port"] = "--port=" + _port;

			agentServer.Run(argdict);

			return 0;
		}

		protected override void AddCustomOptions(OptionSet options)
		{
			options.Add(
				"port=",
				"Port to listen for incoming connections on (defaults to 9001).",
				v => _port = v
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
		}

		protected override int ShowUsage(List<string> args)
		{
			const string syntax1 =
@"Starts a Peach Agent server process.

A Peach Agent can be started on a remote machine (remote to Peach) to
accept connections from a Peach instance to run various utility modules
called Monitors and also host remote Publishers. Peach Agents do not need
any specific configuration outside of which port to listen on. All
configuration is provided by a Peach instance.

Some options may be disabled depending on your Peach License options.

Please submit any bugs to support@peachfuzzer.com.

Syntax: PeachAgent.exe [--port=9001]
        ./peachagent [--port=9001]

";

			Console.Write("\n");
			Console.ForegroundColor = ConsoleColor.DarkRed;
			Console.Write("[[ ");
			Console.ForegroundColor = ConsoleColor.DarkCyan;
			Console.WriteLine(ProductName);
			Console.ForegroundColor = ConsoleColor.DarkRed;
			Console.Write("[[ ");
			Console.ForegroundColor = ConsoleColor.DarkCyan;
			Console.WriteLine(Copyright);
			Console.ForegroundColor = DefaultForground;
			Console.WriteLine();

			Console.WriteLine(syntax1);
			_options.WriteOptionDescriptions(Console.Out);

			return 0;
		}

	}
}
