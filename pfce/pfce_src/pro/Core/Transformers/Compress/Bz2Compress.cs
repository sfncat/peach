

using System;
using System.Collections.Generic;
using System.IO;
using Ionic.BZip2;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Transformers.Compress
{
	[Description("Compress on output using bz2.")]
	[Transformer("Bz2Compress", true)]
	[Transformer("compress.Bz2Compress")]
	[Serializable]
	public class Bz2Compress : Transformer
	{
		public Bz2Compress(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}

		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			BitStream ret = new BitStream();

			using (var bzip2 = new BZip2OutputStream(ret, true))
			{
				data.CopyTo(bzip2);
			}

			ret.Seek(0, SeekOrigin.Begin);
			return ret;
		}

		protected override BitStream internalDecode(BitStream data)
		{
			BitStream ret = new BitStream();

			using (var bzip2 = new BZip2InputStream(data, true))
			{
				try
				{
					// For some reason, Ionic decided the BZip2InputStream
					// should return -1 from Read() when EOF is reached.  This
					// breaks Stream.CopyTo() as it expects 0 on EOF.
					// We need to use ReadByte() instead.
					int val;
					while ((val = bzip2.ReadByte()) != -1)
						ret.WriteByte((byte)val);
				}
				catch (Exception ex)
				{
					throw new SoftException("Could not BZip decompress data.", ex);
				}
			}

			ret.Seek(0, SeekOrigin.Begin);
			return ret;
		}
	}
}

// end
