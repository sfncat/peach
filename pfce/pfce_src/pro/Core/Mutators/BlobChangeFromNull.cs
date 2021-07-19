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
	/// Alter the blob by a random number of bytes between 1 and 100.
	/// Pick a random start position in the blob.
	/// Alter size bytes starting at position where each null byte is
	/// changed to different randomly selected non-null value.
	/// </summary>
	[Mutator("BlobChangeFromNull")]
	[Description("Change the blob by replacing nulls with non-nulls")]
	[Hint("BlobChangeFromNull-N", "Standard deviation of number of bytes to change")]
	[Hint("BlobMutator-N", "Standard deviation of number of bytes to change")]
	public class BlobChangeFromNull : Utility.BlobMutator
	{
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		public BlobChangeFromNull(DataElement obj)
			: base(obj, 100, true)
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

			// Read length bytes
			var buf = new byte[length];
			var len = data.Read(buf, 0, buf.Length);

			System.Diagnostics.Debug.Assert(len == length);

			// Alter so each null byte is a new random non-null value
			for (int i = 0; i < buf.Length; ++i)
			{
				if (buf[i] == 0)
					buf[i] = (byte)context.Random.Next(1, 256);
			}

			ret.Add(new BitStream(buf));

			// Slice off from start to end
			var remain = data.Length - data.Position;
			if (remain > 0)
				ret.Add(data.SliceBits(remain * 8));

			return ret;
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			// Don't attach to elements that are empty
			return Utility.BlobMutator.supportedNonEmptyDataElement(obj);
		}
	}
}
