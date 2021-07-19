using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Core.Dom.XPath;
using Peach.Core.Fixups;
using Peach.Core.IO;
using Peach.Pro.Core;
using Peach.Pro.Core.MutationStrategies;
using Action = Peach.Core.Dom.Action;
using StateModel = Peach.Core.Dom.StateModel;
using Peach.Core.Cracker;

#if DEBUG
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;
#endif

namespace Peach.Pro.PitTester
{
#if DEBUG
	class QuickAttribute : CategoryAttribute { }
	class SlowAttribute : CategoryAttribute { }
#endif
	
	public static class ThePitTester
	{
		public static void OnIterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			if (context.config.singleIteration)
				return;

			if (context.controlRecordingIteration)
				Console.Write('r');
			else if (context.controlIteration)
				Console.Write('c');
			else if ((currentIteration % 10) == 0)
				Console.Write(".");
		}

#if DEBUG
		public static void MakeTestAssembly(
			string pitLibraryPath,
			string pitTestFile,
			string pitAssemblyFile)
		{
			var dir = Path.GetDirectoryName(pitAssemblyFile);
			var asmName = Path.GetFileNameWithoutExtension(pitAssemblyFile);
			var fileName = Path.GetFileName(pitAssemblyFile);

			var builder = AppDomain.CurrentDomain.DefineDynamicAssembly(
				new AssemblyName(asmName),
				AssemblyBuilderAccess.Save,
				dir
			);

			var module = builder.DefineDynamicModule(asmName, fileName);

			MakeTestBase(module);
			MakeTestFixture(module, asmName, pitLibraryPath, pitTestFile);

			builder.Save(fileName);
		}

		static CustomAttributeBuilder MakeCustomAttribute(Type type)
		{
			var ctor = type.GetConstructor(new Type[0]);
			return new CustomAttributeBuilder(ctor, new object[0]);
		}

		static void MakeTestBase(ModuleBuilder module)
		{
			var type = module.DefineType("TestBase");
			type.SetParent(typeof(TestBase));
			type.CreateType();
		}

		static void MakeTestFixture(ModuleBuilder module, string asmName, string pitLibraryPath, string pitTestFile)
		{
			var type = module.DefineType(asmName);
			type.SetCustomAttribute(MakeCustomAttribute(typeof(TestFixtureAttribute)));

			var testAttr = MakeCustomAttribute(typeof(TestAttribute));
			MakeTestPit("TestSingleIteration", type, testAttr, pitLibraryPath, pitTestFile, 1);
			MakeTestPit("TestManyIterations", type, testAttr, pitLibraryPath, pitTestFile, 500);
			MakeTestDatasets(type, testAttr, pitLibraryPath, pitTestFile);

			type.CreateType();
		}

		static void MakeTestPit(
			string name,
			TypeBuilder type,
			CustomAttributeBuilder testAttr,
			string pitLibraryPath,
			string pitTestFile,
			int iterations)
		{
			var method = type.DefineMethod(name, MethodAttributes.Public);
			method.SetCustomAttribute(testAttr);
			if (iterations == 1)
				method.SetCustomAttribute(MakeCustomAttribute(typeof(QuickAttribute)));
			else
				method.SetCustomAttribute(MakeCustomAttribute(typeof(SlowAttribute)));
			
			var testPitMethod = typeof(ThePitTester).GetMethod("TestPit");

			var il = method.GetILGenerator();
			var local = il.DeclareLocal(typeof(uint?));
			il.Emit(OpCodes.Ldstr, pitLibraryPath);
			il.Emit(OpCodes.Ldstr, pitTestFile);
			il.Emit(OpCodes.Ldloca, local);
			il.Emit(OpCodes.Initobj, typeof(uint?));
			il.Emit(OpCodes.Ldloc_0);
			il.Emit(OpCodes.Ldc_I4_1); // true
			il.Emit(OpCodes.Ldc_I4, iterations);
			il.Emit(OpCodes.Call, testPitMethod);
			il.Emit(OpCodes.Ret);
		}

