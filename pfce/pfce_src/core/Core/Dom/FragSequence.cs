using System;
using System.Collections.Generic;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Specialized version of Sequence for Fragmentation support.
	/// </summary>
	/// <remarks>
	/// The main driving force for having a this class is the special
	/// handling of array expantion and contraction that is needed.
	/// 
	/// The Outfrag action will iterate over the children of this sequence
	/// outputting each child in a separate output action. Our normal
	/// way of expanding/contracting sequences will not work. Instead
	/// we need to override how iteration of this sequence works.
	/// </remarks>
	[Serializable]
	public class FragSequence : Sequence
	{
		public FragSequence()
			: base()
		{
		}

		public FragSequence(string name)
			: base(name)
		{
		}

		protected override string GetDisplaySuffix(DataElement child)
		{
			return "";
		}

		public IEnumerable<BitwiseStream> GetFragments()
		{
			var countOverride = GetCountOverride();

			// Output more items than normal
			if (countOverride > Count)
			{
				var itemCount = countOverride - Count;

				for(var cnt = 0; cnt < Count; cnt++)
				{
					// Expand item
					if (ExpandedValueIndex == cnt)
					{
						for (; itemCount > 0; itemCount--)
							yield return ExpandedValue;
					}

					yield return this[cnt].Value;
				}

				for (; itemCount > 0; itemCount--)
					yield return ExpandedValue;
			}
			// Output just our items
			else
			{
				foreach (var child in this)
					yield return child.Value;
			}
		}
	}
}
