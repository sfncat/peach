using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core;
using System.ComponentModel;
using System.Linq;

namespace Peach.Pro.Test.Core
{
	#region Helper Classes

	public enum BasicEnum {
		[XmlEnum("morning")]
		Morning,

		[XmlEnum("afternoon")]
		Afternoon,

		[XmlEnum("evening")]
		Evening,
	}

	/// <summary>
	/// All child elements occur many times
	/// </summary>
	[XmlRoot("Root")]
	public class ManyChildren
	{
		public abstract class ChildElement
		{
			[XmlAttribute("name")]
			public string Name { get; set; }

			[XmlAttribute("timeOfDay")]
			[DefaultValue(BasicEnum.Afternoon)]
			public BasicEnum TimeOfDay { get; set; }

			public override int GetHashCode()
			{
				return Name.GetHashCode() ^ TimeOfDay.GetHashCode();
			}

			public override bool Equals(object obj)
			{
				if (obj == null || obj.GetType() != this.GetType())
				{
					return false;
				}

				var other = obj as ChildElement;
				return this.Name == other.Name && this.TimeOfDay == other.TimeOfDay;
			}
		}

		public class ChildOne : ChildElement
		{
		}

		public class ChildTwo : ChildElement
		{
		}

		public class ChildThree : ChildElement
		{
		}

		public class ChildFour : ChildElement
		{
		}

		[XmlElement("ChildOne", Type = typeof(ChildOne))]
		[XmlElement("ChildTwo", Type = typeof(ChildTwo))]
		[XmlElement("ChildThree", Type = typeof(ChildThree))]
		public List<ChildElement> Children { get; set; }

		[XmlElement("ChildFour")]
		public List<ChildFour> Fours { get; set; }

		public override int GetHashCode()
		{
			return Children.GetHashCode() ^ Fours.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null || obj.GetType() != this.GetType())
			{
				return false;
			}

