

using System;
using System.Collections.Generic;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers.Encode
{
    [Description("Decode on output from HTML encoding.")]
    [Transformer("HtmlDecode", true)]
    [Transformer("encode.HtmlDecode")]
    [Serializable]
    public class HtmlDecode : Transformer
    {
        public HtmlDecode(DataElement parent, Dictionary<string, Variant> args)
            : base(parent, args)
        {
        }

        protected override BitwiseStream internalEncode(BitwiseStream data)
        {
            var s = new BitReader(data).ReadString();
            var ds = System.Web.HttpUtility.HtmlDecode(s);
            var ret = new BitStream();
            var writer = new BitWriter(ret);
            writer.WriteString(ds);
            ret.Seek(0, System.IO.SeekOrigin.Begin);
            return ret;
        }

        protected override BitStream internalDecode(BitStream data)
        {
            var s = new BitReader(data).ReadString();
            var ds = System.Web.HttpUtility.HtmlAttributeEncode(s);
            var ret = new BitStream();
            var writer = new BitWriter(ret);
            writer.WriteString(ds);
            ret.Seek(0, System.IO.SeekOrigin.Begin);
            return ret;
        }
    }
}

// end
