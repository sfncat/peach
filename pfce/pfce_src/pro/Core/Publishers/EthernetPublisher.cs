using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Peach.Core;
using Peach.Core.IO;

namespace Peach.Pro.Core.Publishers
{
	public abstract class EthernetPublisher : Peach.Core.Publishers.StreamPublisher
	{
		#region MTU Related Declarations

		// Max IP len is 65535, ensure we can fit that plus ip header plus ethernet header.
		// In order to account for Jumbograms which are > 65535, max MTU is double 65535
		// MinMTU is 1280 so that IPv6 info isn't lost if MTU is fuzzed

		public const string DefaultMinMtu = "1280";
		public const string DefaultMaxMtu = "131070"; // 65535 * 2

		#endregion

		// ReSharper disable InconsistentNaming
		public int Timeout { get; set; }
		public uint MinMTU { get; set; }
		public uint MaxMTU { get; set; }
		// ReSharper restore InconsistentNaming

		/// <summary>
		/// Do not throw exceptions when reading from socket. This includes timeout
		/// exceptions.
		/// </summary>
		/// <remarks>
		/// This property can be set through publisher parameters, or via
		/// SetProperty and GetProperty.
		/// </remarks>
		public bool NoReadException { get; set; }

		protected abstract string DeviceName { get; }

		protected uint? Mtu { get { return _currMtu; } }

		private uint? _currMtu;
		private uint? _origMtu;

		protected EthernetPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override void OnStart()
		{
			if (!string.IsNullOrEmpty(DeviceName))
			{
				try
				{
					using (var cfg = NetworkAdapter.CreateInstance(DeviceName))
					{
						_origMtu = cfg.MTU;
					}
				}
				catch (Exception ex)
				{
					var msg = ex.Message;
					if (ex is TypeInitializationException || ex is TargetInvocationException)
						msg = ex.InnerException.Message;

					_origMtu = null;
					Logger.Debug("Could not query the MTU of '{0}'. {1}", DeviceName, msg);
				}
			}
			else
			{
				Logger.Debug("MTU tracking is disabled because the device name is unknown.");
			}

			_currMtu = _origMtu;

			base.OnStart();
		}

		protected override void OnStop()
		{
			if (_currMtu != _origMtu)
			{
				using (var cfg = NetworkAdapter.CreateInstance(DeviceName))
				{
					Logger.Debug("Restoring the MTU of '{0}' to {1}.", DeviceName, _origMtu.HasValue ? _origMtu.ToString() : "<null>");
					cfg.MTU = _origMtu;
				}
			}

			base.OnStop();
		}

		protected override Variant OnGetProperty(string property)
		{
			if (property == "MTU")
			{
				if (string.IsNullOrEmpty(DeviceName))
					throw new SoftException("Cannot get the MTU because the device name is unknown.");

				if (_currMtu == null)
				{
					Logger.Debug("MTU of '{0}' is unknown.", DeviceName);
					return null;
				}

				Logger.Debug("MTU of '{0}' is {1}.", DeviceName, _currMtu);
				return new Variant(_currMtu.Value);
			}

			if (property == "Timeout")
				return new Variant(Timeout);

			if (property == "NoReadException")
				return new Variant(NoReadException ? "True" : "False");

			return base.OnGetProperty(property);
		}

		protected override void OnSetProperty(string property, Variant value)
		{
			switch (property)
			{
				case "MTU":
					_SetMtu(value);
					break;
				case "Timeout":
					_SetTimeout(value);
					break;
				case "NoReadException":
					_SetNoReadException(value);
					break;
				default:
					base.OnSetProperty(property, value);
					break;
			}
		}

		void _SetMtu(Variant value)
		{
			if (string.IsNullOrEmpty(DeviceName))
				throw new SoftException("Cannot set the MTU because the device name is unknown.");

			uint mtu;

			if (value.GetVariantType() == Variant.VariantType.BitStream)
			{
				var bs = (BitwiseStream)value;
				bs.SeekBits(0, SeekOrigin.Begin);
				ulong bits;
				var len = bs.ReadBits(out bits, 32);
				mtu = Endian.Little.GetUInt32(bits, len);
			}
			else if (value.GetVariantType() == Variant.VariantType.ByteString)
			{
				var buf = (byte[])value;
				var len = Math.Min(buf.Length * 8, 32);
				mtu = Endian.Little.GetUInt32(buf, len);
			}
			else
			{
				try
				{
					mtu = (uint)value;
				}
				catch
				{
					throw new SoftException("Can't set MTU, 'value' is an unsupported type.");
				}
			}

			if (mtu < MinMTU || mtu > MaxMTU)
			{
				Logger.Debug("Not setting MTU of '{0}', value is out of range.", DeviceName);
			}

			using (var cfg = NetworkAdapter.CreateInstance(DeviceName))
			{
				try
				{
					cfg.MTU = mtu;
				}
				catch (Exception ex)
				{
					var msg = ex.Message;
					if (ex is TypeInitializationException || ex is TargetInvocationException)
						msg = ex.InnerException.Message;

					var err = "Failed to change MTU of '{0}' to {1}. {2}".Fmt(DeviceName, mtu, msg);
					Logger.Error(err);
					throw new SoftException(err, ex);
				}

				_currMtu = cfg.MTU;

				// ReSharper disable once ConditionIsAlwaysTrueOrFalse
				// Can return null if mtu change didn't take effect

				if (!_currMtu.HasValue || _currMtu.Value != mtu)
				{
					var err = "Failed to change MTU of '{0}' to {1}. The change did not take effect.".Fmt(DeviceName, mtu);
					Logger.Error(err);
					throw new SoftException(err);
				}

				Logger.Debug("Changed MTU of '{0}' to {1}.", DeviceName, mtu);
			}
		}

		void _SetTimeout(Variant value)
		{
			if (value.GetVariantType() == Variant.VariantType.BitStream)
			{
				var bs = (BitwiseStream)value;
				bs.SeekBits(0, SeekOrigin.Begin);
				ulong bits;
				var len = bs.ReadBits(out bits, 32);
				Timeout = Endian.Little.GetInt32(bits, len);
			}
			else if (value.GetVariantType() == Variant.VariantType.ByteString)
			{
				var buf = (byte[])value;
				var len = Math.Min(buf.Length * 8, 32);
				Timeout = Endian.Little.GetInt32(buf, len);
			}
			else
			{
				try
				{
					Timeout = (int)value;
				}
				catch
				{
					throw new SoftException("Can't set Timeout, 'value' is an unsupported type.");
				}
			}
		}

		void _SetNoReadException(Variant value)
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
					throw new SoftException("Can't set NoReadException, 'value' is an unsupported type.");
				}
			}

			NoReadException = val.ToLower() == "true";
		}
	}
}
