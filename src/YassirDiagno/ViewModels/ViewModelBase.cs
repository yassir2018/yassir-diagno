using CommunityToolkit.Mvvm.ComponentModel;

namespace YassirDiagno.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    public virtual Task LoadAsync(CancellationToken ct = default) => Task.CompletedTask;
    public virtual Task UnloadAsync(CancellationToken ct = default) => Task.CompletedTask;
}
