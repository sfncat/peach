using System.Collections;
using System.Net.Http;
using System.Threading;
using NUnit.Framework;
using Peach.Core.Test;
using Peach.Pro.Core.License;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Test.WebApi.Controllers
{
	[TestFixture]
	[Quick]
	class LibrariesControllerTests : ControllerTestsBase
	{
		class Comparer : IComparer
		{
			public int Compare(object x, object y)
			{
				if (x == null && y == null)
					return 0;

				var lhs = x as Library;
				if (lhs == null)
					return -1;

				var rhs = y as Library;
				if (rhs == null)
					return 1;

				return string.Compare(lhs.Name, rhs.Name);
			}
		}

		[Test]
		public void GetLibraries()
		{
			_license.Setup(x => x.Status).Returns(LicenseStatus.Valid);
			_license.Setup(x => x.EulaAccepted).Returns(true);

			var expected = new Library[] 
			{
				new Library { Name = "Foo" }
			};

			_pitDatabase.Setup(x => x.Libraries).Returns(expected);

			using (var request = new HttpRequestMessage(HttpMethod.Get, "/p/libraries"))
			using (var response = _server.HttpClient.SendAsync(request, CancellationToken.None).Result)
			{
				var actual = response.DeserializeJson<Library[]>();
				CollectionAssert.AreEqual(expected, actual, new Comparer());
			}
		}
	}
}
