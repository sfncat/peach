

using System;
using System.Collections.Generic;
using System.Globalization;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;
using NLog;
using Logger = Peach.Core.Logger;

namespace Peach.Pro.Core.Transformers.Type
{
	[Description("Transforms an integer into hex.")]
	[Transformer("IntToHex", true)]
	[Transformer("type.IntToHex")]
	[Parameter("FormatString", typeof(string), ".NET style format string. Defaults to 'X'", "X")]
	[Serializable]
	public class IntToHex : Transformer
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public string FormatString { get; set; }

		public IntToHex(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
			ParameterParser.Parse(this, args);
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			try
			{
				string dataAsStr = new BitReader(data).ReadString();
				int dataAsInt = Int32.Parse(dataAsStr);
				string dataAsHexStr = dataAsInt.ToString(FormatString);

				logger.Trace("Converting '{0}' to '{1}' with FormatString of '{2}'", dataAsInt, dataAsHexStr, FormatString);

				var ret = new BitStream();
				var writer = new BitWriter(ret);
				writer.WriteString(dataAsHexStr);
				ret.Seek(0, System.IO.SeekOrigin.Begin);
				return ret;
			}
			catch (Exception ex)
			{
				throw new SoftException(ex);
			}
		}

		protected override BitStream internalDecode(BitStream data)
		{
			try
			{
				string dataAsHexStr = new BitReader(data).ReadString();
				int dataAsInt = Int32.Parse(dataAsHexStr, NumberStyles.HexNumber);
				string dataAsStr = dataAsInt.ToString();
				var ret = new BitStream();
				var writer = new BitWriter(ret);
				writer.WriteString(dataAsStr);
				ret.Seek(0, System.IO.SeekOrigin.Begin);
				return ret;
			}
			catch (Exception ex)
			{
				throw new SoftException(ex);
			}
		}
	}
}

// end
