using System;
using System.IO;
using Peach.Core.Agent;
using Peach.Core.Xsd;
using System.Xml.Serialization;

namespace Peach.Core.Test
{
	[XmlRoot("Foo")]
	public class TestElement
	{
		[PluginElement("class", typeof(IMonitor))]
		public NamedCollection<Dom.Monitor> Monitors { get; set; }
	}

	public class TestObject : INamed
	{
		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		#endregion

		[XmlAttribute("name")]
		public string Name { get; set; }
	}

	[XmlRoot("IntRoot")]
	public class IntObject
	{
		[XmlAttribute]
		public int Int { get; set; }

		[XmlAttribute]
		public uint UnsignedInt { get; set; }

		[XmlAttribute]
		public int Long { get; set; }

		[XmlAttribute]
		public uint UnsignedLong { get; set; }

		[XmlAttribute]
		public Dom.Test.Lifetime Endian { get; set; }
	}
	public abstract class TestAbstract
	{
		protected TestAbstract()
		{
			Objects = new System.Collections.Generic.List<TestObject>();
		}

		[XmlElement]
		public System.Collections.Generic.List<TestObject> Objects { get; set; }

		[XmlIgnore]
		public abstract bool include { get; }
	}

	public class TestTrue : TestAbstract
	{
		public override bool include { get { return true; } }

		[XmlAttribute]
		public string foo { get; set; }

		[XmlElement]
		public TestObject FooObj { get; set; }
	}

	public class TestFalse : TestAbstract
	{
		public override bool include { get { return false; } }

		[XmlAttribute]
		public string bar { get; set; }

		[XmlElement]
		public TestObject BarObj { get; set; }
	}

	[XmlRoot("Root")]
	public class TestRootElement
	{
		public TestRootElement()
		{
			Filters = new System.Collections.Generic.List<TestAbstract>();
		}

		[XmlElement(typeof(TestTrue))]
		[XmlElement(typeof(TestFalse))]
		public System.Collections.Generic.List<TestAbstract> Filters { get; set; }

		[XmlElement(typeof(TestObject))]
		public TestObject MyObj { get; set; }
	}

	[XmlRoot("Root")]
	public class ComplexObjext
	{
		[XmlAttribute("default")]
		public TestObject def { get; set; }

		[XmlElement("Object")]
		public NamedCollection<TestObject> Objects { get; set; }
	}

	public class SchemaTests
	{
		private void TestType(Type type)
		{
			var stream = new MemoryStream();

			SchemaBuilder.Generate(type, stream);

			var buf = stream.ToArray();
			var xsd = Encoding.UTF8.GetString(buf);

			Console.WriteLine(xsd);
		}

		private void Serialize<T>(T obj)
		{
			var wtr = new XmlSerializer(typeof(T));
			wtr.Serialize(Console.Out, obj);
		}

		public void Test1()
		{
			TestType(typeof(Xsd.Dom));
		}

		public void Test2()
		{
			TestType(typeof(TestElement));
		}

		public void Test3()
		{
			TestType(typeof(TestRootElement));
		}

		public void Test4()
		{
			TestType(typeof(ComplexObjext));
		}

		public void Test5()
		{
			var obj = new ComplexObjext
			{
				Objects = new NamedCollection<TestObject>
				{
					new TestObject() {Name = "Foo"}, 
					new TestObject() {Name = "Bar"}
				}
			};
			obj.def = obj.Objects[0];

			Serialize(obj);
		}

		public void Test6()
		{
			TestType(typeof(IntObject));
		}
	}
}
