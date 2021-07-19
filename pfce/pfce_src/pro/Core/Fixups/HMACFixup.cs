


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)
//   Ross Salpino (rsal42@gmail.com)
//   Mikhail Davidov (sirus@haxsys.net)

// $Id$

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Pro.Core.Fixups
{
    [Description("Standard Hmac checksum.")]
	[Fixup("Hmac", true)]
	[Fixup("HMAC")]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
    [Parameter("Key", typeof(HexString), "Key used in the hash algorithm")]
    [Parameter("Hash", typeof(Algorithms), "Hash algorithm to use", "HMACSHA1")]
    [Parameter("Length", typeof(int), "Length in bytes to return (Value of 0 means don't truncate)", "0")]
    [Parameter("DefaultValue", typeof(HexString), "Default value to use when recursing (default is parent's DefaultValue)", "")]
    [Serializable]
    public class HMACFixup : Fixup
    {
        public HexString DefaultValue { get; protected set; }
        public HexString Key { get; protected set; }
        public Algorithms Hash { get; protected set; }
        public int Length { get; protected set; }
        public DataElement _ref { get; protected set; }

        public enum Algorithms { HMACSHA1, HMACMD5, HMACRIPEMD160, HMACSHA256, HMACSHA384, HMACSHA512, MACTripleDES  };

        public HMACFixup(DataElement parent, Dictionary<string, Variant> args)
            : base(parent, args, "ref")
        {
            ParameterParser.Parse(this, args);
            HMAC hashSizeTest = HMAC.Create(Hash.ToString());
            if (Length > (hashSizeTest.HashSize / 8))
                throw new PeachException("The truncate length is greater than the hash size for the specified algorithm.");
            if (Length < 0)
                throw new PeachException("The truncate length must be greater than or equal to 0.");
        }

		protected override Variant fixupImpl()
		{
			var from = elements["ref"];
			var data = from.Value;
			HMAC hashTool = HMAC.Create(Hash.ToString());
			hashTool.Key = Key.Value;
			byte[] hash = hashTool.ComputeHash(data);

			var bs = new BitStream();

			if (Length == 0)
				bs.Write(hash, 0, hash.Length);
			else
				bs.Write(hash, 0, Length);

			return new Variant(bs);
		}

		protected override Variant GetDefaultValue(DataElement obj)
		{
			return DefaultValue != null ? new Variant(DefaultValue.Value) : base.GetDefaultValue(obj);
		}
    }
}

// end
