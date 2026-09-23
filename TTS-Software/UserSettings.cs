using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace TTS_Software;

public sealed class UserSettings
{
    public string Theme { get; set; } = "Default";
    public string Background { get; set; } = "Zwart";
    public bool AutoStart { get; set; }
    public bool AutoRead { get; set; }
    public double SpeechRate { get; set; } = 1;

    /// <summary>Herkende tekst ook automatisch naar het klembord kopiëren.</summary>
    public bool AutoCopyToClipboard { get; set; }

    /// <summary>Stemvolume, 0-100.</summary>
    public int Volume { get; set; } = 100;

    /// <summary>
    /// Naam van een handmatig gekozen stem (uit SpeechSynthesizer.GetInstalledVoices()),
    /// of null om automatisch een stem te kiezen op basis van de herkende taal.
    /// </summary>
    public string? PreferredVoiceName { get; set; }
}

public static class SettingsStore
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TTS-Software",
        "settings.json");

    public static UserSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var settings = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(SettingsPath));
                if (settings is not null)
                {
                    return settings;
                }
            }
        }
        catch (JsonException)
        {
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (NotSupportedException)
        {
        }

        return new UserSettings();
    }

    public static void Save(UserSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (directory is not null)
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public static void ApplyToResources(UserSettings settings)
    {
        ApplyTheme(settings.Theme);
        ApplyBackground(settings.Background);
    }

    public static void ApplyTheme(string themeName)
    {
        var colors = themeName switch
        {
            "Ocean" => ("#FF00B8D4", "#FF2979FF", "#FF00E5FF", "#CC071B2A"),
            "Sunset" => ("#FFFF6D00", "#FFFF1744", "#FFFFC400", "#CC2A1010"),
            "Forest" => ("#FF43A047", "#FF00897B", "#FFAEEA00", "#CC0B1F18"),
            "Pink" => ("#FFC2185B", "#FF8E24AA", "#FFFF80AB", "#CC24061A"),
            "Gold" => ("#FFFFB300", "#FFD700", "#FFFFF59D", "#CC211803"),
            "Red" => ("#FFE53935", "#FF880015", "#FFFF8A80", "#CC280505"),
            "Brown" => ("#FFA72646", "#FF6D4C41", "#FFD7CCC8", "#CC1A110E"),
            _ => ("#FF3F3F46", "#FF71717A", "#FFA1A1AA", "#E618181B")
        };

        var resources = System.Windows.Application.Current.Resources;
        resources["AccentColor"] = ParseColor(colors.Item1);
        resources["SecondaryColor"] = ParseColor(colors.Item2);
        resources["HighlightColor"] = ParseColor(colors.Item3);
        resources["PanelColor"] = ParseColor(colors.Item4);
    }

    public static void ApplyBackground(string backgroundName)
    {
        var colors = backgroundName switch
        {
            "Wit" => (Background: "#CCFFFFFF", Hover: "#CCC0C0C0", HoverBorder: "#FFBDBDBD"),
            "Systeem" => GetSystemColors(),
            _ => (Background: "#CC010101", Hover: "#CC202020", HoverBorder: "#33FFFFFF")
        };

        var backgroundColor = ParseColor(colors.Background);
        var hoverColor = ParseColor(colors.Hover);
        var hoverBorderColor = ParseColor(colors.HoverBorder);
        System.Windows.Application.Current.Resources["BackgroundColor"] = backgroundColor;
        System.Windows.Application.Current.Resources["PanelBrush"] = new SolidColorBrush(backgroundColor);
        System.Windows.Application.Current.Resources["HoverColor"] = hoverColor;
        System.Windows.Application.Current.Resources["HoverBrush"] = new SolidColorBrush(hoverColor);
        System.Windows.Application.Current.Resources["TransparentBorderColor"] = hoverBorderColor;
        System.Windows.Application.Current.Resources["TransparentBorderBrush"] = new SolidColorBrush(hoverBorderColor);
        System.Windows.Application.Current.Resources["BackgroundMode"] = backgroundName;
    }

    private static System.Windows.Media.Color ParseColor(string value) =>
        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(value)!;

    private static (string Background, string Hover, string HoverBorder) GetSystemColors()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        var usesLightTheme = key?.GetValue("AppsUseLightTheme") as int?;

        return usesLightTheme == 0
            ? ("#CC010101", "#CC202020", "#33FFFFFF")
            : ("#CCFFFFFF", "#CCC0C0C0", "#FFBDBDBD");
    }
}