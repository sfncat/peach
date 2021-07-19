

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Pro.Core.OS.Windows.Publishers.Com;

namespace Peach.Pro.OS.Windows.Publishers
{
	[Publisher("Com")]
	[Alias("com.Com")]
	[Parameter("clsid", typeof(string), "COM CLSID of object")]
	public class ComPublisher : Publisher
	{
		private static readonly NLog.Logger ClassLogger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return ClassLogger; } }

		public string clsid { get; protected set; }

		private IComContainer _container;

		public ComPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		private Variant Try(Func<Variant> action)
		{
			try
			{
				if (_container == null)
				{
					var logLevel = Logger.IsTraceEnabled ? 2 : (Logger.IsDebugEnabled ? 1 : 0);
					var server = (ComContainerServer)Activator.GetObject(
						typeof(ComContainerServer),
						"ipc://Peach_Com_Container/PeachComContainer");

					_container = server.GetComContainer(logLevel, clsid);
				}

				try
				{
					return action();
				}
				catch (Exception ex)
				{
					// Errors using the com object should be treated as recoverable
					throw new SoftException(ex);
				}
			}
			catch (SoftException)
			{
				throw;
			}
			catch (Exception ex)
			{
				OnStop();

				// Errors constructing the com object should be treated as fatal
				throw new PeachException("Error, ComPublisher was unable to create object.  " + ex.Message, ex);
			}
		}

		protected static object GetObj(Variant v)
		{
			switch (v.GetVariantType())
			{
				case Variant.VariantType.BitStream:
					var ms = new MemoryStream();
					((BitwiseStream)v).Seek(0, SeekOrigin.Begin);
					((BitwiseStream)v).CopyTo(ms);
					return ms.ToArray();
				case Variant.VariantType.Boolean:
					return (bool)v;
				case Variant.VariantType.ByteString:
					return (byte[])v;
				case Variant.VariantType.Int:
					return (int)v;
				case Variant.VariantType.Long:
					return (long)v;
				case Variant.VariantType.String:
					return (string)v;
				case Variant.VariantType.ULong:
					return (ulong)v;
				default:
					throw new NotImplementedException();
			}
		}

		protected override void OnStop()
		{
			_container = null;
		}

		protected override Variant OnCall(string method, List<BitwiseStream> args)
		{
			// This publisher only supports calling with ActionParameters.
			// This function is never called when the publisher is run locally.
			// This exception is needed so the agent generates a nice error message.
			throw new NotSupportedException();
		}

		protected override Variant OnCall(string method, List<ActionParameter> parameters)
		{
			var args = parameters.Select(i => GetObj(i.dataModel[0].InternalValue)).ToArray();

			return Try(() =>
			{
				var ret = _container.CallMethod(method, args);

				if (ret != null)
					return new Variant(ret.ToString());

				return null;
			});
		}

		protected override void OnSetProperty(string property, Variant value)
		{
			var arg = GetObj(value);

			Try(() =>
			{
				_container.SetProperty(property, arg);
				return null;
			});
		}

		protected override Variant OnGetProperty(string property)
		{
			return Try(() =>
			{
				var ret = _container.GetProperty(property);

				if (ret != null)
					return new Variant(ret.ToString());

				return null;
			});
		}
	}
}

// END
