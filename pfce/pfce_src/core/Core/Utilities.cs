


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using SysProcess = System.Diagnostics.Process;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Conditions;

namespace Peach.Core
{
	/// <summary>
	/// Helper class to add a debug listener so asserts get written to the console.
	/// </summary>
	// NOTE: Tell msvs this is not a 'Component'
	[DesignerCategory("Code")]
	public class AssertWriter : TraceListener
	{
		static readonly NLog.Logger Logger = LogManager.GetLogger("TraceListener");

		public static void Register()
		{
			Register<AssertWriter>();
		}

		public static void Register<T>() where T : AssertWriter, new()
		{
			Debug.Listeners.Insert(0, new T());
		}

		protected virtual void OnAssert(string message)
		{
			Console.Error.WriteLine(message);
		}

		public override void Fail(string message)
		{
			var sb = new StringBuilder();

			sb.AppendLine("Assertion " + message);
			sb.AppendLine(new StackTrace(2, true).ToString());

			OnAssert(sb.ToString());
		}

		public override void Write(string message)
		{
			Logger.Trace(message);
		}

		public override void WriteLine(string message)
		{
			Logger.Trace(message);
		}
	}

	/// <summary>
	/// A simple number generation class.
	/// </summary>
	public static class NumberGenerator
	{
		/// <summary>
		/// Generate a list of numbers around size edge cases.
		/// </summary>
		/// <param name="size">The size (in bits) of the data</param>
		/// <param name="n">The +/- range number</param>
		/// <returns>Returns a list of all sizes to be used</returns>
		public static long[] GenerateBadNumbers(int size, int n = 50)
		{
			if (size == 8)
				return BadNumbers8(n);
			if (size == 16)
				return BadNumbers16(n);
			if (size == 24)
				return BadNumbers24(n);
			if (size == 32)
				return BadNumbers32(n);
			if (size == 64)
				return BadNumbers64(n);
			throw new ArgumentOutOfRangeException("size");
		}

		public static long[] GenerateBadPositiveNumbers(int size = 16, int n = 50)
		{
			if (size == 16)
				return BadPositiveNumbers16(n);
			return null;
		}

		public static ulong[] GenerateBadPositiveUInt64(int n = 50)
		{
			var edgeCases = new ulong[] { 50, 127, 255, 32767, 65535, 2147483647, 4294967295, 9223372036854775807, 18446744073709551615 };
			var temp = new List<ulong>();

			ulong start;
			ulong end;
			for (var i = 0; i < edgeCases.Length - 1; ++i)
			{
				start = edgeCases[i] - (ulong)n;
				end = edgeCases[i] + (ulong)n;

				for (var j = start; j <= end; ++j)
					temp.Add(j);
			}

			start = edgeCases[8] - (ulong)n;
			end = edgeCases[8];
			for (var i = start; i < end; ++i)
				temp.Add(i);
			temp.Add(end);

			return temp.ToArray();
		}

		private static long[] BadNumbers8(int n)
		{
			var edgeCases = new long[] { 0, -128, 127, 255 };
			return Populate(edgeCases, n);
		}

		private static long[] BadNumbers16(int n)
		{
			var edgeCases = new long[] { 0, -128, 127, 255, -32768, 32767, 65535 };
			return Populate(edgeCases, n);
		}

		private static long[] BadNumbers24(int n)
		{
			var edgeCases = new long[] { 0, -128, 127, 255, -32768, 32767, 65535, -8388608, 8388607, 16777215 };
			return Populate(edgeCases, n);
		}

		private static long[] BadNumbers32(int n)
		{
			var edgeCases = new long[] { 0, -128, 127, 255, -32768, 32767, 65535, -2147483648, 2147483647, 4294967295 };
			return Populate(edgeCases, n);
		}

		private static long[] BadNumbers64(int n)
		{
			var edgeCases = new long[] { 0, -128, 127, 255, -32768, 32767, 65535, -2147483648, 2147483647, 4294967295, -9223372036854775808, 9223372036854775807 };    // UInt64.Max = 18446744073709551615;
			return Populate(edgeCases, n);
		}

		private static long[] BadPositiveNumbers16(int n)
		{
			var edgeCases = new long[] { 50, 127, 255, 32767, 65535 };
			return Populate(edgeCases, n);
		}

