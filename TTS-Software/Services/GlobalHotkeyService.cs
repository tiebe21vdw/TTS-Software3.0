using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TTS_Software.Services
{
    /// <summary>
    /// Registreert een systeembrede sneltoets (werkt ook als de app geen focus heeft)
    /// via de Win32 RegisterHotKey-API, en koppelt die aan een venster via een
    /// window-message hook.
    /// </summary>
    public sealed class GlobalHotkeyService : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        public const uint ModAlt = 0x0001;
        public const uint ModControl = 0x0002;
        public const uint ModShift = 0x0004;
        private const uint ModNoRepeat = 0x4000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private readonly Window window;
        private readonly int hotkeyId;
        private HwndSource? source;
        private bool isRegistered;

        /// <summary>Vuurt wanneer de geregistreerde sneltoets wordt ingedrukt.</summary>
        public event Action? HotkeyPressed;

        public GlobalHotkeyService(Window window, int hotkeyId = 0x4A5A)
        {
            this.window = window;
            this.hotkeyId = hotkeyId;
        }

        /// <returns>False als de combinatie al door een andere app in gebruik is.</returns>
        public bool Register(uint virtualKey, uint modifiers)
        {
            var handle = new WindowInteropHelper(window).EnsureHandle();
            source = HwndSource.FromHwnd(handle);
            source?.AddHook(WndProc);

            isRegistered = RegisterHotKey(handle, hotkeyId, modifiers | ModNoRepeat, virtualKey);
            return isRegistered;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == hotkeyId)
            {
                HotkeyPressed?.Invoke();
                handled = true;
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (isRegistered)
            {
                var handle = new WindowInteropHelper(window).Handle;
                if (handle != IntPtr.Zero)
                {
                    UnregisterHotKey(handle, hotkeyId);
                }

                isRegistered = false;
            }

            source?.RemoveHook(WndProc);
        }
    }
}
