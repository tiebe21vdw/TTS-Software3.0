using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;

namespace TTS_Software
{
    /// <summary>
    /// Transparant venster over het hele (virtuele) scherm waarmee de gebruiker
    /// met de muis een rechthoek kan slepen. Na loslaten staat <see cref="SelectedRegion"/>
    /// klaar in fysieke schermpixels, direct bruikbaar voor een schermafbeelding.
    /// </summary>
    public partial class SelectionOverlayWindow : Window
    {
        private WpfPoint startPoint;
        private bool isSelecting;

        public System.Drawing.Rectangle? SelectedRegion { get; private set; }

        public SelectionOverlayWindow()
        {
            InitializeComponent();

            // Dek het volledige virtuele bureaublad (alle beeldschermen) af.
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Activate();
            Focus();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            isSelecting = true;
            startPoint = e.GetPosition(this);

            Canvas.SetLeft(SelectionRectangle, startPoint.X);
            Canvas.SetTop(SelectionRectangle, startPoint.Y);
            SelectionRectangle.Width = 0;
            SelectionRectangle.Height = 0;
            SelectionRectangle.Visibility = Visibility.Visible;

            CaptureMouse();
        }

        private void Window_MouseMove(object sender, WpfMouseEventArgs e)
        {
            if (!isSelecting) return;

            var current = e.GetPosition(this);

            var x = Math.Min(current.X, startPoint.X);
            var y = Math.Min(current.Y, startPoint.Y);
            var width = Math.Abs(current.X - startPoint.X);
            var height = Math.Abs(current.Y - startPoint.Y);

            Canvas.SetLeft(SelectionRectangle, x);
            Canvas.SetTop(SelectionRectangle, y);
            SelectionRectangle.Width = width;
            SelectionRectangle.Height = height;
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!isSelecting) return;
            isSelecting = false;
            ReleaseMouseCapture();

            var current = e.GetPosition(this);
            var selection = new Rect(startPoint, current);

            // Te klein om een bedoelde selectie te zijn (bv. een losse klik): annuleren.
            if (selection.Width < 4 || selection.Height < 4)
            {
                DialogResult = false;
                Close();
                return;
            }

            SelectedRegion = ToPhysicalPixels(selection);
            PlayCaptureFlash();
        }

        /// <summary>
        /// Korte visuele bevestiging dat de selectie is vastgelegd, voordat het
        /// overlay-venster sluit en de OCR-herkenning start.
        /// </summary>
        private void PlayCaptureFlash()
        {
            var flash = new DoubleAnimation
            {
                From = 1.0,
                To = 0.1,
                Duration = TimeSpan.FromMilliseconds(180)
            };

            flash.Completed += (_, _) =>
            {
                DialogResult = true;
                Close();
            };

            SelectionRectangle.BeginAnimation(OpacityProperty, flash);
        }

        private void Window_KeyDown(object sender, WpfKeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;

            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Zet venster-coördinaten (WPF device-independent units) om naar fysieke
        /// schermpixels via PointToScreen, zodat DPI-schaling per beeldscherm
        /// automatisch correct wordt afgehandeld.
        /// </summary>
        private System.Drawing.Rectangle ToPhysicalPixels(Rect wpfRect)
        {
            var topLeft = PointToScreen(wpfRect.TopLeft);
            var bottomRight = PointToScreen(wpfRect.BottomRight);

            return new System.Drawing.Rectangle(
                (int)Math.Round(topLeft.X),
                (int)Math.Round(topLeft.Y),
                (int)Math.Round(bottomRight.X - topLeft.X),
                (int)Math.Round(bottomRight.Y - topLeft.Y));
        }
    }
}
