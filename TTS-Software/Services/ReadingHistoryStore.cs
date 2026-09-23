using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TTS_Software.Services
{
    public sealed class ReadingHistoryEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Text { get; set; } = string.Empty;
        public string? LanguageCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Bewaart de laatste voorgelezen tekstfragmenten op schijf, zodat je ze later
    /// terug kunt vinden en opnieuw kunt laten voorlezen.
    /// </summary>
    public static class ReadingHistoryStore
    {
        private const int MaxEntries = 50;

        private static readonly string HistoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TTS-Software",
            "history.json");

        public static List<ReadingHistoryEntry> Load()
        {
            try
            {
                if (File.Exists(HistoryPath))
                {
                    var entries = JsonSerializer.Deserialize<List<ReadingHistoryEntry>>(File.ReadAllText(HistoryPath));
                    if (entries is not null)
                    {
                        return entries;
                    }
                }
            }
            catch (JsonException) { }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (NotSupportedException) { }

            return new List<ReadingHistoryEntry>();
        }

        /// <summary>Voegt een nieuw fragment vooraan toe en snoeit de lijst tot <see cref="MaxEntries"/>.</summary>
        public static void Add(string text, string? languageCode)
        {
            var history = Load();
            history.Insert(0, new ReadingHistoryEntry { Text = text, LanguageCode = languageCode });

            if (history.Count > MaxEntries)
            {
                history.RemoveRange(MaxEntries, history.Count - MaxEntries);
            }

            Save(history);
        }

        public static void Remove(Guid id)
        {
            var history = Load();
            history.RemoveAll(entry => entry.Id == id);
            Save(history);
        }

        public static void Clear() => Save(new List<ReadingHistoryEntry>());

        private static void Save(List<ReadingHistoryEntry> entries)
        {
            try
            {
                var directory = Path.GetDirectoryName(HistoryPath);
                if (directory is not null)
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(HistoryPath, JsonSerializer.Serialize(entries, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
