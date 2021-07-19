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
	/// Alter size bytes starting at position where each byte is
	/// changed to different randomly selected value from the
	/// special set of { 0x00, 0x01, 0xFE, 0xFF }.
	/// </summary>
	[Mutator("BlobChangeSpecial")]
	[Description("Change the blob by replacing bytes with special bytes")]
	[Hint("BlobChangeSpecial-N", "Standard deviation of number of bytes to change")]
	[Hint("BlobMutator-N", "Standard deviation of number of bytes to change")]
	public class BlobChangeSpecial : Utility.BlobMutator
	{
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		static byte[] special = new byte[] { 0x00, 0x01, 0xFE, 0xFF };

		public BlobChangeSpecial(DataElement obj)
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

			// Add length bytes of special values
			var buf = new byte[length];
			for (int i = 0; i < buf.Length; ++i)
				buf[i] = context.Random.Choice(special);
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
