using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyRoute
{
    public sealed class KeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        // Kept alive for the lifetime of the hook: if this delegate is garbage
        // collected while the hook is installed, the hook fails silently.
        private readonly LowLevelKeyboardProc _proc;
        private IntPtr _hookHandle = IntPtr.Zero;
        private Dictionary<int, int> _mappings = new();

        public KeyboardHook()
        {
            _proc = HookCallback;
        }

        public void Start()
        {
            if (_hookHandle != IntPtr.Zero)
                return;

            _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
        }

        public void Stop()
        {
            if (_hookHandle == IntPtr.Zero)
                return;

            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }

        public void UpdateMappings(IEnumerable<KeyMapping> mappings)
        {
            _mappings = mappings.ToDictionary(m => (int)m.PhysicalKey, m => (int)m.SubstituteKey);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (_mappings.TryGetValue(vkCode, out int substituteVkCode))
                {
                    keybd_event((byte)substituteVkCode, 0, 0, UIntPtr.Zero); // key down
                    keybd_event((byte)substituteVkCode, 0, 2, UIntPtr.Zero); // key up
                    return (IntPtr)1;
                }
            }

            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        public void Dispose() => Stop();

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    }
}
