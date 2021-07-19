//
// Copyright (c) Peach Fuzzer, LLC
//

using System.ComponentModel;
using System.IO;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Pro.Core.Mutators
{
	/// <summary>
	/// Alter the blob by a random number of bytes between 1 and 100.
	/// Pick a random start position in the blob.
	/// Alter size bytes starting at positionand set each byte to null.
	/// </summary>
	[Mutator("BlobChangeToNull")]
	[Description("Change the blob by replacing bytes with null bytes")]
	[Hint("BlobChangeToNull-N", "Standard deviation of number of bytes to change")]
	[Hint("BlobChangeToNull-N", "Standard deviation of number of bytes to change")]
	public class BlobChangeToNull : Utility.BlobMutator
	{
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		public BlobChangeToNull(DataElement obj)
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

			// Add length bytes of null
			var buf = new byte[length];
			ret.Add(new BitStream(buf));

			// Skip length bytes from data
			data.Seek(length, SeekOrigin.Current);

			// Slice off from start + length to end
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