		static void MakeTestDatasets(
			TypeBuilder type,
			CustomAttributeBuilder testAttr,
			string pitLibraryPath,
			string pitTestFile)
		{
			var method = type.DefineMethod("TestDatasets", MethodAttributes.Public);
			method.SetCustomAttribute(testAttr);
			method.SetCustomAttribute(MakeCustomAttribute(typeof(SlowAttribute)));

			var testPitMethod = typeof(ThePitTester).GetMethod("VerifyDataSets");

			var il = method.GetILGenerator();
			il.Emit(OpCodes.Ldstr, pitLibraryPath);
			il.Emit(OpCodes.Ldstr, pitTestFile);
			il.Emit(OpCodes.Call, testPitMethod);
			il.Emit(OpCodes.Ret);
		}
#endif

		public static void Ignore(string fmt, params string[] args)
		{
#if DEBUG
			Assert.Ignore(fmt, args);
#else
			Console.WriteLine(fmt, args);
#endif
		}

		public static void TestPit(
			string libraryPath,
			string testPath,
			uint? seed,
			bool keepGoing,
			uint stop)
		{
			if (!File.Exists(testPath))
				throw new FileNotFoundException("Invalid PitTestPath", testPath);

			var testData = TestData.Parse(testPath);
			if (testData.Tests.Any(x => x.Skip))
				Ignore("Skipping test: {0}", testPath);

			var cleanme = new List<IDisposable>();
			var pitFile = Path.Combine(libraryPath, testData.Pit);
			Console.WriteLine("PitFile: {0}", pitFile);
			for (var i = 0; i < testData.Tests.Count; ++i)
			{
				if (testData.Tests[i].Skip)
					continue;

				try
				{
					foreach (var tmp in testData.Defines.OfType<TestData.TempFileDefine>())
					{
						cleanme.Add(tmp);
						tmp.Populate();
					}

					DoTestPit(
						testData,
						libraryPath,
						pitFile,
						seed,
						keepGoing,
						stop,
						i
					);
				}
				finally
				{
					foreach (var item in cleanme)
						item.Dispose();
				}
			}
		}

