using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using NLog;
using PacketDotNet;
using Peach.Core;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Peach.Pro.Core.Publishers
{
	/// <summary>
	/// Listen and queue incoming packets for use by Publisher
	/// </summary>
	/// <remarks>
	/// This class is intended for use by raw publishers as the receiving
	/// mechanism allowing filters to limit the received packets.
	/// </remarks>
	public class PcapListener
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected NLog.Logger Logger { get { return logger; } }

		ICaptureDevice _device;

		/// <summary>
		/// Queue of received packets. This is a thread safe queue.
		/// </summary>
		public BlockingCollection<RawCapture> PacketQueue = new BlockingCollection<RawCapture>();

		public PcapListener(PhysicalAddress macAddress)
		{
			var devices = CaptureDeviceList.Instance.OfType<LibPcapLiveDevice>();

			foreach (var device in devices)
			{
				if (!device.Interface.MacAddress.Equals(macAddress))
					continue;

				_device = device;
				break;
			}

			if (_device == null)
				throw new ArgumentException("Unable to locate network device with mac '{0}'.".Fmt(macAddress));

			_device.OnPacketArrival += _device_OnPacketArrival;
		}

		public PcapListener(string deviceName)
		{
			var devices = CaptureDeviceList.Instance.OfType<LibPcapLiveDevice>();

			foreach (var device in devices)
			{
				if (device.Interface.FriendlyName != deviceName)
					continue;

				_device = device;
				break;
			}

			if (_device == null)
				throw new ArgumentException("Unable to locate network device '{0}'.".Fmt(deviceName));
		}

		public PcapListener(IPAddress Interface)
		{
			PhysicalAddress macAddress = null;

			foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (!adapter.Supports(NetworkInterfaceComponent.IPv4))
					continue;

				var addrs = adapter.GetIPProperties().UnicastAddresses;

				if (!addrs.Any(a => a.Address.Equals(Interface)))
					continue;

				macAddress = adapter.GetPhysicalAddress();
				break;
			}

			if (macAddress == null)
				throw new PeachException("Unable to locate adapter for interface '{0}'.".Fmt(Interface));

			var devices = CaptureDeviceList.Instance.OfType<LibPcapLiveDevice>();

			foreach (var device in devices)
			{
				if (!device.Interface.MacAddress.Equals(macAddress))
					continue;

				_device = device;
				break;
			}

			if (_device == null)
				throw new ArgumentException("Unable to locate network device with mac '{0}'.".Fmt(macAddress));

			_device.OnPacketArrival += _device_OnPacketArrival;
		}

		/// <summary>
		/// Start capturing packets
		/// </summary>
		public void Start()
		{
			Start("", 1000);
		}

		/// <summary>
		/// Start capturing packets
		/// </summary>
		/// <param name="filter">Capture filter. Follows the libpcap format.</param>
		/// <param name="timeout">pcap_open_live to_ms parameter.</param>
		public void Start(string filter, int timeout)
		{
			Logger.Debug("Starting capture (filter: {0}, timeout: {1})", filter, timeout);
			PacketQueue = new BlockingCollection<RawCapture>();

			_device.Open(DeviceMode.Promiscuous, timeout);

			_device.Filter = filter;

			_device.OnPacketArrival += _device_OnPacketArrival;

			_device.StartCapture();
		}

		/// <summary>
		/// Stop capturing packets
		/// </summary>
		public void Stop()
		{
			Logger.Debug("Stopping capture");
			_device.StopCapture();

			_device.OnPacketArrival -= _device_OnPacketArrival;

			_device.Close();
			_device = null;

			PacketQueue = null;
		}

		/// <summary>
		/// Clear queue
		/// </summary>
		public void Clear()
		{
			var cnt = 0;

			while (PacketQueue.Count > 0)
			{
				RawCapture item;
				PacketQueue.TryTake(out item);
				++cnt;
			}

			Logger.Debug("Cleared {0} packets from queue", cnt);
		}

		void _device_OnPacketArrival(object sender, CaptureEventArgs e)
		{
			Logger.Debug("Queuing packet");
			PacketQueue.Add(e.Packet);
		}

		/// <summary>
		/// Send raw packet at the lowest layer supported by interface.
		/// </summary>
		/// <remarks>
		/// This interface works on both Windows and Linux, allowing true raw sockets
		/// on Windows via the winpcap service.
		/// </remarks>
		/// <param name="data">Packet to send</param>
		public void SendPacket(byte[] data)
		{
			_device.SendPacket(data);
		}

		/// <summary>
		/// Try and get an IPv4 packet from the captured data.
		/// </summary>
		/// <param name="capture"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public static bool TryAsIpv4(RawCapture capture, out byte [] data)
		{
			try
			{
				var packet = Packet.ParsePacket(LinkLayers.Ethernet, capture.Data);
				var ip = (IPv4Packet)packet.PayloadPacket;

				data = new byte[ip.Bytes.Length];
				Array.Copy(ip.Bytes, data, data.Length);

				return true;
			}
			catch
			{
				data = null;
				return false;
			}
		}

		/// <summary>
		/// Try and get an IPv6 packet from the captured data.
		/// </summary>
		/// <param name="capture"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public static bool TryAsIpv6(RawCapture capture, out byte[] data)
		{
			try
			{
				var packet = Packet.ParsePacket(LinkLayers.Ethernet, capture.Data);
				var ip = (IPv6Packet)packet.PayloadPacket;

				data = new byte[ip.Bytes.Length];
				Array.Copy(ip.Bytes, data, data.Length);

				return true;
			}
			catch
			{
				data = null;
				return false;
			}
		}

		/// <summary>
		/// Try and get a TCP packet from the captured data.
		/// </summary>
		/// <param name="capture"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public static bool TryAsTcp(RawCapture capture, out byte[] data)
		{
			try
			{
				var packet = Packet.ParsePacket(LinkLayers.Ethernet, capture.Data);
				var ip = packet.PayloadPacket;
				var tcp = (TcpPacket)ip.PayloadPacket;

				System.Diagnostics.Debug.Assert(tcp != null);

				data = new byte[ip.Bytes.Length];
				Array.Copy(ip.Bytes, data, data.Length);

				return true;
			}
			catch
			{
				data = null;
				return false;
			}
		}
	}
}
