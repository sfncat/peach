using System;
using Peach.Core;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using NLog;
using Peach.Core.IO;

namespace Peach.Pro.Core.OS.OSX
{
	[PlatformImpl(Platform.OS.OSX)]
	public class DatagramImpl : Unix.DatagramImpl
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public DatagramImpl(string publisher) 
			: base(publisher)
		{
		}

		protected override IAddress CreateAddress(IPEndPoint ep)
		{
			if (ep.AddressFamily == AddressFamily.InterNetwork)
				return new IPv4Address(ep);
			return new IPv6Address(ep);
		}

		protected override void IncludeIpHeader(int fd)
		{
			var opt = 1;
			var ret = setsockopt(fd, IPPROTO_IP, IP_HDRINCL, ref opt, sizeof(int));
			ThrowPeachExceptionIf(ret, "setsockopt(IPPROTO_IP, IP_HDRINCL) failed.");
		}

		protected override void SetBufferSize(int fd, int bufSize)
		{
			var ret = setsockopt(fd, SOL_SOCKET, SO_SNDBUF, ref bufSize, sizeof(int));
			ThrowPeachExceptionIf(ret, "setsockopt(SOL_SOCKET, SO_SNDBUF) failed.");

			ret = setsockopt(fd, SOL_SOCKET, SO_RCVBUF, ref bufSize, sizeof(int));
			ThrowPeachExceptionIf(ret, "setsockopt(SOL_SOCKET, SO_RCVBUF) failed.");
		}

		protected override void EnableReuseAddr(int fd)
		{
			var opt = 1;
			var ret = setsockopt(fd, SOL_SOCKET, SO_REUSEADDR, ref opt, sizeof(int));
			ThrowPeachExceptionIf(ret, "setsockopt(SOL_SOCKET, SO_REUSEADDR) failed.");
		}

		protected override void OpenMulticast(
			int fd,
			IPEndPoint localEp, 
			IPEndPoint remoteEp, 
			NetworkInterface iface, 
			string ifaceName)
		{
			// Multicast needs to bind to the group on *nix
			if (remoteEp.Address.AddressFamily == AddressFamily.InterNetwork)
			{
				int ret;

				Logger.Debug("Binding to {0}", remoteEp.Address);
				using (var sa = CreateAddress(new IPEndPoint(IPAddress.Any, localEp.Port)))
				{
					ret = bind(fd, sa.Ptr, sa.Length);
					ThrowPeachExceptionIf(ret, "bind() failed.");
				}

				var mreq = new ip_mreq 
				{
					imr_multiaddr = remoteEp.Address.GetAddressBytes(),
					imr_interface = localEp.Address.GetAddressBytes(),
				};

				ret = setsockopt(fd, IPPROTO_IP, IP_ADD_MEMBERSHIP, ref mreq, Marshal.SizeOf(mreq));
				ThrowPeachExceptionIf(ret, "Error, failed to join group '{0}' on interface '{1}'.".Fmt(remoteEp.Address, ifaceName));

				Logger.Trace("Setting multicast interface for {0} socket to {1}.", _publisher, localEp.Address);
				var ifaddr = localEp.Address.GetAddressBytes();
				ret = setsockopt(fd, IPPROTO_IP, IP_MULTICAST_IF, ifaddr, ifaddr.Length);
				ThrowPeachExceptionIf(ret, "Error, failed to set outgoing interface to '{1}' for group '{0}'.".Fmt(remoteEp.Address, localEp.Address));
			}
			else
			{
				int ret;

				Logger.Debug("Binding to {0}", IPAddress.IPv6Any);
				using (var sa = CreateAddress(new IPEndPoint(IPAddress.IPv6Any, localEp.Port)))
				{
					ret = bind(fd, sa.Ptr, sa.Length);
					ThrowPeachExceptionIf(ret, "bind() failed.");
				}

				if (ifaceName == null)
					throw new PeachException("Error, could not resolve local interface name for local IP '{0}'.".Fmt(localEp.Address));

				var ifindex = (int)if_nametoindex(ifaceName);
				if (ifindex == 0)
					throw new PeachException("Error, could not resolve interface index for interface name '{0}'.".Fmt(ifaceName));

				var mreq = new ipv6_mreq() 
				{
					ipv6mr_multiaddr = remoteEp.Address.GetAddressBytes(),
					ipv6mr_interface = ifindex
				};

				ret = setsockopt(fd, IPPROTO_IPV6, IPV6_JOIN_GROUP, ref mreq, Marshal.SizeOf(mreq));
				ThrowPeachExceptionIf(ret, "Error, failed to join group '{0}' on interface '{1}'.".Fmt(remoteEp.Address, ifaceName));

				Logger.Trace("Setting multicast interface for {0} socket to {1}.", _publisher, localEp.Address);
				ret = setsockopt(fd, IPPROTO_IPV6, IPV6_MULTICAST_IF, ref ifindex, sizeof(int));

				ThrowPeachExceptionIf(ret, "Error, failed to set outgoing interface to '{1}' for group '{0}'.".Fmt(remoteEp.Address, ifaceName));
			}
		}

