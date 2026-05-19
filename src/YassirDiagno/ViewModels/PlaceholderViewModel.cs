using CommunityToolkit.Mvvm.ComponentModel;

namespace YassirDiagno.ViewModels;

public partial class PlaceholderViewModel : ViewModelBase
{
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _subtitle = "";
    [ObservableProperty] private string _icon = "🚧";
}

public sealed class SystemViewModel : PlaceholderViewModel
{
    public SystemViewModel()
    {
        Title = "Système";
        Subtitle = "OS, RAM, BIOS, processus système";
        Icon = "💻";
    }
}

public sealed class HardwareViewModel : PlaceholderViewModel
{
    public HardwareViewModel()
    {
        Title = "Matériel";
        Subtitle = "Inventaire complet — CPU, GPU, RAM, stockage, périphériques";
        Icon = "🔧";
    }
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

public sealed class SettingsViewModel : PlaceholderViewModel
{
    public SettingsViewModel()
    {
        Title = "Paramètres";
        Subtitle = "Thèmes, langue, intervalle de polling, démarrage Windows";
        Icon = "⚙";
    }
}
