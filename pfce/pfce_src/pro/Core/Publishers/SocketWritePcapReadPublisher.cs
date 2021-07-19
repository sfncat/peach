using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NLog;
using Peach.Core;

#if DISABLED

namespace Peach.Pro.Core.Publishers
{
	/// <summary>
	/// Base class for publishers that will write data over a Stream interface, but read data via
	/// Pcap capture device.
	/// </summary>
	/// <remarks>
	/// Publishers based on this interface can set or expose a Filter parameter to restrict the
	/// packets accepted as input.
	/// 
	/// Inheritors should also implement the TryInterpretData method to extract data from the 
	/// captured packet that will be returned. The PcapListener class has several static helper
	/// methods. The PacketDotNet set of classes can also be of use.
	/// </remarks>
	public abstract class SocketWritePcapReadPublisher : SocketPublisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		PcapListener _listener;

		public SocketWritePcapReadPublisher(string name, Dictionary<string, Variant> args)
			: base(name, args)
		{
		}

		public string Filter { get; set; }

		protected override void OnStart()
		{
			_listener = new PcapListener(Interface);
			_listener.Start(Filter);
			base.OnStart();
		}

		protected override void OnStop()
		{
			_listener.Stop();
			base.OnStop();
		}

		protected override void OnOpen()
		{
			_listener.Clear();
			base.OnOpen();
		}

		protected override void OnInput()
		{
			SharpPcap.RawCapture capture;
			byte[] data;

			DateTime readStart = DateTime.Now;
			while(!_listener.PacketQueue.TryDequeue(out capture) && (DateTime.Now - readStart).TotalMilliseconds < Timeout)
				Thread.Sleep(100);

			if (capture == null)
				throw new SoftException("Timeout waiting for input from interface '" + Interface + "'.");

			if (!TryInterpretData(capture, out data))
			{
				_recvBuffer.SetLength(0);
				return;
			}

			_recvBuffer = new MemoryStream(data);
		}

		protected virtual bool TryInterpretData(SharpPcap.RawCapture capture, out byte[] data)
		{
			return PcapListener.TryAsIpv4(capture, out data);
		}
	}
}

#endif