		#region Native definitions

		private const ushort AF_INET = 2;
		private const ushort AF_INET6 = 30;

		private const int SOL_SOCKET = 0xffff;
		private const int SO_REUSEADDR = 0x0004;
		private const int SO_SNDBUF = 0x1001;
		private const int SO_RCVBUF = 0x1002;

		private const int IPPROTO_IP = 0;
		private const int IP_HDRINCL = 2;
		private const int IP_MULTICAST_IF = 9;
		private const int IP_ADD_MEMBERSHIP = 12;

		private const int IPPROTO_IPV6 = 41;
		private const int IPV6_MULTICAST_IF = 9;
		private const int IPV6_JOIN_GROUP = 12;

		[StructLayout(LayoutKind.Sequential)]
		private class sockaddr_in
		{
			public byte sin_len;
			public byte sin_family;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public byte[] sin_port;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public byte[] sin_addr;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public byte[] sin_padding;
		}

		[StructLayout(LayoutKind.Sequential)]
		private class sockaddr_in6
		{
			public byte sin6_len;
			public byte sin6_family;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public byte[] sin6_port;
			public uint sin6_flowinfo;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public byte[] sin6_addr;
			public uint sin6_scope_id;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct ip_mreq 
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public byte[] imr_multiaddr;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public byte[] imr_interface;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct ipv6_mreq
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public byte[] ipv6mr_multiaddr;
			public int    ipv6mr_interface;
		}

		[DllImport("libc", SetLastError = true)]
		private static extern uint if_nametoindex(string ifname);

		[DllImport("libc", SetLastError = true)]
		private static extern int setsockopt(int socket, int level, int optname, ref ip_mreq opt, int optlen);

		[DllImport("libc", SetLastError = true)]
		private static extern int setsockopt(int socket, int level, int optname, ref ipv6_mreq opt, int optlen);

		#endregion

		#region IPvXAddress 

		private class IPv4Address : IAddress
		{
			private readonly IntPtr ptr;

			public IPv4Address(IPEndPoint ep)
			{
				var sa = new sockaddr_in 
				{
					sin_len = (byte)Length,
					sin_family = (byte)AddressFamily,
					sin_addr = ep.Address.GetAddressBytes(),
					sin_port = Endian.Big.GetBytes(ep.Port, 16),
					sin_padding = new byte[8] // Must be zeroed out!
				};

				ptr = Marshal.AllocHGlobal(Length);
				Marshal.StructureToPtr(sa, ptr, false);
			}

			public void Dispose()
			{
				Marshal.FreeHGlobal(ptr);
			}

			public ushort AddressFamily { get { return AF_INET; } }
			public int Length { get { return Marshal.SizeOf(typeof(sockaddr_in)); } }
			public IntPtr Ptr { get { return ptr; } }

			public IPEndPoint EndPoint
			{
				get
				{
					var sa = new sockaddr_in
					{
						sin_len = (byte)Length,
						sin_family = (byte)AddressFamily,
					};
					Marshal.PtrToStructure(ptr, sa);
					return new IPEndPoint(
						new IPAddress(sa.sin_addr), 
						Endian.Big.GetUInt16(sa.sin_port, 16)
					);
				}
			}
		}

		private class IPv6Address : IAddress
		{
			private readonly IntPtr ptr;

			public IPv6Address(IPEndPoint ep)
			{
				var sa = new sockaddr_in6
				{
					sin6_len = (byte)Length,
					sin6_family = (byte)AddressFamily,
					sin6_addr = ep.Address.GetAddressBytes(),
					sin6_scope_id = (uint)ep.Address.ScopeId,
					sin6_port = Endian.Big.GetBytes(ep.Port, 16),
				};

				ptr = Marshal.AllocHGlobal(Length);
				Marshal.StructureToPtr(sa, ptr, false);
			}

			public void Dispose()
			{
				Marshal.FreeHGlobal(ptr);
			}

			public ushort AddressFamily { get { return AF_INET6; } }
			public int Length { get { return Marshal.SizeOf(typeof(sockaddr_in6)); } }
			public IntPtr Ptr { get { return ptr; } }

			public IPEndPoint EndPoint
			{
				get
				{
					var sa = new sockaddr_in6
					{
						sin6_len = (byte)Length,
						sin6_family = (byte)AddressFamily,
					};
					Marshal.PtrToStructure(ptr, sa);
					return new IPEndPoint(
						new IPAddress(sa.sin6_addr, sa.sin6_scope_id), 
						Endian.Big.GetUInt16(sa.sin6_port, 16)
					);
				}
			}
		}

		#endregion
	}
}
