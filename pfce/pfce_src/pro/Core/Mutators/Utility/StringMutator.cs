//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.Text;
using NLog;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators.Utility
{
	/// <summary>
	/// Generate random strings using characters randomly selected
	/// from the specified range.  By default this mutator only
	/// supports unicode strings.
	/// </summary>
	public abstract class StringMutator : Utility.IntegerVariance
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Construct base string mutator
		/// </summary>
		/// <param name="obj">Data element to attach to.</param>
		public StringMutator(DataElement obj)
			: base(obj, true)
		{
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			var asStr = obj as Peach.Core.Dom.String;
			if (asStr != null && asStr.isMutable && asStr.stringType != StringType.ascii)
				return true;

			return false;
		}

		protected override NLog.Logger Logger
		{
			get { return logger; }
		}

		protected override void GetLimits(DataElement obj, out bool signed, out long value, out long min, out long max)
		{
			var str = (string)obj.InternalValue;

			signed = false;
			max = Utility.SizedHelpers.MaxSize(obj);
			min = Math.Min(max, 1);
			value = Math.Min(str.Length, max);
			value = Math.Max(min, value);
		}

		protected abstract int GetCodePoint();

		protected virtual string GetChar()
		{
			var cp = GetCodePoint();
			var ch = char.ConvertFromUtf32(cp);

			return ch;
		}

		protected override void performMutation(DataElement obj, long value)
		{
			System.Diagnostics.Debug.Assert(value <= int.MaxValue);

			var limit = Utility.SizedHelpers.MaxSize(obj);
			if (value > limit)
			{
				logger.Trace("Skipping mutation, expansion to {0} would exceed max output size.", value);
				return;
			}

			var sb = new StringBuilder((int)value);

			while (sb.Length < value)
				sb.Append(GetChar());

			obj.MutatedValue = new Variant(sb.ToString());
			obj.mutationFlags = MutateOverride.Default;
		}

		protected override void performMutation(DataElement obj, ulong value)
		{
			// Should never get a ulong
			throw new NotImplementedException();
		}
	}
}
