using System;
using System.IO.Ports;
using System.Threading;

namespace Peach.Pro.Core.OS.Windows
{
	internal class Pal : IPal
	{
		public ISerialStream OpenSerial(
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
			return new WinSerialStream(
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

		public ISingleInstance SingleInstance(string name)
		{
			return new SingleInstanceImpl(name);
		}

		public IDisposable GetCanary(Guid id)
		{
			var name = "Global\\" + id;
			bool createdNew;

			var mutex = new Mutex(true, name, out createdNew);

			if (createdNew)
				return mutex;

			mutex.Dispose();
			return null;
		}
	}
}
