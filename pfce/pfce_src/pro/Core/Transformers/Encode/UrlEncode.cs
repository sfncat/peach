


// Authors:
//   Mikhail Davidov (sirus@haxsys.net)

// $Id$

using System;
using System.Collections.Generic;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers.Encode
{
    [Description("Encode on output as a URL with spaces turned to pluses.")]
    [Transformer("UrlEncode", true)]
    [Transformer("UrlEncodePlus")]
    [Transformer("encode.UrlEncode")]
    [Transformer("encode.UrlEncodePlus")]
    [Serializable]
    public class UrlEncode : Transformer
    {
        public UrlEncode(DataElement parent, Dictionary<string,Variant>  args)
            : base(parent, args)
        {
        }

        protected override BitwiseStream internalEncode(BitwiseStream data)
        {
			byte[] buf = null;
			long startPosition = data.PositionBits;

			try
			{
				var str = new BitReader(data).ReadString();
				buf = System.Web.HttpUtility.UrlEncodeToBytes(str);
			}
			catch (System.Text.DecoderFallbackException)
			{
				data.PositionBits = startPosition;
				buf = new BitReader(data).ReadBytes((int)data.Length);
				buf = System.Web.HttpUtility.UrlEncodeToBytes(buf);
			}

            var ret = new BitStream();
            ret.Write(buf, 0, buf.Length);
            ret.Seek(0, System.IO.SeekOrigin.Begin);
            return ret;
        }

        protected override BitStream internalDecode(BitStream data)
        {
            var str = new BitReader(data).ReadString();
            var buf = System.Web.HttpUtility.UrlDecodeToBytes(str);
            var ret = new BitStream();
            ret.Write(buf, 0, buf.Length);
            ret.Seek(0, System.IO.SeekOrigin.Begin);
            return ret;
        }
    }
}

// end
