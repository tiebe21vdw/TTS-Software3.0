using System.Windows;

namespace TTS_Software
{
    public partial class ProcessingIndicatorWindow : Window
    {
        public ProcessingIndicatorWindow()
        {
            InitializeComponent();
            Loaded += (_, _) => PositionBottomCenter();
        }

        private void PositionBottomCenter()
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Left + ((workArea.Width - ActualWidth) / 2);
            Top = workArea.Bottom - ActualHeight - 24;
        }
    }
}
