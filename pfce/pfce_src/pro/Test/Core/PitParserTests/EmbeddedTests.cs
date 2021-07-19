using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Moq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Test;
using Peach.Pro.Core;
using Peach.Pro.Core.License;

namespace Peach.Pro.Test.Core.PitParserTests
{
	[TestFixture]
	[Quick]
	[Peach]
	public class EmbeddedTests
	{
		static readonly ResourceRoot ResourceRoot = new ResourceRoot
		{
			Assembly = Assembly.GetExecutingAssembly(),
			Prefix = "Peach.Pro.Test.Core.Resources.Pits"
		};
		const string MasterSalt = "salt";

		TempDirectory _tmpDir;

		[SetUp]
		public void SetUp()
		{
			_tmpDir = new TempDirectory();
		}

		[TearDown]
		public void TearDown()
		{
			_tmpDir.Dispose();
		}

#if DEBUG
		[Test]
		public void TestUnlicensedPit()
		{
			var featureName = "PeachPit-Net-DNP3_Slave";
			var pitFile = Path.Combine(_tmpDir.Path, "Net", "DNP3_Slave.xml");

			ExtractDirectory(_tmpDir.Path, "Net");
			ExtractFile(_tmpDir.Path, "DNP3.py", "_Common", "Models", "Net");
			ExtractDirectory(_tmpDir.Path, "_Common", "Samples", "Net", "DNP3");

			var encrypted = Path.Combine(_tmpDir.Path, "Peach.Pro.Pits.dll");

			PitResourceLoader.EncryptResources(ResourceRoot, encrypted, MasterSalt);

			var license = new Mock<ILicense>();
			license.Setup(x => x.CanUsePit(pitFile))
				   .Returns(() => new PitFeature
				   {
					   Name = featureName,
					   Path = pitFile,
				   });

			var encryptedRoot = new ResourceRoot
			{
				Assembly = LoadAssembly(encrypted),
				Prefix = ResourceRoot.Prefix
			};

			Assert.That(() =>
			{
				new ProPitParser(
					license.Object,
					_tmpDir.Path,
					pitFile,
					encryptedRoot
				);
			},
				Throws.TypeOf<PeachException>().With.Message.Contains(
					"The 'PeachPit-Net-DNP3_Slave' pit is not supported with your current license."
				)
			);
		}

		[Test]
		public void TestLoadFromAssembly()
		{
			var featureName = "PeachPit-Net-DNP3_Slave";
			var pitFile = Path.Combine(_tmpDir.Path, "Net", "DNP3_Slave.xml");
			var pitConfigFile = pitFile + ".config";

			ExtractDirectory(_tmpDir.Path, "Net");
			ExtractFile(_tmpDir.Path, "DNP3.py", "_Common", "Models", "Net");
			ExtractDirectory(_tmpDir.Path, "_Common", "Samples", "Net", "DNP3");

			var encrypted = Path.Combine(_tmpDir.Path, "Peach.Pro.Pits.dll");

			var master = PitResourceLoader.EncryptResources(ResourceRoot, encrypted, MasterSalt);

			var license = new Mock<ILicense>();
			license.Setup(x => x.CanUsePit(pitFile))
				   .Returns(new PitFeature
				   {
					   Path = pitFile,
					   Name = featureName,
					   IsValid = true,
					   Key = master.Features[featureName].Key
				   });

			var encryptedRoot = new ResourceRoot
			{
				Assembly = LoadAssembly(encrypted),
				Prefix = ResourceRoot.Prefix
			};

			var defs = PitDefines.ParseFile(pitConfigFile, _tmpDir.Path, new Dictionary<string, string> {
				{"Source", "0"},
				{"Destination", "0"},
			});

			var args = new Dictionary<string, object> {
				{ PitParser.DEFINED_VALUES, defs.Evaluate() }
			};

			var parser = new ProPitParser(
				license.Object,
				_tmpDir.Path,
				pitFile,
				encryptedRoot
			);
			var dom = parser.asParser(args, pitFile);
			var config = new RunConfiguration() { singleIteration = true, };
			var e = new Engine(null);
			e.startFuzzing(dom, config);
		}
#endif

		[Test]
		public void TestLoadFromDisk()
		{
			var license = new Mock<ILicense>();

			ExtractDirectory(_tmpDir.Path, "Net");
			ExtractDirectory(_tmpDir.Path, "_Common", "Models", "Net");
			ExtractDirectory(_tmpDir.Path, "_Common", "Samples", "Net", "DNP3");

			var pitFile = Path.Combine(_tmpDir.Path, "Net", "DNP3_Slave.xml");
			var pitConfigFile = pitFile + ".config";

			var defs = PitDefines.ParseFile(pitConfigFile, _tmpDir.Path, new Dictionary<string, string> {
				{"Source", "0"},
				{"Destination", "0"},
			});

			var args = new Dictionary<string, object> {
				{ PitParser.DEFINED_VALUES, defs.Evaluate() }
			};

			var parser = new ProPitParser(license.Object, _tmpDir.Path, pitFile);
			var dom = parser.asParser(args, pitFile);
			var config = new RunConfiguration() { singleIteration = true, };
			var e = new Engine(null);
			e.startFuzzing(dom, config);
		}

