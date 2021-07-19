using System;
using System.Linq;
using System.Threading;
using NDesk.DBus;
using Peach.Core;
using Peach.Pro.Core.Publishers.Bluetooth;

namespace Peach.Pro.Test.Bluetooth
{
	public class Program
	{
		public static byte[] ToBytes(string s)
		{
			if (s.Length % 2 != 0)
				throw new ArgumentException("s");

			var ret = new byte[s.Length / 2];

			for (var i = 0; i < s.Length; i += 2)
			{
				var nibble1 = GetNibble(s[i]);
				var nibble2 = GetNibble(s[i + 1]);

				if (nibble1 < 0 || nibble1 > 0xF || nibble2 < 0 | nibble2 > 0xF)
					return null;

				ret[i / 2] = (byte)((nibble1 << 4) | nibble2);
			}

			return ret;
		}

		private static int GetNibble(char c)
		{
			if (c >= 'a')
				return 0xA + (c - 'a');
			if (c >= 'A')
				return 0xA + (c - 'A');
			return c - '0';
		}

		private static string ToHexString(byte[] val)
		{
			return string.Join("", val.Select(x => x.ToString("X2")));
		}

		static int Main(string[] args)
		{
			if (args.Length < 1 || args.Length > 2)
				Console.WriteLine("Usage:Bluetooth.exe <hci0> [remote_address]");

			Utilities.ConfigureLogging(2);

			var app = new GattApplication
			{
				Path = new ObjectPath("/com/peach/example"),
				Services =
				{
					new LocalService
					{
						// DeviceInformation
						UUID = "0000180a-0000-1000-8000-00805f9b34fb",
						Primary = true,
						Characteristics =
						{
							new LocalCharacteristic
							{
								// PnP ID
								UUID = "00002a50-0000-1000-8000-00805f9b34fb",
								Descriptors = {},
								Flags = new[] { "read" },
								Value = ToBytes("010D0000001001")
							},
							new LocalCharacteristic
							{
								// IEEE 11073-20601 Regulatory Certification Data List
								UUID = "00002a2a-0000-1000-8000-00805f9b34fb",
								Descriptors = {},
								Flags = new[] { "read" },
								Value =  ToBytes("FE006578706572696D656E74616C")
							},
							new LocalCharacteristic
							{
								// Manufacturer Name String
								UUID = "00002a29-0000-1000-8000-00805f9b34fb",
								Descriptors = {},
								Flags = new[] { "read" },
								Value = ToBytes("4775616E67646F6E672042696F6C69676874204D6564697465636820436F2E2C4C74642E00")
							},
							new LocalCharacteristic
							{
								// Software Revision String
								UUID = "00002a28-0000-1000-8000-00805f9b34fb",
								Descriptors = {},
								Flags = new[] { "read" },
								Value = ToBytes("56312E3000")
							},
							new LocalCharacteristic
							{
								// Hardware Revision String
								UUID = "00002a27-0000-1000-8000-00805f9b34fb",
								Descriptors = {},
								Flags = new[] { "read" },
								Value = ToBytes("56312E3000")
							},
							new LocalCharacteristic
							{
								// Firmware Revision String
								UUID = "00002a26-0000-1000-8000-00805f9b34fb",
								Descriptors = {},
								Flags = new[] { "read" },
								Value = ToBytes("56312E3000")
							},
							new LocalCharacteristic
							{
								// Serial Number String
								UUID = "00002a25-0000-1000-8000-00805f9b34fb",
								Descriptors = {},
								Flags = new[] { "read", "write" },
								Value = ToBytes("313030303030303100")
							},
							new LocalCharacteristic
							{
								// Model Number String
								UUID = "00002a24-0000-1000-8000-00805f9b34fb",
								Descriptors = {},
								Flags = new[] { "read" },
								Value = ToBytes("424C542D4D313000")
							},
							new LocalCharacteristic
							{
								// System ID
								UUID = "00002a23-0000-1000-8000-00805f9b34fb",
								Descriptors = {},
								Flags = new[] { "read" },
								Value = ToBytes("D787A300006DE394")
							},
						}
					},
					new LocalService
					{
						// OXI
						UUID = "0000ffe0-0000-1000-8000-00805f9b34fb",
						Primary = true,
						Advertise = true,
						Characteristics =
						{
							new LocalCharacteristic
							{
								UUID = "0000ffe2-0000-1000-8000-00805f9b34fb",
								Descriptors =
								{
									new LocalDescriptor
									{
										UUID = "00002901-0000-1000-8000-00805f9b34fb",
										Flags = new[] { "read", "write" },
										Value = ToBytes("546865726D6F6D6574657244656D6F2034"),
										OnWrite = (d, v, o) => Console.WriteLine("WriteDesc> {0} {1}", d.UUID, ToHexString(v))
									},
									//new LocalDescriptor
									//{
									//	UUID = "00002902-0000-1000-8000-00805f9b34fb",
									//	Flags = new[] { "read", "write" },
									//	Read = (d,o) => ToBytes("0000"),
									//	Write = (d, v, o) => Console.WriteLine("WriteDesc> {0} {1}", d.UUID, ToHexString(v))
									//}
								},
								Flags = new[] { "read", "write" },
							},
							new LocalCharacteristic
							{
								UUID = "00002a21-0000-1000-8000-00805f9b34fb",
								Value = ToBytes("0000"),
								OnWrite = (c,v,o) => Console.WriteLine("WriteDesc> {0} {1}", c.UUID, ToHexString(v)),
								Descriptors =
								{
									new LocalDescriptor
									{
										UUID = "00002906-0000-1000-8000-00805f9b34fb",
										Flags = new[] { "read", "write" },
										Value = ToBytes("043C"),
										OnWrite = (d, v, o) => Console.WriteLine("WriteDesc> {0} {1}", d.UUID, ToHexString(v))
									},
									//new LocalDescriptor
									//{
									//	UUID = "00002902-0000-1000-8000-00805f9b34fb",
									//	Flags = new[] { "read", "write" },
									//	Read = (d,o) => ToBytes("0000"),
									//	Write = (d, v, o) => Console.WriteLine("WriteDesc> {0} {1}", d.UUID, ToHexString(v))
									//}
								},
								Flags = new[] { "read", "write" },
							},
							new LocalCharacteristic
							{
								UUID = "0000ffe1-0000-1000-8000-00805f9b34fb",
								Value = ToBytes("0000"),
								OnWrite = (c,v,o) => Console.WriteLine("WriteDesc> {0} {1}", c.UUID, ToHexString(v)),
								Descriptors =
								{
									//new LocalDescriptor
									//{
									//	UUID = "00002902-0000-1000-8000-00805f9b34fb",
									//	Flags = new[] { "read", "write" },
									//	Read = (d,o) => ToBytes("0000"),
									//	Write = (d, v, o) => Console.WriteLine("WriteDesc> {0} {1}", d.UUID, ToHexString(v))
									//}
								},
								Flags = new[] { "read", "write", "notify" },
							},
							new LocalCharacteristic
							{
								UUID = "00002a1d-0000-1000-8000-00805f9b34fb",
								Value = ToBytes("09"),
								OnWrite = (c,v,o) => Console.WriteLine("WriteDesc> {0} {1}", c.UUID, ToHexString(v)),
								Flags = new[] { "read" },
							},
							new LocalCharacteristic
							{
								UUID = "00002a1c-0000-1000-8000-00805f9b34fb",
								Value = ToBytes("0000"),
								OnWrite = (c,v,o) => Console.WriteLine("WriteDesc> {0} {1}", c.UUID, ToHexString(v)),
								Descriptors =
								{
									//new LocalDescriptor
									//{
									//	UUID = "00002902-0000-1000-8000-00805f9b34fb",
									//	Flags = new[] { "read", "write" },
									//	Read = (d,o) => ToBytes("0000"),
									//	Write = (d, v, o) => Console.WriteLine("WriteDesc> {0} {1}", d.UUID, ToHexString(v))
									//}
								},
								Flags = new[] { "indicate" },
							},
						}
					}
				},
			};

			app.Advertisement.ManufacturerData.Add(0x0100, ToBytes("00304d3037364531373931313600"));

			/*
Service: 0000ffe0-0000-1000-8000-00805f9b34fb, Primary: True
  Char: 0000ffe2-0000-1000-8000-00805f9b34fb, Flags: read|write, Value: org.bluez.Error.Failed: Operation failed with ATT error: 0x0a
   Descriptor: 00002901-0000-1000-8000-00805f9b34fb, Value: 546865726D6F6D6574657244656D6F2034
   Descriptor: 00002902-0000-1000-8000-00805f9b34fb, Value: 0000
  Char: 00002a21-0000-1000-8000-00805f9b34fb, Flags: read|write, Value: 0000
   Descriptor: 00002906-0000-1000-8000-00805f9b34fb, Value: 043C
   Descriptor: 00002902-0000-1000-8000-00805f9b34fb, Value: 0000
  Char: 0000ffe1-0000-1000-8000-00805f9b34fb, Flags: read|write|notify, Value: 0000
   Descriptor: 00002902-0000-1000-8000-00805f9b34fb, Value: 0000
  Char: 00002a1d-0000-1000-8000-00805f9b34fb, Flags: read, Value: 06
  Char: 00002a1c-0000-1000-8000-00805f9b34fb, Flags: indicate
   Descriptor: 00002904-0000-1000-8000-00805f9b34fb
   Descriptor: 00002902-0000-1000-8000-00805f9b34fb
Service: 00001801-0000-1000-8000-00805f9b34fb, Primary: True
  Char: 00002a05-0000-1000-8000-00805f9b34fb, Flags: indicate
   Descriptor: 00002902-0000-1000-8000-00805f9b34fb
			*/

			using (var mgr = new Manager())
			{
				mgr.Dump();

				mgr.Adapter = args[0];
				mgr.Open();

				if (args.Length == 2)
				{
					mgr.Device = args[1];
					mgr.Connect(false, true);

					var svc = mgr.RemoteServices[Guid.Parse("0000ffe0-0000-1000-8000-00805f9b34fb")];
					var chr = svc.Characteristics[Guid.Parse("0000ffe1-0000-1000-8000-00805f9b34fb")];

					var d = mgr.RemoteServices.SelectMany(x => x.Characteristics).SelectMany(x => x.Descriptors).First();

					Console.WriteLine(d.Introspect());

					chr.StartNotify();

					Console.WriteLine("Connected, press any key to continue");
				}
				else
				{
					mgr.Serve(app);

					var th = new Thread(() =>
					{
						var chr = app.Services
							.SelectMany(x => x.Characteristics)
							.First(x => x.Flags.Contains("notify"));

						var i = 0;

						while (true)
						{
							chr.Write(Encoding.UTF8.GetBytes((++i).ToString()));
							Thread.Sleep(1000);
						}
					});

					th.Start();

					Console.WriteLine("Registered, press any key to continue");
				}

				while (true)
				{
					mgr.Iterate();
					Thread.Sleep(1000);
					Console.WriteLine("Iterate");
				}
			}
		}
	}
}
