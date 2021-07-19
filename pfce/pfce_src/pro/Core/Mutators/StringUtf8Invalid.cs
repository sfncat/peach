//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.Linq;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;
using Peach.Pro.Core.Mutators.Utility;

namespace Peach.Pro.Core.Mutators
{
	[Mutator("StringUtf8Invalid")]
	[Description("Encode string as invalid UTF-8.")]
	public class StringUtf8Invalid : Mutator
	{
		int total;

		public StringUtf8Invalid(DataElement obj)
			: base(obj)
		{
			var str = (string)obj.InternalValue;

			// For sequential, use the length total number of mutations
			total = Encoding.UTF8.GetByteCount(str);
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

		static int GetCount(byte b)
		{
			if ((b & 0x80) == 0x00)
				return 1;
			if ((b & 0xe0) == 0xc0)
				return 2;
			if ((b & 0xf0) == 0xe0)
				return 3;
			if ((b & 0xf8) == 0xf0)
				return 4;
			if ((b & 0xfc) == 0xf8)
				return 5;
			if ((b & 0xfe) == 0xfc)
				return 6;

			throw new NotSupportedException();
		}

		static int GetMutableBitCount(int start, int pos, int len)
		{
			// If position is not at start of sequence, there
			// are only two bits to mutate
			if (start != pos)
				return 2;

			// If the length is 1, there is only 1 bit to mutate
			if (len == 1)
				return 1;

			// In all other cases, number of bits is len + 1
			return len + 1;
		}

		public override void randomMutation(DataElement obj)
		{
			// 1) Encode string to valid utf8
			// 2) Pick 1-6 byte indices
			// 3) Flip bits that control bits in the the underlying byte sequence

			var str = (string)obj.InternalValue;
			var buf = Encoding.UTF8.GetBytes(str);

			// Pick number from 1-6 (stddev = 5/3
			var num = context.Random.PickSix();

			var indices = context.Random.Permutation(buf.Length, num);

			int i = 0;
			while (i < buf.Length)
			{
				var n = GetCount(buf[i]);

				for (int j = i; j < i + n; ++j)
				{
					if (!indices.Contains(j + 1))
						continue;

					var bitCount = GetMutableBitCount(i, j, n);

					var bit = 0x80 >> context.Random.Next(0, bitCount);

					if ((buf[j] & bit) == bit)
						buf[j] &= (byte)~bit;
					else
						buf[j] |= (byte)bit;
				}

				i += n;
			}

			obj.MutatedValue = new Variant(new BitStream(buf));
			obj.mutationFlags = MutateOverride.Default | MutateOverride.TypeTransform;
		}
	}
}
