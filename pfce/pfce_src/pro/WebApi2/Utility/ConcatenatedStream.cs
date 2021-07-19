using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Peach.Pro.WebApi2.Utility
{
	internal class ConcatenatedStream : Stream
	{
		readonly Queue<Stream> _streams;
		readonly long _length;
		long _position;

		public ConcatenatedStream(IEnumerable<Stream> streams)
		{
			_streams = new Queue<Stream>(streams);
			_length = _streams.Sum(s => s.Length);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			var result = 0;

			while (count > 0 && _streams.Count > 0)
			{
				var bytesRead = _streams.Peek().Read(buffer, offset, count);
				result += bytesRead;
				offset += bytesRead;
				count -= bytesRead;
				_position += bytesRead;

				if (count > 0)
					_streams.Dequeue();
			}

			return result;
		}

		public override bool CanRead
		{
			get { return true; }
		}

		public override bool CanSeek
		{
			get { return false; }
		}

		public override bool CanWrite
		{
			get { return false; }
		}

		public override void Flush()
		{
			foreach (var stream in _streams)
				stream.Flush();
		}

		public override long Length
		{
			get { return _length; }
		}

		public override long Position
		{
			get { return _position; }
			set { throw new NotImplementedException(); }
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}
	}
}