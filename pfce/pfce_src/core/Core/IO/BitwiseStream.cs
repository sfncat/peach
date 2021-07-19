using System;
using System.IO;

namespace Peach.Core.IO
{
	[Serializable]
	public abstract class BitwiseStream : Stream
	{
		#region Const Members

		public const int BlockCopySize = 4 * 1024 * 1024;

		#endregion

		#region Constructor

		protected BitwiseStream()
		{
		}

		#endregion

		#region Bitwise Interface

		public abstract long LengthBits { get; }

		public abstract long PositionBits { get; set; }

		public abstract long SeekBits(long offset, SeekOrigin origin);

		public abstract int ReadBits(out ulong bits, int count);

		public abstract int ReadBit();

		public abstract void SetLengthBits(long value);

		public abstract void WriteBits(ulong bits, int count);

		public abstract void WriteBit(int value);

		public abstract BitwiseStream SliceBits(long length);

		#endregion

		#region Element Positions

		public string Name { get; set; }

		public virtual bool TryGetPosition(string name, out long position)
		{
			throw new NotSupportedException("Stream does not support element positions.");
		}

		public virtual bool TryGetName(long postion, out string name)
		{
			throw new NotSupportedException("Stream does not support element positions.");
		}

		#endregion

		#region Stream Specializations

		public void CopyTo(BitwiseStream destination)
		{
			// Copying a BitwiseStream of 7 bits means
			// length will be 0 so always add one to our minimum size.
			// Also, on mono objects larger than 8k are considered large
			// objects so keep our temp buffer as small as possible so it
			// stays in the nursery
			CopyTo(destination, (int)Math.Min(Length + 1, BlockCopySize));
		}

		public void CopyTo(BitwiseStream destination, int bufferSize)
		{
			if (destination == null)
				throw new ArgumentNullException("destination");
			if (!CanRead)
				throw new NotSupportedException("This stream does not support reading");
			if (!destination.CanWrite)
				throw new NotSupportedException("This destination stream does not support writing");
			if (bufferSize <= 0)
				throw new ArgumentOutOfRangeException("bufferSize");

			var buffer = new byte[bufferSize];
			int nread;
			while ((nread = Read(buffer, 0, bufferSize)) != 0)
				destination.Write(buffer, 0, nread);

			ulong bits;
			nread = ReadBits(out bits, 7);
			destination.WriteBits(bits, nread);
		}

		public void CopyTo(BitwiseStream destination, int offset, int count)
		{
			CopyTo(destination, (int)Math.Min(Length + 1, BlockCopySize), offset, count);
		}

