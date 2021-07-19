using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Fixups
{
	[Description("UNIX time (seconds since the midnight starting Jan 1, 1970)")]
	[Fixup("UnixTime", true)]
	[Parameter("Gmt", typeof(bool), "Is time in GMT?", "true")]
	[Parameter("Format", typeof(string), "Format string to encode value with", "")]
	[Serializable]
	public class UnixTimeFixup : Peach.Core.Fixups.VolatileFixup
	{
		public bool Gmt { get; set; }
		public string Format { get; set; }

		public UnixTimeFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
			ParameterParser.Parse(this, args);

			if (!string.IsNullOrEmpty(Format))
			{
				try
				{
					var asStr = DateTime.Now.ToString(Format);
					Debug.Assert(asStr != null);
				}
				catch (Exception ex)
				{
					throw new PeachException("The UnixTime fixup 'Format' parameter '{0}' is not a valid date format string.", ex);
				}
			}
		}

		protected override Variant OnActionRun(RunContext ctx)
		{
			if (!string.IsNullOrEmpty(Format))
			{
				var now = Gmt ? DateTime.UtcNow : DateTime.Now;
				var asStr = now.ToString(Format);
				return new Variant(asStr);
			}

			var span = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, Gmt ? DateTimeKind.Utc : DateTimeKind.Local));
			var unixTime = (int)span.TotalSeconds;

			return new Variant(unixTime);
		}
	}
}
