using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YassirDiagno.Services;

namespace YassirDiagno.ViewModels;

public partial class ProcessesPageViewModel : ViewModelBase
{
    private readonly IProcessExplorerService _explorer;
    private DispatcherTimer? _timer;

    [ObservableProperty] private ObservableCollection<ProcessInfo> _processes = new();
    [ObservableProperty] private string _sortBy = "cpu";
    [ObservableProperty] private int _topCount = 20;
    [ObservableProperty] private string _refreshStatus = "";

    public ProcessesPageViewModel(IProcessExplorerService explorer) => _explorer = explorer;

    public override async Task LoadAsync(CancellationToken ct = default)
    {
        await Refresh();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += async (_, _) => await Refresh();
        _timer.Start();
    }

    public override Task UnloadAsync(CancellationToken ct = default)
    {
        _timer?.Stop();
        _timer = null;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task RefreshNow() => await Refresh();

    [RelayCommand]
    private async Task SortByCpu() { SortBy = "cpu"; await Refresh(); }

    [RelayCommand]
    private async Task SortByRam() { SortBy = "ram"; await Refresh(); }

    [RelayCommand]
    private async Task SortByName() { SortBy = "name"; await Refresh(); }

    [RelayCommand]
    private async Task Kill(ProcessInfo p)
    {
        if (p is null) return;
        var ok = _explorer.TryKill(p.Pid);
        RefreshStatus = ok ? $"✓ Process {p.Name} (PID {p.Pid}) tué" : $"✕ Impossible de tuer {p.Name}";
        await Refresh();
    }

    private async Task Refresh()
    {
        try
        {
            var list = await _explorer.GetTopProcessesAsync(TopCount, SortBy);
            Processes.Clear();
            foreach (var p in list) Processes.Add(p);
        }
        catch (Exception ex) { RefreshStatus = $"Erreur: {ex.Message}"; }
    }
}
