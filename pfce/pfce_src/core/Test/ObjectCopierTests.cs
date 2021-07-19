using System;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using Peach.Core.Dom;
using Peach.Core.Analyzers;
using System.Collections;
using System.Runtime.Serialization;
using System.Reflection;
using System.Diagnostics;
using System.Linq;

namespace Peach.Core.Test
{
	[TestFixture]
	[Peach]
	[Quick]
	class ObjectCopierTests
	{
		[Test]
		public void ValidateNonDiverganceListVsDictionary()
		{
			// Our ordered dictionary class contains both a 
			// dictionary and list of the same objects.
			// When copied do both sides match?

			DataModel dm = new DataModel("root");
			dm.Add(new Block("block1"));
			dm.Add(new Block("block2"));
			((DataElementContainer)dm[0]).Add(new Block("block1_1"));
			((DataElementContainer)dm[0]).Add(new Block("block1_2"));
			((DataElementContainer)dm[1]).Add(new Block("block2_1"));
			((DataElementContainer)dm[1]).Add(new Block("block2_2"));

			((DataElementContainer)((DataElementContainer)dm[0])[0]).Add(new Dom.String("string1_1_1"));
			((DataElementContainer)((DataElementContainer)dm[0])[0]).Add(new Dom.String("string1_1_2"));
			((DataElementContainer)((DataElementContainer)dm[0])[1]).Add(new Dom.String("string1_2_1"));
			((DataElementContainer)((DataElementContainer)dm[0])[1]).Add(new Dom.String("string1_2_2"));

			((DataElementContainer)((DataElementContainer)dm[1])[0]).Add(new Dom.String("string2_1_1"));
			((DataElementContainer)((DataElementContainer)dm[1])[0]).Add(new Dom.String("string2_1_2"));
			((DataElementContainer)((DataElementContainer)dm[1])[1]).Add(new Dom.String("string2_2_1"));
			((DataElementContainer)((DataElementContainer)dm[1])[1]).Add(new Dom.String("string2_2_2"));

			var dmCopy = dm.Clone() as DataModel;
			ValidateListVsDictionary(dmCopy, null);
		}

		[Test]
		public void ValidateRelations()
		{
			// Our ordered dictionary class contains both a 
			// dictionary and list of the same objects.
			// When copied do both sides match?

			DataModel dm = new DataModel("root");
			dm.Add(new Block("block1"));
			dm.Add(new Block("block2"));
			((DataElementContainer)dm[0]).Add(new Block("block1_1"));
			((DataElementContainer)dm[0]).Add(new Block("block1_2"));
			((DataElementContainer)dm[1]).Add(new Block("block2_1"));
			((DataElementContainer)dm[1]).Add(new Block("block2_2"));

			((DataElementContainer)((DataElementContainer)dm[0])[0]).Add(new Dom.String("string1_1_1"));
			((DataElementContainer)((DataElementContainer)dm[0])[0]).Add(new Dom.String("string1_1_2"));
			((DataElementContainer)((DataElementContainer)dm[0])[1]).Add(new Dom.String("string1_2_1"));
			((DataElementContainer)((DataElementContainer)dm[0])[1]).Add(new Dom.String("string1_2_2"));

			((DataElementContainer)((DataElementContainer)dm[1])[0]).Add(new Dom.String("string2_1_1"));
			((DataElementContainer)((DataElementContainer)dm[1])[0]).Add(new Dom.String("string2_1_2"));
			((DataElementContainer)((DataElementContainer)dm[1])[1]).Add(new Dom.String("string2_2_1"));
			((DataElementContainer)((DataElementContainer)dm[1])[1]).Add(new Dom.String("string2_2_2"));

			((DataElementContainer)((DataElementContainer)dm[0])[0])[0].relations.Add(new SizeRelation(((DataElementContainer)((DataElementContainer)dm[0])[0])[0]));

			((DataElementContainer)((DataElementContainer)dm[0])[0])[0].relations[0].OfName = "string1_1_2";

			var dmCopy = dm.Clone() as DataModel;
			ValidateListVsDictionary(dmCopy, null);
		}

