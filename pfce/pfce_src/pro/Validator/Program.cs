using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using Peach.Core;
using Peach.Core.Runtime;
using Peach.Pro.Core.Runtime;

namespace PeachValidator
{
	class Program : BaseProgram
	{
		string _pitLibraryPath;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			using (var program = new Program())
			{
				program.Run(args);
			}
		}

		protected override void AddCustomOptions(OptionSet options)
		{
			_options.Add(
				"pits=",
				"The path to the pit library.",
				v => _pitLibraryPath = v
			);
		}

		protected override string UsageLine
		{
			get
			{
				var name = Assembly.GetEntryAssembly().GetName();
				return "Usage: {0} [OPTION...] [pit] [sample] [save]".Fmt(name.Name);
			}
		}

		protected override int OnRun(List<string> args)
		{
			var pit = (args.Count > 0) ? args[0] : null;
			var sample = (args.Count > 1) ? args[1] : null;
			var save = (args.Count > 2) ? args[2] : null;

			var pitLibraryPath = FindPitLibrary(_pitLibraryPath);

			PrepareLicensing(pitLibraryPath, false);

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm(_license, pitLibraryPath, pit, sample, save));

			return 0;
		}

		protected override int ReportError(List<string> args, bool showUsage, Exception ex)
		{
			MessageBox.Show(ex.Message);

			return base.ReportError(args, showUsage, ex);
		}
	}
}