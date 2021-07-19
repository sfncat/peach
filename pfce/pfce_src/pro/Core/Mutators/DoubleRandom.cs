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
	[Mutator("DoubleRandom")]
	[Description("Produce random number in range of underlying element.")]
	public class DoubleRandom : Mutator
	{
		const int MaxCount = 5000; // Maximum count is 5000

		readonly Func<Variant> _gen;

		public DoubleRandom(DataElement obj)
			: base(obj)
		{
			var asNum = obj as Double;

			if (asNum == null)
			{
				Debug.Assert(obj is String);

				_gen = () => new Variant(BitConverter.Int64BitsToDouble(context.Random.NextInt64()).ToString(CultureInfo.InvariantCulture));
			}
			else switch (asNum.lengthAsBits)
			{
				case 32:
					_gen = () => new Variant(BitConverter.ToSingle(BitConverter.GetBytes(context.Random.NextInt32()), 0));
					break;
				case 64:
					_gen = () => new Variant(BitConverter.Int64BitsToDouble(context.Random.NextInt64()));
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
			// Same as random, but seed is fixed
			randomMutation(obj);
		}

		public override void randomMutation(DataElement obj)
		{
			obj.MutatedValue = _gen();
			obj.mutationFlags = MutateOverride.Default;
		}
	}
}