		public void CopyTo(BitwiseStream destination, int bufferSize, int offset, int count)
		{
			if (destination == null)
				throw new ArgumentNullException("destination");
			if (!CanRead)
				throw new NotSupportedException("This stream does not support reading");
			if (!destination.CanWrite)
				throw new NotSupportedException("This destination stream does not support writing");
			if (bufferSize <= 0)
				throw new ArgumentOutOfRangeException("bufferSize");
			if (offset < 0)
				throw new ArgumentOutOfRangeException("offset");
			if (count < 0)
				throw new ArgumentOutOfRangeException("count");

			Seek(offset, SeekOrigin.Current);

			var buffer = new byte[bufferSize];

			// using +1 here to ensure that the last partial byte is included, 
			// but only if the specified count is larger than what remains in the source
			int nread;
			var remain = Math.Min(count, (Length - Position) + 1);
			while (remain > bufferSize)
			{
				nread = Read(buffer, 0, bufferSize);
				destination.Write(buffer, 0, nread);
				remain -= nread;
			}

			nread = Read(buffer, 0, (int)remain);
			destination.Write(buffer, 0, nread);
			remain -= nread;

			// if anything remains, there must be bits to consume
			if (remain > 0)
			{
				ulong bits;
				nread = ReadBits(out bits, 7);
				destination.WriteBits(bits, nread);
			}
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Ensures that the data stream length is in full bytes
		/// by returning a new stream padded with up to 7 bits of '0'
		/// </summary>
		/// <returns>Stream of bits padded to a byte boundary</returns>
		public BitwiseStream PadBits()
		{
			var data = this;
			var lengthBits = LengthBits;

			var extra = 8 - (int)(lengthBits % 8);
			if (extra != 8)
			{
				var lst = new BitStreamList();
				lst.Add(data);

				var pad = new BitStream();
				pad.WriteBits(0, extra);
				lst.Add(pad);
				lengthBits += extra;

				data = lst;
			}

			return data;
		}

		/// <summary>
		/// Reports the position of the first occurrence of the specified BitStream in this
		/// instance. The search starts at a specified BitStream position.
		/// </summary>
		/// <param name="value">The BitStream to seek.</param>
		/// <param name="offsetBits">The search starting position.</param>
		/// <returns>
		/// The zero-based index position of value if that BitStream is found, or -1 if it is not.
		/// </returns>
		/// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="offsetBits"/> specifies a position not within this instance.</exception>
		public long IndexOf(BitwiseStream value, long offsetBits)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			if (value.LengthBits % 8 != 0)
				throw new ArgumentOutOfRangeException("value");
			if (offsetBits < 0 || offsetBits > LengthBits)
				throw new ArgumentOutOfRangeException("offsetBits");

			var needle = new byte[value.Length];
			long pos = value.PositionBits;
			value.SeekBits(0, SeekOrigin.Begin);
			int len = value.Read(needle, 0, needle.Length);
			System.Diagnostics.Debug.Assert(len == needle.Length);
			value.SeekBits(pos, SeekOrigin.Begin);

			pos = PositionBits;
			SeekBits(offsetBits, SeekOrigin.Begin);

			int idx = 0;
			long ret = -1;
			long end = (LengthBits - PositionBits) / 8;

			for (long i = 0; i < end; ++i)
			{
				int b = ReadByte();

				if (b != needle[idx])
				{
					SeekBits(idx * -8, SeekOrigin.Current);
					i -= idx;
					idx = 0;
				}
				else if (++idx == needle.Length)
				{
					ret = 8 * (i - idx + 1) + offsetBits;
					break;
				}
			}

			SeekBits(pos, SeekOrigin.Begin);
			return ret;
		}

		/// <summary>
		/// Create a new BitwiseSteam be replicating a source BitwiseStream over and over.
		/// </summary>
		/// <param name="len">How many bytes long the returned stream should be</param>
		/// <returns></returns>
		public BitwiseStream GrowTo(long len)
		{
			var data = this;

			if (len < 0)
				return new BitStream();

			// If there is no source data, replicate 'A'
			if (data.Length == 0)
				data = new BitStream(Encoding.ASCII.GetBytes("A"));

			if (data.Length > len)
			{
				var pos = data.PositionBits;
				data.PositionBits = 0;
				var ret = data.SliceBits(len * 8);
				data.PositionBits = pos;
				return ret;
			}

			var item = data;
			var cnt = data.Length;
			var remain = len - cnt;

			while (remain > 0)
			{
				var lst = new BitStreamList();
				lst.Add(item);
				lst.Add(item);

				remain -= cnt;
				cnt *= 2;

				item = lst;
			}

			{
				// Always slice, to ensure trailing bits get lopped off
				var pos = item.PositionBits;
				item.PositionBits = 0;
				var ret = item.SliceBits(len * 8);
				item.PositionBits = pos;

				return ret;
			}
		}

		/// <summary>
		/// Create a new BitwiseSteam be replicating a source BitwiseStream over and over.
		/// </summary>
		/// <param name="len">How many bits long the returned stream should be</param>
		/// <returns></returns>
		public BitwiseStream GrowToBits(long len)
		{
			var data = this;

			if (len < 0)
				return new BitStream();

			// If there is no source data, replicate 'A'
			if (data.Length == 0)
				data = new BitStream(Encoding.ASCII.GetBytes("A"));

			if (data.LengthBits > len)
			{
				var pos = data.PositionBits;
				data.PositionBits = 0;
				var ret = data.SliceBits(len);
				data.PositionBits = pos;
				return ret;
			}

			var item = data;
			var cnt = data.LengthBits;
			var remain = len - cnt;

			while (remain > 0)
			{
				var lst = new BitStreamList();
				lst.Add(item);
				lst.Add(item);

				remain -= cnt;
				cnt *= 2;

				item = lst;
			}

			{
				// Always slice, to ensure trailing bits get lopped off
				var pos = item.PositionBits;
				item.PositionBits = 0;
				var ret = item.SliceBits(len);
				item.PositionBits = pos;

				return ret;
			}
		}

		#endregion
	}
}