		private static long[] Populate(long[] values, int n)
		{
			var temp = new List<long>();

			for (var i = 0; i < values.Length; ++i)
			{
				var start = values[i] - n;
				var end = values[i] + n;

				for (var j = start; j <= end; ++j)
					temp.Add(j);
			}

			return temp.ToArray();
		}
	}

	[Serializable]
	public class HexString
	{
		static readonly Regex reHexWhiteSpace = new Regex(@"[h{},\s\r\n:-]+", RegexOptions.Singleline);

		public byte[] Value { get; private set; }

		private HexString(byte[] value)
		{
			Value = value;
		}

		public static HexString Parse(string s)
		{
			s = reHexWhiteSpace.Replace(s, "");
			if (s.Length % 2 == 0)
			{
				var array = ToArray(s);
				if (array != null)
					return new HexString(array);
			}

			throw new FormatException("An invalid hex string was specified.");
		}

		public static byte[] ToArray(string s)
		{
			if (s.Length % 2 != 0)
				throw new ArgumentException("s");

			var ret = new byte[s.Length / 2];

			for (var i = 0; i < s.Length; i += 2)
			{
				var nibble1 = GetNibble(s[i]);
				var nibble2 = GetNibble(s[i + 1]);

				if (nibble1 < 0 || nibble1 > 0xF || nibble2 < 0 | nibble2 > 0xF)
					return null;

				ret[i / 2] = (byte)((nibble1 << 4) | nibble2);
			}

			return ret;
		}

		private static int GetNibble(char c)
		{
			if (c >= 'a')
				return 0xA + (c - 'a');
			if (c >= 'A')
				return 0xA + (c - 'A');
			return c - '0';
		}
	}

	/// <summary>
	/// Some utility methods that can be useful
	/// </summary>
	public class Utilities
	{
		// Ensure trailing slash is stripped from ExecutionDirectory
		private static readonly string PeachDirectory =
			AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);

		const string Layout = "${longdate} ${logger} ${message} ${exception:format=tostring}";

		/// <summary>
		/// Configure NLog.
		/// </summary>
		/// <remarks>
		/// Level = 0 --&gt; Info
		/// Level = 1 --&gt; Debug
		/// Level &gt; 1 --&gt; Trace
		/// </remarks>
		/// <param name="level"></param>
		public static void ConfigureLogging(int level)
		{
			if (LogManager.Configuration != null && LogManager.Configuration.LoggingRules.Count > 0)
			{
				Console.Error.WriteLine("Logging was configured by a .config file, not changing the configuration.");
				return;
			}

			var config = new LoggingConfiguration();

			LogLevel logLevel;

			switch (level)
			{
				case 0:
					logLevel = LogLevel.Off;
					break;
				case 1:
					logLevel = LogLevel.Debug;
					break;
				default:
					logLevel = LogLevel.Trace;
					break;
			}

			if (logLevel.CompareTo(LogLevel.Off) != 0)
			{
				var target = new ColoredConsoleTarget
				{
					Layout = Layout,
					ErrorStream = true,
				};
				target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
				{
					Condition = ConditionParser.ParseExpression("level == LogLevel.Trace"),
					ForegroundColor = ConsoleOutputColor.Gray,
				});

				config.AddTarget("console", target);
				config.LoggingRules.Add(new LoggingRule("*", logLevel, target));
			}

