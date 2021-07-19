using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Be.Windows.Forms;
using NLog.Config;
using NLog.Targets;
using Peach.Core.Dom;
using Peach.Core;
using Peach.Core.Cracker;
using Peach.Core.IO;
using Peach.Core.Analyzers;
using System.Reflection;
using System.Text.RegularExpressions;
using NLog;
using Peach.Pro.Core;
using Peach.Pro.Core.License;

namespace PeachValidator
{
	public partial class MainForm : Form
	{
		static Version Version = Assembly.GetAssembly(typeof(Engine)).GetName().Version;
		readonly string _windowTitle = "Peach Validator v" + Version;
		readonly string _windowTitlePit = "Peach Validator v" + Version + " - {0}";
		readonly string _windowTitlePitSample = "Peach Validator v" + Version + " - {0} - {1}";
		readonly string _windowTitleSample = "Peach Validator v" + Version + " - None - {0}";

		ILicense _license;
		string _pitLibraryPath;
		string _pitFileName;
		string _sampleFileName;
		string _saveFileName;

		readonly Dictionary<string, object> _parserArgs = new Dictionary<string, object>();
		readonly Dictionary<DataElement, CrackNode> _crackMap = new Dictionary<DataElement, CrackNode>();
		readonly MemoryTarget _logTarget;
		CrackModel _crackModel = new CrackModel();
		List<DataElement> _exceptions = new List<DataElement>();

		public MainForm(
			ILicense license,
			string pitLibraryPath,
			string pitFileName,
			string sampleFileName,
			string saveFileName)
		{
			_license = license;
			_pitLibraryPath = pitLibraryPath;
			_pitFileName = pitFileName;
			_sampleFileName = sampleFileName;
			_saveFileName = saveFileName;
			
			InitializeComponent();

			setTitle();

			var nconfig = new LoggingConfiguration();
			_logTarget = new MemoryTarget();
			nconfig.AddTarget("console", _logTarget);
			_logTarget.Layout = "${logger} ${message} ${exception:format=tostring}";

			var rule = new LoggingRule("*", LogLevel.Debug, _logTarget);
			nconfig.LoggingRules.Add(rule);

			LogManager.Configuration = nconfig;
		}

		protected void setTitle()
		{
			if (!string.IsNullOrEmpty(_sampleFileName) && !string.IsNullOrEmpty(_pitFileName))
				Text = string.Format(_windowTitlePitSample, Path.GetFileName(_pitFileName), Path.GetFileName(_sampleFileName));
			else if (string.IsNullOrEmpty(_sampleFileName) && !string.IsNullOrEmpty(_pitFileName))
				Text = string.Format(_windowTitlePit, Path.GetFileName(_pitFileName));
			else if (!string.IsNullOrEmpty(_sampleFileName) && string.IsNullOrEmpty(_pitFileName))
				Text = string.Format(_windowTitleSample, Path.GetFileName(_sampleFileName));
			else
				Text = _windowTitle;
		}

		private void toolStripButtonOpenSample_Click(object sender, EventArgs e)
		{
			var ofd = new OpenFileDialog();
			if (ofd.ShowDialog() != DialogResult.OK)
				return;

			_sampleFileName = ofd.FileName;
			setTitle();

			toolStripButtonRefreshSample_Click(null, null);
		}