		[Test]
		public void ParseManifest()
		{
			var manifest = PitResourceLoader.LoadManifest(ResourceRoot);
			CollectionAssert.Contains(manifest.Features.Keys, "PeachPit-Net-DNP3_Slave");
		}

#if DEBUG
		[Test]
		public void TestProtectResources()
		{
			var featureName = "PeachPit-Net-DNP3_Slave";
			var otherFeatureName = "PeachPit-Net-DNP3_Master";
			var asset1 = "_Common.Models.Net.DNP3_State.xml";
			var asset2 = "_Common.Models.Net.DNP3_Data.xml";
			var expected = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
			var encrypted = Path.Combine(_tmpDir.Path, "TestProtectResources.dll");

			var master = PitResourceLoader.EncryptResources(ResourceRoot, encrypted, MasterSalt);

			var root = new ResourceRoot
			{
				Assembly = LoadAssembly(encrypted),
				Prefix = ResourceRoot.Prefix
			};

			{
				var actual = GetFirstLine(root.Assembly, featureName, asset1);
				Assert.AreNotEqual(expected, actual);
			}

			{
				var actual = GetFirstLine(root.Assembly, featureName, asset2);
				Assert.AreNotEqual(expected, actual);
			}

			var feature = new PitFeature
			{
				Name = featureName,
				Path = featureName,
				IsValid = true,
				Key = master.Features[featureName].Key
			};
			using (var stream = PitResourceLoader.DecryptResource(root, feature, asset1))
			using (var reader = new StreamReader(stream))
			{
				var actual = reader.ReadLine();
				Assert.AreEqual(expected, actual);
			}

			using (var stream = PitResourceLoader.DecryptResource(root, feature, asset2))
			using (var reader = new StreamReader(stream))
			{
				var actual = reader.ReadLine();
				Assert.AreEqual(expected, actual);
			}

			// other features use different passwords
			// this should fail since we are using the wrong password
			var otherFeature = new PitFeature
			{
				Name = otherFeatureName,
				Path = otherFeatureName,
				IsValid = true,
				Key = master.Features[featureName].Key
			};
			using (var stream = PitResourceLoader.DecryptResource(root, otherFeature, asset1))
			{
				Assert.IsNull(stream);
			}
		}

		private string GetFirstLine(Assembly asm, string featureName, string asset)
		{
			var rawAssetName = featureName + "." + asset;

			var name = PitResourceLoader.MakeFullName(ResourceRoot.Prefix, rawAssetName);
			using (var stream = asm.GetManifestResourceStream(name))
			using (var reader = new StreamReader(stream))
			{
				return reader.ReadLine();
			}
		}

		private static Assembly LoadAssembly(string encrypted)
		{
			using (var ms = new MemoryStream())
			using (var fs = File.OpenRead(encrypted))
			{
				fs.CopyTo(ms);
				return Assembly.Load(ms.ToArray());
			}
		}
#endif

		private static void ExtractDirectory(string targetDir, params string[] parts)
		{
			var sep = new string(new[] { Path.DirectorySeparatorChar });
			var dir = string.Join(sep, new[] { targetDir }.Concat(parts));
			Directory.CreateDirectory(dir);

			var prefix = string.Join(".",
				new[] { ResourceRoot.Prefix }
				.Concat(parts)
			);
			foreach (var name in ResourceRoot.Assembly.GetManifestResourceNames())
			{
				if (name.StartsWith(prefix))
				{
					var fileName = name.Substring(prefix.Length + 1); // exclude last '.'
					var target = Path.Combine(dir, fileName);
					Utilities.ExtractEmbeddedResource(ResourceRoot.Assembly, name, target);
				}
			}
		}

		private static void ExtractFile(string targetDir, string filename, params string[] dirs)
		{
			var sep = new string(new[] { Path.DirectorySeparatorChar });
			var dir = string.Join(sep, new[] { targetDir }.Concat(dirs));
			Directory.CreateDirectory(dir);

			var name = string.Join(".",
				new[] { ResourceRoot.Prefix }
				.Concat(dirs)
				.Concat(new[] { filename })
			);
			var target = Path.Combine(dir, filename);
			Utilities.ExtractEmbeddedResource(ResourceRoot.Assembly, name, target);
		}
	}
}
