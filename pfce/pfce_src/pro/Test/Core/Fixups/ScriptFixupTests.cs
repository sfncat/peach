using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Fixups
{
	[TestFixture]
	[Quick]
	[Peach]
    class ScriptFixupTests : DataModelCollector
    {
        [Test]
        public void StringTest1()
        {
            string fixup = @"
class FixupReturningString:
    def __init__(self, parent):
        self._parent = parent
	
    def fixup(self, element):
        return 'Hello from FixupReturningString'
";
            string tmpPath = Path.GetTempPath();
            string tmpFile = tmpPath + "/fixup.py";
            string xml = @"<Peach>
                                <PythonPath path='{0}'/>
	                            <Import import='fixup' />

	                            <!-- Create a simple data template containing a single string -->
	                            <DataModel name='TheDataModel'>
		                            <String value='Hello World!'>
			                            <Fixup class='ScriptFixup'>
				                            <Param name='class' value='fixup.FixupReturningString'/>
				                            <Param name='ref' value='TheDataModel' />
			                            </Fixup>
		                            </String>
	                            </DataModel>

	                            <StateModel name='State' initialState='State1' >
		                            <State name='State1'  >
			                            <Action type='output' >
				                            <DataModel ref='TheDataModel'/>
			                            </Action>
		                            </State>
	                            </StateModel>

	                            <Test name='Default'>
		                            <StateModel ref='State'/>
		                            <Publisher class='Null' />
	                            </Test>
                            </Peach>".Fmt(tmpPath, tmpFile);

            try
            {
                //Writes tmp python fixup
                File.WriteAllText(tmpFile, fixup);

                PitParser parser = new PitParser();

                Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

                RunConfiguration config = new RunConfiguration();
                config.singleIteration = true;

                Engine e = new Engine(this);
                e.startFuzzing(dom, config);

                // verify values
                byte[] expected = Encoding.ASCII.GetBytes("Hello from FixupReturningString");
                Assert.AreEqual(1, dataModels.Count);
                Assert.AreEqual(expected, dataModels[0].Value.ToArray());
            }
            finally
            {
                File.Delete(tmpFile);
            }
        }
    }
}
