


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NLog;
using Peach.Core;
using Peach.Core.IO;

namespace Peach.Pro.Core.Publishers
{
	[Publisher("File")]
	[Alias("FileStream")]
	[Alias("file.FileWriter")]
	[Alias("file.FileReader")]
	[Parameter("FileName", typeof(string), "Name of file to open for reading/writing")]
	[Parameter("Overwrite", typeof(bool), "Replace existing file? [true/false, default true]", "true")]
	[Parameter("Append", typeof(bool), "Append to end of file [true/false, default flase]", "false")]
	public class FilePublisher : Peach.Core.Publishers.StreamPublisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		public string FileName { get; protected set; }
		public bool Overwrite { get; protected set; }
		public bool Append { get; protected set; }

		private const int maxOpenAttempts = 10;
		private readonly FileMode fileMode;

		public FilePublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			if (Overwrite && Append)
				throw new PeachException("File publisher does not support Overwrite and Append being enabled at once.");

			if (Overwrite)
				fileMode = FileMode.Create;
			else if (Append)
				fileMode = FileMode.OpenOrCreate | FileMode.Append;
			else
				fileMode = FileMode.OpenOrCreate;
		}

		protected override void OnOpen()
		{
			Debug.Assert(stream == null);

			var dir = Path.GetDirectoryName(FileName);
			if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			try
			{
				Retry.Execute(() =>
				{
					stream = File.Open(FileName, fileMode);
				}, TimeSpan.FromMilliseconds(200), maxOpenAttempts);
			}
			catch (Exception ex)
			{
				Logger.Error("Could not open file '{0}' after {1} attempts.  {2}", FileName, maxOpenAttempts, ex.Message);
				throw new SoftException(ex);
			}
		}

		protected override void OnClose()
		{
			Debug.Assert(stream != null);

			try
			{
				stream.Close();
			}
			catch (Exception ex)
			{
				logger.Error(ex.Message);
			}

			stream = null;
		}

		protected override void OnOutput(BitwiseStream data)
		{
			data.CopyTo(stream, BitwiseStream.BlockCopySize);
		}
	}
}
