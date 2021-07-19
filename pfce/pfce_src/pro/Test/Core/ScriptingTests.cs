using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Peach]
	[Quick]
	class ScriptingTests
	{
		[Test]
		public void TestPeachModule()
		{
			const string script = @"
from peach import webproxy

def on_request(context, action, body):
	pass

webproxy.register_event(webproxy.EVENT_ACTION, on_request)
";

			using (var d = new TempDirectory())
			{
				var p = new PythonScripting();
				p.AddSearchPath(Configuration.ScriptsPath);

				File.WriteAllText(Path.Combine(d.Path, "mymodule.py"), script);
				p.AddSearchPath(d.Path);
				p.ImportModule("mymodule");
			}
		}
	}
}
