using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TTS_Software.Services;

namespace TTS_Software
{
    public partial class SettingsWindow : Window
    {
        private readonly UserSettings settings;
        private bool isLoading;

        public SettingsWindow()
        {
            isLoading = true;
            InitializeComponent();

            settings = SettingsStore.Load();

            try
            {
                settings.Theme ??= "Default";
                settings.Background ??= "Zwart";
                if (!double.IsFinite(settings.SpeechRate))
                {
                    settings.SpeechRate = 1;
                }

                if (settings.Volume is < 0 or > 100)
                {
                    settings.Volume = 100;
                }

                LoadSettingsIntoControls();
            }
            catch (Exception)
            {
                settings.Theme = "Default";
                settings.Background = "Zwart";
                settings.AutoStart = false;
                settings.AutoRead = false;
                settings.AutoCopyToClipboard = false;
                settings.SpeechRate = 1;
                settings.Volume = 100;
                settings.PreferredVoiceName = null;

                ThemeComboBox.SelectedIndex = 0;
                BackgroundComboBox.SelectedIndex = 1;
                AutoStartCheckBox.IsChecked = false;
                AutoReadCheckBox.IsChecked = false;
                AutoCopyCheckBox.IsChecked = false;
                SpeechRateSlider.Value = 1;
                VolumeSlider.Value = 100;
                LoadVoicesIntoComboBox();
            }

            isLoading = false;
        }

        private void LoadSettingsIntoControls()
        {
            var themeIndex = ThemeComboBox.Items
                .OfType<ComboBoxItem>()
                .Select((item, index) => (item, index))
                .Where(item => item.item.Content?.ToString() == settings.Theme)
                .Select(item => item.index)
                .FirstOrDefault(-1);
            if (themeIndex < 0)
            {
                themeIndex = 0;
                settings.Theme = "Default";
            }

            ThemeComboBox.SelectedIndex = themeIndex;

            if (settings.Background is not ("Wit" or "Zwart" or "Systeem"))
            {
                settings.Background = "Zwart";
            }

            BackgroundComboBox.SelectedIndex = settings.Background switch
            {
                "Wit" => 0,
                "Systeem" => 2,
                _ => 1
            };

            settings.AutoStart = AutoStartManager.IsAutoStartEnabled();
            AutoStartCheckBox.IsChecked = settings.AutoStart;
            AutoReadCheckBox.IsChecked = settings.AutoRead;
            AutoCopyCheckBox.IsChecked = settings.AutoCopyToClipboard;
            SpeechRateSlider.Value = Math.Clamp(settings.SpeechRate, SpeechRateSlider.Minimum, SpeechRateSlider.Maximum);
            VolumeSlider.Value = Math.Clamp(settings.Volume, VolumeSlider.Minimum, VolumeSlider.Maximum);

            LoadVoicesIntoComboBox();
        }

        private void LoadVoicesIntoComboBox()
        {
            VoiceComboBox.Items.Clear();
            VoiceComboBox.Items.Add(new ComboBoxItem { Content = "Automatisch (op basis van herkende taal)", Tag = null });

            try
            {
                foreach (var voice in SpeechService.GetAvailableVoices()
                             .OrderBy(v => v.VoiceInfo.Culture.DisplayName)
                             .ThenBy(v => v.VoiceInfo.Name))
                {
                    VoiceComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = $"{voice.VoiceInfo.Name}  ({voice.VoiceInfo.Culture.DisplayName})",
                        Tag = voice.VoiceInfo.Name
                    });
                }
            }
            catch (Exception)
            {
                // Als de spraakengine even niet bereikbaar is, blijft alleen "Automatisch" over.
            }

            var selected = VoiceComboBox.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(item => string.Equals((string?)item.Tag, settings.PreferredVoiceName, StringComparison.OrdinalIgnoreCase));

            VoiceComboBox.SelectedItem = selected ?? VoiceComboBox.Items[0];
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var historyWindow = new HistoryWindow
                {
                    Owner = this
                };

                historyWindow.ShowDialog();
            }
            catch (Exception exception)
            {
                System.Windows.MessageBox.Show(
                    $"De geschiedenis kon niet worden geopend.\n\n{exception.Message}",
                    "Geschiedenis",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is not ComboBoxItem selectedItem)
            {
                return;
            }

            if (isLoading) return;

            settings.Theme = selectedItem.Content?.ToString() ?? "Default";
            SettingsStore.ApplyTheme(settings.Theme);
            SaveSettings();
        }

        private void BackgroundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BackgroundComboBox.SelectedItem is not ComboBoxItem selectedItem)
            {
                return;
            }

            if (isLoading) return;

            settings.Background = selectedItem.Content?.ToString() ?? "Zwart";
            SettingsStore.ApplyBackground(settings.Background);
            SaveSettings();
        }

        private void AutoStartCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (isLoading) return;

            settings.AutoStart = AutoStartCheckBox.IsChecked == true;
            if (!AutoStartManager.SetAutoStart(settings.AutoStart))
            {
                isLoading = true;
                AutoStartCheckBox.IsChecked = !settings.AutoStart;
                settings.AutoStart = !settings.AutoStart;
                isLoading = false;
                return;
            }

            SaveSettings();
        }

        private void AutoReadCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (isLoading) return;
            settings.AutoRead = AutoReadCheckBox.IsChecked == true;
            SaveSettings();
        }

        private void AutoCopyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (isLoading) return;
            settings.AutoCopyToClipboard = AutoCopyCheckBox.IsChecked == true;
            SaveSettings();
        }

        private void SpeechRateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isLoading) return;
            settings.SpeechRate = SpeechRateSlider.Value;
            SaveSettings();
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isLoading) return;
            settings.Volume = (int)Math.Round(VolumeSlider.Value);
            SaveSettings();
        }

        private void VoiceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isLoading) return;
            if (VoiceComboBox.SelectedItem is not ComboBoxItem selectedItem) return;

            settings.PreferredVoiceName = selectedItem.Tag as string;
            SaveSettings();
        }

        private void SaveSettings()
        {
            SettingsStore.Save(settings);
        }
    }
}
