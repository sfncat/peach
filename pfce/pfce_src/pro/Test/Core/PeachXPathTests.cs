

using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Core.Dom.XPath;
using String = Peach.Core.Dom.String;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
	class PeachXPathTests
	{
		[Test]
		public void BasicTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number name=\"TheNumber\" size=\"8\">" +
				"			<Relation type=\"count\" of=\"Array\" />" +
				"		</Number>" +
				"		<String name=\"Array\" value=\"1\" maxOccurs=\"100\"/>" +
				"	</DataModel>" +
				"	<StateModel name=\"TheState\" initialState=\"State1\">" +
				"		<State name=\"State1\">"+
				"			<Action type=\"output\">"+
				"				<DataModel ref=\"TheDataModel\" />"+
				"			</Action>"+
				"		</State>"+
				"	</StateModel>"+
				"	<Test name=\"Default\">"+
				"		<StateModel ref=\"TheState\"/>"+
				"		<Publisher class=\"Console\" />"+
				"	</Test>"+
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			PeachXPathNavigator navi = new PeachXPathNavigator(dom.tests[0].stateModel);
			XPathNodeIterator iter = navi.Select("//TheNumber");

			Assert.IsTrue(iter.MoveNext());
			Assert.AreEqual(dom.tests["Default"].stateModel.states["State1"].actions[0].dataModel["TheNumber"],
				((PeachXPathNavigator)iter.Current).CurrentNode);
			Assert.IsFalse(iter.MoveNext());
		}

		[Test]
		public void BasicTest2()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number name=\"TheNumber\" size=\"8\">" +
				"			<Relation type=\"count\" of=\"Array\" />" +
				"		</Number>" +
				"		<Block>" +
				"			<Block>" +
				"				<String name=\"FindMe\"/>" +
				"			</Block>" +
				"		</Block>" +
				"		<String name=\"Array\" value=\"1\" maxOccurs=\"100\"/>" +
				"		<Block>" +
				"			<String name=\"FindMe\"/>" +
				"		</Block>" +
				"	</DataModel>" +
				"	<StateModel name=\"TheState\" initialState=\"State1\">" +
				"		<State name=\"State1\">" +
				"			<Action type=\"output\">" +
				"				<DataModel ref=\"TheDataModel\" />" +
				"			</Action>" +
				"		</State>" +
				"	</StateModel>" +
				"	<Test name=\"Default\">" +
				"		<StateModel ref=\"TheState\"/>" +
				"		<Publisher class=\"Console\" />" +
				"	</Test>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var dataModel = dom.tests["Default"].stateModel.states["State1"].actions[0].dataModel;
			DataElement findMe1 = ((DataElementContainer)((DataElementContainer)dataModel[1])[0])[0];
			DataElement findMe2 = ((DataElementContainer)dataModel[3])[0];

			PeachXPathNavigator navi = new PeachXPathNavigator(dom.tests[0].stateModel);
			XPathNodeIterator iter = navi.Select("//FindMe");

			Assert.IsTrue(iter.MoveNext());
			Assert.AreEqual(findMe1, ((PeachXPathNavigator)iter.Current).CurrentNode);
			Assert.IsTrue(iter.MoveNext());
			Assert.AreEqual(findMe2, ((PeachXPathNavigator)iter.Current).CurrentNode);
			Assert.IsFalse(iter.MoveNext());
		}

		[Test]
		public void BasicTest3()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number name=\"TheNumber\" size=\"8\">" +
				"			<Relation type=\"count\" of=\"Array\" />" +
				"		</Number>" +
				"		<Block name=\"Block1\">" +
				"			<Block name=\"Block1_1\">" +
				"				<String name=\"FindMe\"/>" +
				"			</Block>" +
				"		</Block>" +
				"		<String name=\"Array\" value=\"1\" maxOccurs=\"100\"/>" +
				"		<Block>" +
				"			<String name=\"FindMe\"/>" +
				"		</Block>" +
				"	</DataModel>" +
				"	<StateModel name=\"TheState\" initialState=\"State1\">" +
				"		<State name=\"State1\">" +
				"			<Action type=\"output\">" +
				"				<DataModel ref=\"TheDataModel\" />" +
				"			</Action>" +
				"		</State>" +
				"	</StateModel>" +
				"	<Test name=\"Default\">" +
				"		<StateModel ref=\"TheState\"/>" +
				"		<Publisher class=\"Console\" />" +
				"	</Test>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var dataModel = dom.tests["Default"].stateModel.states["State1"].actions[0].dataModel;
			DataElement findMe = ((DataElementContainer)((DataElementContainer)dataModel[1])[0])[0];

			PeachXPathNavigator navi = new PeachXPathNavigator(dom.tests[0].stateModel);
			XPathNodeIterator iter = navi.Select("//Block1//FindMe");

			Assert.IsTrue(iter.MoveNext());
			Assert.AreEqual(findMe, ((PeachXPathNavigator)iter.Current).CurrentNode);
			Assert.IsFalse(iter.MoveNext());
		}

		[Test]
		public void BasicTest4()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number name=\"TheNumber\" size=\"8\">" +
				"			<Relation type=\"count\" of=\"Array\" />" +
				"		</Number>" +
				"		<Block name=\"Block1\">" +
				"			<Block name=\"Block1_1\">" +
				"				<String name=\"FindMe\"/>" +
				"			</Block>" +
				"		</Block>" +
				"		<String name=\"Array\" value=\"1\" maxOccurs=\"100\"/>" +
				"		<Block>" +
				"			<String name=\"FindMe\"/>" +
				"		</Block>" +
				"	</DataModel>" +
				"	<StateModel name=\"TheState\" initialState=\"State1\">" +
				"		<State name=\"State1\">" +
				"			<Action type=\"output\">" +
				"				<DataModel ref=\"TheDataModel\" />" +
				"			</Action>" +
				"		</State>" +
				"	</StateModel>" +
				"	<Test name=\"Default\">" +
				"		<StateModel ref=\"TheState\"/>" +
				"		<Publisher class=\"Console\" />" +
				"	</Test>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var dataModel = dom.tests["Default"].stateModel.states["State1"].actions[0].dataModel;
			DataElement findMe = ((DataElementContainer)((DataElementContainer)dataModel[1])[0])[0];

			PeachXPathNavigator navi = new PeachXPathNavigator(dom.tests[0].stateModel);
			XPathNodeIterator iter = navi.Select("//TheDataModel/Block1/Block1_1/FindMe");

			Assert.IsTrue(iter.MoveNext());
			Assert.AreEqual(findMe, ((PeachXPathNavigator)iter.Current).CurrentNode);
			Assert.IsFalse(iter.MoveNext());
		}

		[Test]
		public void BasicAttributeTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number name=\"TheNumber\" size=\"8\">" +
				"			<Relation type=\"count\" of=\"Array\" />" +
				"		</Number>" +
				"		<Block name=\"Block1\">" +
				"			<Block name=\"Block1_1\">" +
				"				<String name=\"FindMe\"/>" +
				"			</Block>" +
				"		</Block>" +
				"		<String name=\"Array\" value=\"1\" maxOccurs=\"100\"/>" +
				"		<Block>" +
				"			<String name=\"FindMe\" token=\"true\"/>" +
				"		</Block>" +
				"	</DataModel>" +
				"	<StateModel name=\"TheState\" initialState=\"State1\">" +
				"		<State name=\"State1\">" +
				"			<Action type=\"output\">" +
				"				<DataModel ref=\"TheDataModel\" />" +
				"			</Action>" +
				"		</State>" +
				"	</StateModel>" +
				"	<Test name=\"Default\">" +
				"		<StateModel ref=\"TheState\"/>" +
				"		<Publisher class=\"Console\" />" +
				"	</Test>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var dataModel = dom.tests["Default"].stateModel.states["State1"].actions[0].dataModel;
			DataElement findMe = ((DataElementContainer)dataModel[3])[0];

			PeachXPathNavigator navi = new PeachXPathNavigator(dom.tests[0].stateModel);
			XPathNodeIterator iter = navi.Select("//FindMe[@isToken='True']");

			Assert.IsTrue(iter.MoveNext());
			Assert.AreEqual(findMe, ((PeachXPathNavigator)iter.Current).CurrentNode);
			Assert.IsFalse(iter.MoveNext());
		}

		[Test]
		public void StateModelTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number name=\"TheNumber\" size=\"8\">" +
				"			<Relation type=\"count\" of=\"Array\" />" +
				"		</Number>" +
				"		<Block name=\"Block1\">" +
				"			<Block name=\"Block1_1\">" +
				"				<String name=\"FindMe\"/>" +
				"			</Block>" +
				"		</Block>" +
				"		<String name=\"Array\" value=\"1\" maxOccurs=\"100\"/>" +
				"		<Block>" +
				"			<String name=\"FindMe\" token=\"true\"/>" +
				"		</Block>" +
				"	</DataModel>" +
				"	<StateModel name=\"TheState\" initialState=\"State1\">" +
				"		<State name=\"State1\">" +
				"			<Action type=\"output\">" +
				"				<DataModel ref=\"TheDataModel\" />" +
				"			</Action>" +
				"		</State>" +
				"	</StateModel>" +
				"	<Test name=\"Default\">" +
				"		<StateModel ref=\"TheState\"/>" +
				"		<Publisher class=\"Console\" />" +
				"	</Test>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var dataModel = dom.tests["Default"].stateModel.states["State1"].actions[0].dataModel;
			DataElement findMe1 = ((DataElementContainer)((DataElementContainer)dataModel[1])[0])[0];
			DataElement findMe2 = ((DataElementContainer)dataModel[3])[0];

			PeachXPathNavigator navi = new PeachXPathNavigator(dom.tests[0].stateModel);
			XPathNodeIterator iter = navi.Select("//FindMe");

			Assert.IsTrue(iter.MoveNext());
			Assert.AreEqual(findMe1, ((PeachXPathNavigator)iter.Current).CurrentNode);
			Assert.IsTrue(iter.MoveNext());
			Assert.AreEqual(findMe2, ((PeachXPathNavigator)iter.Current).CurrentNode);
			Assert.IsFalse(iter.MoveNext());
		}

		[Test]
		public void ActionParam()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number name=\"FindMe\" size=\"8\"/>" +
				"	</DataModel>" +
				"	<StateModel name=\"TheState\" initialState=\"State1\">" +
				"		<State name=\"State1\">" +
				"			<Action name='call' type=\"call\" method=\"foo\">" +
				"				<Param>" +
				"					<DataModel ref=\"TheDataModel\" />" +
				"				</Param>" +
				"				<Param>" +
				"					<DataModel ref=\"TheDataModel\" />" +
				"				</Param>" +
				"			</Action>" +
				"		</State>" +
				"	</StateModel>" +
				"	<Test name=\"Default\">" +
				"		<StateModel ref=\"TheState\"/>" +
				"		<Publisher class=\"Console\" />" +
				"	</Test>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var action = dom.tests["Default"].stateModel.states["State1"].actions[0] as Peach.Core.Dom.Actions.Call;
			DataElement findMe1 = action.parameters[0].dataModel[0];
			DataElement findMe2 = action.parameters[1].dataModel[0];

			PeachXPathNavigator navi = new PeachXPathNavigator(dom.tests[0].stateModel);
			XPathNodeIterator iter = navi.Select("//FindMe");

			
			Assert.IsTrue(iter.MoveNext());
			Assert.AreEqual(findMe1, ((PeachXPathNavigator)iter.Current).CurrentNode);
			Assert.IsTrue(iter.MoveNext());
			Assert.AreEqual(findMe2, ((PeachXPathNavigator)iter.Current).CurrentNode);
			Assert.IsFalse(iter.MoveNext());
		}

		[Test]
		public void ActionResult()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number name=\"FindMe\" size=\"8\"/>" +
				"	</DataModel>" +
				"	<StateModel name=\"TheState\" initialState=\"State1\">" +
				"		<State name=\"State1\">" +
				"			<Action name='call' type=\"call\" method=\"foo\">" +
				"				<Result>" +
				"					<DataModel ref=\"TheDataModel\" />" +
				"				</Result>" +
				"			</Action>" +
				"		</State>" +
				"	</StateModel>" +
				"	<Test name=\"Default\">" +
				"		<StateModel ref=\"TheState\"/>" +
				"		<Publisher class=\"Console\" />" +
				"	</Test>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var action = dom.tests["Default"].stateModel.states["State1"].actions[0] as Peach.Core.Dom.Actions.Call;
			DataElement findMe1 = action.result.dataModel[0];

			PeachXPathNavigator navi = new PeachXPathNavigator(dom.tests[0].stateModel);
			XPathNodeIterator iter = navi.Select("//FindMe");

			Assert.IsTrue(iter.MoveNext());
			Assert.AreEqual(findMe1, ((PeachXPathNavigator)iter.Current).CurrentNode);
			Assert.IsFalse(iter.MoveNext());
		}

		[Test]
		public void ActionParamsByName()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number name=\"FindMe\" size=\"8\"/>" +
				"	</DataModel>" +
				"	<StateModel name=\"TheState\" initialState=\"State1\">" +
				"		<State name=\"State1\">" +
				"			<Action name='call' type=\"call\" method=\"foo\">" +
				"				<Param>" +
				"					<DataModel name=\"DM_Param1\" ref=\"TheDataModel\" />" +
				"				</Param>" +
				"				<Param>" +
				"					<DataModel name=\"DM_Param2\" ref=\"TheDataModel\" />" +
				"				</Param>" +
				"				<Result name=\"Result\">" +
				"					<DataModel name=\"DM_Result\" ref=\"TheDataModel\" />" +
				"				</Result>" +
				"			</Action>" +
				"		</State>" +
				"	</StateModel>" +
				"	<Test name=\"Default\">" +
				"		<StateModel ref=\"TheState\"/>" +
				"		<Publisher class=\"Console\" />" +
				"	</Test>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var action = dom.tests["Default"].stateModel.states["State1"].actions[0] as Peach.Core.Dom.Actions.Call;


			PeachXPathNavigator navi = new PeachXPathNavigator(dom.tests[0].stateModel);
			XPathNodeIterator iter = navi.Select("//DM_Param1//FindMe");
			DataElement findMe = action.parameters[0].dataModel[0];

			Assert.IsTrue(iter.MoveNext());
			Assert.AreEqual(findMe, ((PeachXPathNavigator)iter.Current).CurrentNode);
			Assert.IsFalse(iter.MoveNext());

			iter = navi.Select("//DM_Param2//FindMe");
			findMe = action.parameters[1].dataModel[0];

			Assert.IsTrue(iter.MoveNext());
			Assert.AreEqual(findMe, ((PeachXPathNavigator)iter.Current).CurrentNode);
			Assert.IsFalse(iter.MoveNext());

			iter = navi.Select("//Result/DM_Result//FindMe");
			findMe = action.result.dataModel[0];

			Assert.IsTrue(iter.MoveNext());
			Assert.AreEqual(findMe, ((PeachXPathNavigator)iter.Current).CurrentNode);
			Assert.IsFalse(iter.MoveNext());
		}

		static void Dump(PeachXPathNavigator nav, int indent)
		{
			if (nav.Prefix == string.Empty)
				Console.Write("{0}<{1}", new string(' ', indent), nav.LocalName);
			else
				Console.Write("{0}<{1}:{2}", new string(' ', indent), nav.Prefix, nav.LocalName);

			if (nav.NodeType != XPathNodeType.Attribute && nav.MoveToFirstAttribute())
			{
				do
				{
					Console.Write(" {0}='{1}'", nav.Name, nav.Value);
				}
				while (nav.MoveToNextAttribute());

				nav.MoveToParent();
			}

			Console.WriteLine(">");

			if (nav.MoveToFirstChild())
			{
				do
				{
					Dump(nav, indent + 2);
				} while (nav.MoveToNext());

				nav.MoveToParent();
			}

			Console.WriteLine("{0}</{1}>", new string(' ', indent), nav.Name);
		}

		[Test]
		public void NewNav()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number name=\"FindMe\" size=\"8\"/>" +
				"		<Block name=\"Empty\"/>" +
				"	</DataModel>" +
				"	<StateModel name=\"TheState\" initialState=\"State1\">" +
				"		<State name=\"State1\">" +
				"			<Action name='call' type=\"call\" method=\"foo\">" +
				"				<Param>" +
				"					<DataModel name=\"DM_Param1\" ref=\"TheDataModel\" />" +
				"				</Param>" +
				"				<Param>" +
				"					<DataModel name=\"DM_Param2\" ref=\"TheDataModel\" />" +
				"				</Param>" +
				"				<Result>" +
				"					<DataModel name=\"DM_Result\" ref=\"TheDataModel\" />" +
				"				</Result>" +
				"			</Action>" +
				"		</State>" +
				"	</StateModel>" +
				"	<Test name=\"Default\">" +
				"		<StateModel ref=\"TheState\"/>" +
				"		<Publisher class=\"Console\" />" +
				"	</Test>" +
				"</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			dom.Name = "Peach";

			var nav = new PeachXPathNavigator(dom.tests[0].stateModel);

			Dump(nav, 0);
		}
	}
}

// end
