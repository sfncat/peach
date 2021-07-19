//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.ComponentModel;
using System.Text;
using NLog;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Pro.Core.Mutators.Utility;

namespace Peach.Pro.Core.Mutators
{
	/// <summary>
	/// Picks a gaussian random number X centered on 1, with a
	/// sigma of 1/3 the string length.
	/// Then, pick X random indices in the string.
	/// At each selected index, toggle the case of the character.
	/// </summary>
	[Mutator("StringCaseRandom")]
	[Description("Change the case of random characters in the string.")]
	public class StringCaseRandom : Mutator
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		int total;

		public StringCaseRandom(DataElement obj)
			: base(obj)
		{
			var str = (string)obj.InternalValue;

			// For sequential, use the length total number of mutations
			total = str.Length;
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Peach.Core.Dom.String && obj.isMutable)
			{
				// Esure the string changes when changing the case.
				// TODO: Investigate if it is faster to go 1 char at a time.
				var str = (string)obj.InternalValue;

				if (str != str.ToUpper())
					return true;

				if (str != str.ToLower())
					return true;
			}

			return false;
		}

		public override int count
		{
			get
			{
				return total;
			}
		}

		public override uint mutation
		{
			get;
			set;
		}

		public override void sequentialMutation(DataElement obj)
		{
			randomMutation(obj);
		}

		public override void randomMutation(DataElement obj)
		{
			string asStr;

			try
			{
				asStr = (string)obj.InternalValue;
			}
			catch (NotSupportedException ex)
			{
				logger.Debug("Skipping mutation of {0}, {1}", obj.debugName, ex.Message);
				return;
			}

			var sb = new StringBuilder(asStr);

			// Pick gaussian from 1 to string length
			var stddev = sb.Length / 3;
			var cnt = 0;

			do
			{
				cnt = (int)Math.Round(Math.Abs(context.Random.NextGaussian(0, stddev))) + 1;
			}
			while (cnt > sb.Length);

			// Pick cnt indices
			var indices = context.Random.Permutation(sb.Length, cnt);

			for (int i = 0; i < indices.Length; ++i)
			{
				// Permutation is [1,Length] inclusive
				var idx = indices[i] - 1;

				var ch = sb[idx];
				var upper = char.ToUpper(ch);

				// Toggle the case at the picked indices
				if (ch != upper)
					sb[idx] = upper;
				else
					sb[idx] = char.ToLower(ch);
			}

			obj.MutatedValue = new Variant(sb.ToString());
			obj.mutationFlags = MutateOverride.Default;
		}
	}
}
