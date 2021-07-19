using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Monitors
{
	[TestFixture]
	[Quick]
	[Peach]
	internal class ReplayMonitorTests
	{
		private readonly List<Fault> _nonRerpoFaults = new List<Fault>();
		private readonly List<Fault> _reproFaults = new List<Fault>();
		private readonly List<Fault> _initialFaults = new List<Fault>();

		[TestCase(true)]
		[TestCase(false)]
		public void Run(bool controlIteration)
		{
			const string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String name='str1' value='Hello, World!' />
		<String name='str2' value='Hello, World!' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default' targetLifetime='iteration' faultWaitTime='0.0'>
		<StateModel ref='TheState' />
		<Publisher class='Null' />
		<Strategy class='RandomDeterministic' />
		<Mutators mode='include'>
			<Mutator class='StringCaseUpper' />
			<Mutator class='StringCaseLower' />
		</Mutators>
		<Strategy class='RandomDeterministic' />
	</Test>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration();
			var e = new Engine(null);

			e.IterationStarting += (ctx, it, tot) =>
			{
				if (controlIteration || !ctx.controlIteration)
					ctx.InjectFault();
			};

			e.ReproFault += (ctx, it, sm, faults) =>
			{
				Assert.AreEqual(1, faults.Length);
				faults[0].collectedData.AddRange(
					sm.dataActions.Select(kv => new Fault.Data(kv.Key, kv.Value.ToArray())));
				_initialFaults.Add(faults[0]);
			};

			e.Fault += (ctx, it, sm, faults) =>
			{
				Assert.AreEqual(1, faults.Length);
				faults[0].collectedData.AddRange(
					sm.dataActions.Select(kv => new Fault.Data(kv.Key, kv.Value.ToArray())));
				_reproFaults.Add(faults[0]);
			};

			e.ReproFailed += (ctx, it) =>
			{
				Assert.Greater(_initialFaults.Count, 1);
				var last = _initialFaults.Count - 1;
				_nonRerpoFaults.Add(_initialFaults[last]);
				_initialFaults.RemoveAt(last);
			};

			_nonRerpoFaults.Clear();
			_reproFaults.Clear();
			_initialFaults.Clear();

			int expectedFaults;

			if (controlIteration)
			{
				var ex = Assert.Throws<PeachException>(() => e.startFuzzing(dom, cfg));

				Assert.AreEqual("Fault detected on control record iteration.", ex.Message);

				expectedFaults = 1;
			}
			else
			{
				e.startFuzzing(dom, cfg);

				// 2 elements, 2 mutators = 4 faults
				expectedFaults = 4;
			}

			Assert.AreEqual(0, _nonRerpoFaults.Count);
			Assert.AreEqual(expectedFaults, _reproFaults.Count);
			Assert.AreEqual(_reproFaults.Count, _initialFaults.Count);

			for (var i = 0; i < _reproFaults.Count; ++i)
			{
				var initial = _initialFaults[i];
				var repro = _reproFaults[i];

				Assert.AreEqual(initial.title, repro.title);
				Assert.AreEqual(initial.iteration, repro.iteration);

				// One piece of data: sm.dataActions
				Assert.AreEqual(1, initial.collectedData.Count);
				Assert.AreEqual(initial.collectedData.Count, repro.collectedData.Count);

				for (var j = 0; j < initial.collectedData.Count; ++j)
				{
					Assert.AreEqual(initial.collectedData[j].Key, repro.collectedData[j].Key);
					Assert.AreEqual(initial.collectedData[j].Path, repro.collectedData[j].Path);
					Assert.AreEqual(initial.collectedData[j].Value, repro.collectedData[j].Value);
				}
			}
		}
	}
}
