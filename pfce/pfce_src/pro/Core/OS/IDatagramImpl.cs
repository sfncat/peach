using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Peach.Pro.Core.OS
{
	public interface IDatagramImpl
	{
		IPEndPoint Open(
			SocketType socketType,
			byte protocol,
			bool ipHeaderInclude,
			IPEndPoint localEp, 
			IPEndPoint remoteEp, 
			NetworkInterface iface, 
			string ifaceName,
			int bufSize
		);

		void Close();

		int Send(IPEndPoint remoteEp, byte[] buf, int len, int timeout);

		IPEndPoint Receive(IPEndPoint expected, byte[] buf, out int len, int timeout);
	}
}
