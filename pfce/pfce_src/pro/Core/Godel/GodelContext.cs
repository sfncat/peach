using System;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using NLog;
using Peach.Core;
using Logger = NLog.Logger;

namespace Peach.Pro.Core.Godel
{
	[Serializable]
	public class Dom : Peach.Core.Dom.Dom
	{
		public NamedCollection<GodelContext> godel = new NamedCollection<GodelContext>();
	}

	[Serializable]
	public class StateModel : Peach.Core.Dom.StateModel
	{
		[NonSerialized]
		public NamedCollection<GodelContext> godel = new NamedCollection<GodelContext>();
	}

	[Serializable]
	public class GodelContext : INamed
	{
		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		#endregion

		static readonly Logger logger = LogManager.GetCurrentClassLogger();

		public string debugName { get; set; }
		public string type { get; set; }
		public string Name { get; set; }
		public string refName { get; set; }
		public bool? controlOnly { get; set; }
		public string inv { get; set; }
		public string pre { get; set; }
		public string post { get; set; }

		private RunContext context;
		private ScriptScope globalScope;
		private CompiledCode invScript;
		private CompiledCode preScript;
		private CompiledCode postScript;

		public void Pre(object self)
		{
			Run(invScript, "pre-inv", self, null);
			Run(preScript, "pre", self, null);
		}

		public void Post(object self, object pre)
		{
			Run(invScript, "post-inv", self, null);
			Run(postScript, "post", self, pre);
		}

		public void OnTestStarting(RunContext context, ScriptScope globalScope)
		{
			this.context = context;
			this.globalScope = globalScope;

			invScript = Compile("inv", inv);
			preScript = Compile("pre", pre);
			postScript = Compile("post", post);
		}

		public void OnTestFinished()
		{
			context = null;
			globalScope = null;
			invScript = null;
			preScript = null;
			postScript = null;
		}

		CompiledCode Compile(string dir, string eval)
		{
			if (string.IsNullOrEmpty(eval))
				return null;

			try
			{
				var source = globalScope.Engine.CreateScriptSourceFromString(eval, SourceCodeKind.Expression);
				var ret = source.Compile();

				return ret;
			}
			catch (Exception ex)
			{
				var err = "Error compiling Godel {0} expression for {1}. {2}".Fmt(dir, debugName, ex.Message);
				throw new PeachException(err, ex);
			}
		}

		void Run(CompiledCode code, string dir, object self, object pre)
		{
			if (code == null)
				return;

			if (controlOnly.GetValueOrDefault() && !context.controlIteration)
			{
				logger.Debug("Godel {0}: Ignoring control only. ({1})", dir, debugName);
				return;
			}

			globalScope.SetVariable("self", self);
			globalScope.SetVariable("context", context);

			if (pre == null)
				globalScope.RemoveVariable("pre");
			else
				globalScope.SetVariable("pre", pre);

			object ret;

			try
			{
				ret = code.Execute(globalScope);
			}
			catch (Exception ex)
			{
				var err = "Error, Godel failed to execute {0} expression for {1}. {2}".Fmt(dir, debugName, ex.Message);
				throw new SoftException(err, ex);
			}

			bool bRet;

			try
			{
				bRet = (bool)ret;
			}
			catch (Exception ex)
			{
				var err = "Error, Godel failed to parse the return value for the {0} expression for {1}. {2}".Fmt(dir, debugName, ex.Message);
				throw new SoftException(err, ex);
			}

			if (bRet)
			{
				logger.Debug("Godel {0}: Passed. ({1})", dir, debugName);
				return;
			}

			var msg = "Godel {0} expression for {1} failed.".Fmt(dir, debugName);

			logger.Error(msg);

			var fault = new Fault
			{
				detectionSource = "Godel",
				type = FaultType.Fault,
				title = msg,
				description = msg,
				folderName = "Godel",
				majorHash = "Godel",
				minorHash = debugName
			};

			context.faults.Add(fault);

			throw new SoftException(msg);
		}
	}
}
