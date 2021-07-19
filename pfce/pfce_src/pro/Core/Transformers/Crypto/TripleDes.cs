using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Peach.Core;
using Peach.Core.Dom;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers.Crypto
{
    [Description("TripleDes transform (hex & binary).")]
    [Transformer("TripleDes", true)]
    [Transformer("crypto.TripleDes")]
    [Parameter("Key", typeof(HexString), "Secret Key")]
    [Parameter("IV", typeof(HexString), "Initialization Vector")]
    [Parameter("CipherMode", typeof(CipherMode), "Cipher Mode, CBC, ECB, CFB, CTS, OFB", "CBC")]
    [Parameter("PaddingMode", typeof(PaddingMode), "Padding Mode: Zeros, None, PKCS7, ANSIX923, ISO101026", "Zeros")]
    [Serializable]
    public class TripleDes : SymmetricAlgorithmTransformer
    {
        public CipherMode CipherMode { get; set; }
        public PaddingMode PaddingMode { get; set; }

        public TripleDes(DataElement parent, Dictionary<string, Variant> args)
            : base(parent, args)
        {
        }

        protected override SymmetricAlgorithm GetEncryptionAlgorithm()
        {
            TripleDES tdes = TripleDES.Create();
            tdes.Mode = CipherMode;
            tdes.Padding = PaddingMode;
            tdes.Key = Key.Value;
            tdes.IV = IV.Value;
            return tdes;
        }
    }
}