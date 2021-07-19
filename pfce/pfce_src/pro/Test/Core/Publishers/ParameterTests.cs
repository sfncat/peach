using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using NLog;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Publishers
{
	[TestFixture]
	[Quick]
	[Peach]
	class ParameterTests
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		[Publisher("testA")]
		[Parameter("req1", typeof(int), "desc")]
		class PubMissingDefaultName : Publisher
		{
			protected override NLog.Logger Logger { get { return logger; } }
			public PubMissingDefaultName(Dictionary<string, Variant> args)
				: base(args)
			{
			}

			public int req1 { get; set; }
		}

		[Publisher("testA1.default")]
		[Alias("testA1")]
		[Parameter("req1", typeof(int), "desc")]
		class PubDefaultName : Publisher
		{
			protected override NLog.Logger Logger { get { return logger; } }
			public PubDefaultName(Dictionary<string, Variant> args)
				: base(args)
			{
			}

			public int req1 { get; set; }
		}

		[Publisher("testA2.default")]
		[Parameter("req1", typeof(int), "desc")]
		class PubMissingParam : Publisher
		{
			protected override NLog.Logger Logger { get { return logger; } }
			public PubMissingParam(Dictionary<string, Variant> args)
				: base(args)
			{
			}
		}

		[Publisher("enumPub")]
		[Parameter("enum1", typeof(FileMode), "File Mode")]
		[Parameter("enum2", typeof(ConsoleColor), "Console Color", "Red")]
		class EnumPub : Publisher
		{
			protected override NLog.Logger Logger { get { return logger; } }
			public FileMode enum1 { get; set; }
			public ConsoleColor enum2 { get; set; }

			public EnumPub(Dictionary<string, Variant> args)
				: base(args)
			{
			}
		}

		[Test]
		public void TestEnums()
		{
			Dictionary<string, Variant> args = new Dictionary<string, Variant>();
			args["enum1"] = new Variant("OpenOrCreate");
			var p1 = new EnumPub(args);
			Assert.AreEqual(p1.enum1, FileMode.OpenOrCreate);
			Assert.AreEqual(p1.enum2, ConsoleColor.Red);

			args["enum2"] = new Variant("DarkCyan");
			var p2 = new EnumPub(args);
			Assert.AreEqual(p2.enum1, FileMode.OpenOrCreate);
			Assert.AreEqual(p2.enum2, ConsoleColor.DarkCyan);

			args["enum2"] = new Variant("DaRkMaGeNtA");
			var p3 = new EnumPub(args);
			Assert.AreEqual(p3.enum1, FileMode.OpenOrCreate);
			Assert.AreEqual(p3.enum2, ConsoleColor.DarkMagenta);
		}

		[Test]
		public void TestNameNoDefault()
		{
			Dictionary<string, Variant> args = new Dictionary<string, Variant>();
			var ex = Assert.Throws<PeachException>(() => new PubMissingDefaultName(args));
			Assert.AreEqual("Publisher 'testA' is missing required parameter 'req1'.", ex.Message);
		}

		[Test]
		public void TestNameDefault()
		{
			Dictionary<string, Variant> args = new Dictionary<string, Variant>();
			var ex = Assert.Throws<PeachException>(() => new PubDefaultName(args));
			Assert.AreEqual("Publisher 'testA1.default' is missing required parameter 'req1'.", ex.Message);
		}

		[Test]
		public void TestBadParameter()
		{
			Dictionary<string, Variant> args = new Dictionary<string, Variant>();
			args["req1"] = new Variant("not a number");

			PeachException pe = null;
			try
			{
				new PubDefaultName(args);
			}
			catch (PeachException ex)
			{
				pe = ex;
			}
			Assert.NotNull(pe);
			Assert.True(pe.Message.StartsWith("Publisher 'testA1.default' could not set parameter 'req1'.  Input string was not in"));
		}

		[Test]
		public void TestMissingProperty()
		{
			Dictionary<string, Variant> args = new Dictionary<string, Variant>();
			args["req1"] = new Variant("100");
			var ex = Assert.Throws<PeachException>(() => new PubMissingParam(args));
			Assert.AreEqual("Publisher 'testA2.default' has no property for parameter 'req1'.", ex.Message);
		}

		[Publisher("good")]
		[Parameter("Param_string", typeof(string), "desc")]
		[Parameter("Param_ip", typeof(IPAddress), "desc", "")]
		class GoodPub : Publisher
		{
			protected override NLog.Logger Logger { get { return logger; } }
			public GoodPub(Dictionary<string, Variant> args)
				: base(args)
			{
			}

			public string Param_string { get; set; }
			public IPAddress Param_ip { get; set; }
		}

		[Test]
		public void TestParse()
		{
			Dictionary<string, Variant> args = new Dictionary<string,Variant>();
			args["Param_string"] = new Variant("the string");
			args["Param_ip"] = new Variant("192.168.1.1");

			var p = new GoodPub(args);
			Assert.AreEqual("the string", p.Param_string);
			Assert.AreEqual(IPAddress.Parse("192.168.1.1"), p.Param_ip);
		}

		[Test]
		public void TestBadIpParameter()
		{
			Dictionary<string, Variant> args = new Dictionary<string, Variant>();
			args["Param_string"] = new Variant("100");
			args["Param_ip"] = new Variant("999.888.777.666");
			var ex = Assert.Throws<PeachException>(() => new GoodPub(args));
			Assert.AreEqual("Publisher 'good' could not set parameter 'Param_ip'.  An invalid IP address was specified.", ex.Message);
		}

		class CustomType
		{
			public string Message { get; set; }

			public CustomType()
			{
			}
		}

		[Publisher("CustomTypePub")]
		[Parameter("param", typeof(CustomType), "Custom Type")]
		class CustomTypePub : Publisher
		{
			protected override NLog.Logger Logger { get { return logger; } }
			public CustomType param { get; set; }

			public CustomTypePub(Dictionary<string, Variant> args)
				: base(args)
			{
			}

			public static void Parse(string str, out IPAddress val)
			{
				val = IPAddress.Parse(str);
			}

			public static void Parse(string str, out CustomType val)
			{
				val = new CustomType();
				val.Message = str;
			}
		}

		[Test]
		public void TestCustomConvert()
		{
			// When the bas Publisher can not convert a string parameter
			// into the type defined in the Parameter attribute, it should
			// look for a conversion function on the derived publisher

			Dictionary<string, Variant> args = new Dictionary<string, Variant>();
			args["param"] = new Variant("foo");
			var pub = new CustomTypePub(args);

			Assert.NotNull(pub);
			Assert.AreEqual(pub.param.Message, "foo");
		}

		[Publisher("PrivatePub")]
		[Parameter("param", typeof(string), "param")]
		[Parameter("param1", typeof(string), "param1")]
		class PrivatePub : Publisher
		{
			protected override NLog.Logger Logger { get { return logger; } }

			public string param { get; set; }
			public string param1 { get; private set; }

			public string GetParam { get { return param; } }

			public PrivatePub(Dictionary<string, Variant> args)
				: base(args)
			{
			}
		}

		[Test]
		public void TestPrivate()
		{
			// Ensure the auto setting supports private properties
			Dictionary<string, Variant> args = new Dictionary<string, Variant>();
			args["param"] = new Variant("foo");
			args["param1"] = new Variant("bar");
			var pub = new PrivatePub(args);

			Assert.NotNull(pub);
			Assert.AreEqual(pub.GetParam, "foo");
			Assert.AreEqual(pub.param1, "bar");
		}

		[Publisher("SetPub")]
		[Parameter("param", typeof(string), "param")]
		class SetPub : Publisher
		{
			protected override NLog.Logger Logger { get { return logger; } }

			private string _val;

			public string param { set { _val = value; } }

			public string GetParam { get { return _val; } }

			public SetPub(Dictionary<string, Variant> args)
				: base(args)
			{
			}
		}

		[Test]
		public void TestSetOnly()
		{
			// Ensure the auto setting supports handles only set properties
			Dictionary<string, Variant> args = new Dictionary<string, Variant>();
			args["param"] = new Variant("foo");
			var pub = new SetPub(args);

			Assert.NotNull(pub);
			Assert.AreEqual(pub.GetParam, "foo");
		}

		[Publisher("GetPub")]
		[Parameter("param", typeof(string), "param")]
		class GetPub : Publisher
		{
			protected override NLog.Logger Logger { get { return logger; } }

			public string param { get { return "hello"; } }

			public GetPub(Dictionary<string, Variant> args)
				: base(args)
			{
			}
		}

		[Test]
		public void TestGetOnly()
		{
			// Ensure the auto setting supports handles only get properties
			Dictionary<string, Variant> args = new Dictionary<string, Variant>();
			args["param"] = new Variant("foo");
			var ex = Assert.Throws<PeachException>(() => new GetPub(args));
			Assert.AreEqual("Publisher 'GetPub' has no settable property for parameter 'param'.", ex.Message);
		}

		[Publisher("NullPlugin")]
		[Parameter("custom", typeof(CustomType), "desc", "")]
		[Parameter("str", typeof(string), "desc", "")]
		[Parameter("num", typeof(int), "desc", "")]
		class NullTest
		{
			public NullTest() { }
			public string str { get; set; }
			public int num { get; set; }
			public CustomType custom { set; get; }
		}

		[Test]
		public void TestNullDefault()
		{
			var obj = new NullTest();

			var onlyNum = new Dictionary<string, Variant>();
			onlyNum["num"] = new Variant(10);
			ParameterParser.Parse(obj, onlyNum);

			Assert.Null(obj.str);
			Assert.AreEqual(10, obj.num);
			Assert.Null(obj.custom);

			var onlyStr = new Dictionary<string, Variant>();
			onlyNum["str"] = new Variant("hi");

			Assert.Throws<PeachException>(delegate() { ParameterParser.Parse(obj, onlyStr); });
		}

		[Publisher("NullablePlugin")]
		[Parameter("num1", typeof(int?), "desc")]
		[Parameter("num2", typeof(int?), "desc", "")]
		[Parameter("num3", typeof(int?), "desc", "")]
		class NullableTest
		{
			public NullableTest() { }
			public int? num1 { get; set; }
			public int? num2 { get; set; }
			public int? num3 { get; set; }
		}

		[Test]
		public void TestNullable()
		{
			var obj = new NullableTest();

			var onlyNum = new Dictionary<string, Variant>();
			onlyNum["num1"] = new Variant(10);
			onlyNum["num2"] = new Variant(20);

			ParameterParser.Parse(obj, onlyNum);

			Assert.True(obj.num1.HasValue);
			Assert.True(obj.num2.HasValue);
			Assert.False(obj.num3.HasValue);

			Assert.AreEqual(10, obj.num1.Value);
			Assert.AreEqual(20, obj.num2.Value);
		}

		[Publisher("ArrayPlugin")]
		[Parameter("num1", typeof(int[]), "desc", "")]
		[Parameter("num2", typeof(int[]), "desc", "")]
		[Parameter("str", typeof(string[]), "desc", "")]
		class ArrayTest
		{
			public ArrayTest() { }
			public int[] num1 { get; set; }
			public int[] num2 { get; set; }
			public string[] str { get; set; }
		}

		[Test]
		public void TestArray()
		{
			// Test that we can parse array parameters, and we strip empty entries
			var obj = new ArrayTest();

			var onlyNum = new Dictionary<string, Variant>();
			onlyNum["num1"] = new Variant("10,11,12");
			onlyNum["str"] = new Variant("string 1,string2,,,,,,,,,,,string three");

			ParameterParser.Parse(obj, onlyNum);

			Assert.NotNull(obj.num1);
			Assert.NotNull(obj.num2);
			Assert.NotNull(obj.str);

			Assert.AreEqual(3, obj.num1.Length);
			Assert.AreEqual(0, obj.num2.Length);
			Assert.AreEqual(3, obj.str.Length);

			Assert.AreEqual(10, obj.num1[0]);
			Assert.AreEqual(11, obj.num1[1]);
			Assert.AreEqual(12, obj.num1[2]);

			Assert.AreEqual("string 1", obj.str[0]);
			Assert.AreEqual("string2", obj.str[1]);
			Assert.AreEqual("string three", obj.str[2]);
		}

		[Publisher("RefPlugin")]
		[Parameter("ref", typeof(string), "desc")]
		class RefPlugin
		{
			public RefPlugin() { }
			public string _ref { get; set; }
		}

		[Test]
		public void TestHexInts()
		{
			var obj = new NullTest();
			var args = new Dictionary<string, Variant>();
			args["num"] = new Variant("0x1ff");

			ParameterParser.Parse(obj, args);

			Assert.AreEqual(0x1ff, obj.num);
		}

		[Test]
		public void TestUnderscore()
		{
			// If property 'xxx' doesn't exist, look for property '_xxx'
			var obj = new RefPlugin();
			var args = new Dictionary<string, Variant>();
			args["ref"] = new Variant("foo");

			ParameterParser.Parse(obj, args);

			Assert.AreEqual("foo", obj._ref);
		}

		class MyCustomType
		{
			public MyCustomType(string val)
			{
				this.val = val;
			}

			public string val;
		}

		abstract class MyBaseClass
		{
			public static void Parse(string str, out MyCustomType val)
			{
				val = new MyCustomType(str);
			}
		}

		[Publisher("InheritPlugin")]
		[Parameter("arg", typeof(MyCustomType), "desc")]
		class InheritPlugin : MyBaseClass
		{
			public MyCustomType arg { get; set; }
		}

		[Test]
		public void TestConvertInherit()
		{
			// Look for base classes for static convert methods for custom types
			var obj = new InheritPlugin();

			var args = new Dictionary<string, Variant>();
			args["arg"] = new Variant("description of my custom type");

			ParameterParser.Parse(obj, args);

			Assert.AreEqual("description of my custom type", obj.arg.val);
		}

		[Publisher("HexString")]
		[Parameter("arg", typeof(HexString), "desc")]
		class HexPlugin
		{
			public HexString arg { get; set; }
		}

		[Test]
		public void TestHexStringGood()
		{
			var obj = new HexPlugin();
			var args = new Dictionary<string, Variant>();
			args["arg"] = new Variant("000102030405");

			ParameterParser.Parse(obj, args);

			Assert.NotNull(obj.arg);
			Assert.NotNull(obj.arg.Value);
			Assert.AreEqual(6, obj.arg.Value.Length);

			for (int i = 0; i < obj.arg.Value.Length; ++i)
			{
				Assert.AreEqual(obj.arg.Value[i], i);
			}
		}

		[Test]
		[TestCase(" ")]
		[TestCase("-")]
		[TestCase(":")]
		public void TestHexStringSep(string sep)
		{
			var obj = new HexPlugin();
			var args = new Dictionary<string, Variant>();
			args["arg"] = new Variant("00{0}01{0}02{0}03{0}04{0}05".Fmt(sep));

			ParameterParser.Parse(obj, args);

			Assert.NotNull(obj.arg);
			Assert.NotNull(obj.arg.Value);
			Assert.AreEqual(6, obj.arg.Value.Length);

			for (int i = 0; i < obj.arg.Value.Length; ++i)
			{
				Assert.AreEqual(obj.arg.Value[i], i);
			}
		}

		[Test]
		public void TestHexStringBad()
		{
			var obj = new HexPlugin();
			var args = new Dictionary<string, Variant>();
			args["arg"] = new Variant("Hello");

			var ex = Assert.Throws<PeachException>(() => ParameterParser.Parse(obj, args));
			Assert.AreEqual("Publisher 'HexString' could not set parameter 'arg'.  An invalid hex string was specified.", ex.Message);
		}

		[Publisher("Regex")]
		[Parameter("Expr", typeof(Regex), "desc", "")]
		class RegexPlugin : MyBaseClass
		{
			public Regex Expr { get; set; }
		}

		[Test]
		public void TestRegex()
		{
			var obj = new RegexPlugin();
			ParameterParser.Parse(obj, new Dictionary<string, string>());
			Assert.Null(obj.Expr);

			ParameterParser.Parse(obj, new Dictionary<string, string>
			{
				{ "Expr" , "\\s+" }
			});
			Assert.NotNull(obj.Expr);

			var ex = Assert.Throws<PeachException>(() =>
				ParameterParser.Parse(obj, new Dictionary<string, string>
				{
					{ "Expr" , "(" }
				})
			);

			Assert.AreEqual("Publisher 'Regex' could not set parameter 'Expr'.  The value '(' is not a valid regular expression.", ex.Message);
		}

		[Publisher("Exclusive")]
		[Parameter("Param1", typeof(string), "desc", "")]
		[Parameter("Param2", typeof(string), "desc", "")]
		[Parameter("Param3", typeof(string), "desc", "")]
		class ExclusivePlugin : MyBaseClass
		{
			public string Param1 { get; set; }
			public string Param2 { get; set; }
			public string Param3 { get; set; }
		}

		[Test]
		public void TestExclusiveNone()
		{
			var plugin = new ExclusivePlugin();
			var ex = Assert.Throws<PeachException>(() => ParameterParser.EnsureOne(plugin, "Param1", "Param2", "Param3"));
			Assert.AreEqual("Publisher 'Exclusive' requires one of the following parameters be set: 'Param1', 'Param2', 'Param3'.", ex.Message);
		}

		[Test]
		public void TestExclusiveOne()
		{
			var plugin = new ExclusivePlugin { Param2 = "foo" };
			Assert.DoesNotThrow(() => ParameterParser.EnsureOne(plugin, "Param1", "Param2", "Param3"));
		}

		[Test]
		public void TestExclusiveMany()
		{
			var plugin = new ExclusivePlugin { Param2 = "foo", Param3 = "bar" };
			var ex = Assert.Throws<PeachException>(() => ParameterParser.EnsureOne(plugin, "Param1", "Param2", "Param3"));
			Assert.AreEqual("Publisher 'Exclusive' only suports one of the following parameters be set at the same time: 'Param2', 'Param3'.", ex.Message);
		}
	}
}
