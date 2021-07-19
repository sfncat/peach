using System;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using Mono.Unix;
using Mono.Unix.Native;

namespace Peach.Pro.Core.OS.Unix
{
	public class SerialPortStream : Stream, ISerialStream
	{
		readonly Pollfd[] fds;
		int read_timeout;
		int write_timeout;
		bool disposed;

		public SerialPortStream(string portName, int baudRate, int dataBits, Parity parity, StopBits stopBits,
				bool dtrEnable, bool rtsEnable, Handshake handshake, int readTimeout, int writeTimeout,
				int readBufferSize, int writeBufferSize)
		{
			fds = new[]
			{
				new Pollfd { fd = -1, events = PollEvents.POLLIN },
				new Pollfd { fd = -1, events = PollEvents.POLLIN },
				new Pollfd { fd = -1, events = PollEvents.POLLIN }
			};

			try
			{
				fds[0].fd = Syscall.open(portName,  OpenFlags.O_RDWR | OpenFlags.O_NOCTTY | OpenFlags.O_NONBLOCK);

				if (fds[0].fd == -1 && Errno.ENOENT == Stdlib.GetLastError())
					throw new IOException("The port '" + portName + "' does not exist.");

				UnixMarshal.ThrowExceptionForLastErrorIf(fds[0].fd);

				TryBaudRate(baudRate);

				if (!set_attributes(fds[0].fd, baudRate, parity, dataBits, stopBits, handshake))
					UnixMarshal.ThrowExceptionForLastError();

				SetSignal(SerialSignal.Dtr, dtrEnable);

				if (handshake != Handshake.RequestToSend && handshake != Handshake.RequestToSendXOnXOff)
					SetSignal(SerialSignal.Rts, rtsEnable);

				var ret = Syscall.pipe(out fds[1].fd, out fds[2].fd);
				UnixMarshal.ThrowExceptionForLastErrorIf(ret);

				read_timeout = readTimeout;
				write_timeout = writeTimeout;
			}
			catch
			{
				if (fds[0].fd != -1)
					Syscall.close(fds[0].fd);
				if (fds[1].fd != -1)
					Syscall.close(fds[1].fd);
				if (fds[2].fd != -1)
					Syscall.close(fds[2].fd);

				throw;
			}
		}

		public override bool CanRead {
			get {
				return true;
			}
		}

		public override bool CanSeek {
			get {
				return false;
			}
		}

		public override bool CanWrite {
			get {
				return true;
			}
		}

		public override bool CanTimeout {
			get {
				return true;
			}
		}

		public override int ReadTimeout {
			get {
				return read_timeout;
			}
			set {
				if (value < 0 && value != SerialPort.InfiniteTimeout)
					throw new ArgumentOutOfRangeException ("value");

				read_timeout = value;
			}
		}

		public override int WriteTimeout {
			get {
				return write_timeout;
			}
			set {
				if (value < 0 && value != SerialPort.InfiniteTimeout)
					throw new ArgumentOutOfRangeException ("value");

				write_timeout = value;
			}
		}

		public override long Length {
			get {
				throw new NotSupportedException ();
			}
		}