		[Test]
		public void ValidateRelations2()
		{
			// Our ordered dictionary class contains both a 
			// dictionary and list of the same objects.
			// When copied do both sides match?

			DataModel dm = new DataModel("root");
			dm.Add(new Block("block1"));
			dm.Add(new Block("block2"));
			((DataElementContainer)dm[0]).Add(new Block("block1_1"));
			((DataElementContainer)dm[0]).Add(new Block("block1_2"));
			((DataElementContainer)dm[1]).Add(new Block("block2_1"));
			((DataElementContainer)dm[1]).Add(new Block("block2_2"));

			((DataElementContainer)((DataElementContainer)dm[0])[0]).Add(new Dom.String("string1_1_1"));
			((DataElementContainer)((DataElementContainer)dm[0])[0]).Add(new Dom.String("string1_1_2"));
			((DataElementContainer)((DataElementContainer)dm[0])[1]).Add(new Dom.String("string1_2_1"));
			((DataElementContainer)((DataElementContainer)dm[0])[1]).Add(new Dom.String("string1_2_2"));

			((DataElementContainer)((DataElementContainer)dm[1])[0]).Add(new Dom.String("string2_1_1"));
			((DataElementContainer)((DataElementContainer)dm[1])[0]).Add(new Dom.String("string2_1_2"));
			((DataElementContainer)((DataElementContainer)dm[1])[1]).Add(new Dom.String("string2_2_1"));
			((DataElementContainer)((DataElementContainer)dm[1])[1]).Add(new Dom.String("string2_2_2"));

			((DataElementContainer)((DataElementContainer)dm[0])[0])[0].relations.Add(new SizeRelation(((DataElementContainer)((DataElementContainer)dm[0])[0])[0]));

			((DataElementContainer)((DataElementContainer)dm[0])[0])[0].relations[0].OfName = "string2_1_2";

			((DataElementContainer)((DataElementContainer)dm[1])[0])[1].relations.Add(((DataElementContainer)((DataElementContainer)dm[0])[0])[0].relations[0]);

			dm.find("string1_1_1").DefaultValue = new Variant("10");
			dm.find("string2_1_2").DefaultValue = new Variant("1234567890");

			// Generate the value
			var value = dm.Value;
			Assert.NotNull(value);

			var dmCopy = dm.Clone() as DataModel;
			for (int count = 0; count < 10; count++)
				dmCopy = dmCopy.Clone() as DataModel;

			ValidateListVsDictionary(dmCopy, null);
		}

		[Test]
		public void OnlyCloneChildren()
		{
			var str1 = new Dom.String("str1");
			var model = new DataModel("DM");
			model.Add(str1);

			var str2 = str1.Clone("str2");

			Assert.AreEqual(1, model.Count);
			Assert.AreEqual(model, str2.parent);
			Assert.AreEqual(model.GetHashCode(), str2.parent.GetHashCode());

		}

		[Test]
		public void ValidateRelations3()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob name=\"Data\" value=\"12345\"/>" +
				"		<Number name=\"TheNumber\" size=\"8\">" +
				"			<Relation type=\"size\" of=\"Data\" />" +
				"		</Number>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			DataModel dmCopy = dom.dataModels[0].Clone() as DataModel;
			ValidateListVsDictionary(dmCopy, null);
		}

		public void ValidateListVsDictionary(DataElementContainer elem, DataElementContainer parent)
		{
			Assert.NotNull(elem);

			if(parent != null)
				Assert.AreEqual(elem.parent.GetHashCode(), parent.GetHashCode(), elem.fullName);

			for (int count = 0; count < elem.Count; count++)
			{
				var countItem = elem[count];
				var dictItem = elem[countItem.Name];

				Assert.AreEqual(countItem.GetHashCode(), dictItem.GetHashCode(), countItem.fullName);

				foreach (var rel in countItem.relations)
					Assert.AreEqual(rel.From.getRoot().GetHashCode(), countItem.getRoot().GetHashCode(), countItem.fullName);
			}

			foreach (var child in elem)
			{
				if (child is DataElementContainer)
					ValidateListVsDictionary(child as DataElementContainer, elem);
			}
		}

		public class TestClass
		{
			public int xqw = 1;
		}

		[Test]
		public void NewCopier()
		{
			DataModel dm = new DataModel("root");
			dm.Add(new Block("block1"));
			dm.Add(new Block("block2"));

			var t = new Hashtable();
			t["foo"] = "bar";

			var mi = typeof(Hashtable).GetMethod("get_Item");
			object ret1 = mi.Invoke(t, new object[] { "foo" });
			mi = typeof(Hashtable).GetMethod("set_Item");
			mi.Invoke(t, new object[] { "foo", "qux" });

			object ret2 = t["foo"];

			Assert.AreEqual("bar", ret1);
			Assert.AreEqual("qux", ret2);

			string f = (string)t["bunk"];
			Assert.AreEqual(f, null);

			var c = ObjectCopier.Clone(new SimpleClass(), null);
			Assert.NotNull(c);

			var ret = ObjectCopier.Clone(dm, null);
			Assert.NotNull(ret);
		}

		[Serializable]
		public class ComplexClass : SimpleClass
		{
			[OnSerializing]
			void OnSerializing(StreamingContext ctx)
			{
			}

			[OnSerialized]
			void OnSerialized(StreamingContext ctx)
			{
			}

			[OnDeserializing]
			void OnDeserializing(StreamingContext ctx)
			{
			}

			[OnDeserialized]
			void OnDeserialized(StreamingContext ctx)
			{
			}

			[OnCloned]
			void OnCloned(SimpleClass orig, object ctx)
			{
			}

			[OnCloned]
			void OnCloned2(SimpleClass orig, object ctx)
			{
			}

			[OnCloning]
			void OnCloning(object ctx)
			{
			}

			[OnCloning]
			void OnCloning2(object ctx)
			{
			}

			[ShouldClone]
			bool ShouldClone(object ctx)
			{
				return true;
			}

			[ShouldClone]
			bool ShouldClone2(object ctx)
			{
				return true;
			}
		}

		[Serializable]
		public class ByRefClass
		{
			public object member = new object();

