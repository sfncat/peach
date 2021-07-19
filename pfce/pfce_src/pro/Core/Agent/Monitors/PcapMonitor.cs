

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using Peach.Core;
using Peach.Core.Agent;
using SharpPcap;
using SharpPcap.LibPcap;
using Monitor = Peach.Core.Agent.Monitor2;

namespace Peach.Pro.Core.Agent.Monitors
{
	[Monitor("NetworkCapture")]
	[Alias("Pcap")]
	[Alias("network.PcapMonitor")]
	[Description("Performs a network capture during the fuzzing iteration")]
	[Parameter("Device", typeof(string), "Device name for capturing on")]
	[Parameter("Filter", typeof(string), "PCAP Style filter", "")]
	public class PcapMonitor : Monitor
	{
		static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		const int ReadTimeout = 1000;

		private readonly string _tempFileName = Path.GetTempFileName();
		private readonly object _lock = new object();
		private int _numPackets;

		private LibPcapLiveDevice _device;
		private CaptureFileWriterDevice _writer;

		public string Device { get; set; }
		public string Filter { get; set; }

		public PcapMonitor(string name)
			: base(name)
		{
		}

		private void _OnPacketArrival(object sender, CaptureEventArgs packet)
		{
			lock (_lock)
			{
				// _writer can be null if a packet arrives before the 1st iteration
				if (_writer != null)
				{
					_writer.Write(packet.Packet);
					_numPackets += 1;

					OnInternalEvent(EventArgs.Empty);
				}
			}
		}

		private CaptureDeviceList _GetDeviceList()
		{
			try
			{
				return CaptureDeviceList.New();
			}
			catch (DllNotFoundException ex)
			{
				throw new PeachException("Error, PcapMonitor was unable to get the device list.  Ensure libpcap is installed and try again.", ex);
			}
		}

		private void _CloseWriter()
		{
			lock (_lock)
			{
				if (_writer != null)
				{
					_writer.Close();
					_writer = null;
				}
			}
		}

		public override void StopMonitor()
		{
			if (File.Exists(_tempFileName))
				File.Delete(_tempFileName);
		}

		public override void SessionStarting()
		{
			if (Device == null)
				throw new PeachException("Error, PcapMonitor requires a device name.");

			// Retrieve all capture devices
			// Don't use the singlton interface so we can support multiple
			// captures on the same device with different filters
			var devices = _GetDeviceList();

			if (devices.Count == 0)
				throw new PeachException("No pcap devices found. Ensure appropriate permissions for using libpcap.");

			// differentiate based upon types
			foreach (var item in devices)
			{
				var dev = item as LibPcapLiveDevice;
				System.Diagnostics.Debug.Assert(dev != null);
				if (dev.Interface.FriendlyName == Device)
				{
					_device = dev;
					break;
				}
			}

			if (_device == null)
			{
				var devs = string.Join(Environment.NewLine,
					devices
						.OfType<LibPcapLiveDevice>()
						.Select(d => d.Interface.FriendlyName)
						.Where(s => !string.IsNullOrEmpty(s))
					);

				Logger.Debug("Found the following pcap devices:{0}{1}", Environment.NewLine, devs);

				throw new PeachException("Error, PcapMonitor was unable to locate device '" + Device + "'.");
			}

			_device.OnPacketArrival += _OnPacketArrival;
			_device.Open(DeviceMode.Normal, ReadTimeout);

			try
			{
				_device.Filter = Filter;
			}
			catch (PcapException ex)
			{
				throw new PeachException("Error, PcapMonitor was unable to set the filter '" + Filter + "'.", ex);
			}

			_device.StartCapture();
		}

		public override void SessionFinished()
		{
			_CloseWriter();

			if (_device != null)
			{
				try
				{
					_device.StopCapture();
				}
				catch (PcapException ex)
				{
					System.Diagnostics.Debug.Assert(ex != null);
				}

				_device.Close();
			}
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			lock (_lock)
			{
				if (_writer != null)
					_writer.Close();

				_writer = new CaptureFileWriterDevice(_device, _tempFileName);
				_numPackets = 0;
			}
		}

		public override MonitorData GetMonitorData()
		{
			// Need to ensure the read timeout has elapsed before closing the writer
			// so that all packets will be delivered to the monitor
			Thread.Sleep(ReadTimeout);

			_CloseWriter();

			var ret = new MonitorData
			{
				Title = "Collected {0} packet{1}.".Fmt(_numPackets, _numPackets == 1 ? "" : "s"),
				Data = new Dictionary<string, Stream>
				{
					{ "pcap", new MemoryStream(File.ReadAllBytes(_tempFileName)) }
				}
			};

			return ret;
		}
	}
}
