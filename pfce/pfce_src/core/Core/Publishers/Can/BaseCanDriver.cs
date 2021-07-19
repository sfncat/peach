using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using NLog;

namespace Peach.Core.Publishers.Can
{
	/// <summary>
	/// Base class for CAN Drivers.  Provides some skafolding for
	/// patterns same across all drivers.
	/// </summary>
	public abstract class BaseCanDriver : ICanDriver, ICanInterface
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Store registered event handlers with a CAN ID filter
		/// </summary>
		private readonly ConcurrentDictionary<uint, HashSet<CanRxEventHandler>> _canFrameReceivedHandlers = new ConcurrentDictionary<uint, HashSet<CanRxEventHandler>>();

		private readonly HashSet<CanRxEventHandler> _canFrameErrorReceivedHandlers = new HashSet<CanRxEventHandler>();

		private readonly BlockingCollection<CanFrame> _notifyQueue = new BlockingCollection<CanFrame>(new ConcurrentQueue<CanFrame>());
		private readonly ConcurrentQueue<CanFrame> _rxFrameQueue = new ConcurrentQueue<CanFrame>();
		private readonly ConcurrentQueue<Tuple<DateTime, string, Exception>> _rxLogQueue = new ConcurrentQueue<Tuple<DateTime,string, Exception>>();

		private int _openCount = 0;
		private Thread _notifyThread;
		private readonly Object _lock = new Object();

		private bool _isCapturing = false;
		private readonly Object _lockCapturing = new Object();
		private readonly List<CanFrame> _capture = new List<CanFrame>();

		#region ICanDriver

		public abstract string Name { get; }
		public abstract IEnumerable<ParameterAttribute> Parameters { get; }
		public abstract ICanInterface CreateInterface(Dictionary<string, Variant> args);

		#endregion

		#region ICanInterface

		public abstract ICanDriver Driver { get; }
		public abstract IEnumerable<ICanChannel> Channels { get; }
		public abstract bool IsOpen { get; protected set; }
		public Dictionary<uint,string> MonitorFrameIds { get; }

		protected BaseCanDriver()
		{
			MonitorFrameIds = new Dictionary<uint, string>();
		}

		public void ValidateTxId(uint id)
		{
			if (!MonitorFrameIds.ContainsKey(id))
				return;

			var msg = string.Format("Error, monitor '{0}' configured with frame ID matching fuzzed frame ID '0x{1:X}'.",
				MonitorFrameIds[id], id);

			Logger.Error(msg);
			throw new SoftException(msg);
		}

		public IEnumerable<CanFrame> Capture
		{
			get
			{
				lock (_lockCapturing)
				{
					return _capture.ToArray();
				}
			}
		}

		public void Open()
		{
			lock (_lock)
			{
				StopCapture();
				ClearCapture();

				if (_notifyThread == null)
				{
					_notifyThread = new Thread(() =>
					{
						HashSet<CanRxEventHandler> handlers;

						while (true)
						{
							var msg = _notifyQueue.Take();

							// Notify interested parties based on id filter

							if (!_canFrameReceivedHandlers.TryGetValue(msg.Identifier, out handlers))
								continue;

							lock (handlers)
							{
								handlers.ForEach(x => x(this, msg));
							}

							// If frame has error flag set, notify interested parties

							if (msg.IsError)
							{
								lock (_canFrameErrorReceivedHandlers)
								{
									_canFrameErrorReceivedHandlers.ForEach(x => x(this, msg));
								}
							}

							// If we are capturing, add to captured frames

							if (_isCapturing && msg.Channel.Capturing)
							{
								lock (_lockCapturing)
								{
									_capture.Add(msg);
								}
							}
						}
					});

					_notifyThread.Start();
				}

				if (_openCount < 0)
					_openCount = 0;

				if (_openCount == 0)
				{
					Logger.Trace("Opening can driver, open count is zero");
					OpenImpl();
				}
				else
				{
					Logger.Trace("Not opening can driver, open count is {0}", _openCount);
				}

				_openCount++;
			}
		}

		/// <summary>
		/// Children must implement this method instead of Open()
		/// </summary>
		protected abstract void OpenImpl();

