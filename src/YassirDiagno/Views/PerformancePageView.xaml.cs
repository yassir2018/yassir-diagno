using System.Windows.Controls;
using YassirDiagno.ViewModels;

namespace YassirDiagno.Views;

public partial class PerformancePageView : UserControl
{
    public PerformancePageView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is ViewModelBase vm) await vm.LoadAsync();
        };
        Unloaded += async (_, _) =>
        {
            if (DataContext is ViewModelBase vm) await vm.UnloadAsync();
        };
    }
}
