


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Globalization;
using System.Text;
using System.IO;

namespace Peach.Core
{
	/// <summary>
	/// A logging mechanism for fuzzing runs.
	/// </summary>
	public abstract class Logger : Watcher
	{
		/// <summary>
		/// Make the actual log path to use based on the run name,
		/// run time and path parameter.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		protected static string GetLogPath(RunContext context, string path)
		{	
			var sb = new StringBuilder();

			sb.Append(Path.Combine(path, Path.GetFileName(context.config.pitFile)));

			if (context.config.runName != "Default")
			{
				sb.Append("_");
				sb.Append(context.config.runName);
			}

			sb.Append("_");
			sb.Append(context.config.runDateTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));

			return sb.ToString();
		}
	}

	/// <summary>
	/// Used to indicate a class is a valid Publisher and 
	/// provide it's invoking name used in the Pit XML file.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class LoggerAttribute : PluginAttribute
	{
		public LoggerAttribute(string name, bool isDefault = false)
			: base(typeof(Logger), name, isDefault)
		{
		}
	}

}
