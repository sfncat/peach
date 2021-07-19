using System;

namespace Peach.Pro.Core.OS
{
	public interface ISingleInstance : IDisposable
	{
		bool TryLock();
		void Lock();
	}
}
