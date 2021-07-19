using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NLog;
using Peach.Core;
using Peach.Core.IO;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Peach.Pro.Core.Publishers
{
	[Publisher("RawEther")]
	[Alias("raw.RawEther")]
	[Parameter("Interface", typeof(string), "Name of interface to bind to")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default 3000)", "3000")]
	[Parameter("PcapTimeout", typeof(int), "Pcap internal read timeout (default 100)", "100")]
	[Parameter("MinMTU", typeof(uint), "Minimum allowable MTU property value", DefaultMinMtu)]
	[Parameter("MaxMTU", typeof(uint), "Maximum allowable MTU property value", DefaultMaxMtu)]
	[Parameter("MinFrameSize", typeof(uint), "Short frames are padded to this minimum length", DefaultMinFrameSize)]
	[Parameter("MaxFrameSize", typeof(uint), "Long frames are truncated to this maximum length", DefaultMaxFrameSize)]
	[Parameter("Filter", typeof(string), "Input filter in libpcap format", "")]
	[ObsoleteParameter("Protocol", "The RawEther publisher parameter 'Protocol' is no longer used.")]
	public class RawEtherPublisher : EthernetPublisher
	{
		// We get BSOD if we don't pad to at least 15 bytes on Windows
		// possibly only needed for VMWare network adapters
		// https://www.winpcap.org/pipermail/winpcap-users/2012-November/004672.html
		// Newer linux kernels (Ubuntu 15.04) will also error if sending packets less than 15 bytes
		const string DefaultMinFrameSize = "64"; // Raw sockets pad to 64 by default
		const string DefaultMaxFrameSize = "65535"; // SharpPcap throws if > 65535

		public string Interface { get; set; }
		public int PcapTimeout { get; set; }
		public string Filter { get; set; }
		public uint MinFrameSize { get; set; }
		public uint MaxFrameSize { get; set; }

		protected override string DeviceName
		{
			get
			{
				return _deviceName;
			}
		}

		static readonly NLog.Logger ClassLogger = LogManager.GetCurrentClassLogger();

		protected override NLog.Logger Logger { get { return ClassLogger; } }

		readonly Queue<byte[]> _queue = new Queue<byte[]>();

		LibPcapLiveDevice _deviceRx;
		LibPcapLiveDevice _deviceTx;
		string _deviceName;

		public RawEtherPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			stream = new MemoryStream();
		}

		public static ICollection<LibPcapLiveDevice> Devices()
		{
			try
			{
				// Must use New() so we can have multiple publishers using the same pcap device
				return CaptureDeviceList.New().OfType<LibPcapLiveDevice>().ToList();
			}
			catch (DllNotFoundException ex)
			{
				throw new PeachException("An error occurred getting the pcap device list.  Ensure libpcap is installed and try again.", ex);
			}
		}

		private void OnPacketArrival(object sender, CaptureEventArgs e)
		{
			OnPacketArrival(e.Packet.Data);
		}

		private void OnPacketArrival(byte[] buf)
		{
			lock (_queue)
			{
				Logger.Trace("OnPacketArrival> {0}", _deviceName);
				_queue.Enqueue(buf);
				Monitor.Pulse(_queue);
			}
		}

		private byte[] GetNextPacket()
		{
			lock (_queue)
			{
				if (_queue.Count == 0 && !Monitor.Wait(_queue, Timeout))
					return null;

				return _queue.Dequeue();
			}
		}

		protected override void OnStart()
		{
			var devs = Devices();

			if (devs.Count == 0)
				throw new PeachException("No pcap devices found. Ensure appropriate permissions for using libpcap.");

			_deviceRx = devs.FirstOrDefault(d => d.Interface.FriendlyName == Interface);

			if (_deviceRx == null)
			{
				var avail = string.Join("', '", devs.Select(d => d.Interface.FriendlyName));
				throw new PeachException("Unable to locate pcap device named '{0}'. The following pcap devices were found: '{1}'.".Fmt(Interface, avail));
			}

			string error;
			if (!PcapDevice.CheckFilter(Filter, out error))
				throw new PeachException("The specified pcap filter string '{0}' is invalid.".Fmt(Filter));

			_deviceRx.Open(DeviceMode.Promiscuous, PcapTimeout);
			_deviceRx.Filter = Filter;
			_deviceRx.OnPacketArrival += OnPacketArrival;
			_deviceName = _deviceRx.Interface.FriendlyName;

			// Open a 2nd pcap device to the same interface for transmitting
			// so that we are guranteed to receive our own transmissions
			// With winpcap, the sending device will receive, but with libpcap
			// you need once device for sending and one for receiving
			// The user should use a pcap filter to control what packets are received

			_deviceTx = Devices().First(d => d.Interface.FriendlyName == Interface);
			_deviceTx.Open();

			Logger.Debug("Starting Capture on {0} (filter: '{1}', timeout: {2})", _deviceName, Filter, PcapTimeout);

			_deviceRx.StartCapture();

			// Sleep to ensure the capture thread is started
			Thread.Sleep(PcapTimeout);

			base.OnStart();
		}

		protected override void OnStop()
		{
			base.OnStop();

			if (_deviceRx != null)
			{
				_deviceRx.StopCapture();
				_deviceRx.OnPacketArrival -= OnPacketArrival;
				_deviceRx.Close();
				_deviceRx = null;
			}

			if (_deviceTx != null)
			{
				_deviceTx.Close();
				_deviceTx = null;
			}

			_deviceName = null;
		}

		protected override void OnOpen()
		{
			lock (_queue)
			{
				// Just need to clear any previously collected packets
				// StartCapture and StopCapture are slow so don't change
				// our capture state on each iteration
				_queue.Clear();
			}
		}

		protected override void OnClose()
		{
			// Don't need to do anything here
		}

		protected override void OnInput()
		{
			stream.Position = 0;
			stream.SetLength(0);

			var buf = GetNextPacket();

			if (buf == null)
			{
				var msg = "Timeout waiting for input from interface '{0}'.".Fmt(Interface);

				Logger.Debug(msg);

				if (!NoReadException)
					throw new SoftException(msg);

				return;
			}

			stream.Write(buf, 0, buf.Length);
			stream.Position = 0;

			if (Logger.IsDebugEnabled)
				Logger.Debug("\n\n" + Utilities.HexDump(stream));
		}

		protected override void OnOutput(BitwiseStream data)
		{
			data.Seek(0, SeekOrigin.Begin);

			if (Logger.IsDebugEnabled)
				Logger.Debug("\n\n" + Utilities.HexDump(data));

			var len = (int)Math.Min(Math.Max(MinFrameSize, data.Length), MaxFrameSize);
			var buf = new byte[len];
			var bufLen = data.Read(buf, 0, len);

			try
			{
				_deviceTx.SendPacket(buf);
			}
			catch (Exception ex)
			{
				throw new SoftException(ex.Message, ex);
			}

			if (bufLen != data.Length)
				Logger.Debug("Only sent {0} of {1} bytes to device.", bufLen, data.Length);
		}
	}
}
