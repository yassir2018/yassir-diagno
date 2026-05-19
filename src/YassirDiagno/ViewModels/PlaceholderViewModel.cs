using CommunityToolkit.Mvvm.ComponentModel;

namespace YassirDiagno.ViewModels;

public partial class PlaceholderViewModel : ViewModelBase
{
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _subtitle = "";
    [ObservableProperty] private string _icon = "🚧";
}

