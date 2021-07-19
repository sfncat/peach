using System;
using NUnit.Framework;

namespace Peach.Core.Test
{
	class Parent : INamed
	{
		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		#endregion

		public string Name { get; set; }
	}

	class Child : INamed, IOwned<Parent>
	{
		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		#endregion

		public string Name { get; set; }
		public Parent parent { get; set; }
	}

	[TestFixture] 
	[Peach]
	[Quick]
	public class CollectionTests
	{
		[Test]
		public void TestAdd()
		{
			var p = new Parent() { Name = "parent" };
			var c1 = new Child() { Name = "child1" };
			var c2 = new Child() { Name = "child2" };

			var items = new OwnedCollection<Parent, Child>(p);

			Assert.Null(c1.parent);
			Assert.Null(c2.parent);

			items.Add(c1);
			Assert.AreEqual(p, c1.parent);

			items[0] = c2;
			Assert.Null(c1.parent);
			Assert.AreEqual(p, c2.parent);

			items.RemoveAt(0);
			Assert.Null(c2.parent);
		}
	}
}
