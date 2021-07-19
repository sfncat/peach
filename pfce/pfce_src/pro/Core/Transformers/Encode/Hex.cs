


// Authors:
//   Mikhail Davidov (sirus@haxsys.net)

// $Id$

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers.Encode
{
	[Description("Encode on output as a hex string.")]
	[Transformer("Hex", true)]
	[Transformer("encode.Hex")]
	[Parameter("lowercase", typeof(bool), "Use lowercase for hex digits", "true")]
	[Serializable]
	public class Hex : Transformer
	{
		#region Hex Encoder Transform

		class Encoder : ICryptoTransform
		{
			private bool _useLowercase;
			
			public Encoder(bool useLowercase)
			{
				_useLowercase = useLowercase;
			}
			
			public bool CanReuseTransform
			{
				get { return true; }
			}

			public bool CanTransformMultipleBlocks
			{
				get { return false; }
			}

			public int InputBlockSize
			{
				get { return 1; }
			}

			public int OutputBlockSize
			{
				get { return 2; }
			}

			public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
			{
				if ((outputBuffer.Length % 2) != 0)
					throw new ArgumentOutOfRangeException("outputBuffer");
				if ((outputOffset % 2) != 0)
					throw new ArgumentOutOfRangeException("outputOffset");

				int offset = outputOffset;
				int end = inputOffset + inputCount;
				for (int i = inputOffset; i < end; ++i)
				{
					outputBuffer[offset++] = GetChar(inputBuffer[i] >> 4);
					outputBuffer[offset++] = GetChar(inputBuffer[i] & 0x0f);
				}
				return offset - outputOffset;
			}

			public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
			{
				var ret = new byte[inputCount * 2];
				int len = TransformBlock(inputBuffer, inputOffset, inputCount, ret, 0);
				System.Diagnostics.Debug.Assert(len == ret.Length);
				return ret;
			}

			private byte GetChar(int nibble)
			{
				if (nibble > 0x0f)
					throw new ArgumentOutOfRangeException("nibble");

				if (nibble < 0x0a)
					return (byte)(nibble + 0x30);
				else
					return (byte)(nibble - 0x0a + (_useLowercase ? 0x61 : 0x41));
			}

			public void Dispose()
			{
			}
		}

		#endregion

		#region Hex Decoder Transform

		class Decoder : ICryptoTransform
		{
			public bool CanReuseTransform
			{
				get { return true; }
			}

			public bool CanTransformMultipleBlocks
			{
				get { return false; }
			}

			public int InputBlockSize
			{
				get { return 2; }
			}

			public int OutputBlockSize
			{
				get { return 1; }
			}

			public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
			{
				if ((inputCount % 2) != 0)
					throw new ArgumentOutOfRangeException("inputCount");

				int offset = outputOffset;
				int end = inputOffset + inputCount;
				for (int i = inputOffset; i < end; ++i)
				{
					outputBuffer[offset] = (byte)(GetNibble(inputBuffer[i++]) << 4);
					outputBuffer[offset++] |= GetNibble(inputBuffer[i]);
				}
				return offset - outputOffset;
			}

			public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
			{
				if ((inputCount % 2) != 0)
					throw new SoftException("Hex decode failed, invalid length.");

				var ret = new byte[inputCount / 2];
				var len = TransformBlock(inputBuffer, inputOffset, inputCount, ret, 0);
				System.Diagnostics.Debug.Assert(len == ret.Length);
				return ret;
			}

			private byte GetNibble(byte c)
			{
				if (c < '0')
					throw new SoftException("Hex decode failed, invalid bytes.");
				if (c <= '9')
					return (byte)(c - '0');
				if (c < 'A')
					throw new SoftException("Hex decode failed, invalid bytes.");
				if (c <= 'F')
					return (byte)(c - 'A' + 0xA);
				if (c < 'a')
					throw new SoftException("Hex decode failed, invalid bytes.");
				if (c <= 'f')
					return (byte)(c - 'a' + 0xA);

				throw new SoftException("Hex decode failed, invalid bytes.");
			}

			public void Dispose()
			{
			}
		}

		#endregion

		public Hex(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
			Variant value;
			if (args.TryGetValue("lowercase", out value))
				_useLowercase = Boolean.Parse((string)value);
			else 
				_useLowercase = true;
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			return CryptoStream(data, new Encoder(_useLowercase), CryptoStreamMode.Write);
		}

		protected override BitStream internalDecode(BitStream data)
		{
			return CryptoStream(data, new Decoder(), CryptoStreamMode.Read);
		}

		private bool _useLowercase;
	}
}
