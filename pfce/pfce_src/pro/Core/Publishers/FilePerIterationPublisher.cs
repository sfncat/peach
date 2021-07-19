using System;
using System.Collections.Generic;
using Peach.Core;

namespace Peach.Pro.Core.Publishers
{
	[Publisher("FilePerIteration")]
	[Parameter("FileName", typeof(string), "Name of file to open for reading/writing")]
	public class FilePerIterationPublisher : FilePublisher
	{
		protected string fileTemplate;

		public FilePerIterationPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			fileTemplate = FileName;

			try
			{
				setFileName(0);

				if (FileName == fileTemplate)
					throw new PeachException("Error, FileName \"" + fileTemplate + "\" missing iteration format identifier.");

				FileName = null;
			}
			catch (FormatException ex)
			{
				throw new PeachException("Error, FileName \"" + fileTemplate + "\" is not a valid format string.", ex);
			}
		}

		protected void setFileName(uint iteration)
		{
			FileName = string.Format(fileTemplate, iteration);

			if (IsControlIteration)
				FileName += ".Control";
		}

		protected override void OnOpen()
		{
			setFileName(this.Iteration);
			base.OnOpen();
		}
	}
}
