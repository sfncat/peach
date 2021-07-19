//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.ComponentModel;
using System.Linq;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Pro.Core.Mutators.Utility;

namespace Peach.Pro.Core.Mutators
{
	[Mutator("StringUtf8ExtraBytes")]
	[Description("Encode string as UTF-8 with overlong encodings.")]
	public class StringUtf8ExtraBytes : Mutator
	{
		int total;

		public StringUtf8ExtraBytes(DataElement obj)
			: base(obj)
		{
			var str = (string)obj.InternalValue;

			// For sequential, use the length total number of mutations
			total = str.Length;
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			var asStr = obj as Peach.Core.Dom.String;

			// Make sure we are a mutable string and Peach.TypeTransform hint is not false
			if (asStr == null || !asStr.isMutable || !getTypeTransformHint(obj) || ((string)asStr.InternalValue).Length == 0)
				return false;

			// Attach to ascii and utf8, since most ascii parsers are utf8
			if (asStr.stringType == StringType.ascii || asStr.stringType == StringType.utf8)
				return true;

			return false;
		}

		public override int count
		{
			get
			{
				return total;
			}
		}

		public override uint mutation
		{
			get;
			set;
		}

		public override void sequentialMutation(DataElement obj)
		{
			// sequential is same as random
			randomMutation(obj);
		}

		public override void randomMutation(DataElement obj)
		{
			// Pick gaussian numer Q of characters 1-6
			// Pick Q indices in the string
			// Figure out the length required to encode the character at index Q (X)
			// Pick a random number between X and 6 inclusive (Y)
			// Encode character at Q using Y bytes

			var str = (string)obj.InternalValue;

			// Pick number from 1-6 (stddev = 5/3
			var num = context.Random.PickSix();

			var indices = context.Random.Permutation(str.Length, num);

			var bs = EncodeString(str, indices);

			obj.MutatedValue = new Variant(bs);
			obj.mutationFlags = MutateOverride.Default | MutateOverride.TypeTransform;
		}

		int GetByteCount(int ch, bool mutate)
		{
			int ret = 0;

			if (ch <= 0x7f)
				ret = 1;
			else if (ch <= 0x7ff)
				ret = 2;
			else if (ch <= 0xffff)
				ret = 3;
			else if (ch <= 0x1fffff)
				ret = 4;
			else if (ch <= 0x3ffffff)
				ret = 5;
			else if (ch <= 0x7fffffff)
				ret = 6;


			if (mutate && ret < 6)
				ret = context.Random.Next(ret + 1, 6 + 1);

			return ret;
		}

		int GetCodePoint(char[] chars, int index, out int count)
		{
			char ch1 = chars[index];

			++index;
			count = 1;

			if (char.IsSurrogate(ch1) && chars.Length - index > 0)
			{
				char ch2 = chars[index];
				if (char.IsSurrogate(ch2))
				{
					++index;
					count = 2;

					int val = 0x400 * (ch1 - 0xd800) + 0x10000 + ch2 - 0xdc00;
					return val;
				}
			}

			return ch1;
		}

		BitStream EncodeString(string str, int[] indices)
		{
			var bs = new BitStream();
			var chars = str.ToCharArray();
			var remain = chars.Length;
			var charIndex = 0;
			var buf = new byte[6];

			while (remain > 0)
			{
				int charCount;
				int ch = GetCodePoint(chars, charIndex, out charCount);
				var mutate = indices.Where(i => charIndex < i && i <= charIndex + charCount).Any();

				int byteCount = GetByteCount(ch, mutate);

				if (byteCount == 1)
				{
					buf[0] = ((byte)ch);
				}
				else if (byteCount == 2)
				{
					buf[0] = ((byte)(0xc0 | (ch >> 6)));
					buf[1] = ((byte)(0x80 | (ch & 0x3f)));
				}
				else if (byteCount == 3)
				{
					buf[0] = ((byte)(0xe0 | (ch >> 12)));
					buf[1] = ((byte)(0x80 | ((ch >> 6) & 0x3f)));
					buf[2] = ((byte)(0x80 | (ch & 0x3f)));
				}
				else if (byteCount == 4)
				{
					buf[0] = ((byte)(0xf0 | (ch >> 18)));
					buf[1] = ((byte)(0x80 | ((ch >> 12) & 0x3f)));
					buf[2] = ((byte)(0x80 | ((ch >> 6) & 0x3f)));
					buf[3] = ((byte)(0x80 | (ch & 0x3f)));
				}
				else if (byteCount == 5)
				{
					buf[0] = ((byte)(0xf8 | (ch >> 24)));
					buf[1] = ((byte)(0x80 | ((ch >> 18) & 0x3f)));
					buf[2] = ((byte)(0x80 | ((ch >> 12) & 0x3f)));
					buf[3] = ((byte)(0x80 | ((ch >> 6) & 0x3f)));
					buf[4] = ((byte)(0x80 | (ch & 0x3f)));
				}
				else if (byteCount == 6)
				{
					buf[0] = ((byte)(0xfc | (ch >> 30)));
					buf[1] = ((byte)(0x80 | ((ch >> 24) & 0x3f)));
					buf[2] = ((byte)(0x80 | ((ch >> 18) & 0x3f)));
					buf[3] = ((byte)(0x80 | ((ch >> 12) & 0x3f)));
					buf[4] = ((byte)(0x80 | ((ch >> 6) & 0x3f)));
					buf[5] = ((byte)(0x80 | (ch & 0x3f)));
				}
				else
				{
					throw new InvalidOperationException();
				}

				bs.Write(buf, 0, byteCount);

				remain -= charCount;
				charIndex += charCount;
			}

			bs.Seek(0, System.IO.SeekOrigin.Begin);
			return bs;
		}
	}
}
