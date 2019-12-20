using System;
using System.Collections.Generic;
using System.Text;

namespace WPF
{
	public class ControlClipboardArgs : EventArgs
	{
		private string text;
		public string Text { get { return this.text; } }
		public ControlClipboardArgs(string str) { this.text = str; }
	}
}
