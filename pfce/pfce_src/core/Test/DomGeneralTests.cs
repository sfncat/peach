using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Peach.Core.Dom;
using System.IO;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;

namespace Peach.Core.Test
{
	[TestFixture]
	[Peach]
	[Quick]
	class DomGeneralTests
	{
		[Test]
		public void Find()
		{
			var dm = new DataModel("root")
			{
				new Block("block1"),
				new Block("block2")
			};

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

			Assert.NotNull(dm.find("string1_1_1"));
			Assert.NotNull(dm.find("string1_1_1").find("string2_1_2"));
		}

		static IEnumerable<string> GetPluginNames(PluginAttribute attr, Type type)
		{
			yield return string.Format("{0} '{1}'", attr.Type.Name, attr.Name);

			foreach (var alias in type.GetAttributes<AliasAttribute>())
				yield return string.Format("{0} '{1}'", attr.Type.Name, alias.Name);
		}

		[Test]
		public void PluginAttributes()
		{
			var errors = new StringBuilder();

			// All plugins should have:
			// 1) One plugin attribute with default=true
			// 2) A description
			// 3) No duplicated parameters
			// 4) No plugin attribute names should conflict
			// 5) Plugins can only be of one type (Monitor/Publisher/Fixup)

			var pluginsByType = new Dictionary<Type, List<PluginAttribute>>();
			var pluginsByName = new Dictionary<string, KeyValuePair<PluginAttribute, Type>>();

			foreach (var kv in ClassLoader.GetAllByAttribute<PluginAttribute>(null))
			{
				var attr = kv.Key;
				var type = kv.Value;

				if (!pluginsByType.ContainsKey(type))
					pluginsByType.Add(type, new List<PluginAttribute>());

				pluginsByType[type].Add(attr);

				foreach (var pluginName in GetPluginNames(attr, type))
				{
					// Verify #4 (no name collisions)
					if (pluginsByName.ContainsKey(pluginName))
					{
						var old = pluginsByName[pluginName];

						errors.AppendLine();

						if (old.Value == kv.Value)
						{
							errors.AppendFormat("{0} declared more than once in assembly '{1}' class '{2}'.",
								pluginName, kv.Value.Assembly.Location, kv.Value.FullName);
						}
						else
						{
							errors.AppendFormat("{0} declared in assembly '{1}' class '{2}' and in assembly {3} and class '{4}'.",
								pluginName, kv.Value.Assembly.Location, kv.Value.FullName, old.Value.Assembly.Location, old.Value.FullName);
						}
					}
					else
					{
						pluginsByName.Add(pluginName, kv);
					}
				}
			}

			foreach (var kv in pluginsByType)
			{
				var type = kv.Key;
				var attrs = kv.Value;

				// Verify #4 (eEnsure all plugin attributes are of the same type)
				var pluginTypes = attrs.Select(a => a.Type.Name).Distinct().ToList();
				if (pluginTypes.Count != 1)
				{
					errors.AppendLine();
					errors.AppendFormat("Plugin declared in assembly '{0}' class '{1}' has multiple types: '{2}'",
						type.Assembly.Location, type.FullName, string.Join("', '", pluginTypes));
				}

				// Verify #1 (ensure there is a single default)
				var defaults = attrs.Where(a => a.IsDefault).Select(a => a.Name).ToList();
				if (defaults.Count == 0)
				{
					errors.AppendLine();
					errors.AppendFormat("{0} declared in assembly '{1}' class '{2}' has no default name.",
						attrs[0].Type.Name, type.Assembly.Location, type.FullName);
				}
				else if (defaults.Count != 1)
				{
					errors.AppendLine();
					errors.AppendFormat("{0} declared in assembly '{1}' class '{2}' has multiple defaults: '{3}'",
						attrs[0].Type.Name, type.Assembly.Location, type.FullName, string.Join("', '", defaults));
				}

				// Verify #2 (ensure there is a description)
				//var desc = type.GetAttributes<DescriptionAttribute>(null).FirstOrDefault();
				//if (desc == null)
				//{
				//    errors.AppendLine();
				//    errors.AppendFormat("{0} declared in assembly '{1}' class '{2}' has no description.",
				//        attrs[0].Type.Name, type.Assembly.Location, type.FullName);
				//}

				// Verify #3 (all the parameters must be unique in name)
				var paramAttrs = type.GetAttributes<ParameterAttribute>(null).Select(a => a.name).ToList();
				var dupes = paramAttrs.GroupBy(a => a).SelectMany(g => g.Skip(1)).Distinct().ToList();
				if (dupes.Count != 0)
				{
					errors.AppendLine();
					errors.AppendFormat("{0} declared in assembly '{1}' class '{2}' has duplicate parameters: '{3}'",
						attrs[0].Type.Name, type.Assembly.Location, type.FullName, string.Join("', '", dupes));
				}

			}

			var msg = errors.ToString();

			StringAssert.IsMatch("$^", msg);
		}