			var peachLog = Environment.GetEnvironmentVariable("PEACH_LOG");
			if (!string.IsNullOrEmpty(peachLog))
			{
				var fileTarget = new FileTarget
				{
					Name = "FileTarget",
					Layout = Layout,
					FileName = peachLog,
					Encoding = System.Text.Encoding.UTF8,
				};
				config.AddTarget("file", fileTarget);
				config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, fileTarget));
			}

			LogManager.Configuration = config;
		}

		public static Configuration GetUserConfig()
		{
			var appConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			return OpenConfig(Path.GetFileNameWithoutExtension(appConfig.FilePath) + ".user.config");
		}

		public static Configuration OpenConfig(string filename)
		{
			var appConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			var userFile = new ExeConfigurationFileMap
			{
				ExeConfigFilename = Path.Combine(Path.GetDirectoryName(appConfig.FilePath), filename)
			};
			return ConfigurationManager.OpenMappedExeConfiguration(userFile, ConfigurationUserLevel.None);
		}

		public static bool DetectConfig(string filename)
		{
			var appConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			var path = Path.Combine(Path.GetDirectoryName(appConfig.FilePath), filename);
			return File.Exists(path);
		}

		public static string FindProgram(string path, string program, string parameter)
		{
			var paths = path;
			if (string.IsNullOrEmpty(path))
			{
				paths = Environment.GetEnvironmentVariable("PATH");
			}
			Debug.Assert(!string.IsNullOrEmpty((paths)));
			var dirs = paths.Split(Path.PathSeparator);
			foreach (var dir in dirs)
			{
				var candidate = Path.Combine(dir, program);
				if (File.Exists(candidate))
					return candidate;
			}

			throw new PeachException("Error, unable to locate '{0}'{1} '{2}' parameter.".Fmt(
				program,
				path != null ? " in specified" : ", please specify using",
				parameter));
		}

		public static int GetCurrentProcessId()
		{
			using (var p = SysProcess.GetCurrentProcess())
				return p.Id;
		}

		/// <summary>
		/// The location on disk where peach is executing from.
		/// Does not include the trailing slash in the directory name.
		/// </summary>
		public static string ExecutionDirectory
		{
			get { return PeachDirectory; }
		}

		/// <summary>
		/// Returns the name of the currently running executable.
		/// Equivalent to argv[0] in C/C++.
		/// </summary>
		public static string ExecutableName
		{
			get { return AppDomain.CurrentDomain.FriendlyName; }
		}

		public static string GetAppResourcePath(string resource)
		{
			return Path.Combine(ExecutionDirectory, resource);
		}

		public static string LoadStringResource(Assembly asm, string fullName)
		{
			using (var stream = asm.GetManifestResourceStream(fullName))
			using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8))
			{
				return reader.ReadToEnd();
			}
		}

		public static MemoryStream LoadBinaryResource(Assembly asm, string fullName)
		{
			using (var stream = asm.GetManifestResourceStream(fullName))
			{
				var ms = new MemoryStream();
				stream.CopyTo(ms);
				ms.Seek(0, SeekOrigin.Begin);
				return ms;
			}
		}

		public static void ExtractEmbeddedResource(Assembly asm, string fullName, string targetFile)
		{
			var path = Path.Combine(ExecutionDirectory, targetFile);
			using (var sout = new FileStream(path, FileMode.Create))
			using (var sin = asm.GetManifestResourceStream(fullName))
			{
				sin.CopyTo(sout);
			}
		}

		public static string FormatAsPrettyHex(byte[] data, int startPos = 0, int length = -1)
		{
			var sb = new StringBuilder();
			var rightSb = new StringBuilder();
			var lineLength = 15;
			var groupLength = 7;
			var gap = "  ";
			byte b;

			if (length == -1)
				length = data.Length;

			var cnt = 0;
			for (var i = startPos; i < data.Length && i < length; i++)
			{
				b = data[i];

				sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));

				if (b >= 32 && b < 127)
					rightSb.Append(Encoding.ASCII.GetString(new byte[] { b }));
				else
					rightSb.Append(".");


				if (cnt == groupLength)
				{
					sb.Append("  ");
				}
				else if (cnt == lineLength)
				{
					sb.Append(gap);
					sb.Append(rightSb);
					sb.Append("\n");
					rightSb.Clear();

					cnt = -1; // (+1 happens later)
				}
				else
				{
					sb.Append(" ");
				}

				cnt++;
			}

			for (; cnt <= lineLength; cnt++)
			{
				sb.Append("  ");

				if (cnt == groupLength)
					sb.Append(" ");
				else if (cnt < lineLength)
				{
					sb.Append(" ");
				}
			}

			sb.Append(gap);
			sb.Append(rightSb);
			sb.Append("\n");
			rightSb.Clear();

			return sb.ToString();
		}

		public static bool TcpPortAvailable(int port)
		{
			var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

			var listeners = ipGlobalProperties.GetActiveTcpListeners();
			if (listeners.Any(endp => endp.Port == port))
				return false;

			var connections = ipGlobalProperties.GetActiveTcpConnections();
			return connections.All(tcpi => tcpi.LocalEndPoint.Port != port);
		}

		/// <summary>
		/// Compute the subrange resulting from diving a range into equal parts
		/// </summary>
		/// <param name="begin">Inclusive range begin</param>
		/// <param name="end">Inclusive range end</param>
		/// <param name="curSlice">The 1 based index of the current slice</param>
		/// <param name="numSlices">The total number of slices</param>
		/// <returns>Range of the current slice</returns>
		public static Tuple<uint, uint> SliceRange(uint begin, uint end, uint curSlice, uint numSlices)
		{
			if (begin > end)
				throw new ArgumentOutOfRangeException("begin");
			if (curSlice == 0 || curSlice > numSlices)
				throw new ArgumentOutOfRangeException("curSlice");

			var total = end - begin + 1;

			if (numSlices == 0 || numSlices > total)
				throw new ArgumentOutOfRangeException("numSlices");

			var slice = total / numSlices;

			end = curSlice * slice + begin - 1;
			begin = end - slice + 1;

			if (curSlice == numSlices)
				end += total % numSlices;

			return new Tuple<uint, uint>(begin, end);
		}

		// Slightly tweaked from:
		// http://www.codeproject.com/Articles/36747/Quick-and-Dirty-HexDump-of-a-Byte-Array
		private delegate void HexOutputFunc(char[] line);
		private delegate int HexInputFunc(byte[] buf, int max);

		private static void HexDump(HexInputFunc input, HexOutputFunc output, int bytesPerLine = 16, long startAddress = 0)
		{
			var bytes = new byte[bytesPerLine];
			var HexChars = "0123456789ABCDEF".ToCharArray();

			var firstHexColumn =
				  8                   // 8 characters for the address
				+ 3;                  // 3 spaces

			var firstCharColumn = firstHexColumn
				+ bytesPerLine * 3       // - 2 digit for the hexadecimal value and 1 space
				+ (bytesPerLine - 1) / 8 // - 1 extra space every 8 characters from the 9th
				+ 2;                  // 2 spaces 

			var lineLength = firstCharColumn
				+ bytesPerLine           // - characters to show the ascii value
				+ Environment.NewLine.Length; // Carriage return and line feed (should normally be 2)

			var line = (new String(' ', lineLength - Environment.NewLine.Length) + Environment.NewLine).ToCharArray();

			for (var i = startAddress; ; i += bytesPerLine)
			{
				var readLen = input(bytes, bytesPerLine);
				if (readLen == 0)
					break;

				line[0] = HexChars[(i >> 28) & 0xF];
				line[1] = HexChars[(i >> 24) & 0xF];
				line[2] = HexChars[(i >> 20) & 0xF];
				line[3] = HexChars[(i >> 16) & 0xF];
				line[4] = HexChars[(i >> 12) & 0xF];
				line[5] = HexChars[(i >> 8) & 0xF];
				line[6] = HexChars[(i >> 4) & 0xF];
				line[7] = HexChars[(i >> 0) & 0xF];

				var hexColumn = firstHexColumn;
				var charColumn = firstCharColumn;

				for (var j = 0; j < bytesPerLine; j++)
				{
					if (j > 0 && (j & 7) == 0) hexColumn++;
					if (j >= readLen)
					{
						line[hexColumn] = ' ';
						line[hexColumn + 1] = ' ';
						line[charColumn] = ' ';
					}
					else
					{
						var b = bytes[j];
						line[hexColumn] = HexChars[(b >> 4) & 0xF];
						line[hexColumn + 1] = HexChars[b & 0xF];
						line[charColumn] = ((b < 32 || b > 126) ? '.' : (char)b);
					}
					hexColumn += 3;
					charColumn++;
				}

				output(line);
			}

		}

		public static byte[] ParseHexDump(string payload)
		{
			var sb = new StringBuilder();
			var rdr = new StringReader(payload);

			string line;
			while ((line = rdr.ReadLine()) != null)
			{
				// Expect 16 byte hex dump
				// some chars chars, whitespace, the bytes, 16 chars

				var space = line.IndexOf(' ') + 1;

				if (line.Length < (space + 16))
					continue;

				var subst = line.Substring(space, line.Length - 16 - space);
				subst = subst.Replace(" ", "");
				sb.Append(subst);
			}

			var ret = HexString.Parse(sb.ToString()).Value;
			return ret;
		}

		public static void HexDump(Stream input, Stream output, int bytesPerLine = 16, long startAddress = 0)
		{
			var pos = input.Position;

			HexInputFunc inputFunc = (buf, max) =>
			{
				return input.Read(buf, 0, max);
			};

			HexOutputFunc outputFunc = line =>
			{
				var buf = System.Text.Encoding.ASCII.GetBytes(line);
				output.Write(buf, 0, buf.Length);
			};

			HexDump(inputFunc, outputFunc, bytesPerLine, startAddress: startAddress);

			input.Seek(pos, SeekOrigin.Begin);
		}

		public static void HexDump(byte[] buffer, int offset, int count, Stream output, int bytesPerLine = 16, long startAddress = 0)
		{
			HexInputFunc inputFunc = (buf, max) =>
			{
				var len = Math.Min(count, max);
				Buffer.BlockCopy(buffer, offset, buf, 0, len);
				offset += len;
				count -= len;
				return len;
			};

			HexOutputFunc outputFunc = line =>
			{
				var buf = System.Text.Encoding.ASCII.GetBytes(line);
				output.Write(buf, 0, buf.Length);
			};

			HexDump(inputFunc, outputFunc, bytesPerLine, startAddress: startAddress);
		}

		public static string HexDump(Stream input, int bytesPerLine = 16, int maxOutputSize = 1024 * 8, long startAddress = 0)
		{
			var sb = new StringBuilder();
			var pos = input.Position;

			HexInputFunc inputFunc = (buf, max) =>
			{
				var len = input.Read(buf, 0, Math.Min(max, maxOutputSize));
				maxOutputSize -= len;
				return len;
			};

			HexDump(inputFunc, line => sb.Append(line), bytesPerLine, startAddress: startAddress);

			if (input.Position != input.Length)
				sb.AppendFormat("---- TRUNCATED (Total Length: {0} bytes) ----", input.Length);

			input.Seek(pos, SeekOrigin.Begin);

			return sb.ToString();
		}

		public static string HexDump(byte[] buffer, int offset, int count, int bytesPerLine = 16, long startAddress = 0)
		{
			var sb = new StringBuilder();

			HexInputFunc inputFunc = (buf, max) =>
			{
				var len = Math.Min(count, max);
				Buffer.BlockCopy(buffer, offset, buf, 0, len);
				offset += len;
				count -= len;
				return len;
			};

			HexDump(inputFunc, line => sb.Append(line), bytesPerLine, startAddress: startAddress);

			return sb.ToString();
		}

		public static string HexDump(string text, int bytesPerLine = 16, long startAddress = 0)
		{
			var buf = Encoding.UTF8.GetBytes(text);
			return HexDump(new MemoryStream(buf), bytesPerLine, startAddress: startAddress);
		}

		public static string PrettyBytes(long bytes)
		{
			if (bytes < 0)
				throw new ArgumentOutOfRangeException("bytes");

			if (bytes > (1024 * 1024 * 1024))
				return (bytes / (1024 * 1024 * 1024.0)).ToString("0.###", CultureInfo.CurrentCulture) + " Gbytes";
			if (bytes > (1024 * 1024))
				return (bytes / (1024 * 1024.0)).ToString("0.###", CultureInfo.CurrentCulture) + " Mbytes";
			if (bytes > 1024)
				return (bytes / 1024.0).ToString("0.###", CultureInfo.CurrentCulture) + " Kbytes";
			return bytes + " Bytes";
		}

		private static readonly Regex ReHexWhiteSpace = new Regex(@"[h{},\s\r\n:-]+", RegexOptions.Singleline);
		public static byte[] HexStringToByteArray(string name, string value)
		{
			// Handle hex data.

			// 1. Remove white space
			value = ReHexWhiteSpace.Replace(value, "");

			// 3. Remove 0x
			value = value.Replace("0x", "");

			// 4. remove \x
			value = value.Replace("\\x", "");

			if (value.Length % 2 != 0)
				throw new PeachException(
					"Error, the hex value of {0} must contain an even number of characters: {1}".Fmt(
						name,
						value)
				);

			var array = HexString.ToArray(value);
			if (array == null)
				throw new PeachException(
					"Error, the value of {0} contains invalid hex characters: {1}".Fmt(
						name,
						value
					));

			return array;
		}

	}

	public class ToggleEventArgs : EventArgs
	{
		public bool Toggle { get; set; }
		public ToggleEventArgs(bool toggle)
		{
			Toggle = toggle;
		}
	}
}
