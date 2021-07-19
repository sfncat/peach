using Peach.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Peach.Pro.Core;

namespace Peach.Pro.PitTester
{
	[XmlRoot("TestData", IsNullable = false, Namespace = "http://peachfuzzer.com/2012/TestData")]
	public class TestData
	{
		public TestData()
		{
			Defines = new List<Define>();
			Ignores = new List<Ignore>();
			Slurps = new List<Slurp>();
			Tests = new List<Test>();
		}

		public static TestData Parse(string fileName)
		{
			return XmlTools.Deserialize<TestData>(fileName);
		}

		public class Define
		{
			[XmlIgnore]
			public string Key { get; set; }

			[XmlIgnore]
			public string Value { get; set; }
		}

		public class ValueDefine : Define
		{
			[XmlAttribute("key")]
			public string KeyAttr
			{
				get { return Key; }
				set { Key = value; }
			}

			[XmlAttribute("value")]
			public string ValueAttr
			{
				get { return Value; }
				set { Value = value; }
			}
		}

		public abstract class TempFileDefine : Define, IDisposable
		{
			[XmlAttribute("key")]
			public string KeyAttr
			{
				get { return Key; }
				set { Key = value; }
			}

			public abstract void Populate();

			public void Dispose()
			{
				if (!string.IsNullOrEmpty(Value))
				{
					try
					{
						File.Delete(Value);
					}
					catch
					{
						// Ignore errors
					}
				}
			}
		}

		public class TextFileDefine : TempFileDefine
		{
			[XmlIgnore]
			public string Payload { get; private set; }

			[XmlText]
			public XmlNode[] CDataSection
			{
				get
				{
					return new XmlNode[] { new XmlDocument().CreateCDataSection(Payload) };
				}
				set
				{
					if (value == null)
					{
						Payload = string.Empty;
						return;
					}

					if (value.Length != 1)
						throw new InvalidOperationException();

					Payload = value[0].Value;
				}
			}

			public override void Populate()
			{
				Value = Path.GetTempFileName();

				File.WriteAllText(Value, Payload);
			}
		}

		public class HexFileDefine : TempFileDefine
		{
			[XmlIgnore]
			public byte[] Payload { get; private set; }

			[XmlText]
			public XmlNode[] CDataSection
			{
				get
				{
					var msg = Utilities.HexDump(Payload, 0, Payload.Length);
					return new XmlNode[] { new XmlDocument().CreateCDataSection(msg) };
				}
				set
				{
					if (value == null)
					{
						Payload = new byte[0];
						return;
					}

					if (value.Length != 1)
						throw new InvalidOperationException();

					Payload = Action.FromCData(value[0].Value);
				}
			}

			public override void Populate()
			{
				Value = Path.GetTempFileName();

				File.WriteAllBytes(Value, Payload);
			}
		}

		public class Slurp
		{
			[XmlAttribute("setXpath")]
			public string SetXpath { get; set; }

			[XmlAttribute("valueType")]
			[DefaultValue("string")]
			public string ValueType { get; set; }

			[XmlAttribute("value")]
			public string Value { get; set; }
		}

		public class Ignore
		{
			[XmlAttribute("xpath")]
			public string Xpath { get; set; }
		}

		public class Test
		{
			public Test()
			{
				Actions = new List<Action>();
			}

			[XmlAttribute("name")]
			public string Name { get; set; }

			[XmlAttribute("verifyDataSets")]
			[DefaultValue(true)]
			public bool VerifyDataSets { get; set; }

			[XmlAttribute("singleIteration")]
			[DefaultValue(false)]
			public bool SingleIteration { get; set; }

			[XmlAttribute("skip")]
			[DefaultValue(false)]
			public bool Skip { get; set; }

			[XmlAttribute("seed")]
			[DefaultValue("")]
			public string Seed { get; set; }

