using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core
{
	internal class StringMutatorTester : DataModelCollector
	{
		MutatorRunner runner;

		protected bool VerifyLength { get; set; }

		StringType[] encodings = new StringType[]
		{
			StringType.ascii,
			StringType.utf16,
			StringType.utf16be,
			StringType.utf32,
			StringType.utf32be,
			StringType.utf7,
			StringType.utf8,
		};

		public StringMutatorTester(string mutatorName)
		{
			runner = new MutatorRunner(mutatorName);
		}

		protected virtual IEnumerable<StringType> InvalidEncodings
		{
			get
			{
				return new StringType[0];
			}
		}

		protected virtual IEnumerable<StringType> ValidEncodings
		{
			get
			{
				foreach (var enc in encodings)
					if (!InvalidEncodings.Contains(enc))
						yield return enc;
			}
		}

		protected void RunSupported()
		{
			var str = new Peach.Core.Dom.String("String") { stringType = ValidEncodings.First() };

			str.isMutable = false;
			Assert.False(runner.IsSupported(str));

			str.isMutable = true;
			Assert.True(runner.IsSupported(str));

			foreach (var enc in encodings)
			{
				var supported = !InvalidEncodings.Contains(enc);
				str.stringType = enc;

				Assert.AreEqual(supported, runner.IsSupported(str));
			}
		}

		protected void RunSequential()
		{
			var str = new Peach.Core.Dom.String("String");

			foreach (var enc in ValidEncodings)
			{
				str.stringType = enc;

				// For counts, the count is string length +/- 50 with a min of 0

				// Len = 0 has [1,51]
				str.DefaultValue = new Variant("");
				var m1 = runner.Sequential(str);
				Verify(m1, 1, 51);

				// Len = 5 has [1,55]
				str.DefaultValue = new Variant("Hello");
				var m2 = runner.Sequential(str);
				Verify(m2, 1, 55);

				// Len = 50 has [1,100]
				str.DefaultValue = new Variant(new string('A', 50));
				var m3 = runner.Sequential(str);
				Verify(m3, 1, 100);

				// Len = 51 has [1,101]
				str.DefaultValue = new Variant(new string('A', 51));
				var m4 = runner.Sequential(str);
				Verify(m4, 1, 101);

				// Len = 100 has [50,150]
				str.DefaultValue = new Variant(new string('A', 100));
				var m5 = runner.Sequential(str);
				Verify(m5, 50, 150);
			}
		}

		protected void RunRandom()
		{
			var str = new Peach.Core.Dom.String("String");
			str.DefaultValue = new Variant(new string('A', 100));

			foreach (var enc in ValidEncodings)
			{
				str.stringType = enc;

				var m = runner.Random(1000, str);

				foreach (var item in m)
				{
					// InternalValue should always be a valid string
					Assert.AreEqual(Variant.VariantType.String, item.InternalValue.GetVariantType());
					var asStr = (string)item.InternalValue;

					// Should produce a valid string
					Assert.NotNull(asStr);

					// C# is utf16, so we need the length in text elements
					var info = new StringInfo(asStr);
					var len = info.LengthInTextElements;

					// Should be between 1 and 65535
					Assert.GreaterOrEqual(len, 1);
					Assert.LessOrEqual(len, ushort.MaxValue);

					// Buffer should be greater or equal to length in text elements
					var buf = item.Value.ToArray();
					Assert.GreaterOrEqual(buf.Length, len);
				}
			}
		}

		private void Verify(IEnumerable<MutatorRunner.Mutation> m, int min, int max)
		{
			var exp = max - min + 1;
			var cnt = m.Count();
			Assert.AreEqual(exp, cnt);

			var i = min;

			foreach (var item in m)
			{
				// InternalValue should always be a valid string
				Assert.AreEqual(Variant.VariantType.String, item.InternalValue.GetVariantType());
				var asStr = (string)item.InternalValue;

				// Should produce a valid string
				Assert.NotNull(asStr);

				if (VerifyLength && asStr.Length != i)
				{
					// C# is utf16, so we need the length in text elements
					var info = new StringInfo(asStr);
					var len = info.LengthInTextElements;

					// Character length should be the length
					Assert.AreEqual(len, i);
				}

				++i;

				// Buffer should be greater or equal to length in text elements
				var buf = item.Value.ToArray();
				Assert.GreaterOrEqual(buf.Length, asStr.Length);
			}
		}
	}
}
