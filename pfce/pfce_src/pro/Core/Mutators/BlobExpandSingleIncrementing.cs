//
// Copyright (c) Peach Fuzzer, LLC
//

using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Pro.Core.Mutators
{
	/// <summary>
	/// Expand the blob by a random size between 1 and 255.
	/// Pick a random start position in the blob.
	/// Pick a starting value between 0 and 255.
	/// Add size bytes starting at position where each byte increments by one
	/// </summary>
	[Mutator("BlobExpandSingleIncrementing")]
	[Description("Expand the blob by filling it with incrementing values")]
	[Hint("BlobExpandSingleIncrementing-N", "Standard deviation of number of bytes to change")]
	[Hint("BlobMutator-N", "Standard deviation of number of bytes to change")]
	public class BlobExpandSingleIncrementing : Utility.BlobMutator
	{
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		public BlobExpandSingleIncrementing(DataElement obj)
			: base(obj, 255, false)
		{
		}

		protected override NLog.Logger Logger
		{
			get
			{
				return logger;
			}
		}

		protected override BitwiseStream PerformMutation(BitStream data, long start, long length)
		{
			var ret = new BitStreamList();

			// Slice off data up to start
			if (start > 0)
				ret.Add(data.SliceBits(start * 8));

			// Add length bytes starting at random value
			var val = (byte)context.Random.Next(0, 256);
			var buf = new byte[length];
			for (int i = 0; i < buf.Length; ++i)
				buf[i] = val++;
			ret.Add(new BitStream(buf));

			// Slice off from start to end
			var remain = data.Length - data.Position;
			if (remain > 0)
				ret.Add(data.SliceBits(remain * 8));

			return ret;
		}
	}
}
