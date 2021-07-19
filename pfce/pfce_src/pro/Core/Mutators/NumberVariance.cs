//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.ComponentModel;
using NLog;
using Peach.Core;
using Peach.Core.Dom;
using String = Peach.Core.Dom.String;

namespace Peach.Pro.Core.Mutators
{
	[Mutator("NumberVariance")]
	[Description("Produce Gaussian distributed numbers around a numerical value.")]
	public class NumberVariance : Utility.IntegerVariance
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public NumberVariance(DataElement obj)
			: base(obj, false)
		{
		}

		protected override NLog.Logger Logger
		{
			get
			{
				return logger;
			}
		}

		/// <summary>
		/// Get the numbers value via InternalValue or DefaultValue
		/// </summary>
		/// <remarks>
		/// If InternalValue returns a non-int/long/ulong then fallback to DefaultValue
		/// </remarks>
		/// <param name="number"></param>
		/// <param name="signed"></param>
		/// <returns></returns>
		private static Variant GetValue(DataElement number, bool signed)
		{
			var value = number.InternalValue;

			switch (value.GetVariantType())
			{
				case Variant.VariantType.Double:
				case Variant.VariantType.Int:
				case Variant.VariantType.Long:
				case Variant.VariantType.ULong:
					return value;

				case Variant.VariantType.String:
					long longResult;
					ulong ulongResult;
					double doubleResult;

					if ((signed && Int64.TryParse((string) value, out longResult)) || UInt64.TryParse((string) value, out ulongResult))
						return value;

					if (double.TryParse((string)value, out doubleResult))
						return new Variant(doubleResult);

					break;
			}

			return number.DefaultValue;
		}

		protected override void GetLimits(DataElement obj, out bool signed, out long value, out long min, out long max)
		{
			var asNum = obj as Number;
			if (asNum != null)
			{
				signed = asNum.Signed;
				min = asNum.MinValue;
				max = (long)asNum.MaxValue;
				value = signed ? (long)GetValue(asNum, true) : (long)(ulong)GetValue(asNum, false);
			}
			else
			{
				System.Diagnostics.Debug.Assert(obj is String);

				signed = true;
				min = long.MinValue;
				max = long.MaxValue;
				value = (long) GetValue(obj, true);
			}
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is String && obj.isMutable)
				return obj.Hints.ContainsKey("NumericalString");

			return obj is Number && obj.isMutable;
		}

		protected override void performMutation(DataElement obj, long value)
		{
			obj.MutatedValue = new Variant(value);
			obj.mutationFlags = MutateOverride.Default;
		}

		protected override void performMutation(DataElement obj, ulong value)
		{
			obj.MutatedValue = new Variant(value);
			obj.mutationFlags = MutateOverride.Default;
		}
	}
}
