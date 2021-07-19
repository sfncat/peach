

using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers.Encode
{
    [Description("Encode on output as HTML (encoding < > & and \").")]
    [Transformer("HtmlEncode", true)]
    [Transformer("encode.HtmlEncode")]
    [Serializable]
    public class HtmlEncode : Transformer
    {
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected NLog.Logger Logger { get { return logger; } }

		public HtmlEncode(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
        {
        }

		protected string HtmlEncodeBytes(byte[] buf)
		{
			var ret = new StringBuilder(buf.Length);

			foreach (byte b in buf)
			{
				ret.Append("&#");
				ret.Append((int)b);
				ret.Append(";");
			}

			return ret.ToString();
		}

        protected override BitwiseStream internalEncode(BitwiseStream data)
        {
			try
			{
				var s = new BitReader(data).ReadString();
				var ds = System.Web.HttpUtility.HtmlAttributeEncode(s);
				var ret = new BitStream();
				var writer = new BitWriter(ret);
				writer.WriteString(ds);
				ret.Seek(0, System.IO.SeekOrigin.Begin);
				return ret;
			}
			catch (System.Text.DecoderFallbackException ex)
			{
				logger.Warn("Caught DecoderFallbackException, throwing soft exception.");
				throw new SoftException(ex);
			}
        }

        protected override BitStream internalDecode(BitStream data)
        {
            var s = new BitReader(data).ReadString();
            var ds = System.Web.HttpUtility.HtmlDecode(s);
            var ret = new BitStream();
            var writer = new BitWriter(ret);
            writer.WriteString(ds);
            ret.Seek(0, System.IO.SeekOrigin.Begin);
            return ret;
        }
    }
}

// end
