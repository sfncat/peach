using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Mono.Unix;
using Mono.Unix.Native;
using NLog;
using Peach.Core;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.OS.Linux.Publishers
{
	[Publisher("Bluetooth", Scope = PluginScope.Internal)]
	[Description("Bluetooth HCI Socket")]
	[Parameter("Interface", typeof(string), "Name of interface to bind to")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default 3000)", "10000")]
	public class BluetoothPublisher : Publisher
	{
		#region P/Invokes

		internal class NativeMethods
		{
			[StructLayout(LayoutKind.Sequential)]
			public struct sockaddr_hci
			{
				public ushort hci_family;
				public ushort hci_dev;
				public ushort hci_channel;
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct hci_filter
			{
				public UInt32 type_mask;
				[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
				public UInt32[] event_mask;
				public UInt16 opcode;
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct msghdr
			{
				public IntPtr msg_name;
				public int msg_namelen;
				public IntPtr msg_iov;
				public int msg_iovlen;
				public IntPtr msg_control;
				public int msg_controllen;
				public uint msg_flags;
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct iovec
			{
				[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
				public byte[] iov_base;
				public int iov_len;
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct cmsghdr
			{
				public UIntPtr cmsg_len;
				public int cmsg_level;
				public int cmsg_type;
			}

			[DllImport("libc", SetLastError = true)]
			public static extern SocketSafeHandle socket(int domain, int type, int protocol);

			[DllImport("libc", SetLastError = true)]
			public static extern int bind(SocketSafeHandle fd, ref sockaddr_hci addr, int addrlen);

			[DllImport("libc", SetLastError = true)]
			public static extern int if_nametoindex(string ifname);

			[DllImport("libc", SetLastError = true)]
			public static extern int setsockopt(
				SocketSafeHandle s, int level, int optname, 
				ref int opt, int optlen);

			[DllImport("libc", SetLastError = true)]
			public static extern int setsockopt(
				SocketSafeHandle s, int level, int optname,
				[In, MarshalAs(UnmanagedType.LPArray)] ref hci_filter buf, int optlen);

			[DllImport("libc", SetLastError = true)]
			public static extern int recvfrom(
				SocketSafeHandle sockfd, [In,Out, MarshalAs(UnmanagedType.LPArray)] ref byte[] buf,
				int len, int flags, ref sockaddr_hci src_addr, int addrlen);

			[DllImport("libc", SetLastError = true)]
			public static extern int recv(
				SocketSafeHandle sockfd, byte[] buf, int len, int flags);

			[DllImport("libc", SetLastError = true)]
			public static extern int recvmsg(SocketSafeHandle sockfd,
				ref msghdr msg, int flags);

			[DllImport("libc", SetLastError = true)]
			public static extern int send(
				SocketSafeHandle sockfd, [Out, MarshalAs(UnmanagedType.LPArray)] byte[] buf,
				int len, int flags);

			[DllImport("libc", SetLastError = true)]
			public static extern int close(int fd);

			[DllImport("libc", SetLastError = true)]
			public extern static int ioctl(SocketSafeHandle fd, int request, int data);
		}

		internal class SocketSafeHandle : SafeHandleMinusOneIsInvalid
		{
			private SocketSafeHandle()
				: base(true)
			{
			}

			protected override bool ReleaseHandle()
			{
				int ret = NativeMethods.close((int)handle);
				UnixMarshal.ThrowExceptionForLastErrorIf(ret);
				return true;
			}
		}

		#endregion

		public const int AF_PACKET = 17;
		public const int SOCK_RAW = 3;
		public const int SIOCGIFMTU = 0x8921;
		public const int SIOCSIFMTU = 0x8922;

		public const int BTPROTO_L2CAP = 0;
		public const int BTPROTO_HCI = 1;
		public const int BTPROTO_SCO = 2;
		public const int BTPROTO_RFCOMM = 3;
		public const int BTPROTO_BNEP = 4;
		public const int BTPROTO_CMTP = 5;
		public const int BTPROTO_HIDP = 6;
		public const int BTPROTO_AVDTP = 7;
		public const int AF_BLUETOOTH = 31;      /* Bluetooth sockets            */
		public const int PF_BLUETOOTH = 31;
		public const int SOL_HCI = 0;

		public const int HCI_DATA_DIR = 1;
		public const int HCI_FILTER = 2;
		public const int HCI_TIME_STAMP = 3;

		public const int HCI_CHANNEL_RAW = 0;
		public const int HCI_CHANNEL_USER = 1;
		public const int HCI_CHANNEL_MONITOR = 2;
		public const int HCI_CHANNEL_CONTROL = 3;

		public const int HCI_MAX_ACL_SIZE       = 1024;
		public const int HCI_MAX_SCO_SIZE      =  255;
		public const int HCI_MAX_EVENT_SIZE    =  260;
		public const int HCI_MAX_FRAME_SIZE    =  (HCI_MAX_ACL_SIZE + 4);
		public const int HCI_LINK_KEY_SIZE      = 16;
		public const int HCI_AMP_LINK_KEY_SIZE  = (2 * HCI_LINK_KEY_SIZE);
		public const int HCI_MAX_AMP_ASSOC_SIZE  =672;
		public const int HCI_MAX_CSB_DATA_SIZE  = 252;

		public const int HCISETRAW = 1074022620;

		public string Interface { get; protected set; }
		public int Timeout { get; protected set; }

		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		private MemoryStream _recvBuffer;
		private int _bufferSize = HCI_MAX_FRAME_SIZE*2;
		private SocketSafeHandle _socket = null;

		private Thread _worker = null;
		private bool _workerExit = false;
		private Exception _workerException = null;
		private AutoResetEvent _workerHasDataEvent = new AutoResetEvent(false);


		public BluetoothPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			_recvBuffer = new MemoryStream(_bufferSize);
		}

		protected override void OnStart()
		{
			if (Platform.GetOS() != Platform.OS.Linux)
				throw new PeachException("The RawEther publisher only works on linux.");
		}

		protected override void OnOpen()
		{
			System.Diagnostics.Debug.Assert(_socket == null);

			_socket = OpenSocket();

			_workerException = null;
			_workerExit = false;
			_worker = new Thread(new ThreadStart(WorkerRead));
			_worker.Start();

			System.Diagnostics.Debug.Assert(_socket != null);

			Logger.Debug("Opened interface \"{0}\" with MTU {1}.", Interface, _bufferSize);
		}

		private SocketSafeHandle OpenSocket()
		{
			int ret = -1;

			try
			{
				var fd = NativeMethods.socket(AF_BLUETOOTH, SOCK_RAW, BTPROTO_HCI);
				UnixMarshal.ThrowExceptionForLastErrorIf((int)fd.DangerousGetHandle());

				// data direction info
				int optval = 1;
				ret = NativeMethods.setsockopt(fd, SOL_HCI, HCI_DATA_DIR, ref optval, Marshal.SizeOf(optval));
				UnixMarshal.ThrowExceptionForLastErrorIf(ret);

				// time stamp
				optval = 1;
				ret = NativeMethods.setsockopt(fd, SOL_HCI, HCI_TIME_STAMP, ref optval, Marshal.SizeOf(optval));
				UnixMarshal.ThrowExceptionForLastErrorIf(ret);

				var filter = new NativeMethods.hci_filter()
				{
					type_mask = 0xffffffff,
					event_mask = new UInt32[] { 0xffffffff, 0xffffffff },
					opcode = 0
				};

				ret = NativeMethods.setsockopt(fd, SOL_HCI, HCI_FILTER,
					ref filter, Marshal.SizeOf(filter));
				UnixMarshal.ThrowExceptionForLastErrorIf(ret);

				var sa = new NativeMethods.sockaddr_hci()
				{
					hci_family = AF_BLUETOOTH,
					hci_dev = 0,
					hci_channel = HCI_CHANNEL_RAW
				};

				ret = NativeMethods.bind(fd, ref sa, Marshal.SizeOf(sa));
				UnixMarshal.ThrowExceptionForLastErrorIf(ret);

				ret = NativeMethods.ioctl(fd, HCISETRAW, 1);
				UnixMarshal.ThrowExceptionForLastErrorIf(ret);

				return fd;
			}
			catch (InvalidOperationException ex)
			{
				if (ex.InnerException != null)
				{
					var inner = ex.InnerException as UnixIOException;
					if (inner != null && inner.ErrorCode == Errno.EPERM)
						throw new PeachException("Access denied when opening the raw bluetooth publisher.  Ensure the user has the appropriate permissions.", ex);
				}

				throw;
			}
		}

		private void WorkerRead()
		{
			System.Diagnostics.Debug.Assert(_socket != null);

			byte[] buf = new byte[HCI_MAX_FRAME_SIZE];
			int snap_len = HCI_MAX_FRAME_SIZE;

			while (!_workerExit)
			{
				try
				{
					var ret = NativeMethods.recv(_socket, buf, buf.Length, 0);
					UnixMarshal.ThrowExceptionForLastErrorIf(ret);

					// disconnect
					if (ret == 0)
					{
						_workerException = new SoftException("Bluebooth device disconnected.");
						return;
					}

					if (Logger.IsDebugEnabled)
						Logger.Debug("\n\nRead:\n" + Utilities.HexDump(new MemoryStream(buf, 0, ret)));

					var pos = _recvBuffer.Position;
					_recvBuffer.Write(buf, 0, ret);
					_recvBuffer.Position = pos;

					var msg = new NativeMethods.msghdr();
					var cmsg = new NativeMethods.cmsghdr();
					var iv = new NativeMethods.iovec();

					iv.iov_base = new byte[snap_len/2];
					iv.iov_len = snap_len;

					msg.msg_iovlen = Marshal.SizeOf(iv);
					msg.msg_iov = Marshal.AllocHGlobal(msg.msg_iovlen);
					Marshal.StructureToPtr(iv, msg.msg_iov, false);

					msg.msg_controllen = Marshal.SizeOf(cmsg);
					msg.msg_control = Marshal.AllocHGlobal(msg.msg_controllen);
					Marshal.StructureToPtr(cmsg, msg.msg_control, false);

					ret = NativeMethods.recvmsg(_socket, ref msg, 0);
					UnixMarshal.ThrowExceptionForLastErrorIf(ret);

					_workerHasDataEvent.Set();

					if (Logger.IsDebugEnabled)
						Logger.Debug("\n\nRead:\n" + Utilities.HexDump(new MemoryStream(buf, 0, ret)));

					return;
				}
				catch (Exception ex)
				{
					_workerException = new SoftException(ex);
					return;
				}
			}
		}

		protected override void OnClose()
		{
			//this never happens....
			System.Diagnostics.Debug.Assert(_socket != null);

			_workerExit = true;
			_worker.Join();

			// disable raw sockets
			NativeMethods.ioctl(_socket, HCISETRAW, 0);

			_socket.Close();
			_socket = null;
		}

		protected override void OnStop()
		{
		}

		protected override void OnInput()
		{
			if (_workerException != null)
				throw new SoftException("Error reading data from bluetooth device.", _workerException);

			logger.Debug("Position: " + _recvBuffer.Position + " Length: " + _recvBuffer.Length);

			if (_recvBuffer.Position < _recvBuffer.Length)
				return;

			if (!_workerHasDataEvent.WaitOne(Timeout))
				throw new SoftException("Bluetooth publisher timed out reading data.");
		}

		protected override void OnOutput(BitwiseStream data)
		{
			if (Logger.IsDebugEnabled)
				Logger.Debug("\n\n" + Utilities.HexDump(data));

			long count = data.Length;
			var buffer = new byte[count];
			int size = data.Read(buffer, 0, buffer.Length);

			Pollfd[] fds = new Pollfd[1];
			fds[0].fd = (int)_socket.DangerousGetHandle();
			fds[0].events = PollEvents.POLLOUT;

			int expires = Environment.TickCount + Timeout;
			int wait = 0;

			for (; ; )
			{
				try
				{
					wait = Math.Max(0, expires - Environment.TickCount);
					fds[0].revents = 0;

					int ret = Syscall.poll(fds, wait);

					if (UnixMarshal.ShouldRetrySyscall(ret))
						continue;

					UnixMarshal.ThrowExceptionForLastErrorIf(ret);

					if (ret == 0)
						throw new TimeoutException();

					if (ret != 1 || (fds[0].revents & PollEvents.POLLOUT) == 0)
						continue;

					NativeMethods.send(_socket, buffer, size, 0);

					if (count != size)
						throw new Exception(string.Format("Only sent {0} of {1} byte packet.", size, count));

					return;
				}
				catch (Exception ex)
				{
					if (ex is TimeoutException)
						Logger.Debug("Bluetooth packet not sent to {0} in {1}ms, timing out.", Interface, Timeout);
					else
						Logger.Error("Unable to read bluetooth packet from {0}. {1}", Interface, ex.Message);

					throw new SoftException(ex);
				}
			}
		}

		#region Read Stream

		public override bool CanRead
		{
			get { return _recvBuffer.CanRead; }
		}

		public override bool CanSeek
		{
			get { return _recvBuffer.CanSeek; }
		}

		public override long Length
		{
			get { return _recvBuffer.Length; }
		}

		public override long Position
		{
			get { return _recvBuffer.Position; }
			set { _recvBuffer.Position = value; }
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _recvBuffer.Seek(offset, origin);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _recvBuffer.Read(buffer, offset, count);
		}

		#endregion
	}
}
