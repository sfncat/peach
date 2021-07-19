using System.IO;
using System.Reflection;
using Microsoft.Owin.Testing;
using Moq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core;
using Peach.Pro.Core.License;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Core.WebServices;
using Peach.Pro.WebApi2;

namespace Peach.Pro.Test.WebApi
{
	[SetUpFixture]
	internal class TestBase : SetUpFixture
	{
		[OneTimeSetUp]
		public void SetUp()
		{
			DoSetUp();

			BaseProgram.Initialize();
		}

		[OneTimeTearDown]
		public void TearDown()
		{
			DoTearDown();
		}
	}

	[TestFixture]
	[Quick]
	internal class CommonTests : TestFixture
	{
		public CommonTests()
			: base(Assembly.GetExecutingAssembly())
		{
		}

		[Test]
		public void AssertWorks()
		{
			DoAssertWorks();
		}

		[Test]
		public void NoMissingAttributes()
		{
			DoNoMissingAttributes();
		}
	}

	abstract class ControllerTestsBase
	{
		protected TempDirectory _tmpDir;
		protected TestServer _server;
		protected WebContext _context;
		protected WebStartup _startup;

		protected Mock<ILicense> _license;
		protected Mock<IPitDatabase> _pitDatabase;
		protected Mock<IJobMonitor> _jobMonitor;

		protected virtual Mock<ILicense> CreateLicense()
		{
			return new Mock<ILicense>();
		}

		protected virtual Mock<IPitDatabase> CreatePitDatabase()
		{
			return new Mock<IPitDatabase>();
		}

		protected virtual Mock<IJobMonitor> CreateJobMonitor()
		{
			return new Mock<IJobMonitor>();
		}

		[SetUp]
		public virtual void SetUp()
		{
			_tmpDir = new TempDirectory();

			Configuration.LogRoot = _tmpDir.Path;

			_context = new WebContext(Path.Combine(_tmpDir.Path, "pits"));
			_license = CreateLicense();
			_pitDatabase = CreatePitDatabase();
			_jobMonitor = CreateJobMonitor();

			_startup = new WebStartup(
				_license.Object,
				_context,
				_jobMonitor.Object,
				ctx => _pitDatabase.Object
			);

			_server = TestServer.Create(_startup.OnStartup);
		}

		[TearDown]
		public virtual void TearDown()
		{
			_startup.Dispose();
			_server.Dispose();
			_tmpDir.Dispose();
		}
	}
}
