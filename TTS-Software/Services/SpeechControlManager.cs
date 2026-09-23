using System;
using System.Windows;
using System.Windows.Threading;

namespace TTS_Software.Services
{
    /// <summary>
    /// Koppelt <see cref="SpeechService"/> aan het zwevende <see cref="SpeechControlWindow"/>:
    /// toont het paneeltje zodra het voorlezen begint, sluit het weer zodra het klaar
    /// of gestopt is. SpeechService-gebeurtenissen komen op een achtergrondthread
    /// binnen, dus alles wordt netjes naar de UI-thread doorgestuurd.
    /// </summary>
    public static class SpeechControlManager
    {
        private static SpeechControlWindow? window;
        private static bool initialized;

        public static void Initialize()
        {
            if (initialized) return;
            initialized = true;

            SpeechService.SpeechStarted += (_, _) => RunOnUiThread(ShowWindow);
            SpeechService.SpeechEnded += (_, _) => RunOnUiThread(CloseWindow);
        }

        private static void RunOnUiThread(Action action)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher is null) return;

            if (dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
            }
        }

        private static void ShowWindow()
        {
            if (window is null)
            {
                window = new SpeechControlWindow();
                window.Closed += (_, _) => window = null;
                window.Show();
                return;
            }

            window.RefreshState();
            if (!window.IsVisible)
            {
                window.Show();
            }
        }

        private static void CloseWindow()
        {
            window?.Close();
            window = null;
        }
    }
}