			[XmlElement("Start", Type = typeof(Start))]
			[XmlElement("Stop", Type = typeof(Stop))]
			[XmlElement("Open", Type = typeof(Open))]
			[XmlElement("Close", Type = typeof(Close))]
			[XmlElement("Accept", Type = typeof(Accept))]
			[XmlElement("Call", Type = typeof(Call))]
			[XmlElement("SetProperty", Type = typeof(SetProperty))]
			[XmlElement("GetProperty", Type = typeof(GetProperty))]
			[XmlElement("Input", Type = typeof(Input))]
			[XmlElement("Output", Type = typeof(Output))]
			public List<Action> Actions { get; set; }
		}

		public abstract class Action
		{
			public abstract string ActionType { get; }

			[XmlAttribute("action")]
			public string ActionName { get; set; }

			[XmlAttribute("publisher")]
			public string PublisherName { get; set; }

			internal static byte[] FromCData(string payload)
			{
				return Utilities.ParseHexDump(payload);
			}
		}

		public class Start : Action
		{
			public override string ActionType { get { return "start"; } }
		}

		public class Stop : Action
		{
			public override string ActionType { get { return "stop"; } }
		}

		public class Open : Action
		{
			public override string ActionType { get { return "open"; } }
		}

		public class Close : Action
		{
			public override string ActionType { get { return "close"; } }
		}

		public class Accept : Action
		{
			public override string ActionType { get { return "accept"; } }
		}

		public class Call : Action
		{
			public override string ActionType { get { return "call"; } }
		}

		public class SetProperty : Action
		{
			public override string ActionType { get { return "setProperty"; } }
		}

		public enum ValueType
		{
			[XmlEnum("hex")]
			Hex,
			[XmlEnum("text")]
			Text,
			[XmlEnum("xml")]
			Xml
		}

		public abstract class DataAction : Action
		{
			protected DataAction()
			{
				Payload = new byte[0];
				ValueType = ValueType.Hex;
			}

			[XmlAttribute("valueType")]
			[DefaultValue(TestData.ValueType.Hex)]
			public ValueType ValueType { get; set; }

			[XmlIgnore]
			public byte[] Payload { get; private set; }

			[XmlText]
			public XmlNode[] CDataSection
			{
				get
				{
					if (ValueType == ValueType.Hex)
					{
						var msg = Utilities.HexDump(Payload, 0, Payload.Length);
						return new XmlNode[] {new XmlDocument().CreateCDataSection(msg)};
					}
					else
					{
						var msg = Encoding.UTF8.GetString(Payload);
						return new XmlNode[] { new XmlDocument().CreateTextNode(msg) };
					}
				}
				set
				{
					if (value == null)
					{
						Payload = new byte[0];
						return;
					}

					if (value.Length != 1)
						throw new InvalidOperationException();

					Payload = ValueType == ValueType.Hex
						? FromCData(value[0].Value)
						: Encoding.UTF8.GetBytes(value[0].Value);
				}
			}
		}

		public class GetProperty : DataAction
		{
			public override string ActionType { get { return "getProperty"; } }
		}

		public class Input : DataAction
		{
			public override string ActionType { get { return "input"; } }

			[XmlAttribute("datagram")]
			[DefaultValue(false)]
			public bool IsDatagram { get; set; }
		}

		public enum ExpectedOutputSource
		{
			[XmlEnum("cdata")]
			CData,

			[XmlEnum("dataFile")]
			DataFile,
		}

		public class Output : DataAction
		{
			public override string ActionType { get { return "output"; } }

			[XmlAttribute("ignore")]
			[DefaultValue(false)]
			public bool Ignore { get; set; }

			[XmlAttribute("verifyAgainst")]
			[DefaultValue(ExpectedOutputSource.CData)]
			public ExpectedOutputSource VerifyAgainst { get; set; }
		}

		[XmlElement("Define", Type = typeof(ValueDefine))]
		[XmlElement("TextFile", Type = typeof(TextFileDefine))]
		[XmlElement("HexFile", Type = typeof(HexFileDefine))]
		public List<Define> Defines { get; set; }

		[XmlElement("Ignore")]
		public List<Ignore> Ignores { get; set; }

		[XmlElement("Slurp")]
		public List<Slurp> Slurps { get; set; }

		[XmlElement("Test")]
		public List<Test> Tests { get; set; }

		[XmlAttribute("pit")]
		public string Pit { get; set; }
	}
}
