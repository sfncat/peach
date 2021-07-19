using System;
using System.Runtime.InteropServices;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Monitor = Peach.Core.Agent.Monitor2;

#if !MONO
using System.Windows.Automation;
#endif

namespace Peach.Pro.OS.Windows.Agent.Monitors
{
	[Monitor("ButtonClicker")]
	[Parameter("WindowText", typeof(string), "Text to search for")]
	[Parameter("ButtonName", typeof(string), "Name of button to click")]
	[System.ComponentModel.Description("Automatically dismisses popup dialogs.")]
	public class ButtonClicker : Monitor
	{
		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("user32.dll", SetLastError = true)]
		static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr SetActiveWindow(IntPtr hWnd);

		// ReSharper disable once InconsistentNaming
		const int BM_CLICK = 0x00F5;

		protected static NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public string WindowText { get; set; }
		public string ButtonName { get; set; }

		public ButtonClicker(string name)
			: base(name)
		{
		}

#if !MONO
		static AutomationElement Find(AutomationElement elem, Condition cond, string text)
		{
			foreach (AutomationElement item in elem.FindAll(TreeScope.Children, cond))
			{
				try
				{
					if (item.Current.Name.Contains(text))
						return item;
				}
				catch (ElementNotAvailableException)
				{
				}
			}

			return null;
		}

		private void OnPopup(object sender, AutomationEventArgs args)
		{
			try
			{
				var elem = (AutomationElement)sender;

				try
				{
					Logger.Trace("Automation event fired: {0}", elem.Current.Name);
				}
				catch (ElementNotAvailableException)
				{
					Logger.Trace("Automation event fired: <Element Not Available>");
					return;
				}

				var tgt = Find(elem, Condition.TrueCondition, WindowText);
				if (tgt == null)
					return;

				var condBtn = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);
				var btn = Find(elem, condBtn, ButtonName);
				if (btn == null)
					return;

				// For some reason the InvokePattern.Invoke() way of clicking
				// the button stops working once the desktop is locked.
				// Resorting to PostMessage appears to work around this

				// Per MSDN: Need to call SetActiveWindow for BM_CLICK
				// to work when the button's parent is a dialog.
				SetActiveWindow(new IntPtr(elem.Current.NativeWindowHandle));

				var hWnd = new IntPtr(btn.Current.NativeWindowHandle);
				PostMessage(hWnd, BM_CLICK, IntPtr.Zero, IntPtr.Zero);

				Logger.Debug("Clicked button '{0}' on window containing text '{1}'.", ButtonName, WindowText);
			}
			catch (Exception ex)
			{
				Logger.Trace("Exception clicking button '{0}' on window containing text '{1}'. {2}", ButtonName, WindowText, ex);
			}
		}

		public override void SessionFinished()
		{
			Automation.RemoveAllEventHandlers();
		}

		public override void SessionStarting()
		{
			Automation.AddAutomationEventHandler(
				WindowPattern.WindowOpenedEvent,
				AutomationElement.RootElement,
				TreeScope.Descendants,
				OnPopup);
		}
#else
		public override void SessionStarting()
		{
			throw new PeachException("The ButtonClicker monitor is not supported on mono.");
		}
#endif
	}
}

