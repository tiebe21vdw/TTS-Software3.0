using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TTS_Software.Services
{
    /// <summary>
    /// Herkent de taal van een stukje tekst zonder externe dienst of taalmodel nodig
    /// te hebben: telt hoe vaak veelvoorkomende korte woorden (lidwoorden,
    /// voegwoorden, ...) van elke taal voorkomen en kiest de taal met de meeste
    /// treffers. Werkt goed genoeg voor de korte stukjes tekst die je via een
    /// schermselectie voorleest; is geen volwaardige taalherkenning voor lange
    /// documenten met meerdere talen door elkaar.
    /// </summary>
    public static class LanguageDetectionService
    {
        private static readonly Dictionary<string, HashSet<string>> StopWordsPerLanguage = new()
        {
            ["nl"] = Words("de het een en van ik je is dat op te niet met voor aan om maar als dan dit deze wordt zijn was worden naar ook bij uit wat zo nog of kan wij hun hij zij geen zich"),
            ["en"] = Words("the and is of to in that it was for on are as with his they at be this have from or one had by but not what all were we when your can there use an each which"),
            ["de"] = Words("der die das und ist nicht von mit den ein eine zu in im auf für sich dem als auch es wird sind aber wie oder war du sie wir nur noch mehr kann muss wenn"),
            ["fr"] = Words("le la les de des et est un une dans pour que qui sur avec ne pas ce cette il elle nous vous se sont mais ou comme plus tout"),
            ["es"] = Words("el la los las de y es un una en que por con para no se su al del como más pero sus le ya o este esta son fue"),
            ["it"] = Words("il lo la i gli le di e che un una in per con non si del della come ma più anche sono era questo questa"),
            ["pt"] = Words("o a os as de e que um uma em para com não se do da como mas mais por foi são este esta"),
        };

        /// <returns>
        /// De ISO 639-1 taalcode (bv. "nl", "en") van de vermoedelijke taal, of null
        /// als de tekst te kort is of geen enkele taal duidelijk naar voren komt.
        /// </returns>
        public static string? DetectLanguage(string text)
        {
            var words = Regex.Matches(text, @"\p{L}+").Select(m => m.Value.ToLowerInvariant()).ToArray();
            if (words.Length < 3)
            {
                return null;
            }

            var scores = new Dictionary<string, int>();
            foreach (var word in words)
            {
                foreach (var (language, stopWords) in StopWordsPerLanguage)
                {
                    if (stopWords.Contains(word))
                    {
                        scores[language] = scores.GetValueOrDefault(language) + 1;
                    }
                }
            }

            if (scores.Count == 0)
            {
                return null;
            }

            var best = scores.OrderByDescending(kv => kv.Value).First();

            // Vereis minstens twee treffers, anders is het te toevallig om op af te gaan.
            return best.Value >= 2 ? best.Key : null;
        }

        private static HashSet<string> Words(string spaceSeparated) =>
            new(spaceSeparated.Split(' '), System.StringComparer.OrdinalIgnoreCase);
    }
}
