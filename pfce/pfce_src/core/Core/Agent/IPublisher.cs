using System;
using System.Collections.Generic;
using System.IO;
using Peach.Core.IO;

namespace Peach.Core.Agent
{
	public interface IPublisher : IDisposable
	{
		Stream InputStream { get; }

		void Open(uint iteration, bool isControlIteration, bool isControlRecordingIteration, bool isIterationAfterFault);
		void Close();
		void Accept();
		Variant Call(string method, List<BitwiseStream> args);
		void SetProperty(string property, Variant value);
		Variant GetProperty(string property);
		void Output(BitwiseStream data);
		void Input();
		void WantBytes(long count);
	}
}
