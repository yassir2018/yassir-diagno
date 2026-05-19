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
    }

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
