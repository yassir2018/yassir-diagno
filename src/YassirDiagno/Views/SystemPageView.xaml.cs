using System.Windows.Controls;
using YassirDiagno.ViewModels;

namespace YassirDiagno.Views;

public partial class SystemPageView : UserControl
{
    public SystemPageView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is ViewModelBase vm) await vm.LoadAsync();
        };
    }
}