			var other = obj as ManyChildren;
			return other.Children.SequenceEqual(Children) && other.Fours.SequenceEqual(Fours);
		}
	}

	/// <summary>
	/// All children occur a single time
	/// </summary>
	[XmlRoot("Root")]
	public class SingleChildren
	{
		public class Child
		{
			[XmlAttribute("name")]
			public string Name { get; set; }

			[XmlAttribute("timeOfDay")]
			[DefaultValue(BasicEnum.Afternoon)]
			public BasicEnum TimeOfDay { get; set; }

			public override int GetHashCode()
			{
				return Name.GetHashCode() ^ TimeOfDay.GetHashCode();
			}

			public override bool Equals(object obj)
			{
				if (obj == null || obj.GetType() != this.GetType())
				{
					return false;
				}

				var other = obj as Child;
				return this.Name == other.Name && this.TimeOfDay == other.TimeOfDay;
			}
		}

		[XmlElement("ChildOne")]
		public Child ChildOne { get; set; }

		[XmlElement("ChildTwo")]
		public Child ChildTwo { get; set; }

		public override int GetHashCode()
		{
			return ChildOne.GetHashCode() ^ ChildTwo.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null || obj.GetType() != this.GetType())
			{
				return false;
			}

			var other = obj as SingleChildren;
			return other.ChildOne.Equals(ChildOne) && other.ChildTwo.Equals(ChildTwo);
		}
	}

	/// <summary>
	/// ChildOne and ChildTwo occur many times
	/// ChildThree and ChildFour occur a single time
	/// </summary>
	[XmlRoot("Root")]
	public class MixedChildren
	{
		public abstract class ChildElement
		{
			[XmlAttribute("name")]
			public string Name { get; set; }

			[XmlAttribute("timeOfDay")]
			[DefaultValue(BasicEnum.Afternoon)]
			public BasicEnum TimeOfDay { get; set; }

			public override int GetHashCode()
			{
				return Name.GetHashCode() ^ TimeOfDay.GetHashCode();
			}

			public override bool Equals(object obj)
			{
				if (obj == null || obj.GetType() != this.GetType())
				{
					return false;
				}

				var other = obj as ChildElement;
				return this.Name == other.Name && this.TimeOfDay == other.TimeOfDay;
			}
		}

		public class ChildOne : ChildElement
		{
		}

		public class ChildTwo : ChildElement
		{
		}

		public class ChildThree : ChildElement
		{
		}

		public class ChildFour : ChildElement
		{
		}

		[XmlElement("ChildOne", Type = typeof(ChildOne))]
		[XmlElement("ChildTwo", Type = typeof(ChildTwo))]
		public List<ChildElement> Children { get; set; }

		[XmlElement("ChildThree")]
		public ChildThree Three { get; set; }

		[XmlElement("ChildFour")]
		public ChildFour Four { get; set; }

		public override int GetHashCode()
		{
			return Children.GetHashCode() ^ Three.GetHashCode() ^ Four.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null || obj.GetType() != this.GetType())
			{
				return false;
			}

			var other = obj as MixedChildren;
			return other.Children.SequenceEqual(Children) && other.Three.Equals(Three) && other.Four.Equals(Four);
		}
	}

	#endregion

	[TestFixture]
	[Quick]
	class XmlToolsTests
	{
		static T Deserialize<T>(string xml)
		{
			var rdr = new StringReader(xml);
			var ret = XmlTools.Deserialize<T>(rdr);

			return ret;
		}

		static void AssertRoundtrips<T>(T obj)
		{
			using (var stream = new MemoryStream())
			{
				XmlTools.Serialize<T>(stream, obj);
				stream.Seek(0, SeekOrigin.Begin);
				using (var txt = new StreamReader(stream))
				{
					var xml = txt.ReadToEnd();
					Assert.AreEqual(obj, Deserialize<T>(xml));
				}
			}
		}

		[Test]
		public void TestSingle()
		{
			var obj = Deserialize<SingleChildren>(@"<Root/>");

			Assert.NotNull(obj);
			Assert.Null(obj.ChildOne);
			Assert.Null(obj.ChildTwo);

			obj = Deserialize<SingleChildren>(@"
<Root>
	<ChildOne name='one'/>
	<ChildTwo name='two' timeOfDay='morning'/>
</Root>");
			AssertRoundtrips(obj);

			Assert.NotNull(obj);
			Assert.NotNull(obj.ChildOne);
			Assert.AreEqual("one", obj.ChildOne.Name);
			Assert.AreEqual(BasicEnum.Afternoon, obj.ChildOne.TimeOfDay);
			Assert.NotNull(obj.ChildTwo);
			Assert.AreEqual("two", obj.ChildTwo.Name);
			Assert.AreEqual(BasicEnum.Morning, obj.ChildTwo.TimeOfDay);

			obj = Deserialize<SingleChildren>(@"
<Root>
	<ChildTwo name='two'/>
	<ChildOne name='one' timeOfDay='evening'/>
</Root>");
			AssertRoundtrips(obj);

			Assert.NotNull(obj);
			Assert.NotNull(obj.ChildOne);
			Assert.AreEqual("one", obj.ChildOne.Name);
			Assert.AreEqual(BasicEnum.Evening, obj.ChildOne.TimeOfDay);
			Assert.NotNull(obj.ChildTwo);
			Assert.AreEqual("two", obj.ChildTwo.Name);
			Assert.AreEqual(BasicEnum.Afternoon, obj.ChildTwo.TimeOfDay);

			var ex = Assert.Throws<PeachException>(() =>
				Deserialize<SingleChildren>(@"
<Root>
	<ChildTwo name='aaa'/>
	<ChildTwo name='ccc'/>
	<ChildOne name='one'/>
</Root>"));

			StringAssert.Contains("failed to validate", ex.Message);
		}

		[Test]
		public void TestMany()
		{
			var obj = Deserialize<ManyChildren>(@"<Root/>");

			Assert.NotNull(obj);
			Assert.NotNull(obj.Children);
			Assert.AreEqual(0, obj.Children.Count);
			Assert.NotNull(obj.Fours);
			Assert.AreEqual(0, obj.Fours.Count);

			obj = Deserialize<ManyChildren>(@"
<Root>
	<ChildFour name='four1' timeOfDay='evening'/>
	<ChildThree name='three1'/>
	<ChildTwo name='two1'/>
	<ChildOne name='one1'/>
	<ChildOne name='one2'/>
	<ChildTwo name='two2'/>
	<ChildThree name='three2'/>
	<ChildFour name='four2' />
</Root>");
			AssertRoundtrips(obj);

			Assert.NotNull(obj);
			Assert.NotNull(obj.Children);
			Assert.AreEqual(6, obj.Children.Count);
			Assert.NotNull(obj.Fours);
			Assert.AreEqual(2, obj.Fours.Count);

			Assert.AreEqual("three1", obj.Children[0].Name);
			Assert.AreEqual("two1", obj.Children[1].Name);
			Assert.AreEqual("one1", obj.Children[2].Name);
			Assert.AreEqual("one2", obj.Children[3].Name);
			Assert.AreEqual("two2", obj.Children[4].Name);
			Assert.AreEqual(BasicEnum.Afternoon, obj.Children[4].TimeOfDay);
			Assert.AreEqual("three2", obj.Children[5].Name);

			Assert.AreEqual("four1", obj.Fours[0].Name);
			Assert.AreEqual(BasicEnum.Evening, obj.Fours[0].TimeOfDay);
			Assert.AreEqual("four2", obj.Fours[1].Name);
		}

		[Test]
		public void TestMixed()
		{
			var obj = Deserialize<MixedChildren>(@"<Root/>");

			Assert.NotNull(obj);
			Assert.NotNull(obj.Children);
			Assert.AreEqual(0, obj.Children.Count);
			Assert.Null(obj.Three);
			Assert.Null(obj.Four);

			obj = Deserialize<MixedChildren>(@"
<Root>
	<ChildFour name='four1' timeOfDay='morning'/>
	<ChildThree name='three1'/>
	<ChildTwo name='two1' timeOfDay='evening'/>
	<ChildOne name='one1'/>
	<ChildOne name='one2'/>
	<ChildTwo name='two2'/>
	<ChildThree name='three2'/>
	<ChildFour name='four2' />
</Root>");
			AssertRoundtrips(obj);

			Assert.NotNull(obj);
			Assert.NotNull(obj.Children);
			Assert.AreEqual(4, obj.Children.Count);
			Assert.NotNull(obj.Three);
			Assert.NotNull(obj.Four);

			Assert.AreEqual("two1", obj.Children[0].Name);
			Assert.AreEqual(BasicEnum.Evening, obj.Children[0].TimeOfDay);
			Assert.AreEqual("one1", obj.Children[1].Name);
			Assert.AreEqual("one2", obj.Children[2].Name);
			Assert.AreEqual("two2", obj.Children[3].Name);

			Assert.AreEqual("three1", obj.Three.Name);
			Assert.AreEqual("four1", obj.Four.Name);
			Assert.AreEqual(BasicEnum.Morning, obj.Four.TimeOfDay);
		}
	}
}