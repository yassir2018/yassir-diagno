using Microsoft.Extensions.DependencyInjection;
using YassirDiagno.ViewModels;

namespace YassirDiagno.Services;

public interface INavigationService
{
    event EventHandler<ViewModelBase>? Navigated;
    void NavigateTo(string viewKey);
    ViewModelBase? CurrentViewModel { get; }
}

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;
    public ViewModelBase? CurrentViewModel { get; private set; }
    public event EventHandler<ViewModelBase>? Navigated;

    public NavigationService(IServiceProvider services)
    {
        _services = services;
    }

    public void NavigateTo(string viewKey)
    {
        ViewModelBase vm = viewKey switch
        {
            "Dashboard"   => _services.GetRequiredService<DashboardViewModel>(),
            "System"      => _services.GetRequiredService<SystemViewModel>(),
            "Hardware"    => _services.GetRequiredService<HardwareViewModel>(),
            "Performance" => _services.GetRequiredService<PerformanceViewModel>(),
            "Tools"       => _services.GetRequiredService<ToolsViewModel>(),
            "Reports"     => _services.GetRequiredService<ReportsViewModel>(),
            "Settings"    => _services.GetRequiredService<SettingsViewModel>(),
            _             => _services.GetRequiredService<DashboardViewModel>(),
        };

        if (CurrentViewModel == vm) return;
        CurrentViewModel = vm;
        Navigated?.Invoke(this, vm);
    }
}
