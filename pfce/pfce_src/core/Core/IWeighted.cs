


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;

namespace Peach.Core
{
	/// <summary>
	/// Weight to use on random selection.
	/// </summary>
	public interface IWeighted
	{
		/// <summary>
		/// Selection weight used for weighted selection.
		/// </summary>
		int SelectionWeight { get; }

		/// <summary>
		/// Return transformed weight based on function.
		/// </summary>
		/// <param name="how"></param>
		/// <returns></returns>
		int TransformWeight(Func<int, int> how);
	}
}

// end