		public void Close()
		{
			lock (_lock)
			{
				_openCount--;

				if (_openCount == 0)
				{
					Logger.Trace("Closing can driver, open count reached zero");
					CloseImpl();

					_notifyThread.Abort();
					_notifyThread = null;

					_canFrameReceivedHandlers.Clear();

					MonitorFrameIds.Clear();
				}
				else if (_openCount < 0)
				{
					Logger.Warn("Close called more than open.  Count is '{0}'.", _openCount);
				}
				else
				{
					Logger.Trace("Not closing can driver, open count is {0}", _openCount);
				}
			}

			StopCapture();
			ClearCapture();
		}

		/// <summary>
		/// Children must implement this method instead of Close()
		/// </summary>
		protected abstract void CloseImpl();

		public CanFrame ReadMessage()
		{
			if (!IsOpen)
				throw new ApplicationException("Error, CAN interface not open");

			CanFrame msg;
			return _rxFrameQueue.TryDequeue(out msg) ? msg : null;
		}

		public abstract void WriteMessage(ICanChannel txChannel, CanFrame frame);

		public Tuple<DateTime, string, Exception> GetLogMessage()
		{
			Tuple<DateTime, string, Exception> log;

			if (_rxLogQueue.TryDequeue(out log))
				return log;

			return null;
		}

		public void StartCapture()
		{
			_isCapturing = true;
		}

		public void StopCapture()
		{
			_isCapturing = false;
		}

		public void ClearCapture()
		{
			lock (_lockCapturing)
			{
				_capture.Clear();
			}
		}

		#endregion

		/// <summary>
		/// Add a log message to the log message queue
		/// </summary>
		/// <param name="when">When message occured</param>
		/// <param name="msg">Log message</param>
		/// <param name="e">Optional exception</param>
		protected void AddLogMessage(DateTime when, string msg, Exception e = null)
		{
			_rxLogQueue.Enqueue(new Tuple<DateTime, string, Exception>(when, msg, e));
		}

		/// <summary>
		/// Notify any registered handlers for this specific CAN ID.
		/// Notifications occur in a Task to not block
		/// </summary>
		/// <param name="msg"></param>
		protected void NotifyCanFrameReceivedHandlers(CanFrame msg)
		{
			_notifyQueue.Add(msg);
			_rxFrameQueue.Enqueue(msg);
			if (_isCapturing)
			{
				lock (_lockCapturing)
				{
					_capture.Add(msg);
				}
			}
		}

		/// <inheritdoc />
		public void RegisterCanFrameErrorReceiveHandler(CanRxEventHandler handler)
		{
			lock (_canFrameErrorReceivedHandlers)
			{
				_canFrameErrorReceivedHandlers.Add(handler);
			}
		}

		/// <inheritdoc />
		public void UnRegisterCanFrameErrorReceiveHandler(CanRxEventHandler handler)
		{
			lock (_canFrameErrorReceivedHandlers)
			{
				_canFrameErrorReceivedHandlers.Remove(handler);
			}
		}

		/// <inheritdoc />
		public void RegisterCanFrameReceiveHandler(uint id, CanRxEventHandler handler)
		{
			HashSet<CanRxEventHandler> handlers;
			lock (_canFrameReceivedHandlers)
			{
				if (!_canFrameReceivedHandlers.ContainsKey(id))
				{
					handlers = new HashSet<CanRxEventHandler>();
					_canFrameReceivedHandlers[id] = handlers;
				}
				else
				{
					handlers = _canFrameReceivedHandlers[id];
				}

				lock (handlers)
				{
					handlers.Add(handler);
				}
			}
		}

		/// <inheritdoc />
		public void RegisterCanFrameReceiveHandler(uint[] ids, CanRxEventHandler handler)
		{
			foreach (var id in ids)
			{
				RegisterCanFrameReceiveHandler(id, handler);
			}
		}

		/// <inheritdoc />
		public void UnRegisterCanFrameReceiveHandler(uint id, CanRxEventHandler handler)
		{
			lock (_canFrameReceivedHandlers)
			{
				if (!_canFrameReceivedHandlers.ContainsKey(id))
					return;

				_canFrameReceivedHandlers[id].Remove(handler);
			}
		}

		/// <inheritdoc />
		public void UnRegisterCanFrameReceiveHandler(uint[] ids, CanRxEventHandler handler)
		{
			foreach (var id in ids)
			{
				UnRegisterCanFrameReceiveHandler(id, handler);
			}
		}

		/// <summary>
		/// Convert unix timestamp to DateTime
		/// </summary>
		/// <param name="unixTimeStamp"></param>
		/// <returns></returns>
		public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
		{
			// Unix timestamp is seconds past epoch
			var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp*0.000001).ToLocalTime();
			return dtDateTime;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
