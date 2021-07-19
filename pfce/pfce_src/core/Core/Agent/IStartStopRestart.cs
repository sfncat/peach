using System;

namespace Peach.Core.Agent
{
	public interface IStartStopRestart : IDisposable
	{
		void Stop();

		void Start();

		void Restart();
	}
}

