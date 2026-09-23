using System.Windows;
using TTS_Software.Services;

namespace TTS_Software;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		var settings = SettingsStore.Load();
		SettingsStore.ApplyToResources(settings);

		SpeechControlManager.Initialize();

		var mainWindow = new MainWindow();
		MainWindow = mainWindow;

		TrayIconService.Initialize();
		TrayIconService.VoorlezenAangevraagd += (_, _) => mainWindow.TriggerTextToSpeechSelection();
		TrayIconService.GeschiedenisAangevraagd += (_, _) => mainWindow.OpenHistoryWindow();
		TrayIconService.InstellingenAangevraagd += (_, _) => mainWindow.OpenSettingsWindow();
		TrayIconService.AfsluitenAangevraagd += (_, _) => Shutdown();

		TrayIconService.ShowBalloon(
			"TTS-Software is actief",
			"Klik met rechts op dit icoontje, of druk op Ctrl+Alt+L om tekst op je scherm te selecteren en te laten voorlezen.");

		mainWindow.Show();
	}

	protected override void OnExit(ExitEventArgs e)
	{
		TrayIconService.Dispose();
		base.OnExit(e);
	}
}