		public override long Position {
			get {
				throw new NotSupportedException ();
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override void Flush ()
		{
			// If used, this _could_ flush the serial port
			// buffer (not the SerialPort class buffer)
		}

		public override int Read ([In,Out] byte[] buffer, int offset, int count)
		{
			CheckDisposed ();
			if (buffer == null)
				throw new ArgumentNullException ("buffer");

			if (offset < 0)
				throw new ArgumentOutOfRangeException ("offset", "offset less than zero.");

			if (count < 0)
				throw new ArgumentOutOfRangeException ("count", "count less than zero.");

			if (buffer.Length - offset < count )
				throw new ArgumentException ("The size of the buffer is less than offset + count.");

			lock (fds)
			{
				while (true)
				{
					// fd[0] == serial port
					// fd[1] == cancel pipe

					// Ensure Dispose() hasnt been called on a different thread
					if (fds[0].fd == -1)
						return 0;

					fds[0].revents = 0;
					fds[1].revents = 0;

					var ret = Syscall.poll(fds, 2, read_timeout);

					if (UnixMarshal.ShouldRetrySyscall(ret))
						continue;

					UnixMarshal.ThrowExceptionForLastErrorIf(ret);

					if (ret == 0)
						throw new TimeoutException();

					// If stop event was signalled, return 0 bytes
					if (fds[1].revents != 0)
						return 0;

					unsafe
					{
						fixed (byte* ptr = buffer)
						{
							var result = (int)Syscall.read(fds[0].fd, ptr + offset, (ulong)count);
							UnixMarshal.ThrowExceptionForLastErrorIf(result);
							return result;
						}
					}
				}
			}
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			throw new NotSupportedException ();
		}

		public override void SetLength (long value)
		{
			throw new NotSupportedException ();
		}

		public override void Write (byte[] buffer, int offset, int count)
		{
			CheckDisposed ();
			if (buffer == null)
				throw new ArgumentNullException ("buffer");

			if (offset < 0)
				throw new ArgumentOutOfRangeException ("offset", "offset less than zero.");

			if (count < 0)
				throw new ArgumentOutOfRangeException ("count", "count less than zero.");

			if (buffer.Length - offset < count)
				throw new ArgumentException ("The size of the buffer is less than offset + count.");

			var fd = new Pollfd[1];
			fd[0].fd = fds[0].fd;
			fd[0].events = PollEvents.POLLOUT;

			var expires = Environment.TickCount + WriteTimeout;

			while (true)
			{
				var wait = Math.Max(0, expires - Environment.TickCount);
				fd[0].revents = 0;

				var ret = Syscall.poll(fd, wait);
				if (UnixMarshal.ShouldRetrySyscall(ret))
					continue;

				UnixMarshal.ThrowExceptionForLastErrorIf(ret);

				if (ret == 0)
					throw new TimeoutException();

				unsafe
				{
					fixed (byte* ptr = buffer)
					{
						ret = (int)Syscall.write(fds[0].fd, ptr + offset, (ulong)count);
						UnixMarshal.ThrowExceptionForLastErrorIf(ret);

						offset += ret;
						count -= ret;

						if (count == 0)
							return;
					}
				}
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (!disposing || disposed)
				return;
			
			disposed = true;

			// Write 1 byte to the pipe to break the poll() on osx
			var stop = new byte[] { 0 };

			unsafe
			{
				fixed (byte* ptr = stop)
				{
					Syscall.write(fds[2].fd, ptr, 1);
				}
			}

			lock (fds)
			{
				Syscall.close(fds[2].fd);
				fds[2].fd = -1;

				Syscall.close(fds[1].fd);
				fds[1].fd = -1;

				var err = Syscall.close(fds[0].fd);
				fds[0].fd = -1;

				UnixMarshal.ThrowExceptionForLastErrorIf(err);
			}
		}

		public override void Close ()
		{
			((IDisposable) this).Dispose ();
		}

		void IDisposable.Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		~SerialPortStream ()
		{
			try {
				Dispose (false);
			} catch (UnixIOException) {
			}
		}

		void CheckDisposed ()
		{
			if (disposed)
				throw new ObjectDisposedException (GetType ().FullName);
		}

		[DllImport ("MonoPosixHelper", SetLastError = true)]
		static extern bool set_attributes (int fd, int baudRate, Parity parity, int dataBits, StopBits stopBits, Handshake handshake);

		public void SetAttributes (int baud_rate, Parity parity, int data_bits, StopBits sb, Handshake hs)
		{
			if (!set_attributes (fds[0].fd, baud_rate, parity, data_bits, sb, hs))
				UnixMarshal.ThrowExceptionForLastError();
		}

		[DllImport("MonoPosixHelper", SetLastError = true)]
		static extern int get_bytes_in_buffer (int fd, int input);
		
		public int BytesToRead {
			get {
				int result = get_bytes_in_buffer (fds[0].fd, 1);
				UnixMarshal.ThrowExceptionForLastErrorIf(result);
				return result;
			}
		}

		public int BytesToWrite {
			get {
				int result = get_bytes_in_buffer (fds[0].fd, 0);
				UnixMarshal.ThrowExceptionForLastErrorIf(result);
				return result;
			}
		}

		[DllImport ("MonoPosixHelper", SetLastError = true)]
		static extern int discard_buffer (int fd, bool inputBuffer);

		public void DiscardInBuffer ()
		{
			if (discard_buffer (fds[0].fd, true) != 0)
				UnixMarshal.ThrowExceptionForLastError();
		}

		public void DiscardOutBuffer ()
		{
			if (discard_buffer (fds[0].fd, false) != 0)
				UnixMarshal.ThrowExceptionForLastError();
		}
		
		[DllImport ("MonoPosixHelper", SetLastError = true)]
		static extern SerialSignal get_signals (int fd, out int error);

		public SerialSignal GetSignals ()
		{
			int error;
			SerialSignal signals = get_signals (fds[0].fd, out error);
			UnixMarshal.ThrowExceptionForLastErrorIf(error);

			return signals;
		}

		[DllImport ("MonoPosixHelper", SetLastError = true)]
		static extern int set_signal (int fd, SerialSignal signal, bool value);

		public void SetSignal (SerialSignal signal, bool value)
		{
			if (signal < SerialSignal.Cd || signal > SerialSignal.Rts ||
					signal == SerialSignal.Cd ||
					signal == SerialSignal.Cts ||
					signal == SerialSignal.Dsr)
				throw new Exception ("Invalid internal value");

			if (set_signal (fds[0].fd, signal, value) == -1)
				UnixMarshal.ThrowExceptionForLastError();
		}

		[DllImport ("MonoPosixHelper", SetLastError = true)]
		static extern int breakprop (int fd);

		public void SetBreakState (bool value)
		{
			if (value)
				if (breakprop (fds[0].fd) == -1)
					UnixMarshal.ThrowExceptionForLastError();
		}
		
		[DllImport ("MonoPosixHelper")]
		static extern bool is_baud_rate_legal (int baud_rate);
		
		private void TryBaudRate (int baudRate)
		{
			if (!is_baud_rate_legal (baudRate))
			{
				// this kind of exception to be compatible with MSDN API
				throw new ArgumentOutOfRangeException ("baudRate",
					"Given baud rate is not supported on this platform.");
			}
		}
	}
}


