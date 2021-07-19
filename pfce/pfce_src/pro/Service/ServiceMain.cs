using Peach.Pro.Core.Runtime;
using Peach.Pro.WebApi2;

namespace PeachService
{
	public class ServiceMain
	{
		static int Main(string[] args)
		{
			using (var service = new Service
			{
				CreateWeb = (license, pitLibraryPath, jobMonitor) =>
					new WebServer(license, pitLibraryPath, jobMonitor)
			})
			{
				return service.Run(args);
			}
		}
	}
}