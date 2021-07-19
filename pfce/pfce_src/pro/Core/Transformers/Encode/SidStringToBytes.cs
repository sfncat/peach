


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
    [Description("Encode on output from a string representation of a SID to bytes. (Format: S-1-5-21-2127521184-1604012920-1887927527-1712781)")]
    [Transformer("SidStringToBytes", true)]
    [Transformer("encode.SidStringToBytes")]
    [Serializable]
    public class SidStringToBytes : Transformer
    {
        public SidStringToBytes(DataElement parent, Dictionary<string, Variant> args)
            : base(parent, args)
        {
        }

        protected override BitwiseStream internalEncode(BitwiseStream data)
        {
            var sids = new BitReader(data).ReadString();

            try
            {
                //Hopefully this is in mono...
                var sid = new System.Security.Principal.SecurityIdentifier(sids);
                byte[] bsid = new byte[sid.BinaryLength];
                sid.GetBinaryForm(bsid, 0);

                var ret = new BitStream();
                ret.Write(bsid, 0, bsid.Length);
                ret.Seek(0, System.IO.SeekOrigin.Begin);
                return ret;
            }
            catch(Exception ex)
            {
                throw new SoftException("Error, cannot convert string '" + sids + "' to SID.", ex);
            }
        }

        protected override BitStream internalDecode(BitStream data)
        {
            var len = data.Length;
            var buf = new BitReader(data).ReadBytes((int)len);
            var sid = new System.Security.Principal.SecurityIdentifier(buf, 0);
            var ret = new BitStream();
            var writer = new BitWriter(ret);
            writer.WriteString(sid.ToString());
            ret.Seek(0, System.IO.SeekOrigin.Begin);
            return ret;
        }
    }
}

// end
