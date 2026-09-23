using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TTS_Software.Services;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;

namespace TTS_Software
{
    public partial class MainWindow : Window
    {
        // Hoogte van het venster als het is ingeklapt / helemaal uitgeklapt (4 icoontjes).
        private const double CollapsedHeight = 50;
        private const double ExpandedHeight = 220;

        private bool isDragging = false;
        private bool isProcessingTts = false;
        private string? lastAutoReadText;

        // Extra variabelen om een klik van een sleep te onderscheiden
        private WpfPoint startMousePosition;
        private DateTime startClickTime;

        private GlobalHotkeyService? readHotkey;
        private GlobalHotkeyService? stopHotkey;
        private ClipboardWatcherService? clipboardWatcher;

        public MainWindow()
        {
            InitializeComponent();

            // Startpositie bepalen (Rechterkant van het scherm)
            double screenWidth = SystemParameters.WorkArea.Width;
            double screenHeight = SystemParameters.WorkArea.Height;

            this.Left = screenWidth - this.Width - 10;
            this.Top = this.Height / 2;

            SpeechService.SpeechStarted += (_, _) => Dispatcher.BeginInvoke(() => SpeakingIndicator.Visibility = Visibility.Visible);
            SpeechService.SpeechEnded += (_, _) => Dispatcher.BeginInvoke(() => SpeakingIndicator.Visibility = Visibility.Collapsed);

            Loaded += (_, _) => SetupSystemIntegration();
            Closed += (_, _) => TeardownSystemIntegration();
        }

        /// <summary>
        /// Registreert systeembrede sneltoetsen en start de klembord-listener.
        /// Gebeurt pas nadat het venster een echte HWND heeft (na Loaded).
        /// </summary>
        private void SetupSystemIntegration()
        {
            try
            {
                readHotkey = new GlobalHotkeyService(this, hotkeyId: 0x4A54_5301);
                readHotkey.HotkeyPressed += () => TriggerTextToSpeechSelection();
                readHotkey.Register((uint)KeyInterop.VirtualKeyFromKey(Key.L), GlobalHotkeyService.ModControl | GlobalHotkeyService.ModAlt);

                stopHotkey = new GlobalHotkeyService(this, hotkeyId: 0x4A54_5302);
                stopHotkey.HotkeyPressed += () => SpeechService.Stop();
                stopHotkey.Register((uint)KeyInterop.VirtualKeyFromKey(Key.S), GlobalHotkeyService.ModControl | GlobalHotkeyService.ModAlt);
            }
            catch (Exception)
            {
                // Sneltoetsen zijn een extraatje; als registratie faalt (bv. combinatie al in
                // gebruik) blijft de rest van de app gewoon werken via de icoontjes.
            }

            try
            {
                clipboardWatcher = new ClipboardWatcherService(this);
                clipboardWatcher.ClipboardTextCopied += OnClipboardTextCopied;
                clipboardWatcher.Start();
            }
            catch (Exception)
            {
                // Ook dit is een extraatje ("automatisch voorlezen") — geen harde afhankelijkheid.
            }
        }

        private void TeardownSystemIntegration()
        {
            readHotkey?.Dispose();
            stopHotkey?.Dispose();
            clipboardWatcher?.Dispose();
        }

        /// <summary>
        /// Wordt aangeroepen zodra de gebruiker ergens op het systeem tekst kopieert
        /// (Ctrl+C). Leest die automatisch voor als de instelling aanstaat.
        /// </summary>
        private void OnClipboardTextCopied(string text)
        {
            var settings = SettingsStore.Load();
            if (!settings.AutoRead) return;
            if (text.Length > 20_000) return; // veiligheidsgrens tegen enorme klembord-inhoud
            if (text == lastAutoReadText) return; // voorkom dubbel voorlezen van dezelfde kopie

            lastAutoReadText = text;

            var language = LanguageDetectionService.DetectLanguage(text);
            SpeechService.Speak(text, settings, language);
            ReadingHistoryStore.Add(text, language);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Sla de tijd en de startpositie van de muis op
                startClickTime = DateTime.Now;
                startMousePosition = e.GetPosition(this);

                isDragging = true;

                try
                {
                    this.DragMove(); // Start het slepen
                }
                catch (InvalidOperationException)
                {
                    // Voorkomt crashes als er heel snel geklikt wordt
                }
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!isDragging) return;
            isDragging = false;

            // Bereken hoelang de muis ingedrukt is geweest en hoeveel deze is verplaatst
            TimeSpan clickDuration = DateTime.Now - startClickTime;
            WpfPoint currentMousePosition = e.GetPosition(this);

            double deltaX = Math.Abs(currentMousePosition.X - startMousePosition.X);
            double deltaY = Math.Abs(currentMousePosition.Y - startMousePosition.Y);

