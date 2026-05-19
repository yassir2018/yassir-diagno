using System.Windows;

namespace YassirDiagno.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    public void SetProgress(int percent, string status)
    {
        Dispatcher.Invoke(() =>
        {
            Progress.Value = Math.Clamp(percent, 0, 100);
            LblStatus.Text = status;
        });
    }
}
