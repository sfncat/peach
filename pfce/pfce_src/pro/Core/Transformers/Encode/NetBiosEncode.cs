

using System;
using System.Collections.Generic;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers.Encode
{
    [Description("Encode on output from a string to a binary NetBios representation.")]
    [Parameter("pad", typeof(bool), "Should the NetBios names be padded/trimmed to 32 bytes?", "false")]
    [Transformer("NetBiosEncode", true)]
    [Transformer("encode.NetBiosEncode")]
    [Serializable]
    public class NetBiosEncode : Transformer
    {
        Dictionary<string, Variant> m_args;

        public NetBiosEncode(DataElement parent, Dictionary<string, Variant> args)
            : base(parent, args)
        {
            m_args = args;
        }

        protected override BitwiseStream internalEncode(BitwiseStream data)
        {
            string name = new BitReader(data).ReadString().ToUpper();
            var sb = new System.Text.StringBuilder(32);

            if (m_args.ContainsKey("pad") && Boolean.Parse((string)m_args["pad"]))
                while (name.Length < 16)
                    name += " ";

            if (name.Length > 16)
                name = name.Substring(0, 16);

            foreach (char c in name)
            {
                var ascii = (int)c;
                sb.Append((Char)((ascii / 16) + 0x41));
                sb.Append((Char)((ascii - (ascii / 16 * 16) + 0x41)));
            }

            var sret = sb.ToString();

            if (m_args.ContainsKey("pad") && Boolean.Parse((string)m_args["pad"]))
            {
                if (sret.Length > 30)
                    sret = sret.Substring(0, 30);

                sret += "AA";
            }

            var ret = new BitStream();
            var writer = new BitWriter(ret);
            writer.WriteString(sret.ToString());
            ret.Seek(0, System.IO.SeekOrigin.Begin);
            return ret;
        }

        protected override BitStream internalDecode(BitStream data)
        {
            if (data.Length % 2 != 0)
                throw new SoftException("NetBiosDecode transformer internalEncode failed: Length must be divisible by two.");

            var sb = new System.Text.StringBuilder((int)data.Length / 2);
            var nbs = new BitReader(data).ReadString();

            for (int i = 0; i < nbs.Length; i += 2)
            {
                char c1 = nbs[i];
                char c2 = nbs[i + 1];

                var part1 = (c1 - 0x41) * 16;
                var part2 = (c2 - 0x41);

                sb.Append((Char)(part1 + part2));
            }

            var ret = new BitStream();
            var writer = new BitWriter(ret);
            writer.WriteString(sb.ToString());
            ret.Seek(0, System.IO.SeekOrigin.Begin);
            return ret;
        }
    }
}

// end
