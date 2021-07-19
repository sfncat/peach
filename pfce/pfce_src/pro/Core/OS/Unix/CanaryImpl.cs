using System;
using System.IO;
using System.Net.Sockets;
using Mono.Unix;

namespace Peach.Pro.Core.OS.Unix
{
	internal static class CanaryImpl
	{
		public static IDisposable Get(Guid id)
		{
			var ep = new UnixEndPoint(Path.Combine(Path.GetTempPath(), id.ToString()));

			if (File.Exists(ep.Filename))
			{
				// Check if anyone is currently bound by trying to connect

				using (var cli = new Socket(AddressFamily.Unix, SocketType.Dgram, 0))
				{
					try
					{
						cli.Connect(ep);
						return null;
					}
					catch
					{
						// Error means no one is bound
					}
				}

				try
				{
					File.Delete(ep.Filename);
				}
				catch
				{
					// Handle two people trying to delete at the same time
				}
			}

			var s = new Socket(AddressFamily.Unix, SocketType.Dgram, 0);

			try
			{
				s.Bind(ep);
				return s;
			}
			catch
			{
				// Handle two people trying to bind at the same time
				s.Dispose();
				return null;
			}
		}
	}
}
