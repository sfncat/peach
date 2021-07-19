using System;
using System.IO.Ports;
using Peach.Core;

namespace Peach.Pro.Core.OS
{
	public interface IPal
	{
		ISerialStream OpenSerial(
			string portName,
			int baudRate,
			int dataBits,
			Parity parity,
			StopBits stopBits,
			bool dtrEnable,
			bool rtsEnable,
			Handshake handshake, 
			int readTimeout, 
			int writeTimeout,
			int readBufferSize,
			int writeBufferSize);

		ISingleInstance SingleInstance(string name);

		/// <summary>
		/// Returns non-null if guid is not referenced by any process.
		/// Returns null if guid is referenced by any process.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		IDisposable GetCanary(Guid id);
	}

	public static class Pal
	{
		public static IPal Instance { get; private set; }

		static Pal()
		{
			switch (Platform.GetOS())
			{
				case Platform.OS.Windows:
					Instance = new Windows.Pal();
					break;
				case Platform.OS.Linux:
					Instance = new Linux.Pal();
					break;
				case Platform.OS.OSX:
					Instance = new OSX.Pal();
					break;
				default:
					throw new NotSupportedException();
			}
		}

		public static ISerialStream OpenSerial(
			string portName,
			int baudRate,
			int dataBits,
			Parity parity,
			StopBits stopBits,
			bool dtrEnable,
			bool rtsEnable,
			Handshake handshake,
			int readTimeout,
			int writeTimeout,
			int readBufferSize,
			int writeBufferSize)
		{
			return Instance.OpenSerial(
				portName,
				baudRate,
				dataBits,
				parity,
				stopBits,
				dtrEnable,
				rtsEnable,
				handshake,
				readTimeout,
				writeTimeout,
				readBufferSize,
				writeBufferSize);
		}

		public static ISingleInstance SingleInstance(string name)
		{
			return Instance.SingleInstance(name);
		}

		public static IDisposable GetCanary(Guid id)
		{
			return Instance.GetCanary(id);
		}
	}
}
