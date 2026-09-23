using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using TTS_Software.Services;

namespace TTS_Software
{
    /// <summary>
    /// Klein zwevend "nu aan het voorlezen"-paneeltje: toont de tekst met het
    /// huidige woord gemarkeerd, en geeft play/pause/stop + volume. Verschijnt
    /// zodra het voorlezen begint en verdwijnt weer zodra het klaar of gestopt is
    /// (beheerd door <see cref="SpeechControlManager"/>).
    /// </summary>
    public partial class SpeechControlWindow : Window
    {
        private static readonly SolidColorBrush HighlightBackground = new(System.Windows.Media.Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF));

        private bool isLoadingVolume = true;

        public SpeechControlWindow()
        {
            InitializeComponent();
            Loaded += (_, _) => PositionBottomCenter();

            CaptionText.Text = SpeechService.CurrentText;
            VolumeSlider.Value = SettingsStore.Load().Volume;

            SpeechService.WordBoundaryReached += OnWordBoundaryReached;
            Closed += (_, _) => SpeechService.WordBoundaryReached -= OnWordBoundaryReached;

            RefreshState();
        }

        private void PositionBottomCenter()
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Left + ((workArea.Width - ActualWidth) / 2);
            Top = workArea.Bottom - ActualHeight - 24;
        }

        public void RefreshState()
        {
            // Pauze-icoon (dubbele streep) tijdens het spreken, afspeel-driehoek als het gepauzeerd is.
            PlayPauseGlyph.Text = SpeechService.IsPaused ? "\u25B6" : "\u275A\u275A";
        }

        private void OnWordBoundaryReached(object? sender, SpeechWordEventArgs e)
        {
            Dispatcher.BeginInvoke(() => HighlightWord(e.Text, e.CharacterPosition, e.CharacterCount));
        }

        private void HighlightWord(string text, int position, int length)
        {
            if (string.IsNullOrEmpty(text) || position < 0 || length < 0 || position + length > text.Length)
            {
                CaptionText.Text = text;
                return;
            }

            CaptionText.Inlines.Clear();
            CaptionText.Inlines.Add(new Run(text[..position]));
            CaptionText.Inlines.Add(new Run(text.Substring(position, length))
            {
                Background = HighlightBackground,
                FontWeight = FontWeights.Bold
            });
            CaptionText.Inlines.Add(new Run(text[(position + length)..]));

            // Ruwe auto-scroll: schuift globaal mee met hoe ver we in de tekst zijn.
            if (TextScrollViewer.ExtentHeight > TextScrollViewer.ViewportHeight && text.Length > 0)
            {
                var progress = position / (double)text.Length;
                TextScrollViewer.ScrollToVerticalOffset(progress * (TextScrollViewer.ExtentHeight - TextScrollViewer.ViewportHeight));
            }
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (SpeechService.IsPaused)
            {
                SpeechService.Resume();
            }
            else
            {
                SpeechService.Pause();
            }

            RefreshState();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // SpeechService vuurt SpeechEnded, waarna SpeechControlManager dit venster sluit.
            SpeechService.Stop();
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isLoadingVolume)
            {
                isLoadingVolume = false;
                return;
            }

            var volume = (int)Math.Round(VolumeSlider.Value);
            SpeechService.SetVolume(volume);

            var settings = SettingsStore.Load();
            settings.Volume = volume;
            SettingsStore.Save(settings);
        }
    }
}
