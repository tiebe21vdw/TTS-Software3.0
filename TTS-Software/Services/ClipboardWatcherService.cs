using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TTS_Software.Services
{
    /// <summary>
    /// Houdt het Windows-klembord in de gaten via de Win32
    /// AddClipboardFormatListener-API en meldt zodra er ergens op het systeem
    /// nieuwe tekst gekopieerd wordt (Ctrl+C). Gebruikt voor de
    /// "automatisch voorlezen"-instelling.
    /// </summary>
    public sealed class ClipboardWatcherService : IDisposable
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        private readonly Window window;
        private HwndSource? source;
        private bool isListening;

        /// <summary>Vuurt (op de UI-thread) met de nieuw gekopieerde tekst.</summary>
        public event Action<string>? ClipboardTextCopied;

        public ClipboardWatcherService(Window window)
        {
            this.window = window;
        }

        public void Start()
        {
            if (isListening) return;

            var handle = new WindowInteropHelper(window).EnsureHandle();
            source = HwndSource.FromHwnd(handle);
            source?.AddHook(WndProc);
            isListening = AddClipboardFormatListener(handle);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                TryReadClipboardText();
            }

            return IntPtr.Zero;
        }

        private void TryReadClipboardText()
        {
            try
            {
                if (!System.Windows.Clipboard.ContainsText()) return;

                var text = System.Windows.Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    ClipboardTextCopied?.Invoke(text);
                }
            }
            catch (Exception)
            {
                // Het klembord kan soms even 'bezet' zijn door een andere toepassing; negeren.
            }
        }

        public void Dispose()
        {
            if (!isListening) return;

            var handle = new WindowInteropHelper(window).Handle;
            if (handle != IntPtr.Zero)
            {
                RemoveClipboardFormatListener(handle);
            }

            source?.RemoveHook(WndProc);
            isListening = false;
        }
    }
}
