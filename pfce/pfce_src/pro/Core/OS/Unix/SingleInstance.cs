using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Mono.Unix;
using Mono.Unix.Native;

namespace Peach.Pro.Core.OS.Unix
{
	internal class SingleInstanceImpl : ISingleInstance
	{
		private static readonly Regex SanitizerRegex = new Regex(@"[\\/]");

		private readonly string _name;

		private int _fd;
		private bool _locked;

		[DllImport("libc", SetLastError = true)]
		private static extern int flock(int fd, int op);

		// ReSharper disable InconsistentNaming
		private const int LOCK_EX = 0x0002;
		private const int LOCK_NB = 0x0004;
		private const int LOCK_UN = 0x0008;
		// ReSharper restore InconsistentNaming

		public SingleInstanceImpl(string name)
		{
			_name = Path.Combine(Path.GetTempPath(), SanitizerRegex.Replace(name, "_"));
			_fd = Syscall.open(_name, OpenFlags.O_RDWR | OpenFlags.O_CREAT, FilePermissions.DEFFILEMODE);
			UnixMarshal.ThrowExceptionForLastErrorIf(_fd);
		}

		public void Dispose()
		{
			lock (_name)
			{
				if (_fd == -1)
					return;

				if (_locked)
				{
					Syscall.unlink(_name);

					flock(_fd, LOCK_UN);
					_locked = false;
				}

				Syscall.close(_fd);
				_fd = -1;
			}
		}

		public bool TryLock()
		{
			lock (_name)
			{
				if (_fd == -1)
					throw new ObjectDisposedException("SingleInstanceImpl");

				if (_locked)
					return true;

				var ret = flock(_fd, LOCK_EX | LOCK_NB);

				if (ret == -1)
				{
					var errno = Stdlib.GetLastError();

					if (errno != Errno.EWOULDBLOCK)
						UnixMarshal.ThrowExceptionForError(errno);

					_locked = false;
				}
				else
				{
					_locked = true;
				}

				return _locked;
			}
		}

		public void Lock()
		{
			lock (_name)
			{
				if (_fd == -1)
					throw new ObjectDisposedException("SingleInstanceImpl");

				if (_locked)
					return;

				while (true)
				{
					var ret = flock(_fd, LOCK_EX);

					Errno error;
					if (UnixMarshal.ShouldRetrySyscall(ret, out error))
						continue;

					UnixMarshal.ThrowExceptionForErrorIf(ret, error);

					_locked = true;
					return;
				}
			}
		}
	}
}
