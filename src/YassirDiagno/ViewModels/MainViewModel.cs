using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YassirDiagno.Services;

namespace YassirDiagno.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService _nav;

    [ObservableProperty] private ViewModelBase? _currentViewModel;

    public MainViewModel(INavigationService nav)
    {
        _nav = nav;
        _nav.Navigated += (_, vm) => CurrentViewModel = vm;
    }

    [RelayCommand]
    private void Navigate(string key) => _nav.NavigateTo(key);

    public override Task LoadAsync(CancellationToken ct = default)
    {
        _nav.NavigateTo("Dashboard");
        return Task.CompletedTask;
    }
}