		private static void DoTestPit(
			TestData testData,
			string libraryPath,
			string pitFile,
			uint? seed,
			bool keepGoing,
			uint stop,
			int testIndex)
		{
			if (testData.Tests[testIndex].SingleIteration)
				stop = 1;

			var defs = PitDefines.ParseFile(pitFile + ".config", libraryPath).Evaluate();

			// separate Defines for each test is not supported at this time
			var testDefs = testData.Defines.ToDictionary(x => x.Key, x => x.Value);

			for (var i = 0; i < defs.Count; ++i)
			{
				string value;
				if (testDefs.TryGetValue(defs[i].Key, out value))
				{
					if (value == defs[i].Value)
					{
						Console.WriteLine("Warning, .test and .config value are identical for PitDefine named: \"{0}\"",
							defs[i].Key
						);
					}

					defs[i] = new KeyValuePair<string, string>(defs[i].Key, value);
					testDefs.Remove(defs[i].Key);
				}
			}

			if (testDefs.Count > 0)
			{
				throw new PeachException("Error, PitDefine(s) in .test not found in .config: {0}".Fmt(
					string.Join(", ", testDefs.Keys))
				);
			}

			var args = new Dictionary<string, object>();
			args[PitParser.DEFINED_VALUES] = defs;

			var parser = new ProPitParser(null, libraryPath, pitFile);
			var dom = parser.asParser(args, pitFile);

			var errors = new List<Exception>();
			var fixupOverrides = new Dictionary<string, Variant>();

			bool foundMatchingPitTest = false;
			foreach (var test in dom.tests)
			{
				// Don't run extra control iterations...
				test.controlIteration = 0;

				test.agents.Clear();

				TestData.Test data = null;
				if (testData.Tests[testIndex].Name != test.Name)
				{
					continue;
				}
				else
				{
					foundMatchingPitTest = true;
					data = testData.Tests[testIndex];
				}

				if (data == null)
					throw new PeachException("Error, no test definition found for pit test named '{0}'.".Fmt(test.Name));

				var logger = new TestLogger(data, testData.Ignores.Select(i => i.Xpath));
				logger.Error += err =>
				{
					var ex = new PeachException(err);
					if (!keepGoing)
						throw ex;
					errors.Add(ex);
				};

				test.loggers.Clear();
				test.loggers.Add(logger);

				for (var i = 0; i < test.publishers.Count; ++i)
				{
					var oldPub = test.publishers[i];
					var newPub = new TestPublisher(logger, stop == 1) { Name = oldPub.Name };
					newPub.Error += err =>
					{
						var ex = new PeachException(err);
						if (!keepGoing)
							throw ex;
						errors.Add(ex);
					};
					test.publishers[i] = newPub;
				}

				if (testData.Slurps.Count > 0)
				{
					ApplySlurps(testData, test.stateModel, fixupOverrides);
				}
			}

			if(!foundMatchingPitTest)
				throw new PeachException("Error, no test definition found for pit test named '{0}'.".Fmt(testData.Tests[testIndex].Name));

			// See #214
			// If there are is any action that has more than one data set
			// that use <Field> and the random strategy is
			// in use, turn off data set switching...
			var noSwitch = dom.tests
				.Select(t => t.stateModel)
				.SelectMany(sm => sm.states)
				.SelectMany(s => s.actions)
				.SelectMany(a => a.allData)
				.Select(ad => ad.dataSets)
				.Any(ds => ds.SelectMany(d => d).OfType<DataField>().Count() > 1);

			if (noSwitch)
			{
				foreach (var t in dom.tests.Where(t => t.strategy is RandomStrategy))
				{
					t.strategy = new RandomStrategy(new Dictionary<string, Variant> {
						{ "SwitchCount", new Variant(int.MaxValue.ToString(CultureInfo.InvariantCulture)) },
					});
				}
			}

			var config = new RunConfiguration
			{
				range = true,
				rangeStart = 0,
				rangeStop = stop,
				pitFile = Path.GetFileName(pitFile),
				runName = testData.Tests[testIndex].Name,
				singleIteration = (stop == 1)
			};

			if (seed.HasValue)
				config.randomSeed = seed.Value;

			var q = testData.Tests[testIndex];
			if (!string.IsNullOrEmpty(q.Seed))
			{
				uint s;
				if (!uint.TryParse(q.Seed, out s))
					throw new PeachException("Error, could not parse test seed '{0}' as an unsigned integer.".Fmt(q.Seed));

				config.randomSeed = s;
			}


			uint num = 0;
			var e = new Engine(null);
			e.IterationStarting += (ctx, it, tot) => num = it;

			e.TestStarting += ctx =>
			{
				if (testData.Slurps.Count > 0)
				{
					ctx.StateModelStarting += (context, model) =>
					{
						ApplySlurps(testData, model, fixupOverrides);

						foreach (var kv in fixupOverrides)
							ctx.stateStore[kv.Key] = kv.Value;
					};
				}
			};

			e.IterationStarting += (ctx, it, tot) => num = it;

			try
			{
				e.startFuzzing(dom, config);
			}
			catch (Exception ex)
			{
				var msg = "Encountered an unhandled exception on iteration {0}, seed {1}.\n{2}".Fmt(
							  num,
							  config.randomSeed,
							  ex.Message);
				errors.Add(new PeachException(msg, ex));
			}

			if (errors.Any())
				throw new AggregateException(errors);
		}

		private static void ApplySlurps(TestData testData, StateModel sm, Dictionary<string, Variant> fixupOverrides)
		{
			var doc = new XmlDocument();
			var resolver = new PeachXmlNamespaceResolver();
			var navi = new PeachXPathNavigator(sm);

			foreach (var slurp in testData.Slurps)
			{
				var iter = navi.Select(slurp.SetXpath, resolver);
				if (!iter.MoveNext())
					throw new SoftException("Error, slurp valueXpath returned no values. [" + slurp.SetXpath + "]");

				var n = doc.CreateElement("Foo");
				n.SetAttribute("valueType", slurp.ValueType);
				n.SetAttribute("value", slurp.Value);

				var blob = new Blob();
				new PitParser().handleCommonDataElementValue(n, blob);

				do
				{
					var setElement = ((PeachXPathNavigator)iter.Current).CurrentNode as DataElement;
					if (setElement == null)
						throw new PeachException("Error, slurp setXpath did not return a Data Element. [" + slurp.SetXpath + "]");

					setElement.DefaultValue = blob.DefaultValue;

					if (fixupOverrides != null && setElement.fixup is VolatileFixup)
					{
						var dm = setElement.root as DataModel;
						if (dm != null && dm.actionData != null)
						{
							// If the element is under an action, and has a volatile fixup
							// store off the value for overriding during TestStarting
							var key = "Peach.VolatileOverride.{0}.{1}".Fmt(dm.actionData.outputName, setElement.fullName);
							fixupOverrides[key] = blob.DefaultValue;
						}
					}

					if (blob.DefaultValue.GetVariantType() == Variant.VariantType.BitStream)
						((BitwiseStream)blob.DefaultValue).Position = 0;
				} while (iter.MoveNext());
			}
		}

