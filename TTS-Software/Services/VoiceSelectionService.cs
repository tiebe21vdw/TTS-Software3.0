using System;
using System.Linq;
using System.Speech.Synthesis;

namespace TTS_Software.Services
{
    /// <summary>
    /// Zoekt, op basis van een taalcode, de best passende geïnstalleerde
    /// Windows-spraakstem op.
    /// </summary>
    public static class VoiceSelectionService
    {
        /// <param name="languageCode">ISO 639-1 code, bv. "nl" of "en". Mag null zijn.</param>
        /// <returns>De best passende stem, of null als er geen (deel)match is.</returns>
        public static InstalledVoice? SelectVoiceForLanguage(SpeechSynthesizer synthesizer, string? languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode)) return null;

            var voices = synthesizer.GetInstalledVoices()
                .Where(v => v.Enabled)
                .ToList();

            // Eerst proberen op exact dezelfde taal (bv. "nl" matcht "nl-NL" en "nl-BE").
            var match = voices.FirstOrDefault(v =>
                v.VoiceInfo.Culture.TwoLetterISOLanguageName.Equals(languageCode, StringComparison.OrdinalIgnoreCase));

            return match;
        }
    }
}
