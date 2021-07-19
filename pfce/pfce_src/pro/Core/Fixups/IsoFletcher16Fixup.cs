using System;
using System.Collections.Generic;
using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Pro.Core.Fixups
{
	// c implementation
	// uint16_t fletcher_checksum(const uint8_t * buffer, size_t len, size_t offset) {
	//   int x, y, c0, c1, i;
	//   uint8_t b;
	//
	//   c0 = 0;
	//   c1 = 0;
	//
	//   for (i = 0; i < len; i++)	{
	//     if(i == offset || i == offset+1)
	//       b = 0;
	//     else
	//       b = buffer[i];
	//     c0 = (c0 + b) % 255;
	//     c1 = (c1 + c0) % 255;
	//   }
	//
	//   /* The cast is important, to ensure the mod is taken as a signed value. */
	//   x = (int)((len - offset - 1) * c0 - c1) % 255;
	//
	//   if (x <= 0)
	//     x += 255;
	//   y = 510 - c0 - x;
	//   if (y > 255)
	//     y -= 255;
	//
	//   return (x << 8) | (y & 0xFF);
	// }

	[Description("ISO fletcher checksum. see IETF rfc1008 section 7.2.1. The checksum must lie within the data to be checksummed on a byte aligned boundary.")]
	[Fixup("IsoFletcher16Checksum", true)]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Serializable]
	public class IsoFletcher16Fixup : Fixup
	{
		/// <summary>
		/// Computes ISO fletcher 16 bit checksum
		/// </summary>
		internal class IsoFletcher16
		{
			// http://en.wikipedia.org/wiki/Fletcher%27s_checksum
			// https://tools.ietf.org/html/rfc1008#7.2.1
			// note: modulus of 255 instead of 256 is intentional

			protected int sum1 = 0;
			protected int sum2 = 0;
			protected int length = 0;
			protected uint offset = 0;

			// offset: the offset at which this checksum exists inside
			// the data stream. This checksum algorithm assumes the
			// checksum bytes lie within the checksum area

			public IsoFletcher16(uint offset)
			{
				this.offset = offset;
			}

			public virtual void Update(byte[] buf, int count)
			{
				for (int i = 0; i < count; i += 1)
				{
					byte b = 0;

					//if this byte lies inside the checksum destination
					//area, leave b equal to 0
					if (i != this.offset && i != (this.offset + 1))
						b = buf[i];

					sum1 = (int)((sum1 + b) % 255);
					sum2 = (int)((sum2 + sum1) % 255);
					this.length += 1;
				}
			}

			public virtual ushort Final()
			{
				var x = (int)((length - this.offset - 1) * this.sum1 - this.sum2) % 255;

				if (x <= 0)
					x += 255;
				var y = 510 - this.sum1 - x;
				if (y > 255)
					y -= 255;

				return (ushort)((x << 8) | (y & 0xff));
			}
		}

		public DataElement _ref { get; protected set; }

		public IsoFletcher16Fixup(DataElement parent, Dictionary<string, Variant> args) 
			: base(parent, args, "ref")
		{
		}

		protected override Variant fixupImpl()
		{
			var elem = elements["ref"];
			var data = elem.Value;
			long offset = 0;

			// if this fails we dont know the offset of the
			// fixup and the checksum will be incorrect, but
			// we dont throw an error because this may be the
			// result of mutatation. in this case, offset will
			// remain 0.
			if (data.TryGetPosition(parent.fullName, out offset))
				offset = offset / 8;

			if (offset < 0)
				offset = 0;

			var sum = new IsoFletcher16((uint)offset);

			var buf = new byte[BitwiseStream.BlockCopySize];
			data.Seek(0, System.IO.SeekOrigin.Begin);

			int nread;
			while ((nread = data.Read(buf, 0, buf.Length)) > 0)
				sum.Update(buf, nread);

			var result = sum.Final();
			return new Variant(result);
		}
	}
}
