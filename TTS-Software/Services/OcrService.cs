using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace TTS_Software.Services
{
    /// <summary>
    /// Herkent tekst in een afbeelding met de ingebouwde OCR-engine van Windows
    /// (dezelfde die Windows zelf gebruikt, geen extra installatie nodig).
    /// </summary>
    public static class OcrService
    {
        public static async Task<string> RecognizeTextAsync(Bitmap screenshot)
        {
            using var prepared = ScaleDownIfNeeded(screenshot);
            using var softwareBitmap = await ToSoftwareBitmapAsync(prepared);

            var engine = CreateEngine();
            if (engine is null)
            {
                throw new InvalidOperationException(
                    "Geen tekstherkenning (OCR) beschikbaar op dit systeem. Installeer een " +
                    "taalpakket met 'Optische tekenherkenning' via Windows Instellingen > " +
                    "Tijd en taal > Taal en regio.");
            }

            var result = await engine.RecognizeAsync(softwareBitmap);
            return result.Text?.Trim() ?? string.Empty;
        }

        private static OcrEngine? CreateEngine()
        {
            // Probeer eerst de taal van de gebruiker, dan Nederlands, dan Engels.
            var candidates = new[]
            {
                CultureInfo.CurrentUICulture.TwoLetterISOLanguageName,
                "nl",
                "en"
            };

            foreach (var code in candidates)
            {
                try
                {
                    var language = new Language(code);
                    if (OcrEngine.IsLanguageSupported(language))
                    {
                        var engine = OcrEngine.TryCreateFromLanguage(language);
                        if (engine is not null) return engine;
                    }
                }
                catch (Exception)
                {
                    // Ongeldige of niet-ondersteunde taalcode: volgende kandidaat proberen.
                }
            }

            return OcrEngine.TryCreateFromUserProfileLanguages();
        }

        private static Bitmap ScaleDownIfNeeded(Bitmap source)
        {
            var maxDimension = (int)OcrEngine.MaxImageDimension;
            if (source.Width <= maxDimension && source.Height <= maxDimension)
            {
                return new Bitmap(source);
            }

            var scale = Math.Min((double)maxDimension / source.Width, (double)maxDimension / source.Height);
            var newWidth = Math.Max(1, (int)(source.Width * scale));
            var newHeight = Math.Max(1, (int)(source.Height * scale));

            var scaled = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(scaled);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(source, 0, 0, newWidth, newHeight);
            return scaled;
        }

        private static async Task<SoftwareBitmap> ToSoftwareBitmapAsync(Bitmap bitmap)
        {
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Png);
            memoryStream.Position = 0;

            using var randomAccessStream = memoryStream.AsRandomAccessStream();
            var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            // De OCR-engine verwacht Bgra8 (premultiplied) of Gray8.
            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
                softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
            {
                using var original = softwareBitmap;
                return SoftwareBitmap.Convert(original, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }

            return softwareBitmap;
        }
    }
}