			[ShouldClone]
			bool ShouldClone(object ctx)
			{
				return false;
			}
		}

		[Serializable]
		public class SimpleClass
		{
			public int member { get; set; }
		}

		[Serializable]
		public struct SimpleStruct
		{
			public int one;
			public int two;

			public SimpleStruct(int one, int two)
			{
				this.one = one;
				this.two = two;
			}
		}

		[Serializable]
		public abstract class ReadOnlyBase
		{
			public readonly string member;
			public readonly SimpleStruct simpleStruct;
			public readonly SimpleClass obj;

			public ReadOnlyBase(string foo)
			{
				obj = new SimpleClass() { member = 100 };
				member = foo;
				simpleStruct = new SimpleStruct();
			}
		}

		[Serializable]
		public class ReadOnlyDerived : ReadOnlyBase
		{
			public string other;

			public ReadOnlyDerived(string member, string other)
				: base(member)
			{
				this.other = other;
			}
		}

		[Test]
		public void OnCloningAttrTest()
		{
			var src = new ByRefClass();
			var copy = ObjectCopier.Clone(src, null);

			Assert.NotNull(copy);
			Assert.AreEqual(src.GetHashCode(), copy.GetHashCode());
			Assert.AreEqual(src.member.GetHashCode(), copy.member.GetHashCode());
		}

		[Test]
		public void ReadOnlyCloneTest()
		{
			var src = new ReadOnlyDerived("base", "derived");
			var copy = ObjectCopier.Clone(src, null);

			Assert.NotNull(copy);
			Assert.AreNotEqual(src.GetHashCode(), copy.GetHashCode());

			Assert.AreEqual(src.other, copy.other);
			Assert.AreEqual(src.member, copy.member);
			Assert.AreEqual(src.simpleStruct.one, copy.simpleStruct.one);
			Assert.AreEqual(src.simpleStruct.two, copy.simpleStruct.two);

			Assert.AreNotEqual(src.obj.GetHashCode(), copy.obj.GetHashCode());
			Assert.AreEqual(src.obj.member, copy.obj.member);
		}

		[Test]
		public void LotsOfObjects1()
		{
			var list = new List<SimpleClass>();
			for (int i = 0; i < 20000; ++i)
				list.Add(new ComplexClass() { member = i });

			var copy = ObjectCopier.Clone(list);
			Assert.NotNull(copy);
			Assert.AreEqual(copy.Count, list.Count);
		}

		[Test]
		public void LotsOfObjects2()
		{
			const int COUNT = 200000;
			const int FACTOR = 10;

			var list1 = new List<SimpleClass>();
			for (var i = 0; i < COUNT; ++i)
				list1.Add(new ComplexClass() {member = i});

			var sw = new Stopwatch();
			sw.Start();
			var copy = ObjectCopier.Clone(list1, "Foo");
			sw.Stop();
			var list1Elaps = sw.ElapsedMilliseconds;

			Assert.NotNull(copy);
			Assert.AreEqual(copy.Count, list1.Count);

			var list2 = new List<SimpleClass>();
			for (var i = 0; i < COUNT * FACTOR; ++i)
				list2.Add(new ComplexClass() {member = i});

			sw = new Stopwatch();
			sw.Start();
			copy = ObjectCopier.Clone(list2, "Foo");
			sw.Stop();
			Assert.NotNull(copy);
			Assert.AreEqual(copy.Count, list2.Count);

			// ensure that copying FACTOR times more objects 
			// does not take longer than 4 * FACTOR time
			var max = list1Elaps * FACTOR * 4;
			Console.WriteLine("Elapsed {0}: {1}", COUNT, list1Elaps);
			Console.WriteLine("Elapsed {0}: {1}", COUNT * FACTOR, sw.ElapsedMilliseconds);
			Console.WriteLine("Max Allowed: {0}", max);

			Assert.LessOrEqual(sw.ElapsedMilliseconds, max);
		}

		bool VerifyField(FieldInfo field, Type type)
		{
			if (field.Attributes.HasFlag(FieldAttributes.NotSerialized))
				return true;
			
			if (type.IsPrimitive || (type == typeof(string)))
				return true;

			if (type.IsArray)
				return VerifyField(field, type.GetElementType());

			if (field.DeclaringType == typeof(Delegate))
				return true;

			if (!type.IsSerializable)
				return false;

			return !type.Namespace.StartsWith("System");
		}

		[Test]
		[Ignore("This test is hard to get right!")]
		public void EnsureSerializable()
		{
			// Challenges:
			// 1) Recursive interface descent (i.e. Peach.Core.Dom.Data)
			// 2) Generic type resolution (i.e. OwnedCollection<T>)
			// maybe more...
			var fails = (
				from kv in ClassLoader.AssemblyCache.Keys
				where kv.GetName().FullName.StartsWith("Peach")
				from type in kv.GetTypes()
				where type.IsSerializable
				from field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				where !VerifyField(field, field.FieldType)
				select "{0}.{1}".Fmt(type.FullName, field.Name)
			).ToList();
			CollectionAssert.IsEmpty(fails);
		}
	}
}
