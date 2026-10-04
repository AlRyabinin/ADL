using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AdlpdsConfig.Utils
{
    /// <summary>
    /// Показывает MessageBox, принудительно центрируя его относительно owner-окна.
    /// </summary>
    public static class CenteredMessageBox
    {
        public static DialogResult Show(IWin32Window owner,
                                        string text,
                                        string caption = "",
                                        MessageBoxButtons buttons = MessageBoxButtons.OK,
                                        MessageBoxIcon icon = MessageBoxIcon.None)
        {
            if(owner == null)
                return MessageBox.Show(text, caption, buttons, icon);

            IntPtr ownerHandle = owner is Control c ? c.Handle : IntPtr.Zero;

            _ownerHandle = ownerHandle;
            _hook = ownerHandle != IntPtr.Zero
                ? SetWindowsHookEx(WH_CBT, CbtProc, IntPtr.Zero, GetCurrentThreadId())
                : IntPtr.Zero;

            try
            {
                return MessageBox.Show(owner, text, caption, buttons, icon);
            }
            finally
            {
                if(_hook != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hook);
                    _hook = IntPtr.Zero;
                }
                _ownerHandle = IntPtr.Zero;
            }
        }

        // ---- Win32 ----

        private const int WH_CBT = 5;
        private const int HCBT_ACTIVATE = 5;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        private static IntPtr _hook = IntPtr.Zero;
        private static IntPtr _ownerHandle = IntPtr.Zero;

        private delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn,
                                                      IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
                                                    IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
                                                int X, int Y, int cx, int cy, uint uFlags);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        private static IntPtr CbtProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if(nCode == HCBT_ACTIVATE && _ownerHandle != IntPtr.Zero)
            {
                IntPtr msgBox = wParam;

                if(GetWindowRect(_ownerHandle, out RECT o) &&
                   GetWindowRect(msgBox, out RECT b))
                {
                    int x = o.Left + (o.Right - o.Left - (b.Right - b.Left)) / 2;
                    int y = o.Top + (o.Bottom - o.Top - (b.Bottom - b.Top)) / 2;

                    SetWindowPos(msgBox, IntPtr.Zero, x, y, 0, 0,
                        SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
                }
            }
            return CallNextHookEx(_hook, nCode, wParam, lParam);
        }
    }
}
