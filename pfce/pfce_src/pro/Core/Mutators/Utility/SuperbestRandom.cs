using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Random = Peach.Core.Random;

namespace Peach.Pro.Core.Mutators.Utility
{
	/// <summary>
	/// https://bitbucket.org/Superbest/superbest-random
	/// </summary>
	public static class SuperbestRandom
	{
		/// <summary>
		///   Return a random number between 1 and 6 inclusive.
		///   The numbers are gaussian and centered around 1.
		/// </summary>
		/// <param name="r"></param>
		/// <returns></returns>
		public static int PickSix(this Random r)
		{
			while (true)
			{
				int i = (int)Math.Round(Math.Abs(r.NextGaussian(0, 1.6666))) + 1;

				if (i <= 6)
					return i;
			}
		}

		/// <summary>
		///   Generates normally distributed numbers. Each operation makes two Gaussians for the 
		///   price of one, and apparently they can be cached or something for better performance, 
		///   but who cares.
		/// </summary>
		/// <param name="r"></param>
		/// <param name = "mu">Mean of the distribution</param>
		/// <param name = "sigma">Standard deviation</param>
		/// <returns></returns>
		public static double NextGaussian(this Random r, double mu = 0, double sigma = 1)
		{
			var u1 = r.NextDouble();
			var u2 = r.NextDouble();

			var rand_std_normal = Math.Sqrt(-2.0 * Math.Log(u1)) *
								Math.Sin(2.0 * Math.PI * u2);

			var rand_normal = mu + sigma * rand_std_normal;

			return rand_normal;
		}

		/// <summary>
		///   Generates values from a triangular distribution.
		/// </summary>
		/// <remarks>
		/// See http://en.wikipedia.org/wiki/Triangular_distribution for a description 
		/// of the triangular probability 
		/// distribution and the algorithm for generating one.
		/// </remarks>
		/// <param name="r"></param>
		/// <param name = "a">Minimum</param>
		/// <param name = "b">Maximum</param>
		/// <param name = "c">Mode (most frequent value)</param>
		/// <returns></returns>
		public static double NextTriangular(this Random r, double a, double b, double c)
		{
			var u = r.NextDouble();

			return u < (c - a) / (b - a)
					   ? a + Math.Sqrt(u * (b - a) * (c - a))
					   : b - Math.Sqrt((1 - u) * (b - a) * (b - c));
		}

		/// <summary>
		///   Equally likely to return true or false./>.
		/// </summary>
		/// <returns></returns>
		public static bool NextBoolean(this Random r)
		{
			return r.Next(2) > 0;
		}

		/// <summary>
		///   Shuffles a list in O(n) time by using the Fisher-Yates/Knuth algorithm.
		/// </summary>
		/// <param name="r"></param>
		/// <param name = "list"></param>
		public static void Shuffle(this Random r, IList list)
		{
			for (var i = 0; i < list.Count; i++)
			{
				var j = r.Next(0, i + 1);

				var temp = list[j];
				list[j] = list[i];
				list[i] = temp;
			}
		}

		/// <summary>
		/// Returns min(n,k) unique random numbers in the range [1, n], inclusive. 
		/// This is equivalent to getting the first n numbers of some random permutation of 
		/// the sequential numbers from 1 to max. 
		/// Runs in O(k^2) time.
		/// </summary>
		/// <param name="rand"></param>
		/// <param name="n">Maximum number possible.</param>
		/// <param name="k">How many numbers to return.</param>
		/// <returns></returns>
		public static int[] Permutation(this Random rand, int n, int k)
		{
			var result = new List<int>();
			var sorted = new SortedSet<int>();
			var cnt = Math.Min(n, k);

			for (var i = 0; i < cnt; i++)
			{
				var r = rand.Next(1, n + 1 - i);

				foreach (var q in sorted)
					if (r >= q) r++;

				result.Add(r);
				sorted.Add(r);
			}

			return result.ToArray();
		}

		/// <summary>
		/// Returns min(n,k) unique random numbers in the range [1, n], inclusive. 
		/// This is equivalent to getting the first n numbers of some random permutation of 
		/// the sequential numbers from 1 to max. 
		/// Runs in O(k^2) time.
		/// </summary>
		/// <param name="rand"></param>
		/// <param name="n">Maximum number possible.</param>
		/// <param name="k">How many numbers to return.</param>
		/// <returns></returns>
		public static long[] Permutation(this Random rand, long n, long k)
		{
			var result = new List<long>();
			var sorted = new SortedSet<long>();
			var cnt = Math.Min(n, k);

			for (long i = 0; i < cnt; i++)
			{
				var r = rand.Next(1, n + 1 - i);

				foreach (var q in sorted)
					if (r >= q) r++;

				result.Add(r);
				sorted.Add(r);
			}

			return result.ToArray();
		}

		/// <summary>
		/// Returns min(n,k) unique random numbers in the range [1, n], inclusive. 
		/// This is equivalent to getting the first n numbers of some random permutation of 
		/// the sequential numbers from 1 to max. 
		/// Runs in O(k^2) time.
		/// </summary>
		/// <param name="rand"></param>
		/// <param name="n">Maximum number possible.</param>
		/// <param name="k">How many numbers to return.</param>
		/// <returns></returns>
		public static long[] SortedPermutation(this Random rand, long n, long k)
		{
			var result = new List<long>();
			var sorted = new SortedSet<long>();
			var cnt = Math.Min(n, k);

			for (long i = 0; i < cnt; i++)
			{
				var r = rand.Next(1, n + 1 - i);

				foreach (var q in sorted)
					if (r >= q) r++;

				result.Add(r);
				sorted.Add(r);
			}

			return sorted.ToArray();
		}
	}
}
