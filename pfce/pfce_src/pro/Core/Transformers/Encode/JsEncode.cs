
using System;
using System.Collections.Generic;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers.Encode
{
    [Description("Encode on output as Javascript string.")]
    [Transformer("JsEncode", true)]
    [Transformer("encode.JsEncode")]
    [Serializable]
    public class JsEncode : Transformer
    {
        public JsEncode(DataElement parent, Dictionary<string, Variant> args)
            : base(parent, args)
        {
        }

        protected override BitwiseStream internalEncode(BitwiseStream data)
        {
            BitStream ret = new BitStream();
            BitWriter writer = new BitWriter(ret);
            int b;

            while ((b = data.ReadByte()) != -1)
            {
                if ((b >= 97 && b <= 122) ||
                    (b >= 65 && b <= 90) ||
                    (b >= 48 && b <= 57) ||
                    b == 32 || b == 44 || b == 46)
                    writer.WriteByte((byte)b);
                else if (b <= 127)
                    writer.WriteString(string.Format("\\x{0:X2}", b));
                else
                    //NOTE: Doing at ASCII byte level.. might not not be necesarry here as the string is not typed...
                    writer.WriteString(string.Format("\\u{0:X4}", b));
            }

            ret.Seek(0, System.IO.SeekOrigin.Begin);
            return ret;
        }

        protected override BitStream internalDecode(BitStream data)
        {
            throw new NotImplementedException();
        }
    }
}
