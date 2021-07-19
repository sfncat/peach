using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Action = Peach.Core.Dom.Action;

namespace Peach.Pro.Core.MutationStrategies
{
	[MutationStrategy("Replay", Scope = PluginScope.Internal)]
	[Description("Replay an existing set of data sets")]
	public class ReplayStrategy : MutationStrategy
	{
		class DataFileMutator : Mutator
		{
			private readonly string _name;

			public DataFileMutator(DataFile file)
				: base((StateModel)null)
			{
				_name = Path.GetFileName(file.FileName);
			}

			public override string Name
			{
				get { return _name; }
			}

			public override int count
			{
				get { throw new NotImplementedException(); }
			}

			public override uint mutation
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			public override void sequentialMutation(DataElement obj)
			{
				throw new NotImplementedException();
			}

			public override void randomMutation(DataElement obj)
			{
				throw new NotImplementedException();
			}
		}

		private Dictionary<ActionData, List<DataSet>> _dataSets;
		private string _actionData;
		private List<DataFile> _mutations;

		public ReplayStrategy(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		public override void Initialize(RunContext context, Engine engine)
		{
			base.Initialize(context, engine);

			_dataSets = new Dictionary<ActionData,List<DataSet>>();
			_actionData = null;
			_mutations = new List<DataFile>();

			foreach (var state in context.test.stateModel.states)
			{
				foreach (var action in state.actions)
				{
					foreach (var actionData in action.outputData)
					{
						// Squirrel away the data sets on each action and then clear
						// the list so that when the state model runs we don't try
						// and apply them to the data model.
						_dataSets.Add(actionData, actionData.dataSets.ToList());
						actionData.dataSets.Clear();
					}
				}
			}

			context.ActionStarting += ActionStarting;
		}

		public override void Finalize(RunContext context, Engine engine)
		{
			context.ActionStarting -= ActionStarting;

			foreach (var state in context.test.stateModel.states)
			{
				foreach (var action in state.actions)
				{
					foreach (var actionData in action.outputData)
					{
						// Put back the data sets we saved off!
						_dataSets[actionData].ForEach(i => actionData.dataSets.Add(i));
						_dataSets.Remove(actionData);
					}
				}
			}

			base.Finalize(context, engine);
		}

		public override bool UsesRandomSeed
		{
			get { return false; }
		}

		public override bool IsDeterministic
		{
			get { return true; }
		}

		public override uint Count
		{
			get { return (uint)_mutations.Count; }
		}

		public override uint Iteration
		{
			get;
			set;
		}

		private void ActionStarting(RunContext context, Action action)
		{
			// Is this a supported action?
			if (!(action.outputData.Any()))
				return;

			if (context.controlRecordingIteration)
			{
				foreach (var ad in action.outputData)
				{
					var dataSets = _dataSets[ad];

					// Only consider actions with data sets
					if (dataSets.Count == 0)
						continue;

					// Only allow a single output
					if (_actionData != null)
						throw new PeachException("Error, the Replay strategy only supports state models with data sets on a single action.");

					// Save off each data set option as an available mutation
					_actionData = ad.outputName;
					_mutations = dataSets.SelectMany(d => d.OfType<DataFile>()).ToList();
				}
			}
			else if (!context.controlIteration)
			{
				foreach (var ad in action.outputData)
				{
					if (_actionData != ad.outputName)
						continue;

					var data = _mutations[(int)Iteration - 1];

					BitStream bs;

					try
					{
						bs = new BitStream(File.OpenRead(data.FileName));
					}
					catch (Exception ex)
					{
						throw new SoftException(ex);
					}

					context.OnDataMutating(ad, ad.dataModel, new DataFileMutator(data));

					ad.dataModel.mutationFlags = MutateOverride.TypeTransform;
					ad.dataModel.MutatedValue = new Variant(bs);

					return;
				}
			}
		}
	}
}