		[Test]
		public void DataElementAttributes()
		{
			var errors = new StringBuilder();

			var deByType = new Dictionary<Type, DataElementAttribute>();
			var deByName = new Dictionary<string, KeyValuePair<DataElementAttribute, Type>>();

			foreach (var kv in ClassLoader.GetAllByAttribute<DataElementAttribute>(null))
			{
				var attr = kv.Key;
				var type = kv.Value;

				// Verify only 1 DataElement attribute per type
				if (deByType.ContainsKey(type))
				{
					var old = deByType[type];

					errors.AppendLine();
					errors.AppendFormat("DataElement in assembly '{0}' class '{1}' declared both '{2}' and '{3}.",
						type.Assembly.Location, type.FullName, attr.elementName, old.elementName);
				}
				else
				{
					deByType.Add(type, attr);
				}

				// Verify no elementName collissions
				if (deByName.ContainsKey(attr.elementName))
				{
					var old = deByName[attr.elementName];

					errors.AppendLine();
					errors.AppendFormat("DataElement '{0}' declared in assembly '{1}' class '{2}' and in assembly {3} and class '{4}'.",
						attr.elementName, kv.Value.Assembly.Location, kv.Value.FullName, old.Value.Assembly.Location, old.Value.FullName);
				}
				else
				{
					deByName.Add(attr.elementName, kv);
				}

				var paramAttrs = type.GetAttributes<ParameterAttribute>(null).Select(a => a.name).ToList();
				var dupes = paramAttrs.GroupBy(a => a).SelectMany(g => g.Skip(1)).Distinct().ToList();
				if (dupes.Count != 0)
				{
					errors.AppendLine();
					errors.AppendFormat("DataElement '{0}' declared in assembly '{1}' class '{2}' has duplicate parameters: '{3}'",
						attr.elementName, type.Assembly.Location, type.FullName, string.Join("', '", dupes));
				}
			}

			var msg = errors.ToString();

			StringAssert.IsMatch("$^", msg);
		}

		[Test]
		public void UniqueNameTest()
		{
			DataElementContainer cont = new Block();

			string unique1 = cont.UniqueName("child");
			Assert.AreEqual("child", unique1);

			cont.Add(new Blob("child"));

			string unique2 = cont.UniqueName("child");
			Assert.AreEqual("child_1", unique2);
		}

		private class TestStrategy : MutationStrategy
		{
			public TestStrategy()
				: base(new Dictionary<string, Variant>())
			{
			}

			public override bool UsesRandomSeed
			{
				get { throw new NotImplementedException(); }
			}

			public override bool IsDeterministic
			{
				get { throw new NotImplementedException(); }
			}

			public override uint Count
			{
				get { throw new NotImplementedException(); }
			}

			public override uint Iteration
			{
				get
				{
					throw new NotImplementedException();
				}
				set
				{
					throw new NotImplementedException();
				}
			}

			public void TestRecurseAllElements(DataElementContainer c, List<DataElement> list)
			{
				RecursevlyGetElements(c, list);
			}
		}

