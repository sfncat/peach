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
	/// Picks a random range of bytes up to 255 inside the blob and removes it.
	/// </summary>
	[Mutator("BlobReduce")]
	[Description("Reduce the size of a blob")]
	[Hint("BlobReduce-N", "Standard deviation of number of bytes to change")]
	[Hint("BlobMutator-N", "Standard deviation of number of bytes to change")]
	public class BlobReduce : Utility.BlobMutator
	{
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		public BlobReduce(DataElement obj)
			: base(obj, 255, true)
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

			// Slice off up to start
			if (start > 0)
				ret.Add(data.SliceBits(start * 8));

			// Slip next length bytes
			data.Seek(length, SeekOrigin.Current);

			// Slice off end
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
