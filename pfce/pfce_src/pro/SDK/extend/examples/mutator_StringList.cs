using System.IO;
using Peach.Core;
using Peach.Core.Dom;
using System.ComponentModel;

namespace Peach.Pro.Core.Mutators
{
	[Mutator("StringListExample")]
	[Description("Mutates string using a lines from a user defined file.")]
	[Hint("StringList", "Filename with one string per line to use as test cases")]
	public class StringList : Mutator
	{
		string[] values;

		public StringList(DataElement obj)
			: base(obj)
		{
			var str = getHint(obj, "StringList");

			if (File.Exists(str))
				values = File.ReadAllLines(str);
			else
				throw new PeachException("The file '{0}' specified in the 'StringList' hint on {1} does not exist.".Fmt(str, obj.debugName));
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Peach.Core.Dom.String && obj.isMutable)
			{
				var str = getHint(obj, "StringList");
				if (!string.IsNullOrEmpty(str))
					return true;
			}

			return false;
		}

		public override int count
		{
			get
			{
				return values.Length;
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

		public override void randomMutation(DataElement obj)
		{

			performMutation(obj, context.Random.Next(values.Length));
		}

		void performMutation(DataElement obj, int index)
		{
			obj.MutatedValue = new Variant(values[index]);
			obj.mutationFlags = MutateOverride.Default;
		}
	}
}

