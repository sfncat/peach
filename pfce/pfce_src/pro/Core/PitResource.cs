using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.IO;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Peach.Core;
using Peach.Core.IO;
using Peach.Pro.Core.License;

namespace Peach.Pro.Core
{
	public class PitManifest
	{
		public Dictionary<string, PitManifestFeature> Features { get; set; }
		public Dictionary<string, string[]> Packs { get; set; }

		public string FindFeatureForPit(string name)
		{
			return Features.SingleOrDefault(x => x.Value.Pit == name).Key;
		}

		public IEnumerable<string> FindFeaturesForLegacyPit(string name)
		{
			return from x in Features
				   where x.Value.Legacy == name
				   select x.Key;
		}
	}

	public class PitManifestFeature
	{
		public string Pit { get; set; }
		public byte[] Key { get; set; }
		public string[] Assets { get; set; }
		public string Legacy { get; set; }
	}

	public interface IPitResource
	{
		Stream Load(string path);
	}

	internal class FilePitResource : IPitResource
	{
		public Stream Load(string path)
		{
			return File.OpenRead(path);
		}
	}

	public class ResourceRoot
	{
		public Assembly Assembly { get; set; }
		public string Prefix { get; set; }

		public static ResourceRoot GetDefault(string pitLibraryPath)
		{
			if (pitLibraryPath == null)
				return null;

			var path = Path.Combine(pitLibraryPath, "Peach.Pro.Pits.dll");
			if (!File.Exists(path))
				return null;

			return new ResourceRoot
			{
				Assembly = Assembly.LoadFrom(path),
				Prefix = "",
			};
		}
	}

	public class PitResource : IPitResource
	{
		private readonly string _pitLibraryPath;
		private readonly ResourceRoot _root;
		private readonly PitFeature _pitFeature;

		public PitResource(
			ILicense license,
			string pitLibraryPath,
			string pitPath,
			ResourceRoot root = null)
		{
			if (root == null)
				root = ResourceRoot.GetDefault(pitLibraryPath);

			_pitLibraryPath = pitLibraryPath;
			_root = root;

			if (root != null)
			{
				if (license == null)
					throw new PeachException("A valid license could not be found.");

				_pitFeature = license.CanUsePit(pitPath);
				if (!_pitFeature.IsValid)
				{
					if (_pitFeature.IsCustom)
					{
						throw new PeachException(
							"Your license does not include support for custom pits. " +
							"Contact Peach Fuzzer sales for more information."
						);
					}

					throw new PeachException(
						"The '{0}' pit is not supported with your current license. ".Fmt(_pitFeature.Name) + 
					    "Contact Peach Fuzzer sales for more information."
					);
				}
			}
		}

		public Stream Load(string path)
		{
			path = Path.GetFullPath(path.Replace("##PitLibraryPath##", _pitLibraryPath));
			Stream stream = null;

			// try to load from assembly
			if (_root != null && 
			    _pitFeature != null && 
			    _pitFeature.Key != null && 
			    path.StartsWith(_pitLibraryPath))
			{
				stream = PitResourceLoader.DecryptResource(_root, _pitFeature, GetRelativePath(path));
			}

			// if fail, try to load from disk
			if (stream == null)
			{
				var uri = new Uri(path);
				if (uri.Scheme != Uri.UriSchemeFile)
					throw new PeachException("Invalid uri scheme for <Include>: {0}".Fmt(path));
				stream = File.OpenRead(uri.AbsolutePath);
			}

			return stream;
		}
	
		private string GetRelativePath(string path)
		{
			var len = _pitLibraryPath.Length;
			while (path[len] == Path.DirectorySeparatorChar)
				len++;
			return path.Substring(len);
		}
	}

	public static class PitResourceLoader
	{
		static readonly string ManifestName = "manifest.json";

		internal static JsonSerializer CreateSerializer()
		{
			var settings = new JsonSerializerSettings
			{
				Formatting = Formatting.Indented,
				NullValueHandling = NullValueHandling.Ignore,
			};
			return JsonSerializer.Create(settings);
		}

		public static PitManifest LoadManifest(ResourceRoot root)
		{
			var name = MakeFullName(root.Prefix, ManifestName);
			using (var stream = root.Assembly.GetManifestResourceStream(name))
				return LoadManifest(stream);
		}

		internal static string MakeFullName(string prefix, string name)
		{
			var parts = new List<string>();
			if (!string.IsNullOrEmpty(prefix))
				parts.Add(prefix);
			parts.Add(name);
			return string.Join(".", parts)
				.Replace("/", ".")
				.Replace("\\", ".");
		}

		private static PitManifest LoadManifest(Stream stream)
		{
			using (var reader = new StreamReader(stream))
			using (var json = new JsonTextReader(reader))
			{
				return CreateSerializer().Deserialize<PitManifest>(json);
			}
		}

