using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Threading;
using NLog;
using Peach.Core;
using Peach.Core.IO;
using Peach.Pro.Core.OS;

namespace Peach.Pro.Core.Publishers
{
	[Publisher("SerialPort")]
	[Parameter("PortName", typeof(string), "Com interface for the device to connect to")]
	[Parameter("Baudrate", typeof(int), "The serial baud rate.")]
	[Parameter("Parity", typeof(Parity), "The parity-checking protocol.")]
	[Parameter("DataBits", typeof(int), "Standard length of data bits per byte.")]
	[Parameter("StopBits", typeof(StopBits), "The standard number of stopbits per byte.")]
	[Parameter("Handshake", typeof(Handshake), "The handshaking protocol for serial port transmission of data.", "None")]
	[Parameter("DtrEnable", typeof(bool), "Enables the Data Terminal Ready (DTR) signal during serial communication.", "false")]
	[Parameter("RtsEnable", typeof(bool), "Enables the Request To Transmit (RTS) signal during serial communication.", "false")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data (default 3000)", "3000")]
	public class SerialPortPublisher : Publisher
	{
		private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		public string PortName { get; protected set; }
		public int Baudrate { get; protected set; }
		public Parity Parity { get; protected set; }
		public int DataBits { get; protected set; }
		public StopBits StopBits { get; protected set; }
		public Handshake Handshake { get; protected set; }
		public bool DtrEnable { get; protected set; }
		public bool RtsEnable { get; protected set; }
		public int Timeout { get; set; }

		private const int BufferSize = 1024;

		private readonly MemoryStream _stream = new MemoryStream();

		private bool _firstWrite;
		private bool _timeout;
		private ISerialStream _serial;
		private Thread _thread;

		public SerialPortPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override void OnOpen()
		{
			Debug.Assert(_serial == null);
			Debug.Assert(_thread == null);
			Debug.Assert(_stream.Position == 0);
			Debug.Assert(_stream.Length == 0);

			var maxRetryDelay = TimeSpan.FromMilliseconds(1000);
			var maxTotalDelay = TimeSpan.FromMilliseconds(Timeout);

			try
			{
				Retry.TimedBackoff(maxRetryDelay, maxTotalDelay, () =>
				{
					// Note: specify infinite read timeout, since we implement the timeout via WantBytes()
					_serial = Pal.OpenSerial(PortName, Baudrate, DataBits, Parity, StopBits, DtrEnable,
						RtsEnable, Handshake, SerialPort.InfiniteTimeout, Timeout, BufferSize, BufferSize);
				});
			}
			catch (Exception ex)
			{
				var msg = "Unable to open Serial Port {0}. {1}.".Fmt(PortName, ex.Message);
				Logger.Debug(msg);

				// Always throw SoftExceptions
				throw new SoftException(msg, ex);
			}

			_thread = new Thread(ReadToEnd);

			lock (_stream)
			{
				_thread.Start();
				Monitor.Wait(_stream);
			}

			_firstWrite = true;

			Logger.Debug("Opened {0}", PortName);
		}

		protected override void OnClose()
		{
			try
			{
				lock (_stream)
				{
					if (_serial == null)
						return;

					Logger.Debug("Closing {0}", PortName);
					_serial.Close();
					_serial = null;
				}
			}
			catch (Exception ex)
			{
				Logger.Debug("Failed to close {0}: {1}", PortName, ex.Message);
			}
			finally
			{
				_thread.Join();
				_thread = null;

				_stream.Position = 0;
				_stream.SetLength(0);
			}
		}

		protected override void OnOutput(BitwiseStream data)
		{
			ISerialStream serial;

			lock (_stream)
			{
				serial = _serial;
			}

			if (serial == null)
				throw new SoftException("Can't write to {0}, the port is closed.".Fmt(PortName));

			if (Logger.IsDebugEnabled)
				Logger.Debug("\n\n" + Utilities.HexDump(data));

			try
			{
				var buf = new byte[BufferSize];
				int len;

				while ((len = data.Read(buf, 0, buf.Length)) != 0)
				{
					if (_firstWrite)
					{
						_firstWrite = true;

						var writeLen = len;
						var maxRetryDelay = TimeSpan.FromMilliseconds(1000);
						var maxTotalDelay = TimeSpan.FromMilliseconds(Timeout);
						TimeoutException caught = null;

						Retry.TimedBackoff(maxRetryDelay, maxTotalDelay, () =>
						{
							try
							{
								serial.Write(buf, 0, writeLen);
							}
							catch (TimeoutException ex)
							{
								caught = ex;
							}
						});

						if (caught != null)
							throw new TimeoutException(caught.Message, caught);
					}
					else
					{
						serial.Write(buf, 0, len);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Debug("Failed to write to {0}: {1}", PortName, ex.Message);

				throw new SoftException(ex);
			}
		}

		protected override void OnInput()
		{
			// Try to make sure 1 byte is available for reading.  Without doing this,
			// state models with an initial state of input can miss the message.
			// Also, ensure the read timeout is reset on every input action.
			_timeout = false;
			WantBytes(1);
		}

		public override void WantBytes(long count)
		{
			if (count == 0)
				return;

			var sw = Stopwatch.StartNew();

			lock (_stream)
			{
				while (true)
				{
					if ((_stream.Length - _stream.Position) >= count || _timeout)
						return;

					// If the port has been closed, we are not going to get anymore bytes.
					if (_serial == null)
						return;

					var remain = Timeout - sw.ElapsedMilliseconds;
					if (remain > 0 && Monitor.Wait(_stream, (int) remain))
						continue;

					Logger.Debug("WantBytes({0}): Timeout waiting for data on {1}", count, PortName);
					_timeout = true;
					return;
				}
			}
		}

		private void ReadToEnd()
		{
			ISerialStream serial;

			lock (_stream)
			{
				serial = _serial;
				Monitor.Pulse(_stream);
			}

			try
			{
				var offset = (long)0;
				var buf = new byte[BufferSize];

				while (true)
				{
					var len = serial.Read(buf, 0, buf.Length);
					if (len == 0)
					{
						Logger.Debug("Finished reading from {0}", PortName);
						return;
					}

					if (Logger.IsDebugEnabled)
						Logger.Debug("\n\n" + Utilities.HexDump(buf, 0, len, startAddress: offset));

					offset += len;

					lock (_stream)
					{
						var pos = _stream.Position;

						_stream.Seek(0, SeekOrigin.End);
						_stream.Write(buf, 0, len);
						_stream.Seek(pos, SeekOrigin.Begin);

						_timeout = false;

						Monitor.Pulse(_stream);
					}
				}
			}
			catch (ObjectDisposedException)
			{
				Logger.Debug("Finished reading from {0}: object disposed", PortName);
			}
			catch (Exception ex)
			{
				Logger.Debug("Error reading from {0}: {1}", PortName, ex.Message);
			}
			finally
			{
				lock (_stream)
				{
					if (_serial != null)
					{
						try
						{
							Logger.Debug("Closing {0}", PortName);
							_serial.Close();
						}
						catch (Exception ex)
						{
							Logger.Debug("Failed to close {0}: {1}", PortName, ex.Message);
						}
						finally
						{
							_serial = null;
						}
					}

					Monitor.Pulse(_stream);
				}
			}
		}

		#region Stream

		public override bool CanRead
		{
			get { return true; }
		}

		public override bool CanSeek
		{
			get { return true; }
		}

		public override long Length
		{
			get
			{
				lock (_stream)
				{
					return _stream.Length;
				}
			}
		}

		public override long Position
		{
			get
			{
				lock (_stream)
				{
					return _stream.Position;
				}
			}
			set
			{
				lock (_stream)
				{
					_stream.Position = value;
				}
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			lock (_stream)
			{
				return _stream.Read(buffer, offset, count);
			}
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			lock (_stream)
			{
				return _stream.Seek(offset, origin);
			}
		}

		#endregion
	}
}
