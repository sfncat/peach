using System;
using System.Windows.Forms;

namespace Peach.Pro.Test.OS.Windows
{
	public partial class ButtonClickerForm : Form
	{
		public bool IsClicked { get; private set; }

		public ButtonClickerForm()
		{
			InitializeComponent();
			IsClicked = false;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			IsClicked = true;
		}
	}
}