		[Test]
		public void CollectAllElements()
		{
			var e = new Blob("element");
			var c = new Block("block");
			var m = new DataModel("model");

			c.Add(e);
			m.Add(c);

			var items = new List<DataElement>();

			new TestStrategy().TestRecurseAllElements(m, items);

			Assert.AreEqual(3, items.Count);
			Assert.AreEqual(m, items[0]);
			Assert.AreEqual(c, items[1]);
			Assert.AreEqual(e, items[2]);
		}

		[Test]
		public void TestEnumeration()
		{
			/*
			        F
			       / \
			      /   \
			     /     \
			    B       G
			   / \     / \
			  A   D   K   I
			 /   / \     / \
			L   C   E   H   J
			*/

			const string xml = @"
<Peach>
	<DataModel name='F'>
		<Block name='B'>
			<Block name='A'>
				<Blob name='L'/>
			</Block>
			<Block name='D'>
				<Blob name='C'/>
				<Blob name='E'/>
			</Block>
		</Block>
		<Block name='G'>
			<Blob name='K'/>
			<Block name='I'>
				<Blob name='H'/>
				<Blob name='J'/>
			</Block>
		</Block>
	</DataModel>
</Peach>
";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var e = dom.dataModels[0].find("E");
			Assert.NotNull(e);

			var d = dom.dataModels[0].find("D");
			Assert.NotNull(d);

			var iter1 = e.EnumerateElementsUpTree().Select(a => a.Name).ToList();
			var iter2 = e.EnumerateUpTree().Select(a => a.Name).ToList();

			const string eEnum = "C,E,A,D,L,B,G,K,I,H,J,F";
			Assert.AreEqual(eEnum, string.Join(",", iter1));
			Assert.AreEqual(eEnum, string.Join(",", iter2));

			var iter3 = d.EnumerateElementsUpTree().Select(a => a.Name).ToList();
			var iter4 = d.EnumerateUpTree().Select(a => a.Name).ToList();

			const string dEnum = "C,E,A,D,L,B,G,K,I,H,J,F";
			Assert.AreEqual(dEnum, string.Join(",", iter3));
			Assert.AreEqual(dEnum, string.Join(",", iter4));

			var iter5 = dom.dataModels[0].EnumerateAllElements().Select(a => a.Name).ToList();
			var iter6 = dom.dataModels[0].EnumerateAll().Select(a => a.Name).ToList();

			const string rootAll = "B,G,A,D,L,C,E,K,I,H,J";
			Assert.AreEqual(rootAll, string.Join(",", iter5));
			Assert.AreEqual(rootAll, string.Join(",", iter6));

			var iter7 = dom.dataModels[0].EnumerateElementsUpTree().Select(a => a.Name).ToList();
			var iter8 = dom.dataModels[0].EnumerateUpTree().Select(a => a.Name).ToList();

			const string rootEnum = "B,G,A,D,L,C,E,K,I,H,J,F";
			Assert.AreEqual(rootEnum, string.Join(",", iter7));
			Assert.AreEqual(rootEnum, string.Join(",", iter8));
		}


		[Test]
		public void TestElementComparison()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String value='https://www.google.com/search?site=&amp;tbm=isch&amp;source=hp&amp;biw=1920&amp;bih=979&amp;q=peach&amp;oq=peach&amp;gs_l=img.3..0l10.1242.1667.0.1828.5.5.0.0.0.0.79.287.5.5.0....0...1ac.1.64.img..0.5.287.8H3bm7Z3gFw'>
		</String>
	</DataModel>
</Peach>
";


			var dom = DataModelCollector.ParsePit(xml);

			var items = new List<DataElement>(dom.dataModels[0].Walk());

			CollectionAssert.IsNotEmpty(items);

			var rng = new Random(1);

			for (var i = 0; i < 1000; ++i)
			{
				var copy = new List<DataElement>(rng.Shuffle(items.ToArray()));

				copy.Sort(new DataCracker.ElementComparer());

				CollectionAssert.AreEqual(items, copy);
			}
		}
	}
}
