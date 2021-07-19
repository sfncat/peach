using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Peach.Core;

namespace Peach.Pro.Core.Mutators.Utility
{
	class WeightedListDebugView<T> where T : IWeighted
	{
		WeightedList<T> obj;

		public WeightedListDebugView(WeightedList<T> obj)
		{
			this.obj = obj;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public KeyValuePair<long, T>[] Items
		{
			get { return obj.items.ToArray(); }
		}
	}

	/// <summary>
	/// A collection of IWeighted items.
	/// The collection tracks the sum total weights allowing for random
	/// choices to honor the various weights of the elements.
	/// </summary>
	/// <remarks>
	/// NOTE: The value of the SelectionWeight is cached at insertion time.
	/// All users of this container must ensure that the SelectionWeight
	/// does not change once the element is added to the container.
	/// If the SelectionWeight is changed, the change will not be reflected
	/// in the sum total weights.
	/// </remarks>
	/// <typeparam name="T"></typeparam>
	[DebuggerDisplay("Count = {Count}, Max = {Max}")]
	[DebuggerTypeProxy(typeof(WeightedListDebugView<>))]
	public class WeightedList<T> : ICollection<T>, IWeighted where T : IWeighted
	{
		internal SortedList<long, T> items = new SortedList<long, T>();

		/// <summary>
		/// Represents a bounded item.
		/// </summary>
		public class BoundedItem
		{
			public long LowerBound { get; set; }
			public long UpperBound { get; set; }
			public T Item { get; set; }
		}

		/// <summary>
		/// The sum of all weights of contained elements.
		/// </summary>
		public long Max
		{
			get
			{
				return Count == 0 ? 0 : items.Keys[Count - 1];
			}
		}

		/// <summary>
		/// Constructs a weighted list.
		/// </summary>
		public WeightedList()
		{
		}

		/// <summary>
		/// Constructs a weighted list and pouplates it with elements from a sequence.
		/// </summary>
		/// <param name="collection">Sequence of items to add to the weighted list.</param>
		public WeightedList(IEnumerable<T> collection)
		{
			AddRange(collection);
		}

		/// <summary>
		/// Find the upper bound using weights.
		/// Returns the first element in the list with a key greater than value.
		/// </summary>
		/// <param name="value">A number greater than or equal to 0 and less than Max.</param>
		/// <returns>The lower bound and the upper bound and the element.</returns>
		public BoundedItem UpperBound(long value)
		{
			if (value < 0 || value >= Max)
				throw new ArgumentOutOfRangeException("value");

			var keys = items.Keys;
			var low = 0;
			var high = keys.Count - 1;

			while (low < high)
			{
				var idx = (low + high) / 2;
				if (keys[idx] <= value)
					low = idx + 1;
				else
					high = idx - 1;
			}

			if (keys[low] <= value)
				++low;

			return new BoundedItem
			{
				LowerBound = low > 0 ? items.Keys[low - 1] : 0,
				UpperBound = items.Keys[low],
				Item = items.Values[low]
			};
		}

		/// <summary>
		/// Returns the element at the specified index
		/// </summary>
		/// <param name="index">Index of the element to return.</param>
		/// <returns>Element at specified index.</returns>
		public T this[int index]
		{
			get
			{
				return items.Values[index];
			}
		}

		/// <summary>
		/// Add a collection of IWeighted elements to the collection.
		/// </summary>
		/// <param name="collection"></param>
		public void AddRange(IEnumerable<T> collection)
		{
			foreach (var i in collection)
				Add(i);
		}

		/// <summary>
		/// The selection weight for this collection.
		/// </summary>
		public int SelectionWeight
		{
			get
			{
				return (int)Math.Min(int.MaxValue, Max);
			}
		}

		/// <summary>
		/// Recompute the weights of the items using a transform function.
		/// </summary>
		/// <param name="how">Given current item weight, compute a new weight</param>
		/// <returns>The updated selection weight for the container.</returns>
		public int TransformWeight(Func<int, int> how)
		{
			var weight = 0;
			var newItems = new SortedList<long, T>();

			foreach (var kv in items)
			{
				weight += kv.Value.TransformWeight(how);
				newItems.Add(weight, kv.Value);
			}

			items = newItems;

			return how(SelectionWeight);
		}

		#region ICollection<T>

		public void Add(T item)
		{
			items.Add(Max + item.SelectionWeight, item);
		}

		public void Clear()
		{
			items.Clear();
		}

		public bool Contains(T item)
		{
			return items.ContainsValue(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			items.Values.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return items.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T item)
		{
			var idx = items.IndexOfValue(item);
			if (idx < 0)
				return false;

			items.RemoveAt(idx);
			return true;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return items.Values.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
