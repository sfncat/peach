using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Peach.Core;
using Peach.Core.Dom;
using Double = Peach.Core.Dom.Double;
using String = Peach.Core.Dom.String;

namespace Peach.Pro.Core.Mutators
{
	[Mutator("DoubleEdgeCase")]
	[Description("Produce edge case double values.")]
	public class DoubleEdgeCase : Mutator
	{
		private static readonly float[] SpecialFloats =
		{
			float.NegativeInfinity,
			float.MinValue,
			0.0f - float.Epsilon,
			-0.0f,
			float.NaN,
			0.0f,
			float.Epsilon,
			float.MaxValue,
			float.PositiveInfinity,
		};

		private static readonly double[] SpecialDoubles =
		{
			double.NegativeInfinity,
			double.MinValue,
			0.0d - double.Epsilon,
			-0.0d,
			double.NaN,
			0.0d,
			double.Epsilon,
			double.MaxValue,
			double.PositiveInfinity,
		};

		readonly Func<int, Variant> _gen;

		public DoubleEdgeCase(DataElement obj)
			: base(obj)
		{
			var asNum = obj as Double;

			if (asNum == null)
			{
				Debug.Assert(obj is String);

				_gen = i => new Variant(SpecialDoubles[i]);
			}
			else switch (asNum.lengthAsBits)
			{
				case 32:
					_gen = i => new Variant(SpecialDoubles[i]);
					break;
				case 64:
					_gen = i => new Variant(SpecialFloats[i]);
					break;
				default:
					throw new NotSupportedException();
			}
		}

		// ReSharper disable once InconsistentNaming
		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is String && obj.isMutable)
				return obj.Hints.ContainsKey("NumericalString");

			return obj is Double && obj.isMutable && (obj.lengthAsBits == 64 || obj.lengthAsBits == 32);
		}

		public override int count
		{
			get
			{
				return SpecialDoubles.Length;
			}
		}

		public override uint mutation
		{
			get;
			set;
		}

		public override void sequentialMutation(DataElement obj)
		{
			performMutation(obj, (int)mutation);
		}

		public sealed override void randomMutation(DataElement obj)
		{
			var idx = context.Random.Next(0, count);
			performMutation(obj, idx);
		}

		private void performMutation(DataElement obj, int index)
		{
			obj.mutationFlags = MutateOverride.Default;
			obj.MutatedValue = _gen(index);
		}

	}
}
