//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.Linq;
using System.Text;
using NLog;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators.Utility
{
	public static class SizedHelpers
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public static void ExpandTo(DataElement obj, long length, bool overrideRelation)
		{
			obj.mutationFlags = MutateOverride.Default;

			var sizeRelation = obj.relations.From<SizeRelation>().FirstOrDefault(r => r.Of.InScope());
			if (sizeRelation == null)
			{
				logger.Error("Error, sizeRelation == null, unable to perform mutation.");
				return;
			}

			var objOf = sizeRelation.Of;
			if (objOf == null)
			{
				logger.Error("Error, sizeRelation.Of == null, unable to perform mutation.");
				return;
			}

			objOf.mutationFlags = MutateOverride.Default;
			objOf.mutationFlags |= MutateOverride.TypeTransform;
			objOf.mutationFlags |= MutateOverride.Transformer;

			if (overrideRelation)
			{
				// Indicate we are overrideing the relation
				objOf.mutationFlags |= MutateOverride.Relations;

				// Keep size indicator the same
				obj.MutatedValue = obj.InternalValue;
			}

			var data = objOf.Value;

			if (sizeRelation.lengthType == LengthType.Bytes)
				data = data.GrowTo(length);
			else
				data = data.GrowToBits(length);

			objOf.MutatedValue = new Variant(data);

		}

		// Artificially limit the maximum expansion to be 65k
		// This is to work around OutOfMemoryExceptions when
		// we try and do BitStream.GrowBy((uint/MaxValue / 4) - 1)
		const long maxExpansion = ushort.MaxValue;

		/// <summary>
		/// Returns the maximum number of bytes the element can be
		/// and still be under the limit of the MaxOutputSize attribute.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static long MaxSize(DataElement obj)
		{
			// For testing.  Figure out a way to not have this check in here
			var root = obj.root as DataModel;
			if (root == null)
				return maxExpansion;
			if (root.actionData == null)
				return maxExpansion;

			var max = (long)root.actionData.MaxOutputSize * 8;
			if (max == 0)
				return maxExpansion;

			// When maxOutputSize hint is greater than the size of an individual
			// element, artificially increase the maxOutputSize to be the size of
			// the element

			var used = root.Value.LengthBits;
			var size = obj.Value.LengthBits;
			var limit = Math.Max(max - used + size, size);

			limit = (limit + 7) / 8;

			return Math.Min(maxExpansion, limit);
		}

		/// <summary>
		/// Returns the maximum number of times the element can be duplicated
		/// by and still be under the limit of the MaxOutputSize attribute.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static long MaxDuplication(DataElement obj)
		{
			// For testing.  Figure out a way to not have this check in here
			var root = obj.root as DataModel;
			if (root == null)
				return maxExpansion;
			if (root.actionData == null)
				return maxExpansion;

			var max = (long)root.actionData.MaxOutputSize * 8;
			if (max == 0)
				return maxExpansion;

			var size = obj.Value.LengthBits;
			if (size == 0)
				return 0;

			var used = root.Value.LengthBits;
			if (max < used)
				return 0;

			var avail = (max - used) / size;

			return Math.Min(maxExpansion, avail);
		}

		public static void ExpandStringTo(DataElement obj, long value)
		{
			var src = (string)obj.InternalValue;
			var dst = ExpandTo(src, value);

			obj.MutatedValue = new Variant(dst);
			obj.mutationFlags = MutateOverride.Default;
		}

		static string ExpandTo(string value, long length)
		{
			if (string.IsNullOrEmpty(value))
			{
				return new string('A', (int)length);
			}
			else if (value.Length >= length)
			{
				return value.Substring(0, (int)length);
			}

			var sb = new StringBuilder();

			while (sb.Length + value.Length < length)
				sb.Append(value);

			sb.Append(value.Substring(0, (int)(length - sb.Length)));

			return sb.ToString();
		}
	}
}
