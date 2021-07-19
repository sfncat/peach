

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers.Compress
{
	[Description("Decompress on output using gzip.")]
	[Transformer("GzipDecompress", true)]
	[Transformer("compress.GzipDecompress")]
	[Serializable]
	public class GzipDecompress : Transformer
	{
		public GzipDecompress(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			BitStream ret = new BitStream();

			using (var strm = new GZipStream(data, CompressionMode.Decompress, true))
			{
				try
				{
					strm.CopyTo(ret);
				}
				catch (Exception ex)
				{
					throw new SoftException("Could not GZip decompress data.", ex);
				}
			}

			ret.Seek(0, SeekOrigin.Begin);
			return ret;
		}

		protected override BitStream internalDecode(BitStream data)
		{
			BitStream ret = new BitStream();

			using (var strm = new GZipStream(ret, CompressionMode.Compress, true))
			{
				data.CopyTo(strm);
			}

			ret.Seek(0, SeekOrigin.Begin);
			return ret;
		}
	}
}

// end