		private void toolStripButtonRefreshSample_Click(object sender, EventArgs e)
		{
			var cursor = Cursor.Current;
			Cursor.Current = Cursors.WaitCursor;
			var holder = (DataModelHolder)toolStripComboBoxDataModel.SelectedItem;
			try
			{
				// Clear the cracking debug logs
				textBoxLogs.Text = "";
				_logTarget.Logs.Clear();

				if (holder == null || string.IsNullOrEmpty(_sampleFileName) || string.IsNullOrEmpty(_pitFileName))
					return;

				// Refresh the hex display in case the file has changed.
				var fileStream = new FileStream(_sampleFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				var dynamicFileByteProvider = new DynamicFileByteProvider(fileStream);
				hexBox1.ByteProvider = dynamicFileByteProvider;

				using (Stream sin = new FileStream(_sampleFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					treeViewAdv1.BeginUpdate();
					treeViewAdv1.Model = null;

					try
					{
						var data = new BitStream(sin);
						var cracker = new DataCracker();
						cracker.EnterHandleNodeEvent += cracker_EnterHandleNodeEvent;
						cracker.ExitHandleNodeEvent += cracker_ExitHandleNodeEvent;
						cracker.AnalyzerEvent += cracker_AnalyzerEvent;
						cracker.ExceptionHandleNodeEvent += cracker_ExceptionHandleNodeEvent;
						//cracker.CrackData(dom.dataModels[dataModel], data);

						try
						{
							var dm = holder.MakeCrackModel();
							cracker.CrackData(dm, data);

							if (!string.IsNullOrEmpty(_saveFileName))
							{
								using (var f = File.Open(_saveFileName, FileMode.Create))
								{
									var val = dm.Value;
									val.CopyTo(f);
								}
							}
						}
						catch (CrackingFailure ex)
						{
							throw new PeachException("Error cracking \"" + ex.element.fullName + "\".\n" + ex.Message);
						}
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error Cracking");

						long endPos = -1;
						foreach (var element in _exceptions)
						{
							CrackNode currentModel;
							if (_crackMap.TryGetValue(element, out currentModel))
							{
								currentModel.Error = true;

								if (endPos == -1)
									endPos = currentModel.StartBits;

								currentModel.StopBits = endPos;

								if (element.parent != null && _crackMap.ContainsKey(element.parent))
									_crackMap[element.parent].Children.Add(currentModel);
							}
						}
					}

					foreach (var node in _crackMap.Values)
					{
						if (node.DataElement.parent != null && _crackMap.ContainsKey(node.DataElement.parent))
							node.Parent = _crackMap[node.DataElement.parent];
					}

					_crackModel.Root = _crackMap.Values.First().Root;
					treeViewAdv1.Model = _crackModel;
					treeViewAdv1.EndUpdate();
					treeViewAdv1.Root.Children[0].Expand();

					// No longer needed
					_crackMap.Clear();

					// Display debug logs
					textBoxLogs.Text = string.Join("\r\n", _logTarget.Logs);
					_logTarget.Logs.Clear();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error cracking file: " + ex.ToString());
			}
			finally
			{
				Cursor.Current = cursor;
			}
		}

		void RemoveElement(DataElement element)
		{
			CrackNode currentModel;
			if (!_crackMap.TryGetValue(element, out currentModel))
				return;

			if (element.parent != null && _crackMap.ContainsKey(element.parent))
				_crackMap[element.parent].Children.Remove(currentModel);
			_crackMap.Remove(element);

			// Remove any elements that have 'element' as a parent
			var res = _crackMap.Select(kv => kv.Key).Where(k => k.parent == element).ToList();
			foreach (var elem in res)
				RemoveElement(elem);
		}

		void cracker_ExceptionHandleNodeEvent(DataElement element, long position, BitStream data, Exception e)
		{
			if (!_crackMap.ContainsKey(element))
			{
				// If offsets can't be figured out - we will get a crack exception
				// before getting a begin element.
				_crackMap.Add(element, new CrackNode(_crackModel, element, position, 0));
			}

			_exceptions.Add(element);
		}

		void cracker_AnalyzerEvent(DataElement element, BitStream data)
		{
			RemoveElement(element);
		}

		void cracker_ExitHandleNodeEvent(DataElement element, long position, BitStream data)
		{
			foreach (var item in _exceptions)
				RemoveElement(item);
			_exceptions.Clear();

			if (!_crackMap.ContainsKey(element))
				return;

			var currentModel = _crackMap[element];
			currentModel.StopBits = position;

			if (element.parent != null && _crackMap.ContainsKey(element.parent))
				_crackMap[element.parent].Children.Add(currentModel);
			else
			{
				// TODO -- Need to handle this case!
			}
		}

		void cracker_EnterHandleNodeEvent(DataElement element, long position, BitStream data)
		{
			_crackMap[element] = new CrackNode(_crackModel, element, position, 0);
		}

		private void toolStripButtonOpenPit_Click(object sender, EventArgs e)
		{
			var ofd = new OpenFileDialog();
			ofd.Title = "Select PIT file";

			if (ofd.ShowDialog() != DialogResult.OK)
				return;

			SelectPit(ofd.FileName, true);
		}

		private void SelectPit(string fileName, bool getDefs)
		{
			_pitFileName = fileName;
			setTitle();

			var defs = PitDefines.ParseFileWithDefaults(_pitLibraryPath, fileName);
			_parserArgs[PitParser.DEFINED_VALUES] = defs;
			toolStripButtonRefreshPit_Click(null, null);
		}


		private void addDataModels(Dom dom, string ns)
		{
			if (!string.IsNullOrEmpty(ns))
				ns += ":";

			foreach (var otherNs in dom.ns)
				addDataModels(otherNs, ns + dom.Name);

			var name = dom.Name;

			if (!string.IsNullOrEmpty(name))
				name += ":";

			foreach (var dm in dom.dataModels)
				toolStripComboBoxDataModel.Items.Add(new DataModelHolder(dm, ns + name + dm.Name));
		}

		private void toolStripButtonRefreshPit_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(_pitFileName))
				return;

			try
			{
				var parser = new ProPitParser(_license, _pitLibraryPath, _pitFileName);

				var dom = parser.asParser(_parserArgs, _pitFileName);

				var previouslySelectedModelName = toolStripComboBoxDataModel.SelectedItem;

				toolStripComboBoxDataModel.Items.Clear();

				addDataModels(dom, "");

				var newModelIndex = -1;

				if (previouslySelectedModelName != null)
					newModelIndex = toolStripComboBoxDataModel.Items.IndexOf(previouslySelectedModelName);

				if (newModelIndex < 0)
					newModelIndex = toolStripComboBoxDataModel.Items.Count - 1;

				if (toolStripComboBoxDataModel.Items.Count > 0)
					toolStripComboBoxDataModel.SelectedIndex = newModelIndex;

				treeViewAdv1.BeginUpdate();
				var model = (DataModelHolder)toolStripComboBoxDataModel.Items[newModelIndex];
				treeViewAdv1.Model = CrackModel.CreateModelFromPit(model.DataModel);
				treeViewAdv1.EndUpdate();
				treeViewAdv1.Root.Children[0].Expand();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error loading file: " + ex.ToString());
			}
		}

		class DataModelHolder
		{
			public DataModel DataModel { get; set; }
			public string FullName { get; set; }

			public DataModelHolder(DataModel DataModel, string FullName)
			{
				this.DataModel = DataModel;
				this.FullName = FullName;
			}

			public DataModel MakeCrackModel()
			{
				var ret = (DataModel)DataModel.Clone();

				// Need to set the dom so scripting environments will work.
				ret.dom = DataModel.dom;

				return ret;
			}

			public override string ToString()
			{
				return FullName;
			}

			public override bool Equals(object obj)
			{
				var other = obj as DataModelHolder;

				if (other == null)
					return false;

				return other.FullName == this.FullName;
			}

			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
		}


		private void toolStripComboBoxDataModel_SelectedIndexChanged(object sender, EventArgs e)
		{
			try
			{
				var dataModel = (DataModelHolder)toolStripComboBoxDataModel.SelectedItem;

				treeViewAdv1.BeginUpdate();

				_crackModel = CrackModel.CreateModelFromPit(dataModel.DataModel);
				treeViewAdv1.Model = _crackModel;
				treeViewAdv1.EndUpdate();
				treeViewAdv1.Root.Children[0].Expand();
			}
			catch
			{
			}
		}

		private void treeViewAdv1_SelectionChanged(object sender, EventArgs e)
		{
			if (treeViewAdv1.SelectedNode == null)
				return;

			var node = (CrackNode)treeViewAdv1.SelectedNode.Tag;
			hexBox1.Select(node.StartBits / 8, (node.StopBits - node.StartBits + 7) / 8);
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			if (_pitFileName != null)
			{
				SelectPit(_pitFileName, false);
			}

			if (_sampleFileName != null)
			{
				var fileStream = new FileStream(_sampleFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				var dynamicFileByteProvider = new DynamicFileByteProvider(fileStream);
				hexBox1.ByteProvider = dynamicFileByteProvider;

				toolStripButtonRefreshSample_Click(null, null);
			}
		}
	}
}