            // Goudmijn-logica: Was het een snelle klik zonder veel beweging?
            if (clickDuration.TotalMilliseconds < 250 && deltaX < 5 && deltaY < 5)
            {
                return; // Stop hier, we gaan niet snappen naar de rand omdat het een klik was
            }

            // Was het géén klik? Dan pas gaan we snappen naar de rand van het scherm
            double screenWidth = SystemParameters.WorkArea.Width;
            double screenCenter = screenWidth / 2;
            double windowCenter = this.Left + (this.Width / 2);
            double BorderOffset = 10;

            if (windowCenter < screenCenter)
            {
                this.Left = BorderOffset;
            }
            else
            {
                this.Left = screenWidth - this.Width - BorderOffset;
            }
        }

        // Dit is je nieuwe centrale functie die wordt aangeroepen bij een succesvolle klik!
        private void IcoonKnop_Uitvoeren()
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void TtsKnop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private async void TtsKnop_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            await StartTextToSpeechSelectionAsync();
        }

        /// <summary>Extern aanroepbaar (systeemvak, sneltoets) om dezelfde flow te starten als op het icoontje klikken.</summary>
        public void TriggerTextToSpeechSelection()
        {
            _ = StartTextToSpeechSelectionAsync();
        }

        /// <summary>
        /// Laat de gebruiker een gebied van het scherm selecteren, herkent de tekst
        /// daarin via OCR en leest die vervolgens hardop voor.
        /// </summary>
        private async Task StartTextToSpeechSelectionAsync()
        {
            // Voorkom dat een dubbelklik twee selecties tegelijk start.
            if (isProcessingTts) return;
            isProcessingTts = true;

            ProcessingIndicatorWindow? processingIndicator = null;

            try
            {
                var overlay = new SelectionOverlayWindow
                {
                    Owner = this
                };

                var result = overlay.ShowDialog();

                if (result != true || overlay.SelectedRegion is not { } region)
                {
                    return; // Geannuleerd (Esc of een te kleine/geen sleepbeweging)
                }

                processingIndicator = new ProcessingIndicatorWindow();
                processingIndicator.Show();

                using var screenshot = ScreenCaptureService.CaptureRegion(region);
                var recognizedText = await OcrService.RecognizeTextAsync(screenshot);

                processingIndicator.Close();
                processingIndicator = null;

                if (string.IsNullOrWhiteSpace(recognizedText))
                {
                    System.Windows.MessageBox.Show(
                        "Er is geen tekst herkend in het geselecteerde gebied.",
                        "Tekst voorlezen",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var settings = SettingsStore.Load();
                var detectedLanguage = LanguageDetectionService.DetectLanguage(recognizedText);

                SpeechService.Speak(recognizedText, settings, detectedLanguage);
                ReadingHistoryStore.Add(recognizedText, detectedLanguage);

                if (settings.AutoCopyToClipboard)
                {
                    TryCopyToClipboard(recognizedText);
                }
            }
            catch (Exception exception)
            {
                System.Windows.MessageBox.Show(
                    $"Het voorlezen is mislukt.\n\n{exception.Message}",
                    "Tekst voorlezen",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                processingIndicator?.Close();
                isProcessingTts = false;
            }
        }

        private static void TryCopyToClipboard(string text)
        {
            try
            {
                System.Windows.Clipboard.SetText(text);
            }
            catch (Exception)
            {
                // Klembord kan soms even 'bezet' zijn door een andere toepassing; niet kritiek.
            }
        }

        private void OffKnop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void OffKnop_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            IcoonKnop_Uitvoeren();
        }

        private void GeschiedenisKnop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void GeschiedenisKnop_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            OpenHistoryWindow();
        }

        /// <summary>Extern aanroepbaar (systeemvak) om het geschiedenisvenster te openen.</summary>
        public void OpenHistoryWindow()
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

        private void InstellingenKnop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void InstellingenKnop_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            OpenSettingsWindow();
        }

        /// <summary>Extern aanroepbaar (systeemvak) om het instellingenvenster te openen.</summary>
        public void OpenSettingsWindow()
        {
            try
            {
                var settingsWindow = new SettingsWindow
                {
                    Owner = this
                };

                settingsWindow.ShowDialog();
            }
            catch (Exception exception)
            {
                System.Windows.MessageBox.Show(
                    $"De instellingen konden niet worden geopend.\n\n{exception.Message}",
                    "Instellingen",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Window_MouseEnter(object sender, WpfMouseEventArgs e)
        {
            if (isDragging) return;
            Height = ExpandedHeight;
        }

        private void Window_MouseLeave(object sender, WpfMouseEventArgs e)
        {
            if (isDragging) return;
            Height = CollapsedHeight;
        }
    }
}
