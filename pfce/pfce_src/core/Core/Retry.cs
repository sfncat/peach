using System.Diagnostics;
using NLog;
using System;
using System.Threading;

namespace Peach.Core
{
	public class Retry
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(0.5);
		public const int DefaultRetryCount = 3;

		public static void Execute(Action fn, int retryCount = DefaultRetryCount)
		{
			Execute(fn, DefaultRetryDelay, retryCount);
		}

		public static void Execute(Action fn, TimeSpan retryDelay, int retryCount = DefaultRetryCount)
		{
			int count = 0;
			while (true)
			{
				try
				{
					fn();
					break;
				}
				catch (Exception ex)
				{
					if (count++ == retryCount)
						throw;

					Logger.Trace("Retrying in {0}ms after error: {1}", 
						retryDelay.TotalMilliseconds, 
						ex.Message);
		
					Thread.Sleep(retryDelay);
				}
			}
		}

		public static void Backoff(TimeSpan maxDelay, int retryCount, Action fn)
		{
			int count = 0;
			var retryDelay = TimeSpan.FromMilliseconds(1);
			while (true)
			{
				try
				{
					fn();
					break;
				}
				catch (Exception ex)
				{
					if (count++ == retryCount)
						throw;

					Logger.Trace("Retrying in {0}ms after error: {1}", 
						retryDelay.TotalMilliseconds,
						ex.Message);

					Thread.Sleep(retryDelay);
		
					retryDelay = retryDelay.Add(retryDelay);
					if (retryDelay.CompareTo(maxDelay) >= 0)
						retryDelay = maxDelay;
				}
			}
		}

		public static void TimedBackoff(bool retry, TimeSpan maxRetryDelay, TimeSpan maxTotalDelay, Action fn)
		{
			TimedBackoff(maxRetryDelay, maxTotalDelay, fn, retry);
		}

		public static void TimedBackoff(TimeSpan maxRetryDelay, TimeSpan maxTotalDelay, Action fn, bool retry = true)
		{
			var retryDelay = TimeSpan.FromMilliseconds(1);
			var sw = Stopwatch.StartNew();

			while (true)
			{
				try
				{
					fn();
					break;
				}
				catch (Exception ex)
				{
					var remain = maxTotalDelay - sw.Elapsed;

					if (remain <= TimeSpan.Zero || !retry)
						throw;

					if (retryDelay.CompareTo(remain) >= 0)
						retryDelay = remain;

					Logger.Trace("Retrying in {0}ms after error: {1}",
						retryDelay.TotalMilliseconds,
						ex.Message);

					Thread.Sleep(retryDelay);

					retryDelay = retryDelay.Add(retryDelay);
					if (retryDelay.CompareTo(maxTotalDelay) >= 0)
						retryDelay = maxTotalDelay;
				}
			}
		}
	}
}
