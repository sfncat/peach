using System;
using System.IO;

namespace Peach.Core
{
	public class TempFile : IDisposable
	{
		public string Path { get; private set; }

		public TempFile()
		{
			Path = System.IO.Path.Combine(
				System.IO.Path.GetTempPath(),
				"Peach-{0}".Fmt(Guid.NewGuid()));
		}

		public TempFile(string contents)
		{
			Path = System.IO.Path.Combine(
				System.IO.Path.GetTempPath(),
				"Peach-{0}".Fmt(Guid.NewGuid()));

			File.WriteAllText(Path, contents);
		}

		public void Dispose()
		{
			try { File.Delete(Path); }
			catch { }
		}

		public override string ToString()
		{
			return Path;
		}
	}

	public class TempDirectory : IDisposable
	{
		public string Path { get; private set; }

		public TempDirectory()
		{
			Path = System.IO.Path.Combine(
				System.IO.Path.GetTempPath(),
				"Peach-{0}".Fmt(Guid.NewGuid()));
			Directory.CreateDirectory(Path);
		}

		public void Dispose()
		{
			try 
			{ 
				Directory.Delete(Path, true); 
			}
			catch(Exception ex) 
			{
				Console.WriteLine("{0}: {1}", Path, ex.Message);
			}
		}

		public override string ToString()
		{
			return Path;
		}
	}
}
