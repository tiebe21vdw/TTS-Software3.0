using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace TTS_Software.Services
{
    /// <summary>
    /// Icoontje in het systeemvak (rechtsonder naast de klok) met een snelmenu.
    /// Werkt onafhankelijk van het zwevende widgetje — handig als dat per ongeluk
    /// buiten beeld gesleept is, of gewoon als vertrouwd Windows-aanknopingspunt.
    /// </summary>
    public static class TrayIconService
    {
        private static NotifyIcon? notifyIcon;

        public static event EventHandler? VoorlezenAangevraagd;
        public static event EventHandler? InstellingenAangevraagd;
        public static event EventHandler? GeschiedenisAangevraagd;
        public static event EventHandler? AfsluitenAangevraagd;

        public static void Initialize()
        {
            if (notifyIcon is not null) return;

            var menu = new ContextMenuStrip();
            menu.Items.Add("Tekst voorlezen  (Ctrl+Alt+L)", null, (_, _) => VoorlezenAangevraagd?.Invoke(null, EventArgs.Empty));
            menu.Items.Add("Geschiedenis...", null, (_, _) => GeschiedenisAangevraagd?.Invoke(null, EventArgs.Empty));
            menu.Items.Add("Instellingen...", null, (_, _) => InstellingenAangevraagd?.Invoke(null, EventArgs.Empty));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Afsluiten", null, (_, _) => AfsluitenAangevraagd?.Invoke(null, EventArgs.Empty));

            notifyIcon = new NotifyIcon
            {
                Icon = TryGetAppIcon() ?? SystemIcons.Application,
                Visible = true,
                Text = "TTS-Software",
                ContextMenuStrip = menu
            };

            notifyIcon.DoubleClick += (_, _) => VoorlezenAangevraagd?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>Toont een klein, niet-blokkerend meldingsballonnetje bij het systeemvakicoon.</summary>
        public static void ShowBalloon(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
        {
            if (notifyIcon is null) return;

            notifyIcon.BalloonTipTitle = title;
            notifyIcon.BalloonTipText = text;
            notifyIcon.BalloonTipIcon = icon;
            notifyIcon.ShowBalloonTip(4000);
        }

        public static void Dispose()
        {
            if (notifyIcon is null) return;

            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            notifyIcon = null;
        }

        private static Icon? TryGetAppIcon()
        {
            try
            {
                var path = Process.GetCurrentProcess().MainModule?.FileName;
                return string.IsNullOrWhiteSpace(path) ? null : Icon.ExtractAssociatedIcon(path);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
