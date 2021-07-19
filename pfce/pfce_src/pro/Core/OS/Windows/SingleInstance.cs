using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace Peach.Pro.Core.OS.Windows
{
	internal class SingleInstanceImpl : ISingleInstance
	{
		private static readonly Regex SanitizerRegex = new Regex(@"[\\/]");

		private readonly Mutex _mutex;
		private bool _locked;
		private bool _disposed;

		public SingleInstanceImpl(string name)
		{
			_mutex = new Mutex(false, "Global\\" + SanitizerRegex.Replace(name, "_"));
		}

		public void Dispose()
		{
			lock (_mutex)
			{
				if (_disposed)
					return;

				if (_locked)
				{
					_mutex.ReleaseMutex();
					_locked = false;
				}

				_mutex.Dispose();
				_disposed = true;
			}
		}

		public bool TryLock()
		{
			lock (_mutex)
			{
				if (_disposed)
					throw new ObjectDisposedException("SingleInstanceImpl");

				if (_locked)
					return true;

				try
				{
					_locked = _mutex.WaitOne(0);
					return _locked;
				}
				catch (AbandonedMutexException)
				{
					return TryLock();
				}
			}
		}

		public void Lock()
		{
			lock (_mutex)
			{
				if (_disposed)
					throw new ObjectDisposedException("SingleInstanceImpl");

				if (_locked)
					return;

				try
				{
					_mutex.WaitOne();
					_locked = true;
				}
				catch (AbandonedMutexException)
				{
					Lock();
				}
			}
		}
	}
}