		public static void ProfilePit(string pitLibraryPath, string fileName)
		{
			var testData = TestData.Parse(fileName);
			if (testData.Tests.Any(x => x.Skip))
				Ignore("Skipping test: {0}", fileName);

			var pitFile = Path.Combine(pitLibraryPath, testData.Pit);
			Console.WriteLine("PitFile: {0}", pitFile);

			var defs = PitDefines.ParseFile(pitFile + ".config", pitLibraryPath).Evaluate();

			var args = new Dictionary<string, object>();
			args[PitParser.DEFINED_VALUES] = defs;

			var parser = new PitParser();

			var dom = parser.asParser(args, pitFile);

			dom.context = new RunContext();

			foreach (var test in dom.tests)
			{
				dom.context.test = test;

				foreach (var state in test.stateModel.states)
				{
					foreach (var action in state.actions)
					{
						foreach (var actionData in action.allData)
						{
							foreach (var data in actionData.allData.OfType<DataFile>())
							{
								var dm = actionData.dataModel;

								for (var i = 0; i < 1000; ++i)
								{
									var clone = (DataModel)dm.Clone();
									clone.actionData = actionData;
									data.Apply(action, clone);
								}

								return;
							}
						}
					}
				}
			}
		}

		public static void VerifyDataSets(string pitLibraryPath, string pitTestPath)
		{
			if (!File.Exists(pitTestPath))
				throw new FileNotFoundException("Invalid PitTestPath", pitTestPath);
			var testData = TestData.Parse(pitTestPath);

			var cleanme = new List<IDisposable>();
			var pitFile = Path.Combine(pitLibraryPath, testData.Pit);

			try
			{
				foreach (var tmp in testData.Defines.OfType<TestData.TempFileDefine>())
				{
					cleanme.Add(tmp);
					tmp.Populate();
				}

				DoVerifyDataSets(testData, pitLibraryPath, pitFile);
			}
			finally
			{
				foreach (var item in cleanme)
					item.Dispose();
			}
		}

		private static void DoVerifyDataSets(
			TestData testData,
			string pitLibraryPath,
			string fileName)
		{
			var defs = PitDefines.ParseFileWithDefaults(pitLibraryPath, fileName);

			var testDefs = testData.Defines.ToDictionary(x => x.Key, x => x.Value);

			for (var i = 0; i < defs.Count; ++i)
			{
				string value;
				if (testDefs.TryGetValue(defs[i].Key, out value))
					defs[i] = new KeyValuePair<string, string>(defs[i].Key, value);
			}

			var args = new Dictionary<string, object>();
			args[PitParser.DEFINED_VALUES] = defs;

			var parser = new ProPitParser(null, pitLibraryPath, fileName);
			var dom = parser.asParser(args, fileName);

			dom.context = new RunContext();

			var sb = new StringBuilder();
			foreach (var pitTest in testData.Tests)
			{
				foreach (var test in dom.tests)
				{
					dom.context.test = test;
					TestData.Test testTest = null;
					if (pitTest.Name == test.Name)
					{
						testTest = pitTest;
					}

					foreach (var state in test.stateModel.states)
					{
						foreach (var action in state.actions)
						{
							foreach (var actionData in action.allData)
							{
								foreach (var data in actionData.allData)
								{
									var verify = testTest == null || testTest.VerifyDataSets;
									VerifyDataSet(verify, data, actionData, test, state, action, sb);
								}
							}
						}
					}
				}
			}

			if (sb.Length > 0)
				throw new PeachException(sb.ToString());
		}

