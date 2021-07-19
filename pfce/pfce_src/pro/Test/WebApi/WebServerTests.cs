using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Moq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core;
using Peach.Pro.Core.License;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.WebApi2;

namespace Peach.Pro.Test.WebApi
{
	[TestFixture]
	[Quick]
	class WebServerTests
	{
		Mock<ILicense> _license = new Mock<ILicense>();
		
		[Test]
		public void MultipleServers()
		{
			using (var tmpDir = new TempDirectory())
			{
				var listener = new TcpListener(IPAddress.Any, 0);

				try
				{
					listener.Start();
					var port = ((IPEndPoint)listener.LocalEndpoint).Port;
					Assert.AreNotEqual(0, port);

					using (var web = new WebServer(_license.Object, tmpDir.Path, new InternalJobMonitor(_license.Object)))
					{
						web.Start(port, true);

						var actualPort = web.Uri.Port;
						Assert.Greater(actualPort, port);

						using (var web2 = new WebServer(_license.Object, tmpDir.Path, new InternalJobMonitor(_license.Object)))
						{
							web2.Start(actualPort, true);
							Assert.Greater(web2.Uri.Port, actualPort);
						}
					}
				}
				finally
				{
					listener.Stop();
				}
			}
		}

		[Test]
		public void SpecicPortInUse()
		{
			using (var tmpDir = new TempDirectory())
			{
				var listener = new TcpListener(IPAddress.Any, 0);

				try
				{
					listener.Start();
					var port = ((IPEndPoint)listener.LocalEndpoint).Port;
					Assert.AreNotEqual(0, port);

					using (var web = new WebServer(_license.Object, tmpDir.Path, new InternalJobMonitor(_license.Object)))
					{
						var ex = Assert.Throws<PeachException>(() =>
							web.Start(port, false));

						var expected = "Unable to start the web server at http://localhost:{0}/ because the port is currently in use.".Fmt(port);
						Assert.AreEqual(expected, ex.Message);
					}
				}
				finally
				{
					listener.Stop();
				}
			}
		}

		[Test]
		public void JsonTests()
		{
			var start = DateTime.Parse("2001-05-01 18:38:09-06:00");

			Assert.AreEqual(DateTimeKind.Local, start.Kind);

			var j = new Job
			{
				Guid = Guid.Empty,
				StartDate = start,
				Runtime = TimeSpan.FromMilliseconds(36100),
				IterationCount = 5,
			};

			var ser = JsonUtilities.CreateSerializer();

			var sb = new StringBuilder();

			using (var writer = new StringWriter(sb))
				ser.Serialize(writer, j);

			var asJson = sb.ToString();

			// 5 iterations in 36 sec is speed of 500 iterations per hour
			StringAssert.Contains("\"speed\": 500", asJson);

			// Enums should be camel case
			StringAssert.Contains("\"status\": \"stopped\"", asJson);

			// TimeSpan should be total seconds
			StringAssert.Contains("\"runtime\": 36", asJson);

			// DateTime should be in ISO8601 in UTC
			StringAssert.Contains("\"startDate\": \"2001-05-02T00:38:09Z\"", asJson);

			using (var reader = new StringReader(asJson))
				j = (Job)ser.Deserialize(reader, typeof(Job));

			Assert.NotNull(j);

			Assert.AreEqual(JobStatus.Stopped, j.Status);
			Assert.AreEqual(TimeSpan.FromSeconds(36), j.Runtime);
			Assert.AreEqual(DateTimeKind.Utc, j.StartDate.Kind);
			Assert.AreEqual(start.ToUniversalTime(), j.StartDate);
		}
	}
}
