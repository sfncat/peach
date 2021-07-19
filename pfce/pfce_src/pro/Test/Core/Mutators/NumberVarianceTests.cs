using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	[Quick]
	[Peach]
	class NumberVarianceTests
	{
		// TODO: Numerical string test
		// TODO: Sequential is +/- 50
		// TODO: Never get DefaultValue
		// TODO: If default value is 3 and unsigned, mutated value is 0 to 53

		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("NumberVariance");

			Assert.False(runner.IsSupported(new Blob()));

			Assert.True(runner.IsSupported(new Number() { length = 7 }));
			Assert.True(runner.IsSupported(new Number() { length = 8 }));
			Assert.True(runner.IsSupported(new Number() { length = 9 }));
			Assert.True(runner.IsSupported(new Number() { length = 32 }));

			Assert.True(runner.IsSupported(new Flag() { length = 7 }));
			Assert.True(runner.IsSupported(new Flag() { length = 8 }));
			Assert.True(runner.IsSupported(new Flag() { length = 9 }));
			Assert.True(runner.IsSupported(new Flag() { length = 32 }));

			Assert.False(runner.IsSupported(new Peach.Core.Dom.String() { DefaultValue = new Variant("Hello") }));
			Assert.True(runner.IsSupported(new Peach.Core.Dom.String() { DefaultValue = new Variant("0") }));
			Assert.True(runner.IsSupported(new Peach.Core.Dom.String() { DefaultValue = new Variant("100") }));
			Assert.True(runner.IsSupported(new Peach.Core.Dom.String() { DefaultValue = new Variant("-100") }));
		}
	}
}
