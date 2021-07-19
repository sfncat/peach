using System;
using System.IO.Ports;
using Peach.Pro.Core.OS.Unix;

namespace Peach.Pro.Core.OS.Linux
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
			return new SerialPortStream(
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
			return CanaryImpl.Get(id);
		}
	}
}
