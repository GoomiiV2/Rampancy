using System;
using System.Runtime.InteropServices;

namespace Plugins.Rampancy.Runtime
{
    // Inject input into an other process
    // Windows only atm
    public static class InputInjector
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}