		public static void SaveManifest(Stream stream, PitManifest manifest)
		{
			using (var writer = new StreamWriter(stream))
			{
				CreateSerializer().Serialize(writer, manifest);
			}
		}

#if DEBUG
		public static PitManifest EncryptResources(
			ResourceRoot root,
			string output,
			string masterSalt)
		{
			var dir = Path.GetDirectoryName(output);
			var asmName = Path.GetFileNameWithoutExtension(output);
			var fileName = Path.GetFileName(output);

			var builder = AppDomain.CurrentDomain.DefineDynamicAssembly(
				new AssemblyName(asmName),
				AssemblyBuilderAccess.Save,
				dir
			);
			var module = builder.DefineDynamicModule(asmName, fileName);

			var master = new PitManifest
			{
				Features = new Dictionary<string, PitManifestFeature>()
			};

			var manifest = LoadManifest(root);
			foreach (var feature in manifest.Features)
			{
				var key = MakeKey(feature.Key, masterSalt);
				master.Features[feature.Key] = new PitManifestFeature
				{
					Key = key
				};

				//Console.WriteLine("{0,-40}: {1}", feature.Key, Convert.ToBase64String(key));

				EncryptFeature(root, feature, module, key);
			}

			var ms = new MemoryStream();
			using (var wrapper = new NonClosingStreamWrapper(ms))
			{
				SaveManifest(wrapper, manifest);
			}
			var manifestName = MakeFullName(root.Prefix, ManifestName);
			module.DefineManifestResource(manifestName, ms, ResourceAttributes.Public);

			builder.Save(fileName);

			return master;
		}

		static void EncryptFeature(
			ResourceRoot root,
			KeyValuePair<string, PitManifestFeature> feature,
			ModuleBuilder module,
			byte[] key)
		{
			foreach (var asset in feature.Value.Assets)
			{
				var rawAssetName = feature.Key + "." + asset;
				var inputResourceName = MakeFullName(root.Prefix, asset);
				var outputResourceName = MakeFullName(root.Prefix, rawAssetName);

				var cipher = MakeCipher(outputResourceName, key, true);

				var output = new MemoryStream();
				using (var input = root.Assembly.GetManifestResourceStream(inputResourceName))
				using (var wrapper = new NonClosingStreamWrapper(output))
				using (var encrypter = new CipherStream(wrapper, null, cipher))
				{
					input.CopyTo(encrypter);
				}

				output.Seek(0, SeekOrigin.Begin);
				module.DefineManifestResource(outputResourceName, output, ResourceAttributes.Public);
			}
		}
#endif

		public static Stream DecryptResource(
			ResourceRoot root,
			PitFeature feature,
			string asset)
		{
			var rawAssetName = feature.Name + "." + asset;
			var resourceName = MakeFullName(root.Prefix, rawAssetName);

			var cipher = MakeCipher(resourceName, feature.Key, false);

			var output = new MemoryStream();
			using (var input = root.Assembly.GetManifestResourceStream(resourceName))
			using (var wrapper = new NonClosingStreamWrapper(input))
			using (var decrypter = new CipherStream(wrapper, cipher, null))
			{
				try
				{
					decrypter.CopyTo(output);
				}
				catch (InvalidCipherTextException ex)
				{
					// MAC check for GCM failed
					NLog.LogManager.GetCurrentClassLogger().Debug(ex);
					return null;
				}
			}

			output.Seek(0, SeekOrigin.Begin);
			return output;
		}

		static byte[] MakeKey(string feature, string salt)
		{
			var saltBytes = System.Text.Encoding.UTF8.GetBytes(salt);
			var password = System.Text.Encoding.UTF8.GetBytes(feature);

			var digest = new Sha256Digest();
			var key = new byte[digest.GetDigestSize()];

			digest.BlockUpdate(saltBytes, 0, saltBytes.Length);
			digest.BlockUpdate(password, 0, password.Length);
			digest.BlockUpdate(saltBytes, 0, saltBytes.Length);
			digest.DoFinal(key, 0);

			return key;
		}

		static byte[] Digest(byte[] input)
		{
			var digest = new Sha256Digest();
			var output = new byte[digest.GetDigestSize()];
			digest.BlockUpdate(input, 0, input.Length);
			digest.DoFinal(output, 0);
			return output;
		}

		static IBufferedCipher MakeCipher(string asset, byte[] key, bool forEncryption)
		{
			var iv = Digest(System.Text.Encoding.UTF8.GetBytes(asset));
			var keyParam = new KeyParameter(key);
			var cipherParams = new AeadParameters(keyParam, 16 * 8, iv);
			var blockCipher = new AesFastEngine();
			var aeadBlockCipher = new GcmBlockCipher(blockCipher);
			var cipher = new BufferedAeadBlockCipher(aeadBlockCipher);
			cipher.Init(forEncryption, cipherParams);
			return cipher;
		}
	}
}
