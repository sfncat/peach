using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Peach.Core;
using Peach.Core.Agent;
using Encoding = Peach.Core.Encoding;

namespace Peach.Pro.Core.Agent.Monitors.Utilities
{
	public class Asan
	{
		// NOTE: Output from GCC can be slightly different than CLANG
		//       These regexes have been updated to work with both.
		private static readonly Regex AsanMatch = new Regex(@"==\d+==\s*ERROR: AddressSanitizer:?");
		private static readonly Regex AsanBucket = new Regex(@"==\d+==\s*ERROR: AddressSanitizer: ([^\s]+) on.*?address ([0-9a-z]+) .*?pc ([0-9a-z]+)");
		private static readonly Regex AsanMessage = new Regex(@"(==\d+==\s*ERROR: AddressSanitizer:.*==\d+==\s*ABORTING)", RegexOptions.Singleline);
		private static readonly Regex AsanTitle = new Regex(@"==\d+==\s*ERROR: AddressSanitizer: ([^\r\n]+)");
		private static readonly Regex AsanOom = new Regex(@"==\d+==\s*ERROR: AddressSanitizer failed to allocate (0x[^\s]+) \((.*)\) bytes of (\w+):\s([^\r\n]+)");

		/// <summary>
		/// Check string for ASAN output
		/// </summary>
		/// <param name="stderr"></param>
		/// <returns></returns>
		public static bool CheckForAsanFault(string stderr)
		{
			return AsanMatch.IsMatch(stderr);
		}

		/// <summary>
		/// Convert ASAN output into Fault
		/// </summary>
		/// <param name="stdout"></param>
		/// <param name="stderr"></param>
		/// <returns></returns>
		public static MonitorData AsanToMonitorData(string stdout, string stderr)
		{
			var data = new MonitorData
			{
				Data = new Dictionary<string, Stream>
				{
					{ "stderr.log", new MemoryStream(Encoding.UTF8.GetBytes(stderr)) }
				}
			};

			if (stdout != null)
				data.Data.Add("stdout.log", new MemoryStream(Encoding.UTF8.GetBytes(stdout)));

			var title = AsanTitle.Match(stderr);

			// failed to allocate ASAN message is different from others
			if (!title.Success)
			{
				var oom = AsanOom.Match(stderr);
				if (!oom.Success)
					throw new ApplicationException("Error: Expected AsanTitle or AsanOom to match");

				data.Title = string.Format("Failed to allocate {0} ({1}) bytes of {2}: {3}",
					oom.Groups[1].Value, oom.Groups[2].Value, oom.Groups[3].Value, oom.Groups[4].Value);

				data.Fault = new MonitorData.Info
				{
					Description = stderr.Substring(oom.Index),
					MajorHash = Monitor2.Hash("ASAN Failed to allocate"),
					MinorHash = Monitor2.Hash(oom.Groups[1].Value),
					Risk = "Out of Memory",
				};

				return data;
			}

			var bucket = AsanBucket.Match(stderr);
			var desc = AsanMessage.Match(stderr);

			data.Title = title.Groups[1].Value;
			data.Fault = new MonitorData.Info
			{
				Description = stderr.Substring(desc.Index, desc.Length),
				MajorHash = Monitor2.Hash(bucket.Groups[3].Value),
				MinorHash = Monitor2.Hash(bucket.Groups[2].Value),
				Risk = bucket.Groups[1].Value,
			};

			return data;
		}
	}
}
