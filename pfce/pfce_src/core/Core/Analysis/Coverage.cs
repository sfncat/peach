


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using SysProcess = System.Diagnostics.Process;
using NLog;
using System.Collections;
using System.Threading;

namespace Peach.Core.Analysis
{
	/// <summary>
	/// Abstract base class for performing code coverage via basic blocks
	/// for native binaries.  Each architecture implements this class.
	/// </summary>
	/// <remarks>
	/// So far only Windows has an implementation.
	/// </remarks>
	public class Coverage
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		private static string Quote(string str)
		{
			if (str.Contains(' '))
				return "\"" + str + "\"";

			return str;
		}

		private static void VerifyExists(string file, string type)
		{
			if (!File.Exists(file))
				throw new FileNotFoundException("Error, can not locate the {0} '{1}'.".Fmt(type, file));
		}

		private readonly ProcessStartInfo StartInfo;
		private readonly bool NeedsKilling;

		public Coverage(string executable, string arguments, bool needsKilling)
		{
			VerifyExists(executable, "target executable");

			// Set 1st since it is used by Setup functions
			NeedsKilling = needsKilling;

			if (!arguments.Contains("%s"))
				throw new ArgumentException("Error, arguments must contain a '%s'.");

			var pwd = Utilities.ExecutionDirectory;

			switch (Platform.GetOS())
			{
				case Platform.OS.Windows:
					StartInfo = SetupWindows(pwd, executable, arguments);
					break;
				case Platform.OS.Linux:
					StartInfo = SetupLinux(pwd, executable, arguments);
					break;
				case Platform.OS.OSX:
					StartInfo = SetupOSX(pwd, executable, arguments);
					break;
				default:
					throw new NotSupportedException("Error, coverage is not supported on this platform.");
			}

			StartInfo.RedirectStandardError = true;
			StartInfo.RedirectStandardOutput = true;
			StartInfo.UseShellExecute = false;
			StartInfo.CreateNoWindow = true;

			Logger.Debug("Using: {0} {1}", StartInfo.FileName, StartInfo.Arguments);
		}

		#region Platform Setup Functions

		private ProcessStartInfo SetupWindows(string pwd,string executable,string arguments)
		{
			var arch = FileArch.GetWindows(executable);

			Logger.Debug("Target Architecture: {0}", arch);

			string pinPath;
			string pinTool;

			if (arch == Platform.Architecture.x86)
			{
				pinPath = "ia32";
				pinTool = "bblocks32.dll";
			}
			else
			{
				pinPath = "intel64";
				pinTool = "bblocks64.dll";
			}

			pinPath = Path.Combine(pwd, "pin", pinPath, "bin", "pin.exe");
			VerifyExists(pinPath, "pin binary");

			pinTool = Path.Combine(pwd, pinTool);
			VerifyExists(pinTool, "pin tool");

			var psi = new ProcessStartInfo
			{
				FileName = pinPath,
				Arguments = "-t {0} -cpukill {1} -debug {2} -- {3} {4}".Fmt(
					Quote(pinTool),
					NeedsKilling ? "1" : "0",
					Logger.IsDebugEnabled ? "1" : "0",
					Quote(executable),
					arguments)
			};

			return psi;
		}

		private ProcessStartInfo SetupLinux(string pwd, string executable, string arguments)
		{
			var arch = FileArch.GetLinux(executable);

			Logger.Debug("Target Architecture: {0}", arch);

			string pinPath;
			string pinTool;

			if (arch == Platform.Architecture.x86)
			{
				pinPath = "ia32";
				pinTool = "bblocks32.so";
			}
			else
			{
				pinPath = "intel64";
				pinTool = "bblocks64.so";
			}

			pinPath = Path.Combine(pwd, "pin", pinPath, "bin", "pinbin");
			VerifyExists(pinPath, "pin binary");

			pinTool = Path.Combine(pwd, pinTool);
			VerifyExists(pinTool, "pin tool");

			var psi = new ProcessStartInfo
			{
				FileName = pinPath,
				Arguments = "-t {0} -cpukill {1} -debug {2} -- {3} {4}".Fmt(
					Quote(pinTool),
					NeedsKilling ? "1" : "0",
					Logger.IsDebugEnabled ? "1" : "0",
					Quote(executable),
					arguments)
			};

			foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
				psi.EnvironmentVariables[de.Key.ToString()] = de.Value.ToString();

			var origin = Path.Combine(pwd, "pin");

			var elf_libs = "{0}/ia32/runtime:{0}/intel64/runtime:".Fmt(origin);
			var glibc_libs = "{0}/ia32/runtime/glibc:{0}/intel64/runtime/glibc:".Fmt(origin);
			var cpp_libs = "{0}/ia32/runtime/cpplibs:{0}/intel64/runtime/cpplibs:".Fmt(origin);

			var libs = "";
			if (psi.EnvironmentVariables.ContainsKey("LD_LIBRARY_PATH"))
				libs = psi.EnvironmentVariables["LD_LIBRARY_PATH"];

			psi.EnvironmentVariables["LD_LIBRARY_PATH"] = elf_libs + cpp_libs + libs;
			psi.EnvironmentVariables["PIN_VM_LD_LIBRARY_PATH"] = elf_libs + cpp_libs + glibc_libs + libs;

			return psi;
		}