		private static void VerifyDataSet(
			bool verifyBytes,
			Data data,
			ActionData actionData,
			Test test,
			State state,
			Action action,
			StringBuilder sb)
		{
			try
			{
				if (data is DataFile)
				{
					// Verify file cracks correctly
					try
					{
						actionData.Apply(data);
					}
					catch (Exception ex)
					{
						throw new PeachException(string.Format("Error cracking data file '{0}' to '{1}.{2}.{3}.{4}'.",
							((DataFile)data).FileName, test.Name, state.Name, action.Name, actionData.dataModel.Name), ex);
					}

					// SHould we skip verifying bytes?
					if (!verifyBytes)
						return;

					var bs = actionData.dataModel.Value;
					var value = new MemoryStream();
					bs.Seek(0, SeekOrigin.Begin);
					bs.CopyTo(value);
					value.Seek(0, SeekOrigin.Begin);

					var dataFileBytes = File.ReadAllBytes(((DataFile)data).FileName);

					// Verify all bytes match
					for (var i = 0; i < dataFileBytes.Length && i < value.Length; i++)
					{
						var b = value.ReadByte();
						if (dataFileBytes[i] != b)
						{
							throw new PeachException(
								string.Format(
									"Error: Data did not match at {0}.  Got {1:x2} expected {2:x2}. Data file '{3}' to '{4}.{5}.{6}.{7}'.",
									i, b, dataFileBytes[i], ((DataFile)data).FileName, test.Name, state.Name, action.Name,
									actionData.dataModel.Name));
						}
					}

					// Verify length matches
					if (dataFileBytes.Length != value.Length)
						throw new PeachException(
							string.Format(
								"Error: Data size mismatch. Got {0} bytes, expected {1}. Data file '{2}' to '{3}.{4}.{5}.{6}'.",
								value.Length, dataFileBytes.Length, ((DataFile)data).FileName, test.Name, state.Name, action.Name,
								actionData.dataModel.Name));
				}
				else if (data is DataField)
				{
					// Verify fields apply correctly
					try
					{
						actionData.Apply(data);
					}
					catch (Exception ex)
					{
						throw new PeachException(string.Format("Error applying data fields '{0}' to '{1}.{2}.{3}.{4}'.\n{5}",
							data.Name, test.Name, state.Name, action.Name, actionData.dataModel.Name, ex.Message), ex);
					}
				}
			}
			catch (Exception ಠ_ಠ)
			{
				sb.AppendLine(ಠ_ಠ.Message);
			}
		}

		public static void Crack(string pitLibraryPath, string pitPath, string dataModelName, string samplePath)
		{
			var pitc = new PitCompiler(pitLibraryPath, pitPath);

			Console.WriteLine("Parsing: '{0}'", pitPath);
			var dom = pitc.Parse(false, false);

			Console.WriteLine("Looking for data model: '{0}'", dataModelName);
			var dm = dom.getRef(dataModelName, x => x.dataModels);
			if (dm == null)
				PitCompiler.RaiseMissingDataModel(dom, dataModelName, pitPath);

			using (var sin = new FileStream(samplePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				var cracker = new DataCracker();
				cracker.CrackData(dm, new BitStream(sin));
				foreach (var item in dm.PreOrderTraverse())
				{
					var sep = '-';
					if (item is DataElementContainer)
						sep = '+';

					var depth = item.fullName.Count(x => x == '.');
					var prefix = string.Concat(Enumerable.Repeat(" |", depth));

					var valuePart = "";
					if (item.DefaultValue != null)
					{
						var vt = item.DefaultValue.GetVariantType();

						string value;
						if (vt == Variant.VariantType.Int || vt == Variant.VariantType.Long)
							value = "{0} (0x{1:X})".Fmt(item.DefaultValue, (long)item.DefaultValue);
						else if (vt == Variant.VariantType.ULong)
							value = "{0} (0x{1:X})".Fmt(item.DefaultValue, (ulong)item.DefaultValue);
						else
							value = item.DefaultValue.ToString();

						if (!string.IsNullOrEmpty(value))
						{
							valuePart = "[{0}]".Fmt(value
								.Replace("\t", "\\t")
								.Replace("\n", "\\n")
								.Replace("\r", "\\r")
							);
						}
					}

					Console.WriteLine("{0}-{1} {2} '{3}' {4}", prefix, sep, item.elementType, item.Name, valuePart);
				}
			}
		}
	}
}
