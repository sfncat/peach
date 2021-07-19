

using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Threading;
using Peach.Core;
using Peach.Pro.Core.OS.Windows.Publishers.Com;

namespace Peach.Pro.ComContainer
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var asm = Assembly.GetExecutingAssembly();

			Console.WriteLine("> Peach Com Container v{0}", asm.GetName().Version);
			Console.WriteLine("> {0}", asm.GetCopyright());
			Console.WriteLine();

			var provider = new BinaryServerFormatterSinkProvider
			{
				TypeFilterLevel = TypeFilterLevel.Full
			};

			var props = new Hashtable();
			props["name"] = "ipc";
			props["portName"] = "Peach_Com_Container";

			var channel = new IpcChannel(props, null, provider);

			ChannelServices.RegisterChannel(channel, false);

			try
			{
				RemotingConfiguration.RegisterWellKnownServiceType(
					typeof(ComContainerServer),
					"PeachComContainer",
					WellKnownObjectMode.Singleton);

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
		}
	}
}