		private ProcessStartInfo SetupOSX(string pwd, string executable, string arguments)
		{
			var pin32 = Path.Combine(pwd, "pin", "ia32", "bin", "pinbin");
			VerifyExists(pin32, "pin binary");

			var pin64 = Path.Combine(pwd, "pin", "intel64", "bin", "pinbin");
			VerifyExists(pin32, "pin binary");

			var pinTool = Path.Combine(pwd, "bblocks.dylib");
			VerifyExists(pinTool, "pin tool");

			var psi = new ProcessStartInfo
			{
				FileName = pin32,
				Arguments = "-p64 {0} -t {1} -cpukill {2} -debug {3} -- {4} {5}".Fmt(
					Quote(pin64),
					Quote(pinTool),
					NeedsKilling ? "1" : "0",
					Logger.IsDebugEnabled ? "1" : "0",
					Quote(executable),
					arguments)
			};

			foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
				psi.EnvironmentVariables[de.Key.ToString()] = de.Value.ToString();

			return psi;
		}

		#endregion

		/// <summary>
		/// Runs code coverage of sample file and saves results in a trace file.
		/// Throws a PeachException on failure.
		/// </summary>
		/// <param name="sampleFile">Name of sample file to use for instrumentation.</param>
		/// <param name="traceFile">Name of result trace file to generate.</param>
		public void Run(string sampleFile, string traceFile)
		{
			const string outFile = "bblocks.out";
			const string pidFile = "bblocks.pid";

			var psi = new ProcessStartInfo
			{
				Arguments = StartInfo.Arguments.Replace("%s", Quote(sampleFile)),
				FileName = StartInfo.FileName,
				RedirectStandardError = StartInfo.RedirectStandardError,
				RedirectStandardOutput = StartInfo.RedirectStandardOutput,
				UseShellExecute = StartInfo.UseShellExecute,
				CreateNoWindow = StartInfo.CreateNoWindow
			};

			foreach (DictionaryEntry de in StartInfo.EnvironmentVariables)
				psi.EnvironmentVariables[de.Key.ToString()] = de.Value.ToString();

			Logger.Debug("Using sample {0}", sampleFile);
			Logger.Debug("{0} {1}", psi.FileName, psi.Arguments);

			try
			{
				if (File.Exists(outFile))
					File.Delete(outFile);
			}
			catch (Exception ex)
			{
				throw new PeachException("Failed to delete old output file '{0}'.".Fmt(outFile), ex);
			}

			try
			{
				if (File.Exists(pidFile))
					File.Delete(pidFile);
			}
			catch (Exception ex)
			{
				throw new PeachException("Failed to delete old pid file '{0}'.".Fmt(pidFile), ex);
			}

			using (var proc = new SysProcess())
			{
				proc.StartInfo = psi;
				proc.OutputDataReceived += OutputDataReceived;
				proc.ErrorDataReceived += ErrorDataReceived;

				try
				{
					proc.Start();
				}
				catch (Exception ex)
				{
					throw new PeachException("Failed to start pin process.", ex);
				}

				proc.BeginErrorReadLine();
				proc.BeginOutputReadLine();

				while (!File.Exists(pidFile) && !proc.HasExited)
					Thread.Sleep(250);

				if (proc.HasExited)
					throw new PeachException("Pin exited without starting the target process.");

				Logger.Debug("Waiting for pin process to exit.");

				proc.WaitForExit();

				Logger.Debug("Pin process exited.");
			}

			if (!File.Exists(outFile))
				throw new PeachException("Pin exited without creating output file.");

			// Ensure outFile is not zero sized
			var fi = new System.IO.FileInfo(outFile);
			if (fi.Length == 0)
				throw new PeachException("Pin exited without creating any trace file entries. This usually means the target did not run to completion.");

			try
			{
				if (File.Exists(traceFile))
					File.Delete(traceFile);
			}
			catch (Exception ex)
			{
				throw new PeachException("Failed to delete old trace file '{0}'.".Fmt(traceFile), ex);
			}

			try
			{
				// Move bblocks.out to target
				File.Move(outFile, traceFile);
			}
			catch (Exception ex)
			{
				throw new PeachException("Failed to move pin outpout file into destination trace file.", ex);
			}
		}

		private static void ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data == null)
			{
				((SysProcess)sender).CancelErrorRead();
			}
			else
			{
				Logger.Debug(e.Data);
			}
		}

		private static void OutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data == null)
			{
				((SysProcess)sender).CancelOutputRead();
			}
			else
			{
				Logger.Debug(e.Data);
			}
		}
	}
}
