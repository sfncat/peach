using System;
using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Pro.Core.Mutators.Utility;
using Double = Peach.Core.Dom.Double;
using String = Peach.Core.Dom.String;

namespace Peach.Pro.Core.Mutators
{
	[Mutator("DoubleVariance")]
	[Description("Produce random number in range of underlying element.")]
	public class DoubleVariance : Mutator
	{
		const int MaxCount = 5000; // Maximum count is 5000

		readonly DoubleVarianceGenerator _gen;

		public DoubleVariance(DataElement obj)
			: base(obj)
		{
			var val = (double)GetValue(obj);
			var abs = Math.Abs(val);

			double max;
			double min;
			double maxRange;

			var asDouble = obj as Double;

			if (asDouble != null && obj.lengthAsBits == 32)
			{
				max = Math.Min(abs + 10, float.MaxValue);
				min = Math.Max(-abs - 10, float.MinValue);
				maxRange = Math.Min(abs + 100, float.MaxValue / 3);
			}
			else
			{
				max = Math.Min(abs + 10, double.MaxValue);
				min = Math.Max(-abs - 10, double.MinValue);
				maxRange = Math.Min(abs + 100, double.MaxValue / 3);
			}

			_gen = new DoubleVarianceGenerator(val, min, max, maxRange);
		}

		/// <summary>
		/// Get the numbers value via InternalValue or DefaultValue
		/// </summary>
		/// <remarks>
		/// If InternalValue returns a non-int/long/ulong then fallback to DefaultValue
		/// </remarks>
		/// <param name="number"></param>
		/// <returns></returns>
		private static Variant GetValue(DataElement number)
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
					double result;

					if (System.Double.TryParse((string)value, out result))
						return value;

					break;
			}

			return number.DefaultValue;
		}

		// ReSharper disable once InconsistentNaming
		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is String && obj.isMutable)
				return obj.Hints.ContainsKey("NumericalString");

			var asDouble = obj as Double;
			if (asDouble != null)
			{
				var supported = !double.IsNaN((double)GetValue(asDouble));
				supported = supported && !double.IsInfinity((double)GetValue(asDouble));
				
				return obj.isMutable && supported;
			}

			return false;
		}

		public override int count
		{
			get
			{
				return MaxCount;
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
			obj.MutatedValue = new Variant(_gen.Next(context.Random));
			obj.mutationFlags = MutateOverride.Default;
		}
	}
}
