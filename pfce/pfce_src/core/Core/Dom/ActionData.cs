using System;
using System.Collections.Generic;
using System.Linq;
using Peach.Core.IO;
using Peach.Core.Cracker;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Peach.Core.Dom
{
	[Serializable]
	public class ActionData : INamed, IActionDataXpath
	{
		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		#endregion

		/// <summary>
		/// Currently unused.  Exists for schema generation.
		/// </summary>
		[XmlElement]
		[DefaultValue(null)]
		public Xsd.DataModel schemaModel
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public ActionData()
		{
			dataSets = new NamedCollection<DataSet>("Data");
		}

		/// <summary>
		/// The action that this belongs to
		/// </summary>
		public Action action { get; set; }

		/// <summary>
		/// The data model we use for input/output when running the state model.
		/// </summary>
		public DataModel dataModel { get; set; }

		/// <summary>
		/// All of the data sets that correspond to the data model
		/// </summary>
		public NamedCollection<DataSet> dataSets { get; private set; }

		/// <summary>
		/// Enumerable view of all Data that corresponds to the data model
		/// </summary>
		public IEnumerable<Data> allData { get { return dataSets.SelectMany(d => d.AsEnumerable()); } }

		/// <summary>
		/// The currently selected Data in use by the model
		/// </summary>
		public Data selectedData { get; private set; }

		/// <summary>
		/// A clean copy of the data model that has never had fields/data applied.
		/// Only set when using dataSets since applying data requires
		/// the clean model.
		/// </summary>
		private DataModel sourceDataModel { get; set; }

		/// <summary>
		/// A cached copy of the clean data model.  Has fields/data applied
		/// when applicable.
		/// </summary>
		public DataModel originalDataModel { get; private set; }

		/// <summary>
		/// Is this action data part of an input/getProperty/call-result action
		/// </summary>
		public bool IsInput { get { return !IsOutput; } }

		/// <summary>
		/// Is this action data part of an output/setProperty/call-param action
		/// </summary>
		public bool IsOutput { get { return action.outputData.Contains(this); } }

		/// <summary>
		/// The name of this record.
		/// </summary>
		/// <remarks>
		/// Non-null when actions have multiple data models
		/// (Action.Call) and null otherwise (Input/Output/SetProperty/GetProperty).
		/// </remarks>
		[XmlAttribute("name")]
		[DefaultValue(null)]
		public string Name { get; protected set; }

		public string FullFieldId
		{
			get { return DataElement.FieldIdConcat(action.parent.FieldId, action.FieldId); }
		}

		/// <summary>
		/// Full name of this record when viewed as input data
		/// </summary>
		public virtual string inputName
		{
			get
			{
				if (Name == null)
					return string.Format("{0}.{1}", action.parent.Name, action.Name);
				else
					return string.Format("{0}.{1}.{2}", action.parent.Name, action.Name, Name);
			}
		}

		/// <summary>
		/// Full name of this record when viewed as output data
		/// </summary>
		public virtual string outputName
		{
			get
			{
				if (Name == null)
					return string.Format("{0}.{1}", action.parent.Name, action.Name);
				else
					return string.Format("{0}.{1}.{2}", action.parent.Name, action.Name, Name);
			}
		}

		public virtual ulong MaxOutputSize
		{
			get;
			protected set;
		}

		/// <summary>
		/// Initialize dataModel to its original state.
		/// If this is the first time through and a dataSet exists,
		/// the data will be applied to the model.
		/// </summary>
		public void UpdateToOriginalDataModel()
		{
			System.Diagnostics.Debug.Assert(dataModel != null);

			// If is the first time through we need to cache a clean data model
			if (originalDataModel == null)
			{
				// Store off the max output size
				if (MaxOutputSize == 0)
					MaxOutputSize = action.parent.parent.parent.context.test.maxOutputSize;

				// Apply data samples
				var option = allData.FirstOrDefault();
				if (option != null)
				{
					// Cache the model before any cracking has ever occured
					// since we can't crack into an model that has previously
					// been cracked (eg: Placement won't work).
					dataModel.actionData = null;
					sourceDataModel = dataModel;
					Apply(option);
				}
				else
				{
					// Evaulate the full dataModel prior to saving as the original
					dataModel.actionData = this;
					var val = dataModel.Value;
					System.Diagnostics.Debug.Assert(val != null);

					originalDataModel = dataModel.Clone() as DataModel;
				}
			}
			else
			{
				dataModel = originalDataModel.Clone() as DataModel;
				dataModel.actionData = this;
			}
		}

		/// <summary>
		/// Apply data from the dataSet to the data model.
		/// </summary>
		/// <param name="option"></param>
		public void Apply(Data option)
		{
			System.Diagnostics.Debug.Assert(allData.Contains(option));

			if (sourceDataModel == null)
			{
				// The strategy is updating our data set
				// before the first call to UpdateToOriginalDataModel
				System.Diagnostics.Debug.Assert(originalDataModel == null);

				// Store off the max output size
				if (MaxOutputSize == 0)
					MaxOutputSize = action.parent.parent.parent.context.test.maxOutputSize;

				// Cache the model before any cracking has ever occured
				// since we can't crack into an model that has previously
				// been cracked (eg: Placement won't work).
				dataModel.actionData = null;
				sourceDataModel = dataModel;
			}

			// Work in a clean copy of the original
			var copy = sourceDataModel.Clone() as DataModel;
			copy.actionData = this;
			option.Apply(action, copy);

			// Evaulate the full dataModel prior to saving as the original
			var val = copy.Value;
			System.Diagnostics.Debug.Assert(val != null);

			originalDataModel = copy;
			selectedData = option;

			UpdateToOriginalDataModel();
		}

		/// <summary>
		/// Crack the BitStream into the data model.
		/// Will automatically update to the original model
		/// prior to cracking.  Used by InOut action parameters.
		/// </summary>
		/// <param name="bs"></param>
		public void Crack(BitStream bs)
		{
			DataModel copy;

			if (selectedData != null)
			{
				// If we have selected data, we need to have the un-cracked data model
				System.Diagnostics.Debug.Assert(sourceDataModel != null);
				copy = sourceDataModel.Clone() as DataModel;
			}
			else
			{
				// If we have never selected data, originalDataModel is fine
				System.Diagnostics.Debug.Assert(sourceDataModel == null);
				copy = originalDataModel.Clone() as DataModel;
			}

			var cracker = new DataCracker();
			cracker.CrackData(copy, bs);

			dataModel = copy;
			dataModel.actionData = this;
		}

		/// <summary>
		/// The unique instance name for this action data.
		/// Includes the run count to disambiguate multiple
		/// runs of the action via a re-enterant state.
		/// </summary>
		public string instanceName
		{
			get
			{
				return string.Format("Run_{0}.{1}", action.parent.runCount, modelName);
			}
		}

		/// <summary>
		/// The name of this action data.  Does not include the
		/// run count so the name will be the same across multiple
		/// runs of the action via a re-enterant state.
		/// </summary>
		public virtual string modelName
		{
			get
			{
				if (string.IsNullOrEmpty(Name))
					return string.Join(".", action.parent.Name, action.Name, dataModel.Name);
				else
					return string.Join(".", action.parent.Name, action.Name, Name, dataModel.Name);
			}
		}

		public virtual IEnumerable<ActionData> XpathData
		{
			get
			{
				return Enumerable.Empty<ActionData>();
			}
		}

	}
}
