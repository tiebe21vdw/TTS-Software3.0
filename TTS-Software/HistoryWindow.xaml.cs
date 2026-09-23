using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TTS_Software.Services;

namespace TTS_Software
{
    public partial class HistoryWindow : Window
    {
        public HistoryWindow()
        {
            InitializeComponent();
            LoadHistory();
        }

        private void LoadHistory()
        {
            var entries = ReadingHistoryStore.Load()
                .Select(entry => new HistoryItemViewModel(entry))
                .ToList();

            HistoryListBox.ItemsSource = entries;
            EmptyStateText.Visibility = entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            HistoryListBox.Visibility = entries.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ReplayButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not HistoryItemViewModel item) return;

            var settings = SettingsStore.Load();
            SpeechService.Speak(item.Entry.Text, settings, item.Entry.LanguageCode);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not HistoryItemViewModel item) return;

            ReadingHistoryStore.Remove(item.Entry.Id);
            LoadHistory();
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            var confirm = System.Windows.MessageBox.Show(
                "Weet je zeker dat je de hele geschiedenis wilt wissen?",
                "Geschiedenis wissen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            ReadingHistoryStore.Clear();
            LoadHistory();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }

    /// <summary>Weergavehulp voor één geschiedenisregel in de lijst.</summary>
    public sealed class HistoryItemViewModel
    {
        public ReadingHistoryEntry Entry { get; }
        public string Preview { get; }
        public string SubText { get; }

        public HistoryItemViewModel(ReadingHistoryEntry entry)
        {
            Entry = entry;

            var text = entry.Text.Trim();
            Preview = text.Length > 160 ? text[..160] + "…" : text;

            var language = string.IsNullOrWhiteSpace(entry.LanguageCode)
                ? "onbekende taal"
                : entry.LanguageCode.ToUpperInvariant();

            SubText = $"{entry.Timestamp:dd-MM-yyyy HH:mm} · {language}";
        }
    }
}
