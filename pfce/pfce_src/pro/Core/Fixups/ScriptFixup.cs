using System;
using System.Collections.Generic;
using System.ComponentModel;
using NLog;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Fixups
{
	/// <summary>
	/// Proxy class to allow writing fixups in a scripting language like python
	/// or ruby.
	/// </summary>
	/// <remarks>
	/// The constructor will be passed a reference to our instance as the only
	/// argument.  A method "fixup" will be called, passing in the element and expecting
	/// a byte[] array as output.
	/// </remarks>
	[Description("Scripting fixup.")]
	[Fixup("Script", true)]
	[Fixup("ScriptFixup")]
	[Parameter("class", typeof(string), "Scripting fixup class to instantiate")]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Serializable]
	public class ScriptFixup : Fixup
	{
		[NonSerialized]
		private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

		[NonSerialized]
		dynamic _pythonFixup;

		public ScriptFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args, "ref")
		{
			try
			{
				var state = new Dictionary<string, object> { { "fixupSelf", this } };

				_pythonFixup = parent.EvalExpression(
					string.Format("{0}(fixupSelf)", (string)args["class"]),
					state);

				if (_pythonFixup == null)
					throw new PeachException("Error, unable to create an instance of the \"" + (string)args["class"] + "\" script class.");

				logger.Trace("ScriptFixup(): _pythonFixup != null");
			}
			catch (Exception ex)
			{
				logger.Debug("class: " + (string)args["class"]);
				logger.Error(ex.Message);
				throw;
			}
		}

		protected override Variant fixupImpl()
		{
			if (_pythonFixup == null)
			{
				var state = new Dictionary<string, object> { { "fixupSelf", this } };

				_pythonFixup = parent.EvalExpression(
					string.Format("{0}(fixupSelf)", (string)args["class"]),
					state);
			}

			var from = elements["ref"];

			logger.Trace("fixupImpl(): ref: " + from.GetHashCode());

			object data = _pythonFixup.fixup(from);

			if (data == null)
				throw new PeachException("Error, script fixup returned null.");

			var asVariant = Scripting.ToVariant(data);

			if (asVariant == null)
				throw new PeachException("Error, script fixup returned unknown type '{0}'.".Fmt(data.GetType()));

			return asVariant;
		}
	}
}

// end
