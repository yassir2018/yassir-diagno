using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using YassirDiagno.ViewModels;

namespace YassirDiagno.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        if (App.Host is not null)
        {
            var vm = App.Host.Services.GetRequiredService<DashboardViewModel>();
            DataContext = vm;
            Loaded += async (_, _) => await vm.LoadAsync();
            Unloaded += async (_, _) => await vm.UnloadAsync();
        }
    }
}
