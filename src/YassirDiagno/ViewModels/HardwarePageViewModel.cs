using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using YassirDiagno.Services.Hardware;

namespace YassirDiagno.ViewModels;

public partial class HardwarePageViewModel : ViewModelBase
{
    private readonly IHardwareInventoryService _inv;

    [ObservableProperty] private ObservableCollection<HardwareItem> _items = new();
    [ObservableProperty] private bool _isLoading = true;

    public HardwarePageViewModel(IHardwareInventoryService inv)
    {
        _inv = inv;
    }

    public override async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        Items = await _inv.GetInventoryAsync();
        IsLoading = false;
    }
}
