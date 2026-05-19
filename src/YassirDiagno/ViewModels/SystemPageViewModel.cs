using CommunityToolkit.Mvvm.ComponentModel;
using YassirDiagno.Services.Hardware;

namespace YassirDiagno.ViewModels;

public partial class SystemPageViewModel : ViewModelBase
{
    private readonly ISystemInfoService _service;

    [ObservableProperty] private SystemInfo? _info;
    [ObservableProperty] private bool _isLoading = true;

    public SystemPageViewModel(ISystemInfoService service)
    {
        _service = service;
    }

    public override async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        Info = await _service.GetSystemInfoAsync();
        IsLoading = false;
    }
}
