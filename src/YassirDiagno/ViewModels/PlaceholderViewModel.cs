using CommunityToolkit.Mvvm.ComponentModel;

namespace YassirDiagno.ViewModels;

public partial class PlaceholderViewModel : ViewModelBase
{
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _subtitle = "";
    [ObservableProperty] private string _icon = "🚧";
}

public sealed class PerformanceViewModel : PlaceholderViewModel
{
    public PerformanceViewModel()
    {
        Title = "Performance";
        Subtitle = "Graphiques historiques + benchmarks CPU/GPU/SSD/RAM";
        Icon = "📊";
    }
}

public sealed class ToolsViewModel : PlaceholderViewModel
{
    public ToolsViewModel()
    {
        Title = "Outils";
        Subtitle = "Stress test, scan rapide, diagnostic avancé";
        Icon = "🛠";
    }
}

public sealed class ReportsViewModel : PlaceholderViewModel
{
    public ReportsViewModel()
    {
        Title = "Rapports";
        Subtitle = "Rapports diagnostic exportables (PDF/CSV/HTML)";
        Icon = "📄";
    }
}
