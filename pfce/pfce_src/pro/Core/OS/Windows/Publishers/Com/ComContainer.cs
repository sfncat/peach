using System;
using System.Collections.Generic;
using Peach.Core;

namespace Peach.Pro.Core.OS.Windows.Publishers.Com
{
	public class ComContainer : MarshalByRefObject, IComContainer
	{
		private readonly object comObject;
		private readonly PythonScripting scripting;

		public ComContainer(string clsid)
		{
			var type = Type.GetTypeFromProgID(clsid);

			if (type == null)
				type = Type.GetTypeFromCLSID(Guid.Parse(clsid));

			if (type == null)
				throw new Exception("ComContainer was unable to create type from id '" + clsid + "'.");

			comObject = Activator.CreateInstance(type);

			if (comObject == null)
				throw new Exception("Error, ComContainer was unable to create object from id '" + clsid + "'.");

			scripting = new PythonScripting();
		}

		public object CallMethod(string method, object[] args)
		{
			var cmd = "ComObject." + method + "(";
			var state = new Dictionary<string, object>
			{
				{ "ComObject", comObject }
			};

			for (var i = 0; i < args.Length; ++i)
			{
				state["ComArgs_" + i] = args[i];
				cmd += "ComArgs_" + i + ",";
			}

			// Remove that last comma
			if (args.Length > 0)
				cmd = cmd.Substring(0, cmd.Length - 1);

			cmd += ")";

			return scripting.Eval(cmd, state);
		}

		public object GetProperty(string property)
		{
			var cmd = "ComObject." + property;
			var state = new Dictionary<string, object>
			{
				{ "ComObject", comObject }
			};

			return scripting.Eval(cmd, state);
		}

		public void SetProperty(string property, object value)
		{
			var cmd = "ComObject." + property + " = ComArg";
			var state = new Dictionary<string, object>
			{
				{ "ComObject", comObject },
				{ "ComArg", value }
			};

			scripting.Exec(cmd, state);
		}
	}
}
