using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Threading;
using Mono.Unix;
using Mono.Unix.Native;

namespace PeachTrampoline
{
	public class TrampolineMain
	{
		static int Main(string[] args)
		{
			if (args.Length == 3 && args[0] == "--ipc")
				return DoRemote(args[1], args[2]);

			if (args.Length > 0 && args[0] != "--ipc")
				return DoExec(args);

			var asm = Assembly.GetExecutingAssembly();
			var asmName = asm.GetName();
			var copywright = asm.GetCustomAttributes(false)
				.OfType<AssemblyCopyrightAttribute>()
				.Select(a => a.Copyright)
				.FirstOrDefault();

			Console.WriteLine(@"{0} v{1}

{2}

Ipc Proxy Usage:
{0}.exe --ipc <channel> <type>

Exec Usage:
{0}.exe <file> [<args>]
", asmName.Name, asmName.Version, copywright ?? "");

			return -1;
		}

		static int DoExec(string[] args)
		{
			var ret = Syscall.fcntl(3, FcntlCommand.F_SETFD, 1);
			UnixMarshal.ThrowExceptionForLastErrorIf(ret);
	
			ret = Syscall.execvp(args[0], args);
			UnixMarshal.ThrowExceptionForLastErrorIf(ret);

			return 0;
		}

		static int DoRemote(string channelName, string typeName)
		{
			var provider = new BinaryServerFormatterSinkProvider
			{
				TypeFilterLevel = TypeFilterLevel.Full
			};

			var props = new Hashtable();
			props["name"] = "ipc";
			props["portName"] = channelName;

			var channel = new IpcChannel(props, null, provider);

			ChannelServices.RegisterChannel(channel, false);

			try
			{
				var type = Type.GetType(typeName, true);

				var baseType = type.BaseType;
				while (baseType != null && baseType != typeof(MarshalByRefObject))
					baseType = baseType.BaseType;
				if (baseType == null)
					throw new NotSupportedException(string.Format("Error, {0} is not a MarshalByRefObject.", type.Name));

				RemotingConfiguration.RegisterWellKnownServiceType(
					type, type.Name, WellKnownObjectMode.Singleton);

				using (var evt = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\" + channelName))
				{
					// Signal we are started
					evt.Set();
				}

				Thread.Sleep(Timeout.Infinite);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				ChannelServices.UnregisterChannel(channel);
			}

			return 0;
		}
	}
}
