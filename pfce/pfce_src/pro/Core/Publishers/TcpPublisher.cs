


using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Peach.Core;
using Peach.Core.IO;

namespace Peach.Pro.Core.Publishers
{
	public abstract class TcpPublisher : Peach.Core.Publishers.BufferedStreamPublisher
	{
		// Leave the setter public, it's used by pits.
		public ushort Port { get; set; }
		
		protected TcpClient _tcp = null;
		protected EndPoint _localEp = null;
		protected EndPoint _remoteEp = null;

		protected TcpPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override void StartClient()
		{
			System.Diagnostics.Debug.Assert(_tcp != null);
			System.Diagnostics.Debug.Assert(_client == null);
			System.Diagnostics.Debug.Assert(_localEp == null);
			System.Diagnostics.Debug.Assert(_remoteEp == null);

			try
			{
				_client = new MemoryStream();
				_localEp = _tcp.Client.LocalEndPoint;
				_remoteEp = _tcp.Client.RemoteEndPoint;
				_clientName = _remoteEp.ToString();
			}
			catch (Exception ex)
			{
				Logger.Error("open: Error, Unable to start tcp client reader. {0}.", ex.Message);
				throw new SoftException(ex);
			}

			base.StartClient();
		}

		protected override void ClientClose()
		{
			_tcp.Close();
			_tcp = null;
			_remoteEp = null;
			_localEp = null;
		}

		protected override IAsyncResult ClientBeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return _tcp.Client.BeginReceive(buffer, offset, count, SocketFlags.None, callback, state);
		}

		protected override int ClientEndRead(IAsyncResult asyncResult)
		{
			return _tcp.Client.EndReceive(asyncResult);
		}

		protected override void ClientShutdown()
		{
			_tcp.Client.Shutdown(SocketShutdown.Send);
		}

		protected override IAsyncResult ClientBeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return _tcp.Client.BeginSend(buffer, offset, count, SocketFlags.None, callback, state);
		}

		protected override int ClientEndWrite(IAsyncResult asyncResult)
		{
			return _tcp.Client.EndSend(asyncResult);
		}

		protected override Variant OnGetProperty(string property)
		{
			switch (property)
			{
				case "Port":
					return new Variant(Port);
				case "NoWriteException":
					return new Variant(NoWriteException ? "True" : "False");
			}

			return base.OnGetProperty(property);
		}

		protected override void OnSetProperty(string property, Variant value)
		{
			switch (property)
			{
				case "Port":
					var newPort = UShortFromVariant(value);
					Logger.Debug("Changing Port from {0} to {1}.\n", Port, newPort);

					Port = newPort;
					OnStop();
					OnStart();
					return;

				case "NoWriteException":
					_SetNoWriteException(value);
					return;
			}

			base.OnSetProperty(property, value);
		}

		void _SetNoWriteException(Variant value)
		{
			string val;

			if (value.GetVariantType() == Variant.VariantType.BitStream)
			{
				var rdr = new BitReader((BitwiseStream)value, true)
				{
					BaseStream = { Position = 0 }
				};

				val = rdr.ReadString(Encoding.UTF8);
			}
			else if (value.GetVariantType() == Variant.VariantType.ByteString)
			{
				val = Encoding.UTF8.GetString((byte[])value);
			}
			else
			{
				try
				{
					val = (string)value;
				}
				catch
				{
					throw new SoftException("Can't set NoWriteException, 'value' is an unsupported type.");
				}
			}

			NoWriteException = val.ToLower() == "true";
		}
	}
}

// end
