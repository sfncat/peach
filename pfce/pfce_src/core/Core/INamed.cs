using System;

namespace Peach.Core
{
	public interface INamed
	{
		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		string name { get; }

		#endregion

		string Name { get; }
	}
}
