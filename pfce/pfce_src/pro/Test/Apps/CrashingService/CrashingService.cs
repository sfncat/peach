using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;

namespace CrashingService
{
	// NOTE: Tell msvs this is not a 'Component'
	[DesignerCategory("Code")]
	public class CrashingService : ServiceBase
	{
		private class Connection
		{
			public Socket Socket { get; private set; }
			public byte[] Buffer { get; private set; }

			public Connection(Socket s)
			{
				Socket = s;
				Buffer = new byte[1024];
			}

			public void BeginRecv()
			{
				Socket.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, OnRecvComplete, null);
			}

			private void OnRecvComplete(IAsyncResult ar)
			{
				try
				{
					var len = Socket.EndReceive(ar);
					if (len == 0)
						return;

					if (len >= 10)
						CrashMe();

					Socket.Send(Buffer, len, SocketFlags.None);

					BeginRecv();
				}
				catch (ObjectDisposedException)
				{
				}
			}
		}

		TcpListener _listener;

		public CrashingService()
		{
			ServiceName = "CrashingService";
		}

		protected override void OnStart(string[] args)
		{
			// Simulate slow service start
			Thread.Sleep(10000);

			_listener = new TcpListener(new IPEndPoint(IPAddress.Any, 9999));

			_listener.Start(10);
			_listener.BeginAcceptSocket(OnAcceptComplete, _listener);
		}

		protected override void OnStop()
		{
			// Simulate slow service stop
			Thread.Sleep(10000);
		}

		private static void OnAcceptComplete(IAsyncResult ar)
		{
			var l = (TcpListener)ar.AsyncState;

			try
			{
				var c = new Connection(l.EndAcceptSocket(ar));

				l.BeginAcceptTcpClient(OnAcceptComplete, l);

				c.BeginRecv();
			}
			catch (ObjectDisposedException)
			{
			}
		}

		private static void CrashMe()
		{
			var ptr = Marshal.AllocHGlobal(10);

			for (var i = 0; i < 10000000; i++)
			{
				Marshal.WriteInt64(ptr, i, -1);
			}			
		}
	}
}
