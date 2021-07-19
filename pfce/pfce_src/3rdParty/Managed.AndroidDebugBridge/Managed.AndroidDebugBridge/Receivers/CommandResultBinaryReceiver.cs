using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Managed.Adb
{
	public class CommandResultBinaryReceiver : IShellOutputReceiver
	{

		List<byte> res = new List<byte>();

		/// <summary>
		/// Adds the output.
		/// </summary>
		/// <param name="data">The data.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="length">The length.</param>
		public void AddOutput(byte[] data, int offset, int length)
		{
			if (!IsCancelled)
			{
				res.AddRange(data.Skip(offset).Take(length).ToArray());
			}
		}
		/// <summary>
		/// Flushes the output. 
		/// </summary>
		/// <remarks>This should always be called at the end of the "process" in order to indicate that the data is ready to be processed further if needed.</remarks>
		public void Flush ( ) {	}

		/// <summary>
		/// Gets a value indicating whether this instance is cancelled.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is cancelled; otherwise, <c>false</c>.
		/// </value>
		public virtual bool IsCancelled { get; protected set; }


		/// <summary>
		/// Gets the result.
		/// </summary>
		/// <value>
		/// The result.
		/// </value>
		public byte[] Result { 
			get
			{
				return res.ToArray();
			}
		}
	}
}
