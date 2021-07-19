using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Peach.Core;
using Peach.Core.Dom;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers.Crypto
{
    [Description("Aes128 transform (hex & binary).")]
    [Transformer("Aes128", true)]
    [Transformer("crypto.Aes128")]
    [Parameter("Key", typeof(HexString), "Secret Key")]
    [Parameter("IV", typeof(HexString), "Initialization Vector")]
    [Parameter("CipherMode", typeof(CipherMode), "Cipher Mode: CBC, ECB, CFB, CTS, OFB", "CBC")]
    [Parameter("PaddingMode", typeof(PaddingMode), "Padding Mode: Zeros, None, PKCS7, ANSIX923, ISO101026", "Zeros")]
    [Serializable]
    public class Aes128 : SymmetricAlgorithmTransformer
    {
        public CipherMode CipherMode { get; set; }
        public PaddingMode PaddingMode { get; set; }

        public Aes128(DataElement parent, Dictionary<string, Variant> args)
            : base(parent, args)
        {
        }

        protected override SymmetricAlgorithm GetEncryptionAlgorithm()
        {
            Rijndael aes = Rijndael.Create();
            aes.Mode = CipherMode;
            aes.Padding = PaddingMode;
            aes.Key = Key.Value;
            aes.IV = IV.Value;
            return aes;
        }
    }
}