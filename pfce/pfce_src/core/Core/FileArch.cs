using System.IO;

namespace Peach.Core
{
	public static class FileArch
	{
		/// <summary>
		/// Returns the architecture of the specified PE file.
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public static Platform.Architecture GetWindows(string fileName)
		{
			const ushort IMAGE_FILE_MACHINE_AMD64 = 0x8664;
			const ushort IMAGE_FILE_MACHINE_I386 = 0x14c;

			//see http://www.microsoft.com/whdc/system/platform/firmware/PECOFF.mspx
			//offset to PE header is always at 0x3C
			//PE header starts with "PE\0\0" =  0x50 0x45 0x00 0x00
			//followed by 2-byte machine type field (see document above for enum)
			using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
			{
				var br = new BinaryReader(fs);
				fs.Seek(0x3c, SeekOrigin.Begin);
				var peOffset = br.ReadInt32();
				fs.Seek(peOffset, SeekOrigin.Begin);
				var peHead = br.ReadUInt32();
				if (peHead != 0x00004550) // "PE\0\0", little-endian
					throw new PeachException(fileName + " does not contain a valid PE header.");
				var machineType = br.ReadUInt16();

				switch (machineType)
				{
					case IMAGE_FILE_MACHINE_AMD64:
						return Platform.Architecture.x64;
					case IMAGE_FILE_MACHINE_I386:
						return Platform.Architecture.x86;
					default:
						throw new PeachException("{0} has unrecognized machine type 0x{1:X}.".Fmt(fileName, machineType));
				}
			}
		}

		/// <summary>
		/// Returns the architecture of the specified ELF file.
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public static Platform.Architecture GetLinux(string fileName)
		{
			// First 16 bytes are elf identification
			using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
			{
				var br = new BinaryReader(fs);
				fs.Seek(0, SeekOrigin.Begin);
				var ei_magic = br.ReadUInt32();
				if (ei_magic != 0x464c457f) // "\x7fELF", little-endian
					throw new PeachException(fileName + " does not contain a valid ELF header.");
				fs.Seek(18, SeekOrigin.Begin);
				var ei_machine = br.ReadUInt16();
				switch (ei_machine)
				{
					case 0x03:
						return Platform.Architecture.x86;
					case 0x3e:
						return Platform.Architecture.x64;
					default:
						throw new PeachException("{0} has unrecognized machine type 0x{1:X}.".Fmt(fileName, ei_machine));
				}
			}
		}
	}
}
