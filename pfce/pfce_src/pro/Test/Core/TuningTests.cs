using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Moq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core.License;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
	class TuningTests
	{
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

		public Job RunTest(
			string xml,
			PitConfig pitConfig, 
			JobRequest jobRequest, 
			Action<Engine> hooker = null)
		{
			pitConfig.OriginalPit = "Test.xml";

			var pitXmlPath = Path.Combine(_tmpDir.Path, pitConfig.OriginalPit);
			File.WriteAllText(pitXmlPath, xml);

			var pitPath = Path.Combine(_tmpDir.Path, "Test.peach");
			PitDatabase.SavePitConfig(pitPath, pitConfig);

			var job = new Job(jobRequest, pitPath);
			var license = new Mock<ILicense>();
			var evtReady = new AutoResetEvent(false);

			var runner = new JobRunner(license.Object, job, _tmpDir.Path, pitPath);
			runner.Run(evtReady, hooker);

			using (var db = new NodeDatabase())
			{
				return db.GetJob(job.Guid);
			}
		}

		[Test]
		public void TestWeights()
		{
			const string xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action name='output1' type='output'>
				<DataModel name='DM'>
					<String name='off' />
					<String name='lowest' />
					<String name='low' />
					<String name='normal' />
					<String name='high' />
					<String name='highest' />
				</DataModel>
			</Action>
			<Action name='output2' type='output'>
				<DataModel name='DM'>
					<Block name='array' occurs='1'>
						<String name='off' />
						<String name='lowest' />
						<String name='low' />
						<String name='normal' />
						<String name='high' />
						<String name='highest' />
					</Block>
				</DataModel>
			</Action>
		</State>
	</StateModel>

	<Test name='Default' maxOutputSize='100'>
		<StateModel ref='StateModel' />
		<Publisher class='Null'/>
		<Strategy class='Random'>
			<Param name='MaxFieldsToMutate' value='1' />
		</Strategy>
	</Test>
</Peach>
";

			var pit = new PitConfig
			{
				Config = new List<Param>(),
				Agents = new List<Pro.Core.WebServices.Models.Agent>(),
				Weights = new List<PitWeight> {
					new PitWeight { Id = "initial.output1.DM.off", Weight = 0 },
					new PitWeight { Id = "initial.output1.DM.lowest", Weight = 1 },
					new PitWeight { Id = "initial.output1.DM.low", Weight = 2 },
					new PitWeight { Id = "initial.output1.DM.normal", Weight = 3 },
					new PitWeight { Id = "initial.output1.DM.high", Weight = 4 },
					new PitWeight { Id = "initial.output1.DM.highest", Weight = 5 },
					new PitWeight { Id = "initial.output2.DM.array.off", Weight = 0 },
					new PitWeight { Id = "initial.output2.DM.array.lowest", Weight = 1 },
					new PitWeight { Id = "initial.output2.DM.array.low", Weight = 2 },
					new PitWeight { Id = "initial.output2.DM.array.normal", Weight = 3 },
					new PitWeight { Id = "initial.output2.DM.array.high", Weight = 4 },
					new PitWeight { Id = "initial.output2.DM.array.highest", Weight = 5 },
				}
			};

			var count = new Dictionary<string, int>();

			Action<Engine> hooker = engine =>
			{
				engine.TestStarting += ctx =>
				{
					ctx.DataMutating += (c, actionData, element, mutator) =>
					{
						int cnt;
						if (!count.TryGetValue(element.Name, out cnt))
							cnt = 0;
						else
							++cnt;

						count[element.Name] = cnt;
					};
				};
			};

			var jobRequest = new JobRequest
			{
				Seed = 0,
				RangeStop = 100,
			};
			RunTest(xml, pit, jobRequest, hooker);

			foreach (var x in count)
				Console.WriteLine("Elem: {0}, Count: {1}", x.Key, x.Value);

			Assert.Less(count["lowest"], count["low"]);
			Assert.Less(count["low"], count["normal"]);
			Assert.Less(count["normal"], count["high"]);
			Assert.Less(count["high"], count["highest"]);

			Assert.False(count.ContainsKey("off"), "off shouldn't be mutated");
		}

		[Test]
		public void TestExcludeFrags()
		{
			const string xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<StateModel name='SM' initialState='initial'>
		<State name='initial'>
			<Action name='outfrag' type='outfrag'>
				<DataModel name='DM'>
					<Frag name='frag' fragLength='5'>
						<Block name='Template'>
							<String name='prefix' value='FRAG:'/>
							<Blob name='FragData' value='Foo'/>
							<String name='EOL' value='\\n'/>
						</Block>
						<String name='Payload' value='1234567890'/>
					</Frag>
				</DataModel>
			</Action>
		</State>
	</StateModel>
	<Test name='Default' maxOutputSize='200'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
		<Strategy class='Random'>
			<Param name='MaxFieldsToMutate' value='1' />
		</Strategy>
	</Test>
</Peach>
";

			var pit = new PitConfig
			{
				Config = new List<Param>(),
				Agents = new List<Pro.Core.WebServices.Models.Agent>(),
				Weights = new List<PitWeight> {
					new PitWeight { Id = "initial.outfrag.DM.frag", Weight = 0 },
					new PitWeight { Id = "initial.outfrag.DM.frag.prefix", Weight = 0 },
					new PitWeight { Id = "initial.outfrag.DM.frag.FragData", Weight = 0 },
					new PitWeight { Id = "initial.outfrag.DM.frag.EOL", Weight = 0 },
				}
			};

			var count = new Dictionary<string, int>();
			var forDisplay = new List<string>();
			var forMutation = new List<string>();

			Action<Engine> hooker = engine =>
			{
				engine.TestStarting += ctx =>
				{
					ctx.DataMutating += (c, actionData, element, mutator) =>
					{
						var name = element.fullName;
						int cnt;
						if (count.TryGetValue(name, out cnt))
							++cnt;

						count[name] = cnt;
					};

					forMutation = ctx.test.stateModel
						.TuningTraverse()
						.Select(x => "{0} -> {1}".Fmt(x.Value.fullName, x.Key))
						.ToList();

					forDisplay = ctx.test.stateModel
						.TuningTraverse(true)
						.Select(x => "{0} -> {1}".Fmt(x.Value.fullName, x.Key))
						.ToList();
				};
			};

			var jobRequest = new JobRequest
			{
				Seed = 0,
				RangeStop = 100,
			};
			var job = RunTest(xml, pit, jobRequest, hooker);

			Assert.AreEqual(JobStatus.Stopped, job.Status);
			Assert.IsNull(job.Result);

			Console.WriteLine("forDisplay:");
			Console.WriteLine(string.Join("\n", forDisplay));
			Console.WriteLine();
			Console.WriteLine("forMutation:");
			Console.WriteLine(string.Join("\n", forMutation));
			Console.WriteLine();
			foreach (var x in count)
				Console.WriteLine("Elem: {0}, Count: {1}", x.Key, x.Value);

			var expected = new[]
			{
				"DM -> initial.outfrag.DM",
				"DM.frag -> initial.outfrag.DM.frag",
				"DM.frag.Rendering -> initial.outfrag.DM.frag",
				"DM.frag.Template -> initial.outfrag.DM.frag",
				"DM.frag.Template.prefix -> initial.outfrag.DM.frag.prefix",
				"DM.frag.Template.FragData -> initial.outfrag.DM.frag.FragData",
				"DM.frag.Template.EOL -> initial.outfrag.DM.frag.EOL",
				"DM.frag.Payload -> initial.outfrag.DM.frag",
			};
			CollectionAssert.AreEqual(expected, forDisplay);
			CollectionAssert.AreEqual(expected, forMutation);

			Assert.IsEmpty(count);
		}


		[Test]
		public void TestNullElements()
		{
			const string xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<StateModel name='SM' initialState='initial'>
		<State name='initial'>
			<Action name='output' type='output'>
				<DataModel name='Request'>
					<Choice name='Strategy'>
						<Blob name='A' fieldId='A' value='aaaa'/>
						<Blob name='B' fieldId='B' value='bbbb'/>
					</Choice>
				</DataModel>
			</Action>
		</State>
	</StateModel>
	<Test name='Default' maxOutputSize='200'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
		<Strategy class='Random'>
			<Param name='MaxFieldsToMutate' value='1' />
		</Strategy>
	</Test>
</Peach>
";

			var pit = new PitConfig
			{
				Config = new List<Param>(),
				Agents = new List<Pro.Core.WebServices.Models.Agent>(),
				Weights = new List<PitWeight>
				{
					new PitWeight {Id = "A", Weight = 0},
					new PitWeight {Id = "B", Weight = 0},
				}
			};

			var count = new Dictionary<string, int>();

			Action<Engine> hooker = engine =>
			{
				engine.TestStarting += ctx =>
				{
					ctx.DataMutating += (c, actionData, element, mutator) =>
					{
						var name = element.fullName;
						int cnt;
						if (count.TryGetValue(name, out cnt))
							++cnt;
						count[name] = cnt;
					};
				};
			};

			var jobRequest = new JobRequest
			{
				Seed = 0,
				RangeStop = 100,
			};
			RunTest(xml, pit, jobRequest, hooker);
			var expected = new[] {
				"Request.Strategy|99"
			};
			CollectionAssert.AreEqual(expected, count.Select(x => "{0}|{1}".Fmt(x.Key, x.Value.ToString())));
		}


		[Test]
		public void TestStateLoop()
		{
			var loopCount = 5;
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<StateModel name='SM' initialState='initial'>
		<State name='initial' onStart='context.iterationStateStore[""countdown""] = {0}'>
			<Action type='changeState' ref='loop'/>
		</State>

		<State name='loop' onStart='context.iterationStateStore[""countdown""] -= 1'>
			<Action name='msg' type='output'>
				<DataModel name='msg' fieldId='msg'>
					<Number name='type' fieldId='type' size='8'/>
					<Number name='value' fieldId='value' size='32'/>
				</DataModel>
			</Action>

			<Action type='changeState' ref='loop' when='context.iterationStateStore[""countdown""] &gt; 0'/>
		</State>
	</StateModel>
	<Test name='Default' maxOutputSize='200'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
		<Strategy class='Random'>
			<Param name='MaxFieldsToMutate' value='1' />
		</Strategy>
	</Test>
</Peach>
".Fmt(loopCount);

			var pit = new PitConfig
			{
				Config = new List<Param>(),
				Agents = new List<Pro.Core.WebServices.Models.Agent>(),
				Weights = new List<PitWeight>
				{
					new PitWeight {Id = "msg.type", Weight = 0},
					new PitWeight {Id = "msg.value", Weight = 5},
				}
			};

			var count = new Dictionary<string, int>();

			Action<Engine> hooker = engine =>
			{
				engine.TestStarting += ctx =>
				{
					ctx.DataMutating += (c, actionData, element, mutator) =>
					{
						var name = element.fullName;
						int cnt;
						if (count.TryGetValue(name, out cnt))
							++cnt;
						count[name] = cnt;
					};
				};
			};

			var jobRequest = new JobRequest
			{
				Seed = 0,
				RangeStop = 100,
			};
			RunTest(xml, pit, jobRequest, hooker);
			var expected = new[] {
				"msg.value|99"
			};
			var actual = count.Select(x => "{0}|{1}".Fmt(x.Key, x.Value.ToString()));
			CollectionAssert.AreEqual(expected, actual);
		}
	}
}
