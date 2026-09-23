using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace TTS_Software.Services
{
    /// <summary>
    /// Maakt een schermafbeelding van een specifiek gebied van het scherm.
    /// </summary>
    public static class ScreenCaptureService
    {
        /// <param name="region">Gebied in fysieke schermpixels.</param>
        public static Bitmap CaptureRegion(Rectangle region)
        {
            var bitmap = new Bitmap(
                Math.Max(1, region.Width),
                Math.Max(1, region.Height),
                PixelFormat.Format32bppArgb);

            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(region.Left, region.Top, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);

            return bitmap;
        }
    }
}
