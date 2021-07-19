using System;
using System.Collections.Generic;
using System.Reflection;
using Peach.Core;
using Peach.Pro.Core.Mutators.Utility;
using Random = Peach.Core.Random;

// This assembly contains Peach plugins
[assembly: PluginAssembly]

namespace Peach.Pro.Core
{
	public static class Extensions
	{
		public static T GetStaticField<T>(this Type type, string name)
		{
			var bindingAttr = BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy;
			var field = type.GetField(name, bindingAttr);
			var ret = (T)field.GetValue(null);
			return ret;
		}

		public static T WeightedChoice<T>(this Random rng, WeightedList<T> list) where T : IWeighted
		{
			// returns between 0 <= x < UpperBound
			var val = rng.Next(list.Max);

			// Finds first element with sum-weight greater than value
			var ret = list.UpperBound(val);

			return ret.Item;
		}

		public static T[] WeightedSample<T>(this Random rng, WeightedList<T> list, int count) where T : IWeighted
		{
			// Shrink count so that we return the list
			// in a weighted order.
			if (count > list.Count)
				count = list.Count;

			var max = list.Max;
			var ret = new List<T>();
			var conversions = new SortedList<long, Func<long, long>>();

			for (int i = 0; i < count; ++i)
			{
				var rand = rng.Next(max);

				foreach (var c in conversions)
					rand = c.Value(rand);

				var item = list.UpperBound(rand);
				var weight = item.UpperBound - item.LowerBound;

				conversions.Add(item.UpperBound, (c) => {
					if (c >= item.LowerBound)
						return c + weight;
					else
						return c;
				});

				System.Diagnostics.Debug.Assert(!ret.Contains(item.Item));
				System.Diagnostics.Debug.Assert(max >= weight);

				ret.Add(item.Item);
				max -= weight;
			}

			return ret.ToArray();
		}

		public static IEnumerable<TSource> DistinctBy<TSource, TKey>
			(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
			HashSet<TKey> seenKeys = new HashSet<TKey>();
			foreach (TSource element in source)
			{
				if (seenKeys.Add(keySelector(element)))
				{
					yield return element;
				}
			}
		}
	}
}
