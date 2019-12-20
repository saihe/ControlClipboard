using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Windows;

namespace WPF
{
	public delegate void ControlClipboardHandler(object sender, ControlClipboardArgs ev);

	[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
	internal class ControlClipboardListener: Window
	{
		class NativeMethods
		{
			[DllImport("user32")]
			public static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

			[DllImport("user32")]
			public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

			[DllImport("user32")]
			public extern static int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
		}
		private const int WM_DRAWCLIPBOARD = 0x0308;
		private const int WM_CHANGECBCHAIN = 0x030D;

		private IntPtr nextHandle;
		private IntPtr nwHandle;
		//private Form parent;
		public event ControlClipboardHandler ClipboardHandler;

		public ControlClipboardListener(Window w)
		{
			w.Loaded += OnHandleCreated;
			w.Closed += OnHandleDestroyed;
		}

		internal void OnHandleCreated(object sender, EventArgs e)
		{
			Window
			AssignHandle(((Form)sender).Handle); // NativeWindowクラスへのForm登録(メッセージフック開始)
			nwhandle = this.handle;
			nextHandle = NativeMethods.SetClipboardViewer(this.Handle); // クリップボードチェインに登録
		}

		internal void OnHandleDestroyed(object sender, EventArgs e)
		{
			NativeMethods.ChangeClipboardChain(this.Handle, nextHandle); // クリップボードチェインから削除
			ReleaseHandle(); // NativeWindowクラスの後始末(Formに対してのメッセージフック解除)
		}

		// ハンドル変更されるシーンがあるか不明。
		//override void OnHandleChange()
		//{
		//	ChangeClipboardChain(nwHandle, nextHandle); // クリップボードチェインから削除
		//	nwHandle = this.Handle;
		//	nextHandle = SetClipboardViewer(this.Handle); // クリップボードチェインに登録
		//}

		protected override void WndProc(ref Message msg)
		{
			switch (msg.Msg)
			{
				case WM_DRAWCLIPBOARD:
					if (Clipboard.ContainsText())
					{ // Note: ここを変更すれば、テキスト以外も通知可能
					  // クリップボードの内容がテキストの場合のみ
						if (ClipboardHandler != null)
						{
							// クリップボードの内容を取得してハンドラを呼び出す
							ClipboardHandler(this, new ControlClipboardArgs(Clipboard.GetText()));
						}
					}
					if (nextHandle != IntPtr.Zero)
					{
						NativeMethods.SendMessage(nextHandle, msg.Msg, msg.WParam, msg.LParam);
					}
					break;

				// クリップボード・ビューア・チェーンが更新された
				case WM_CHANGECBCHAIN:
					if (msg.WParam == nextHandle)
					{
						nextHandle = msg.LParam;
					}
					else if (nextHandle != IntPtr.Zero)
					{
						NativeMethods.SendMessage(nextHandle, msg.Msg, msg.WParam, msg.LParam);
					}
					break;
			}
			base.WndProc(ref msg);
		}
	}
}