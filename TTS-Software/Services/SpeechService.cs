using System;
using System.Linq;
using System.Speech.Synthesis;

namespace TTS_Software.Services
{
    public sealed class SpeechWordEventArgs : EventArgs
    {
        public string Text { get; }
        public int CharacterPosition { get; }
        public int CharacterCount { get; }

        public SpeechWordEventArgs(string text, int characterPosition, int characterCount)
        {
            Text = text;
            CharacterPosition = characterPosition;
            CharacterCount = characterCount;
        }
    }

    public static class SpeechService
    {
        private static readonly SpeechSynthesizer Synthesizer = new();
        private static readonly string DefaultVoiceName = Synthesizer.Voice.Name;

        public static event EventHandler? SpeechStarted;
        public static event EventHandler? SpeechEnded;
        public static event EventHandler<SpeechWordEventArgs>? WordBoundaryReached;

        public static bool IsSpeaking { get; private set; }
        public static bool IsPaused { get; private set; }
        public static string CurrentText { get; private set; } = string.Empty;

        static SpeechService()
        {
            Synthesizer.SpeakStarted += (_, _) =>
            {
                IsSpeaking = true;
                IsPaused = false;
                SpeechStarted?.Invoke(null, EventArgs.Empty);
            };

            Synthesizer.SpeakProgress += (_, e) =>
            {
                WordBoundaryReached?.Invoke(
                    null,
                    new SpeechWordEventArgs(CurrentText, e.CharacterPosition, e.CharacterCount));
            };

            Synthesizer.SpeakCompleted += (_, _) =>
            {
                IsSpeaking = false;
                IsPaused = false;
                SpeechEnded?.Invoke(null, EventArgs.Empty);
            };
        }

        public static InstalledVoice[] GetAvailableVoices() =>
            Synthesizer.GetInstalledVoices().Where(v => v.Enabled).ToArray();

        public static void Speak(string text, UserSettings settings, string? detectedLanguageCode)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            Synthesizer.SpeakAsyncCancelAll();
            CurrentText = text;
            ApplyVoice(settings.PreferredVoiceName, detectedLanguageCode);
            Synthesizer.Rate = ToEngineRate(settings.SpeechRate);
            Synthesizer.Volume = Math.Clamp(settings.Volume, 0, 100);
            Synthesizer.SpeakAsync(text);
        }

        public static void Pause()
        {
            if (!IsSpeaking || IsPaused) return;
            Synthesizer.Pause();
            IsPaused = true;
        }

        public static void Resume()
        {
            if (!IsPaused) return;
            Synthesizer.Resume();
            IsPaused = false;
        }

        public static void Stop() => Synthesizer.SpeakAsyncCancelAll();

        public static void SetVolume(int volume) => Synthesizer.Volume = Math.Clamp(volume, 0, 100);

        private static void ApplyVoice(string? preferredVoiceName, string? detectedLanguageCode)
        {
            if (!string.IsNullOrWhiteSpace(preferredVoiceName))
            {
                var manual = Synthesizer.GetInstalledVoices()
                    .FirstOrDefault(v => v.Enabled &&
                        string.Equals(v.VoiceInfo.Name, preferredVoiceName, StringComparison.OrdinalIgnoreCase));

                if (manual is not null)
                {
                    Synthesizer.SelectVoice(manual.VoiceInfo.Name);
                    return;
                }
            }

            var autoVoice = VoiceSelectionService.SelectVoiceForLanguage(
                Synthesizer,
                detectedLanguageCode);
            Synthesizer.SelectVoice(autoVoice?.VoiceInfo.Name ?? DefaultVoiceName);
        }

        private static int ToEngineRate(double sliderValue)
        {
            var rate = sliderValue >= 1
                ? (sliderValue - 1) * 10.0
                : (sliderValue - 1) / 0.5 * 10.0;

            return Math.Clamp((int)Math.Round(rate), -10, 10);
        }
    }
}