


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using System.Numerics;
using System.IO;
using NLog;
using Peach.Core.IO;

namespace Peach.Core
{
	public class PythonScripting : Scripting
	{
		protected override ScriptEngine GetEngine()
		{
			return Python.CreateEngine();
		}
	}

	/// <summary>
	/// Scripting class provides easy to use
	/// methods for using Python with Peach.
	/// </summary>
	public abstract class Scripting
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		#region Private Members

		private readonly Dictionary<string, object> _modules = new Dictionary<string, object>();
		private readonly ScriptEngine _engine;

		#endregion

		#region Constructor

		protected Scripting()
		{
			_engine = GetEngine();
		}

		#endregion

		#region Abstract Fucntions

		protected abstract ScriptEngine GetEngine();

		#endregion

		#region Module Imports

		public void ImportModule(string module)
		{
			if (!_modules.ContainsKey(module))
				_modules.Add(module, _engine.ImportModule(module));
		}

		public IEnumerable<string> Modules
		{
			get { return _modules.Keys; }
		}

		#endregion

		#region Search Paths

		public void AddSearchPath(string path)
		{
			var paths = _engine.GetSearchPaths();
			if (!paths.Contains(path))
			{
				paths.Add(path);
				_engine.SetSearchPaths(paths);
			}
		}

		public IEnumerable<string> Paths
		{
			get { return _engine.GetSearchPaths(); }
		}

		#endregion

		#region Error Listener

		class ScriptErrorListener : ErrorListener
		{
			public readonly List<string> Errors = new List<string>();
			public readonly List<string> Warnings = new List<string>();

			public override void ErrorReported(ScriptSource source, string message, SourceSpan span, int errorCode, Severity severity)
			{
				switch (severity)
				{
					case Severity.FatalError:
					case Severity.Error:
						Errors.Add("{0} at line {1}.".Fmt(message, source.MapLine(span)));
						break;
					case Severity.Warning:
					case Severity.Ignore:
						Warnings.Add("{0} at line {1}.".Fmt(message, source.MapLine(span)));
						break;
				}
			}
		}

		#endregion

		#region Compile

		readonly Dictionary<string, CompiledCode> _scriptCache = new Dictionary<string, CompiledCode>();

		private CompiledCode CompileCode(ScriptScope scope, string code, SourceCodeKind kind, bool cache)
		{
			CompiledCode compiled;

			if (!_scriptCache.TryGetValue(code, out compiled))
			{
				var errors = new ScriptErrorListener();
				var source = scope.Engine.CreateScriptSourceFromString(code, kind);
				compiled = source.Compile(errors);

				if (compiled == null)
				{
					var err = errors.Errors.FirstOrDefault() ?? errors.Warnings.FirstOrDefault();
					if (err == null)
						throw new PeachException("Failed to compile expression [{0}].".Fmt(code));

					throw new PeachException("Failed to compile expression [{0}], {1}".Fmt(code, err));
				}

				if (cache)
					_scriptCache[code] = compiled;
			}

			return compiled;
		}

		#endregion

		#region Exec & Eval

		/// <summary>
		/// Global scope for this instance of scripting
		/// </summary>
		ScriptScope _scope;

		/// <summary>
		/// Create the global scope, or return existing one
		/// </summary>
		/// <returns></returns>
		public ScriptScope CreateScope()
		{
			if (_scope == null)
			{
				_scope = _engine.CreateScope();

				Apply(_scope, _modules);
			}

			return _scope;
		}

		/// <summary>
		/// Execute a scripting program. Not cached.
		/// </summary>
		/// <param name="code"></param>
		/// <param name="localScope"></param>
		public void Exec(string code, Dictionary<string, object> localScope)
		{
			var scope = CreateScope(localScope);
			var compiled = CompileCode(scope, code, SourceCodeKind.Statements, false);

			try
			{
				compiled.Execute(scope);
			}
			catch (SoftException)
			{
				throw;
			}
			catch (PeachException)
			{
				throw;
			}
			catch (Exception ex)
			{
				if (ex.GetBaseException() is ThreadAbortException)
					throw;

				logger.Debug(ex, "Failed to execute expression [{0}], {1}.".Fmt(code, ex.Message));
				throw new SoftException("Failed to execute expression [{0}], {1}.".Fmt(code, ex.Message), ex);
			}
			finally
			{
				CleanupScope(scope, localScope);
			}
		}

		/// <summary>
		/// Evaluate an expression. Pre-compiled expressions are cached by default.
		/// </summary>
		/// <param name="code">Expression to evaluate</param>
		/// <param name="localScope">Local scope for expression</param>
		/// <param name="cache">Cache compiled script for re-use (defaults to true)</param>
		/// <returns>Result from expression</returns>
		public object Eval(string code, Dictionary<string, object> localScope, bool cache = true)
		{
			var scope = CreateScope(localScope);
			var compiled = CompileCode(scope, code, SourceCodeKind.Expression, cache);

			try
			{
				var obj = compiled.Execute(scope);

				if (obj is BigInteger)
				{
					try
					{
						var bint = (BigInteger)obj;

						if (bint.Sign < 0)
							return (long)bint;

						return (ulong)bint;
					}
					catch (Exception ex)
					{
						throw new SoftException(ex);
					}
				}

				return obj;
			}
			catch (SoftException)
			{
				throw;
			}
			catch (PeachException)
			{
				throw;
			}
			catch (Exception ex)
			{
				if (ex.GetBaseException() is ThreadAbortException)
					throw;

				logger.Debug(ex, "Failed to evaluate expression [{0}], {1}.".Fmt(code, ex.Message));
				throw new SoftException("Failed to evaluate expression [{0}], {1}.".Fmt(code, ex.Message), ex);
			}
		}

		public static Variant ToVariant(object data)
		{
			var asBytes = data as byte[];
			if (asBytes != null)
				return new Variant(asBytes);

			var asString = data as string;
			if (asString != null)
				return new Variant(asString);

			if (data is int)
				return new Variant((int)data);

			if (data is long)
				return new Variant((long)data);

			if (data is ulong)
				return new Variant((ulong)data);

			return null;
		}

		#endregion

		#region Private Helpers

		/// <summary>
		/// Returns the global scope with localScope added in.
		/// </summary>
		/// <param name="localScope"></param>
		/// <returns></returns>
		private ScriptScope CreateScope(Dictionary<string, object> localScope)
		{
			var scope = CreateScope();

			Apply(scope, localScope);

			return scope;
		}

		/// <summary>
		/// Remove local scope items from global scope. This is quick.
		/// </summary>
		/// <param name="scope"></param>
		/// <param name="localScope"></param>
		private void CleanupScope(ScriptScope scope, Dictionary<string, object> localScope)
		{
			foreach (var name in localScope.Keys)
				scope.RemoveVariable(name);
		}

		private static void Apply(ScriptScope scope, Dictionary<string, object> vars)
		{
			foreach (var item in vars)
			{
				var name = item.Key;
				var value = item.Value;

				var bs = value as BitwiseStream;
				if (bs != null)
				{
					var buffer = new byte[bs.Length];
					var offset = 0;
					var count = buffer.Length;

					bs.Seek(0, SeekOrigin.Begin);

					int nread;
					while ((nread = bs.Read(buffer, offset, count)) != 0)
					{
						offset += nread;
						count -= nread;
					}

					value = buffer;
				}

				scope.SetVariable(name, value);
			}
		}

		#endregion
	}
}
