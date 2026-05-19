using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using YassirDiagno.ViewModels;

namespace YassirDiagno;

public partial class MainWindow : Window
{
    public bool ForceClose { get; set; }

    public MainWindow()
    {
        InitializeComponent();
        if (App.Host is not null)
        {
            var vm = App.Host.Services.GetRequiredService<MainViewModel>();
            DataContext = vm;
            Loaded += async (_, _) => await vm.LoadAsync();
        }
        StateChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (MaxBtn is not null)
            MaxBtn.Content = WindowState == WindowState.Maximized ? "" : "";
    }

    private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void OnMaximize(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void OnClose(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!ForceClose)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnClosing(e);
    }
}
